using Backend.Crypto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Crypto;
using Models.DTO;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// COMPLIANT counterpart of <see cref="VulnerableCardsController"/>.
/// <para>
/// Checklist items covered: sensitive data encrypted at rest with AES-256-GCM (<see cref="KeyRing"/>), masked in responses, never logged,
/// key rotation without downtime (key id stored per row) and keys that are not in source control.
/// </para>
/// </summary>
[ApiController]
[Route("lab/secure/crypto/cards")]
public class SecureCardsController : ControllerBase
{
  private readonly AppDbContext _context;
  private readonly KeyRing _keys;
  private readonly ILogger<SecureCardsController> _logger;

  /// <summary>Dependencies come from DI.</summary>
  public SecureCardsController(AppDbContext context, KeyRing keys, ILogger<SecureCardsController> logger)
  {
    _context = context;
    _keys = keys;
    _logger = logger;
  }

  /// <summary>
  /// Lists cards in masked form.
  /// <para>Expectation: only <c>**** **** **** 1234</c>, the owner and the key id are returned; the database holds ciphertext only.</para>
  /// </summary>
  /// <param name="owner">Optional owner filter.</param>
  [HttpGet]
  public async Task<IActionResult> List([FromQuery] string? owner)
  {
    var query = _context.ProtectedCards.AsNoTracking();
    if (!string.IsNullOrEmpty(owner)) query = query.Where(c => c.OwnerName == owner);

    var cards = await query.Take(100).Select(c => new { c.Id, c.OwnerName, masked = "**** **** **** " + c.Last4, c.KeyId }).ToListAsync();
    return Ok(ApiResponse.Ok(cards));
  }

  /// <summary>
  /// Stores a card encrypted.
  /// <para>
  /// Expectation: invalid numbers (length or Luhn) answer 400; the number is encrypted with a fresh nonce and the active key; only the last
  /// four digits are logged. The response contains the masked number only.
  /// </para>
  /// </summary>
  /// <param name="dto">Owner and full card number.</param>
  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CardCreateDto dto)
  {
    string number = new string((dto.CardNumber ?? "").Where(char.IsDigit).ToArray());
    if (number.Length is < 13 or > 19 || !PassesLuhn(number)) throw new AppException("Invalid card number");

    EncryptedField field = _keys.Encrypt(number);
    var card = new ProtectedCardEntity
    {
      Id = Guid.NewGuid(),
      OwnerName = dto.OwnerName,
      Last4 = number[^4..],
      KeyId = field.KeyId,
      Nonce = field.Nonce,
      CipherText = field.CipherText
    };
    _context.ProtectedCards.Add(card);
    await _context.SaveChangesAsync();

    _logger.LogInformation("New card for {Owner} ending {Last4} (key {KeyId})", card.OwnerName, card.Last4, card.KeyId);
    return StatusCode(StatusCodes.Status201Created,
      ApiResponse.Created(new { card.Id, card.OwnerName, masked = "**** **** **** " + card.Last4, card.KeyId }));
  }

  /// <summary>
  /// Switches the active key (rotation step 1). New encryptions use <paramref name="keyId"/>; old rows stay readable.
  /// <para>Expectation: requires header <c>X-Admin-Token</c> (from the key store, never committed); a wrong or missing token answers 401.</para>
  /// </summary>
  /// <param name="keyId">Key id, for example <c>k2</c>.</param>
  [HttpPost("rotation/activate")]
  public IActionResult Activate([FromQuery] string keyId)
  {
    RequireAdmin();
    _keys.Activate(keyId);
    return Ok(ApiResponse.Ok(new { activeKeyId = _keys.ActiveKeyId, keys = _keys.KeyIds }));
  }

  /// <summary>
  /// Re-encrypts every row that still uses an old key with the active key (rotation step 2).
  /// <para>Expectation: same admin header; afterwards <c>SELECT DISTINCT KeyId FROM ProtectedCards</c> shows only the active key id.</para>
  /// </summary>
  [HttpPost("rotation/reencrypt")]
  public async Task<IActionResult> Reencrypt()
  {
    RequireAdmin();

    var stale = await _context.ProtectedCards.Where(c => c.KeyId != _keys.ActiveKeyId).ToListAsync();
    foreach (ProtectedCardEntity card in stale)
    {
      string number = _keys.Decrypt(new EncryptedField(card.KeyId, card.Nonce, card.CipherText));
      EncryptedField field = _keys.Encrypt(number);
      card.KeyId = field.KeyId;
      card.Nonce = field.Nonce;
      card.CipherText = field.CipherText;
      card.UpdatedAt = DateTime.UtcNow;
    }

    await _context.SaveChangesAsync();
    return Ok(ApiResponse.Ok(new { reencrypted = stale.Count, activeKeyId = _keys.ActiveKeyId }));
  }

  private void RequireAdmin()
  {
    string supplied = Request.Headers["X-Admin-Token"].ToString();
    byte[] a = System.Text.Encoding.UTF8.GetBytes(supplied);
    byte[] b = System.Text.Encoding.UTF8.GetBytes(_keys.AdminToken);
    if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b))
      throw AppException.Unauthorized("Admin token required");
  }

  private static bool PassesLuhn(string number)
  {
    int sum = 0;
    for (int i = 0; i < number.Length; i++)
    {
      int d = number[number.Length - 1 - i] - '0';
      if (i % 2 == 1) { d *= 2; if (d > 9) d -= 9; }
      sum += d;
    }

    return sum % 10 == 0;
  }
}
