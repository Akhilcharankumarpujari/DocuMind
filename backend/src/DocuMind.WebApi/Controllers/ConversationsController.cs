using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.WebApi.Controllers;

[ApiController]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(IConversationService conversationService, ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all conversations associated with a specific document.
    /// </summary>
    [HttpGet("api/documents/{documentId:guid}/conversations")]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var conversations = await _conversationService.GetConversationsByDocumentAsync(documentId, cancellationToken);
        return Ok(conversations);
    }

    /// <summary>
    /// Retrieves a conversation by its identifier.
    /// </summary>
    [HttpGet("api/conversations/{id:guid}")]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetConversationByIdAsync(id, cancellationToken);
        if (conversation == null)
            return NotFound(new { message = $"Conversation with ID '{id}' was not found." });

        return Ok(conversation);
    }

    /// <summary>
    /// Retrieves all messages within a conversation.
    /// </summary>
    [HttpGet("api/conversations/{id:guid}/messages")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken cancellationToken)
    {
        var messages = await _conversationService.GetMessagesAsync(id, cancellationToken);
        return Ok(messages);
    }
}
