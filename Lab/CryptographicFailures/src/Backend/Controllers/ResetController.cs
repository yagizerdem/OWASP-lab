using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Crypto;
using Models.DTO;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// Case 2 - predictable reset codes. The vulnerable flow creates the code with <c>new Random(unixSeconds)</c>, so anyone
/// who knows roughly when a code was issued can compute it. The secure flow uses a CSPRNG token, stores only its SHA-256,
/// and lets it be used once within 15 minutes.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ResetController : ControllerBase
{
  private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

  private readonly AppDbContext _context;
  private readonly IWebHostEnvironment _env;

  public ResetController(AppDbContext context, IWebHostEnvironment env)
  {
    _context = context;
    _env = env;
  }

  /// <summary>
  /// VULNERABLE: the 6 digit code is seeded with the current unix time, stored in plaintext, never expires and is never
  /// consumed. The response shows the code and the seed in place of an e-mail, so the prediction can be checked.
  /// </summary>
  [HttpPost]
  [Route("/lab/vulnerable/crypto/reset/request")]
  public async Task<IActionResult> RequestVulnerable([FromBody] ResetRequestDto dto)
  {
    if (!await _context.WeakHashes.AnyAsync(u => u.UserName == dto.UserName))
    {
      throw AppException.NotFound("User not found");
    }

    // VULNERABLE: System.Random seeded with the clock is not a source of secrets.
    long issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    string code = new Random((int)issuedAt).Next(100000, 1000000).ToString();

    _context.ResetTokens.Add(new ResetTokenEntity { Id = Guid.NewGuid(), UserName = dto.UserName, TokenValue = code, IssuedSecurely = false });
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.Ok(new { code, issuedAtUnix = issuedAt }, "Code sent (shown here instead of an e-mail)"));
  }

  /// <summary>VULNERABLE: any code ever issued for the user works, as often as you like.</summary>
  [HttpPost]
  [Route("/lab/vulnerable/crypto/reset/confirm")]
  public async Task<IActionResult> ConfirmVulnerable([FromBody] ResetConfirmDto dto)
  {
    bool valid = await _context.ResetTokens.AnyAsync(t => t.UserName == dto.UserName && t.TokenValue == dto.Token && !t.IssuedSecurely);
    if (!valid)
    {
      throw AppException.Unauthorized("Wrong code");
    }

    WeakHashEntity user = await _context.WeakHashes.FirstAsync(u => u.UserName == dto.UserName);
    user.PlaintextPassword = dto.NewPassword;
    user.HashedPassword = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(dto.NewPassword))).ToLowerInvariant();
    user.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.Ok(null, "Password changed"));
  }

  /// <summary>
  /// SECURE: 32 random bytes from <see cref="RandomNumberGenerator"/>; only the SHA-256 is stored. The answer is the same
  /// whether the user exists or not. In Development the token is returned as <c>devMailbox</c> because there is no mail server.
  /// </summary>
  [HttpPost]
  [Route("/lab/secure/crypto/reset/request")]
  public async Task<IActionResult> RequestSecure([FromBody] ResetRequestDto dto)
  {
    string? devMailbox = null;

    if (await _context.SecureCredentials.AnyAsync(u => u.UserName == dto.UserName))
    {
      string token = Base64Url(RandomNumberGenerator.GetBytes(32));

      _context.ResetTokens.Add(new ResetTokenEntity
      {
        Id = Guid.NewGuid(),
        UserName = dto.UserName,
        TokenValue = Sha256(token),
        IssuedSecurely = true,
        ExpiresAt = DateTime.UtcNow.Add(TokenLifetime)
      });
      await _context.SaveChangesAsync();

      if (_env.IsDevelopment()) devMailbox = token;
    }

    return StatusCode(StatusCodes.Status202Accepted,
      ApiResponse.Ok(new { devMailbox }, "If the account exists, a reset message was sent"));
  }

  /// <summary>SECURE: the token must match, must not be expired and must not have been used before.</summary>
  [HttpPost]
  [Route("/lab/secure/crypto/reset/confirm")]
  public async Task<IActionResult> ConfirmSecure([FromBody] ResetConfirmDto dto)
  {
    if (dto.NewPassword is not { Length: >= 8 })
    {
      throw new AppException("Password must be at least 8 characters");
    }

    string hash = Sha256(dto.Token);
    var now = DateTime.UtcNow;

    ResetTokenEntity? token = await _context.ResetTokens.FirstOrDefaultAsync(t =>
      t.UserName == dto.UserName && t.TokenValue == hash && t.IssuedSecurely && t.UsedAt == null && t.ExpiresAt > now);
    SecureCredentialEntity? user = await _context.SecureCredentials.FirstOrDefaultAsync(u => u.UserName == dto.UserName);

    if (token == null || user == null)
    {
      throw new AppException("Invalid or expired token");
    }

    var (passwordHash, salt, iterations) = Pbkdf2PasswordHasher.Hash(dto.NewPassword);
    user.PasswordHash = passwordHash;
    user.Salt = salt;
    user.Iterations = iterations;
    user.UpdatedAt = now;
    token.UsedAt = now;
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.Ok(null, "Password changed"));
  }

  private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

  private static string Base64Url(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
