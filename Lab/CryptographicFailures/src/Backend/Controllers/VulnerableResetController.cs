using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// VULNERABLE BY DESIGN. Password reset with predictable randomness.
/// <para>
/// Checklist items covered: (8) randomness not designed for cryptography (<see cref="Random"/> seeded with the clock),
/// (9) token stored in plaintext, plus missing expiry and single use.
/// </para>
/// Compliant counterpart: <see cref="SecureResetController"/>.
/// </summary>
[ApiController]
[Route("lab/vulnerable/crypto/reset")]
public class VulnerableResetController : ControllerBase
{
  private readonly AppDbContext _context;

  /// <summary>The database context comes from DI.</summary>
  public VulnerableResetController(AppDbContext context)
  {
    _context = context;
  }

  /// <summary>
  /// Starts a reset and "mails" a 6 digit code.
  /// <para>
  /// Weaknesses: the code is <c>new Random(unixSeconds).Next(100000, 1000000)</c>, so anyone who knows the second a code was created can
  /// compute it (two requests in the same second get the same code); the code is stored in plaintext, never expires and is not
  /// consumed; unknown users answer 404 (enumeration). The response includes the code and <c>issuedAtUnix</c> to stand in for the e-mail and
  /// to make the prediction easy to verify.
  /// </para>
  /// </summary>
  /// <param name="dto">Account name.</param>
  [HttpPost("request")]
  public async Task<IActionResult> RequestReset([FromBody] ResetRequestDto dto)
  {
    if (!await _context.WeakHashes.AnyAsync(u => u.UserName == dto.UserName))
      throw AppException.NotFound("User not found");

    long issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    string code = new Random((int)issuedAt).Next(100000, 1000000).ToString();

    _context.ResetTokens.Add(new ResetTokenEntity { Id = Guid.NewGuid(), UserName = dto.UserName, TokenValue = code, IssuedSecurely = false });
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.Ok(new { code, issuedAtUnix = issuedAt }, "Code sent (shown here instead of an e-mail)"));
  }

  /// <summary>
  /// Completes a reset.
  /// <para>
  /// Weaknesses: unlimited guesses (only 900000 possible codes, no rate limit), no expiry, the same code works again and again, and the new
  /// password is stored as unsalted MD5 like the rest of the weak table.
  /// </para>
  /// </summary>
  /// <param name="dto">Account, code and new password.</param>
  [HttpPost("confirm")]
  public async Task<IActionResult> ConfirmReset([FromBody] ResetConfirmDto dto)
  {
    bool valid = await _context.ResetTokens.AnyAsync(t => t.UserName == dto.UserName && t.TokenValue == dto.Token && !t.IssuedSecurely);
    if (!valid) throw AppException.Unauthorized("Wrong code");

    WeakHashEntity user = await _context.WeakHashes.FirstAsync(u => u.UserName == dto.UserName);
    user.PlaintextPassword = dto.NewPassword;
    user.HashedPassword = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(dto.NewPassword))).ToLowerInvariant();
    user.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.Ok(null, "Password changed"));
  }
}
