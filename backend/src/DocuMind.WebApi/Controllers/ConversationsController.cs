using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.WebApi.Controllers;

[ApiController]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IConversationService conversationService,
        ICurrentUserService currentUserService,
        ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all conversations associated with a specific document owned by the authenticated user.
    /// </summary>
    [HttpGet("api/documents/{documentId:guid}/conversations")]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var conversations = await _conversationService.GetConversationsByDocumentAsync(documentId, userId.Value, cancellationToken);
        return Ok(conversations);
    }

    /// <summary>
    /// Retrieves a conversation by its identifier if owned by the authenticated user.
    /// </summary>
    [HttpGet("api/conversations/{id:guid}")]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var conversation = await _conversationService.GetConversationByIdAsync(id, userId.Value, cancellationToken);
        if (conversation == null)
            return NotFound(new { message = $"Conversation with ID '{id}' was not found." });

        return Ok(conversation);
    }

    /// <summary>
    /// Retrieves all messages within a conversation owned by the authenticated user.
    /// </summary>
    [HttpGet("api/conversations/{id:guid}/messages")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var messages = await _conversationService.GetMessagesAsync(id, userId.Value, cancellationToken);
            return Ok(messages);
        }
        catch (Domain.Exceptions.EntityNotFoundException)
        {
            return NotFound(new { message = $"Conversation with ID '{id}' was not found." });
        }
    }
}
