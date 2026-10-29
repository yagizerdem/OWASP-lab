using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// Case 1 - fail-open on error. The authorization check can throw (missing or malformed userId);
/// the vulnerable endpoint swallows the exception and grants access, the secure one denies.
/// </summary>
[ApiController]
[Route("[controller]")]
public class DocumentController : ControllerBase
{
  private readonly AppDbContext _context;

  public DocumentController(AppDbContext context)
  {
    _context = context;
  }

  /// <summary>
  /// VULNERABLE (fail-open): when the access check throws, the catch block sets canRead = true.
  /// A private document is returned to anyone who sends a missing or malformed userId (e.g. ?userId=abc).
  /// </summary>
  [HttpGet]
  [Route("/lab/vulnerable/exceptions/documents/{id:guid}")]
  public async Task<IActionResult> GetVulnerable(Guid id, [FromQuery] string? userId)
  {
    DocumentEntity doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id)
      ?? throw AppException.NotFound("Document not found");

    bool canRead;
    try
    {
      // Guid.Parse throws on null or malformed input.
      canRead = !doc.IsPrivate || doc.OwnerId == Guid.Parse(userId!);
    }
    catch
    {
      // VULNERABLE: the error is swallowed and the request is treated as authorized.
      canRead = true;
    }

    if (!canRead)
    {
      throw AppException.Forbidden("You cannot read this document");
    }

    return Ok(ApiResponse.Ok(doc));
  }

  /// <summary>
  /// SECURE (fail-closed): input that cannot be validated ends the request with an error, and access is
  /// granted only when the check positively succeeds.
  /// </summary>
  [HttpGet]
  [Route("/lab/secure/exceptions/documents/{id:guid}")]
  public async Task<IActionResult> GetSecure(Guid id, [FromQuery] string? userId)
  {
    if (!Guid.TryParse(userId, out Guid callerId))
    {
      throw AppException.Unauthorized("A valid userId is required");
    }

    DocumentEntity doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id)
      ?? throw AppException.NotFound("Document not found");

    if (doc.IsPrivate && doc.OwnerId != callerId)
    {
      throw AppException.Forbidden("You cannot read this document");
    }

    return Ok(ApiResponse.Ok(doc));
  }

  /// <summary>Lists document ids, owners and privacy flags so the cases above can be tried.</summary>
  [HttpGet]
  [Route("/lab/exceptions/documents")]
  public async Task<IActionResult> List()
  {
    var docs = await _context.Documents
      .OrderBy(d => d.CreatedAt)
      .Select(d => new { d.Id, d.OwnerId, d.Title, d.IsPrivate })
      .Take(100)
      .ToListAsync();

    return Ok(ApiResponse.Ok(docs));
  }
}
