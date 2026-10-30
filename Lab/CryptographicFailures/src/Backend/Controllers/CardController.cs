using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Crypto;
using Models.DTO;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// Case 3 - sensitive data stored in clear. The vulnerable table holds full card numbers in plaintext, returns them in
/// responses and writes them to the log. The secure table holds AES-256-GCM ciphertext and only shows the last four digits.
/// </summary>
[ApiController]
[Route("[controller]")]
public class CardController : ControllerBase
{
  private readonly AppDbContext _context;
  private readonly ICardCipher _cipher;
  private readonly ILogger<CardController> _logger;

  public CardController(AppDbContext context, ICardCipher cipher, ILogger<CardController> logger)
  {
    _context = context;
    _cipher = cipher;
    _logger = logger;
  }

  /// <summary>VULNERABLE: full card numbers, exactly as they are stored in the database.</summary>
  [HttpGet]
  [Route("/lab/vulnerable/crypto/cards")]
  public async Task<IActionResult> ListVulnerable()
  {
    var cards = await _context.LegacyCards.AsNoTracking().Take(100).ToListAsync();
    return Ok(ApiResponse.Ok(cards));
  }

  /// <summary>VULNERABLE: stores the number in plaintext and logs it.</summary>
  [HttpPost]
  [Route("/lab/vulnerable/crypto/cards")]
  public async Task<IActionResult> CreateVulnerable([FromBody] CardCreateDto dto)
  {
    // VULNERABLE: the full card number ends up in the application log.
    _logger.LogInformation("New card for {Owner}: {CardNumber}", dto.OwnerName, dto.CardNumber);

    var card = new LegacyCardEntity { Id = Guid.NewGuid(), OwnerName = dto.OwnerName, CardNumber = dto.CardNumber };
    _context.LegacyCards.Add(card);
    await _context.SaveChangesAsync();

    return StatusCode(StatusCodes.Status201Created, ApiResponse.Created(card));
  }

  /// <summary>SECURE: only the masked number is returned; the database holds ciphertext.</summary>
  [HttpGet]
  [Route("/lab/secure/crypto/cards")]
  public async Task<IActionResult> ListSecure()
  {
    var cards = await _context.ProtectedCards.AsNoTracking().Take(100)
      .Select(c => new { c.Id, c.OwnerName, Masked = "**** **** **** " + c.Last4 })
      .ToListAsync();

    return Ok(ApiResponse.Ok(cards));
  }

  /// <summary>SECURE: validates the number, encrypts it with AES-256-GCM and logs only the last four digits.</summary>
  [HttpPost]
  [Route("/lab/secure/crypto/cards")]
  public async Task<IActionResult> CreateSecure([FromBody] CardCreateDto dto)
  {
    string number = new string((dto.CardNumber ?? "").Where(char.IsDigit).ToArray());
    if (number.Length is < 13 or > 19)
    {
      throw new AppException("Invalid card number");
    }

    var (nonce, cipherText) = _cipher.Encrypt(number);
    var card = new ProtectedCardEntity
    {
      Id = Guid.NewGuid(),
      OwnerName = dto.OwnerName,
      Last4 = number[^4..],
      Nonce = nonce,
      CipherText = cipherText
    };
    _context.ProtectedCards.Add(card);
    await _context.SaveChangesAsync();

    _logger.LogInformation("New card for {Owner} ending {Last4}", card.OwnerName, card.Last4);
    return StatusCode(StatusCodes.Status201Created,
      ApiResponse.Created(new { card.Id, card.OwnerName, Masked = "**** **** **** " + card.Last4 }));
  }
}
