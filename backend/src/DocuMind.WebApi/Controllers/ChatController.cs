using System.Text;
using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.WebApi.Controllers;

[ApiController]
[Route("api/documents/{documentId:guid}/ask")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Submits a question grounded in the specified document and returns the complete answer.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AskQuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ask(
        Guid documentId,
        [FromBody] AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { message = "Question cannot be empty." });
        }

        var response = await _chatService.AskQuestionAsync(
            documentId: documentId,
            question: request.Question,
            conversationId: request.ConversationId,
            cancellationToken: cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Streams an AI answer and source citations in real-time using Server-Sent Events (SSE).
    /// </summary>
    [HttpPost("stream")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task Stream(
        Guid documentId,
        [FromBody] AskQuestionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Question))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { message = "Question cannot be empty." });
            return;
        }

        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Disable proxy buffering (Nginx, etc.)

        var clientAborted = HttpContext.RequestAborted;

        try
        {
            await foreach (var sseEvent in _chatService.StreamQuestionAsync(
                documentId: documentId,
                question: request.Question,
                conversationId: request.ConversationId,
                cancellationToken: clientAborted))
            {
                if (clientAborted.IsCancellationRequested)
                {
                    _logger.LogInformation("Client aborted SSE stream for Document {DocumentId}", documentId);
                    break;
                }

                var message = sseEvent.ToSseMessage();
                var bytes = Encoding.UTF8.GetBytes(message);

                await Response.Body.WriteAsync(bytes, clientAborted);
                await Response.Body.FlushAsync(clientAborted);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client closed the connection during streaming for Document {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during SSE chat stream for Document {DocumentId}", documentId);

            try
            {
                var errorEvent = SseStreamEvent.CreateError("An unexpected error occurred during streaming.");
                var bytes = Encoding.UTF8.GetBytes(errorEvent.ToSseMessage());
                await Response.Body.WriteAsync(bytes, CancellationToken.None);
                await Response.Body.FlushAsync(CancellationToken.None);
            }
            catch
            {
                // Stream might be broken or closed by client
            }
        }
    }
}
