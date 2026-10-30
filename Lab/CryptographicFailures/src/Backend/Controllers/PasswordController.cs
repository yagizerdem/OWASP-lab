using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Crypto;
using Models.DTO;

namespace Backend.Controllers;

/// <summary>
/// Case 1 - weak password storage. The vulnerable table keeps passwords as unsalted MD5, so equal passwords give equal
/// hashes and every hash can be looked up or brute forced in seconds. The secure table uses PBKDF2-HMAC-SHA256 with a
/// random salt per user and 600000 iterations.
/// </summary>
[ApiController]
[Route("[controller]")]
public class PasswordController : ControllerBase
{
  /// <summary>Hash of a throwaway password, so an unknown user costs the same time as a wrong password.</summary>
  private static readonly (string Hash, string Salt, int Iterations) DummyHash = Pbkdf2PasswordHasher.Hash("dummy-password-for-timing");

  private readonly AppDbContext _context;

  public PasswordController(AppDbContext context)
  {
    _context = context;
  }

  /// <summary>
  /// Stands in for a leaked database dump: user names and their unsalted MD5 hashes. Group by hash to see which users
  /// share a password, then crack one hash to get all of them.
  /// </summary>
  [HttpGet]
  [Route("/lab/vulnerable/crypto/password-hashes")]
  public async Task<IActionResult> GetWeakHashes()
  {
    var rows = await _context.WeakHashes.AsNoTracking()
      .Select(u => new { u.UserName, u.HashedPassword })
      .ToListAsync();

    return Ok(ApiResponse.Ok(rows));
  }

  /// <summary>VULNERABLE: compares the unsalted MD5 of the given password with the stored hash.</summary>
  [HttpPost]
  [Route("/lab/vulnerable/crypto/login")]
  public async Task<IActionResult> LoginVulnerable([FromBody] CredentialsDto dto)
  {
    var user = await _context.WeakHashes.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == dto.UserName)
      ?? throw AppException.Unauthorized("Invalid username or password");

    // VULNERABLE: MD5 is fast and unsalted.
    string hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(dto.Password))).ToLowerInvariant();
    if (hash != user.HashedPassword)
    {
      throw AppException.Unauthorized("Invalid username or password");
    }

    return Ok(ApiResponse.Ok(new { user.UserName }, "Logged in"));
  }

  /// <summary>The same dump for the secure table: every user has a different salt, so equal passwords look different.</summary>
  [HttpGet]
  [Route("/lab/secure/crypto/password-hashes")]
  public async Task<IActionResult> GetSecureHashes()
  {
    var rows = await _context.SecureCredentials.AsNoTracking()
      .Select(u => new { u.UserName, u.PasswordHash, u.Salt, u.Iterations })
      .ToListAsync();

    return Ok(ApiResponse.Ok(rows));
  }

  /// <summary>SECURE: verifies the password with PBKDF2 and compares in constant time.</summary>
  [HttpPost]
  [Route("/lab/secure/crypto/login")]
  public async Task<IActionResult> LoginSecure([FromBody] CredentialsDto dto)
  {
    var user = await _context.SecureCredentials.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == dto.UserName);

    // Always run one PBKDF2 check, against the dummy hash when the user does not exist.
    var (hash, salt, iterations) = user == null ? DummyHash : (user.PasswordHash, user.Salt, user.Iterations);
    bool verified = Pbkdf2PasswordHasher.Verify(dto.Password, hash, salt, iterations);

    if (user == null || !verified)
    {
      throw AppException.Unauthorized("Invalid username or password");
    }

    return Ok(ApiResponse.Ok(new { user.UserName }, "Logged in"));
  }
}
