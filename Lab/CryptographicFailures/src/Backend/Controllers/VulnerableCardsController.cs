using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// VULNERABLE BY DESIGN. Sensitive data stored and exposed in clear.
/// <para>
/// Checklist items covered: sensitive data at rest without encryption (full card numbers in the database), sensitive data in API responses
/// and in application logs.
/// </para>
/// Compliant counterpart: <see cref="SecureCardsController"/>.
/// </summary>
[ApiController]
[Route("lab/vulnerable/crypto/cards")]
public class VulnerableCardsController : ControllerBase
{
  private readonly AppDbContext _context;
  private readonly ILogger<VulnerableCardsController> _logger;

  /// <summary>Dependencies come from DI.</summary>
  public VulnerableCardsController(AppDbContext context, ILogger<VulnerableCardsController> logger)
  {
    _context = context;
    _logger = logger;
  }

  /// <summary>
  /// Lists cards.
  /// <para>Weakness: full card numbers are returned (and stored) in plaintext. A database dump or an SQL injection exposes every card.</para>
  /// <para>Expectation: <c>SELECT CardNumber FROM LegacyCards</c> in Adminer shows readable 16 digit numbers.</para>
  /// </summary>
  /// <param name="owner">Optional owner filter.</param>
  [HttpGet]
  public async Task<IActionResult> List([FromQuery] string? owner)
  {
    var query = _context.LegacyCards.AsNoTracking();
    if (!string.IsNullOrEmpty(owner)) query = query.Where(c => c.OwnerName == owner);
    return Ok(ApiResponse.Ok(await query.Take(100).ToListAsync()));
  }

  /// <summary>
  /// Stores a card.
  /// <para>Weaknesses: plaintext storage, no number validation, and the full number is written to the application log.</para>
  /// <para>Expectation: the console shows <c>New card ... 4111...</c>.</para>
  /// </summary>
  /// <param name="dto">Owner and full card number.</param>
  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CardCreateDto dto)
  {
    _logger.LogInformation("New card for {Owner}: {CardNumber}", dto.OwnerName, dto.CardNumber);

    var card = new LegacyCardEntity { Id = Guid.NewGuid(), OwnerName = dto.OwnerName, CardNumber = dto.CardNumber };
    _context.LegacyCards.Add(card);
    await _context.SaveChangesAsync();

    return StatusCode(StatusCodes.Status201Created, ApiResponse.Created(card));
  }
}
