using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Crypto;
using Models.DTO;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// COMPLIANT counterpart of <see cref="VulnerableResetController"/>.
/// <para>
/// Checklist items covered: (8) <see cref="RandomNumberGenerator"/> (CSPRNG, 256 bits, no developer-chosen seed),
/// (9) only the SHA-256 of the token is stored, tokens expire after 15 minutes and work once, the new password is stored with PBKDF2.
/// </para>
/// </summary>
[ApiController]
[Route("lab/secure/crypto/reset")]
public class SecureResetController : ControllerBase
{
  private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

  private readonly AppDbContext _context;
  private readonly IWebHostEnvironment _env;

  /// <summary>Dependencies come from DI.</summary>
  public SecureResetController(AppDbContext context, IWebHostEnvironment env)
  {
    _context = context;
    _env = env;
  }

  /// <summary>
  /// Starts a reset.
  /// <para>
  /// Expectation: always 202 "If the account exists, a reset message was sent", whether the user exists or not (no enumeration). In the
  /// Development environment the token is also returned as <c>devMailbox</c> because there is no mail server; it is never returned otherwise.
  /// The token is 32 random bytes (base64url), the database only holds its SHA-256.
  /// </para>
  /// </summary>
  /// <param name="dto">Account name.</param>
  [HttpPost("request")]
  public async Task<IActionResult> RequestReset([FromBody] ResetRequestDto dto)
  {
    string? devMailbox = null;

    if (await _context.SecureCredentials.AnyAsync(u => u.UserName == dto.UserName))
    {
      string token = Base64Url(RandomNumberGenerator.GetBytes(32));

      _context.ResetTokens.Add(new ResetTokenEntity
      {
        Id = Guid.NewGuid(),
        UserName = dto.UserName,
        TokenValue = Hash(token),
        IssuedSecurely = true,
        ExpiresAt = DateTime.UtcNow.Add(TokenLifetime)
      });
      await _context.SaveChangesAsync();

      if (_env.IsDevelopment()) devMailbox = token;
    }

    return StatusCode(StatusCodes.Status202Accepted,
      ApiResponse.Ok(new { devMailbox }, "If the account exists, a reset message was sent"));
  }

  /// <summary>
  /// Completes a reset.
  /// <para>
  /// Expectation: a wrong, expired or already used token all answer the same 400 "Invalid or expired token"; a valid token works exactly once
  /// and the password is stored as PBKDF2 (min length 8).
  /// </para>
  /// </summary>
  /// <param name="dto">Account, token and new password.</param>
  [HttpPost("confirm")]
  public async Task<IActionResult> ConfirmReset([FromBody] ResetConfirmDto dto)
  {
    if (dto.NewPassword is not { Length: >= 8 }) throw new AppException("Password must be at least 8 characters");

    string hash = Hash(dto.Token);
    var now = DateTime.UtcNow;

    ResetTokenEntity? token = await _context.ResetTokens.FirstOrDefaultAsync(t =>
      t.UserName == dto.UserName && t.TokenValue == hash && t.IssuedSecurely && t.UsedAt == null && t.ExpiresAt > now);
    SecureCredentialEntity? user = await _context.SecureCredentials.FirstOrDefaultAsync(u => u.UserName == dto.UserName);

    if (token == null || user == null) throw new AppException("Invalid or expired token");

    var (passwordHash, salt, iterations) = Pbkdf2PasswordHasher.Hash(dto.NewPassword);
    user.PasswordHash = passwordHash;
    user.Salt = salt;
    user.Iterations = iterations;
    user.Algorithm = Pbkdf2PasswordHasher.AlgorithmName;
    user.UpdatedAt = now;
    token.UsedAt = now;
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.Ok(null, "Password changed"));
  }

  private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

  private static string Base64Url(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
