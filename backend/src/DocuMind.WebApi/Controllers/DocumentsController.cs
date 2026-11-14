using DocuMind.Application.Common.Interfaces;
using DocuMind.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all uploaded documents.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var documents = await _documentService.GetAllDocumentsAsync(cancellationToken);
        return Ok(documents);
    }

    /// <summary>
    /// Retrieves a document by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documentService.GetDocumentByIdAsync(id, cancellationToken);
        if (document == null)
            return NotFound(new { message = $"Document with ID '{id}' was not found." });

        return Ok(document);
    }

    /// <summary>
    /// Retrieves the processing status of a document.
    /// </summary>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(DocumentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var status = await _documentService.GetDocumentStatusAsync(id, cancellationToken);
        if (status == null)
            return NotFound(new { message = $"Document with ID '{id}' was not found." });

        return Ok(status);
    }

    /// <summary>
    /// Streams the raw PDF file content for viewing in the browser.
    /// </summary>
    [HttpGet("{id:guid}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(Guid id, CancellationToken cancellationToken)
    {
        var stream = await _documentService.GetDocumentFileAsync(id, cancellationToken);
        if (stream == null)
            return NotFound(new { message = $"File for document with ID '{id}' was not found." });

        return File(stream, "application/pdf", enableRangeProcessing: true);
    }

    /// <summary>
    /// Uploads a new PDF document and triggers the ingestion pipeline.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
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
    /// Deletes a document, its physical storage file, Qdrant vectors, and MySQL records.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _documentService.DeleteDocumentAsync(id, cancellationToken);
        return NoContent();
    }
}
