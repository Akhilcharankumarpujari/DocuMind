using System.Text;
using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/documents/{documentId:guid}/ask")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        ICurrentUserService currentUserService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Submits a question grounded in the specified document owned by the authenticated user and returns the complete answer.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AskQuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ask(
        Guid documentId,
        [FromBody] AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        if (request == null || string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { message = "Question cannot be empty." });
        }

        var response = await _chatService.AskQuestionAsync(
            documentId: documentId,
            question: request.Question,
            userId: userId.Value,
            conversationId: request.ConversationId,
            cancellationToken: cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Streams an AI answer and source citations in real-time using Server-Sent Events (SSE)
    /// strictly enforcing document ownership for the authenticated user.
    /// </summary>
    [HttpPost("stream")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task Stream(
        Guid documentId,
        [FromBody] AskQuestionRequest request)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(new { message = "Authentication required." });
            return;
        }

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
                userId: userId.Value,
                conversationId: request.ConversationId,
                cancellationToken: clientAborted))
            {
                if (clientAborted.IsCancellationRequested)
                {
                    _logger.LogInformation("Client aborted SSE stream for Document {DocumentId} (User {UserId})", documentId, userId.Value);
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
            _logger.LogInformation("Client closed connection during streaming for Document {DocumentId} (User {UserId})", documentId, userId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during SSE chat stream for Document {DocumentId} (User {UserId})", documentId, userId.Value);

            try
            {
                var errorMsg = "An unexpected error occurred during streaming.";
                var fullExceptionText = ex.ToString();
                if (fullExceptionText.Contains("429") ||
                    fullExceptionText.Contains("rate_limit", StringComparison.OrdinalIgnoreCase) ||
                    fullExceptionText.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                    fullExceptionText.Contains("quota", StringComparison.OrdinalIgnoreCase))
                {
                    errorMsg = "Azure OpenAI rate limit reached (HTTP 429: Too Many Requests). The token quota per minute in Azure AI Foundry was temporarily reached. Please wait a few seconds and try again.";
                }
                else
                {
                    errorMsg = $"AI streaming error: {ex.Message}";
                }

                var errorEvent = SseStreamEvent.CreateError(errorMsg);
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
