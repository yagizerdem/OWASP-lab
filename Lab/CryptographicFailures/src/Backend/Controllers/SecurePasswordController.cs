using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Crypto;
using Models.DTO;

namespace Backend.Controllers;

/// <summary>
/// COMPLIANT counterpart of <see cref="VulnerablePasswordController"/> (unsalted MD5 in <c>WeakHashes</c>).
/// <para>
/// Checklist items covered: (9) no deprecated hash functions, (7) passwords are never used as keys, they go through a password
/// hashing function: PBKDF2-HMAC-SHA256, 600000 iterations, random 16 byte salt per user, constant-time comparison.
/// </para>
/// </summary>
[ApiController]
[Route("lab/secure/crypto/password")]
public class SecurePasswordController : ControllerBase
{
  /// <summary>Precomputed with a random password so an unknown user costs the same time as a wrong password (no user enumeration by timing).</summary>
  private static readonly (string Hash, string Salt, int Iterations) DummyHash = Pbkdf2PasswordHasher.Hash("dummy-password-for-timing");

  private readonly AppDbContext _context;

  /// <summary>The database context comes from DI.</summary>
  public SecurePasswordController(AppDbContext context)
  {
    _context = context;
  }

  /// <summary>
  /// Verifies a password against the PBKDF2 hash.
  /// <para>
  /// Expectation: correct password → 200; wrong password and unknown user → the same 401 "Invalid username or password" with similar
  /// response time. The response never contains the hash or salt. Compare with the vulnerable endpoint, which answers 404 for unknown users
  /// and returns the whole row including the plaintext password.
  /// </para>
  /// </summary>
  /// <param name="dto">User name and password.</param>
  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] CredentialsDto dto)
  {
    var user = await _context.SecureCredentials.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == dto.UserName);

    // Always run one PBKDF2 verification, against a dummy hash when the user does not exist.
    var (hash, salt, iterations) = user == null ? DummyHash : (user.PasswordHash, user.Salt, user.Iterations);
    bool verified = Pbkdf2PasswordHasher.Verify(dto.Password, hash, salt, iterations);

    if (user == null || !verified) throw AppException.Unauthorized("Invalid username or password");

    return Ok(ApiResponse.Ok(new { user = user.UserName, algorithm = user.Algorithm, iterations = user.Iterations }, "Password verified successfully"));
  }
}
