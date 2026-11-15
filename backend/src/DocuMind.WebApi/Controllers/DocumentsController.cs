using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ICurrentUserService currentUserService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all documents owned by the authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var documents = await _documentService.GetAllDocumentsAsync(userId.Value, cancellationToken);
        return Ok(documents);
    }

    /// <summary>
    /// Retrieves a document by its unique identifier if owned by the authenticated user.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var document = await _documentService.GetDocumentByIdAsync(id, userId.Value, cancellationToken);
        if (document == null)
            return NotFound(new { message = $"Document with ID '{id}' was not found." });

        return Ok(document);
    }

    /// <summary>
    /// Retrieves the processing status of a document owned by the authenticated user.
    /// </summary>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(DocumentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var status = await _documentService.GetDocumentStatusAsync(id, userId.Value, cancellationToken);
        if (status == null)
            return NotFound(new { message = $"Document with ID '{id}' was not found." });

        return Ok(status);
    }

    /// <summary>
    /// Streams the raw PDF file content for viewing in the browser if owned by the authenticated user.
    /// </summary>
    [HttpGet("{id:guid}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var stream = await _documentService.GetDocumentFileAsync(id, userId.Value, cancellationToken);
        if (stream == null)
            return NotFound(new { message = $"File for document with ID '{id}' was not found." });

        return File(stream, "application/pdf", enableRangeProcessing: true);
    }

    /// <summary>
    /// Uploads a new PDF document under the authenticated user's ownership and triggers the ingestion pipeline.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded or the file is empty." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _documentService.UploadDocumentAsync(
                fileStream: stream,
                fileName: file.FileName,
                fileSize: file.Length,
                contentType: file.ContentType,
                userId: userId.Value,
                cancellationToken: cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation failed for uploaded file '{FileName}'.", file.FileName);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a document owned by the authenticated user, its physical storage file, Qdrant vectors, and MySQL records.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        await _documentService.DeleteDocumentAsync(id, userId.Value, cancellationToken);
        return NoContent();
    }

    private Guid? GetCurrentUserId() => _currentUserService.UserId;
}
