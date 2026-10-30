namespace backend.Controllers;

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Models.DTO;
using Models.Entity;

/// <summary>
/// SECURE – Example 2: account lockout.
/// <para>
/// Failed logins are counted per e-mail. After <c>Security:MaxFailedLogins</c> (5) failures the account is locked for
/// <c>Security:LockoutMinutes</c> (15): every attempt, even with the right password, gets 429. A successful login resets
/// the counter. Unknown e-mails are counted too, so the lockout does not reveal which accounts exist.
/// </para>
/// </summary>
[ApiController]
[Route("[controller]")]
public class SecureBruteForceController : ControllerBase
{
  private readonly AppDbContext _db;
  private readonly IMemoryCache _cache;
  private readonly int _maxFailures;
  private readonly TimeSpan _lockout;

  public SecureBruteForceController(AppDbContext db, IMemoryCache cache, IConfiguration configuration)
  {
    _db = db;
    _cache = cache;
    _maxFailures = configuration.GetValue("Security:MaxFailedLogins", 5);
    _lockout = TimeSpan.FromMinutes(configuration.GetValue("Security:LockoutMinutes", 15));
  }

  /// <summary>Checks e-mail and password; locks the account after too many failures.</summary>
  [HttpPost]
  [Route("/lab/secure/bruteforce/login")]
  public async Task<IActionResult> Login([FromBody] LoginDto request)
  {
    string key = "failed-logins:" + request.Email.Trim().ToLowerInvariant();
    int failures = _cache.Get<int>(key);

    if (failures >= _maxFailures)
      throw new AppException("Too many failed attempts. Try again later.", HttpStatusCode.TooManyRequests);

    UserEntity? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null || user.Password != request.Password)
    {
      // The window restarts with every failure, so the lock lasts _lockout after the last attempt.
      _cache.Set(key, failures + 1, _lockout);
      throw AppException.Unauthorized("Invalid password or email");
    }

    _cache.Remove(key);
    return Ok(ApiResponse.Ok(new { user.Id, user.Username }, "Login successful"));
  }
}
