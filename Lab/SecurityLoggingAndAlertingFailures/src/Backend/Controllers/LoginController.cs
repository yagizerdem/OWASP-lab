using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// Case 1 - login logging. The vulnerable login writes the password to the log, logs user input raw
/// (CR/LF forges extra log lines) and keeps no record of failures, so brute force goes unnoticed.
/// The secure login stores every attempt as a security event and raises an alert on repeated failures.
/// </summary>
[ApiController]
[Route("[controller]")]
public class LoginController : ControllerBase
{
  private const int AlertThreshold = 5;
  private static readonly TimeSpan AlertWindow = TimeSpan.FromMinutes(5);

  private readonly AppDbContext _context;
  private readonly ILogger<LoginController> _logger;

  public LoginController(AppDbContext context, ILogger<LoginController> logger)
  {
    _context = context;
    _logger = logger;
  }

  /// <summary>
  /// VULNERABLE: (1) the plaintext password ends up in the log, (2) the email is concatenated into the
  /// message, so "a@b.com\nLogin success email=admin@lab.local" forges a second line, (3) failed attempts
  /// are not stored or alerted on.
  /// </summary>
  [HttpPost]
  [Route("/lab/vulnerable/logging/login")]
  public async Task<IActionResult> LoginVulnerable([FromBody] LoginRequestDTO request)
  {
    _logger.LogInformation("Login attempt email=" + request.email + " password=" + request.password);

    UserEntity? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.email);
    if (user == null || user.Password != request.password)
    {
      // No security event, no warning, no alert.
      throw AppException.Unauthorized("Invalid credentials");
    }

    _logger.LogInformation("Login success email=" + request.email);
    return Ok(ApiResponse.Ok("login successful"));
  }

  /// <summary>
  /// SECURE: structured logging without the password, user input stripped of CR/LF, every attempt
  /// persisted as a security event, and a BRUTE_FORCE_ALERT event plus a critical log entry when an email
  /// or IP collects too many failures in a short window.
  /// </summary>
  [HttpPost]
  [Route("/lab/secure/logging/login")]
  public async Task<IActionResult> LoginSecure([FromBody] LoginRequestDTO request)
  {
    string email = Sanitize(request.email);
    string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    UserEntity? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.email);
    bool success = user != null && user.Password == request.password;

    _context.SecurityEvents.Add(new SecurityEventEntity()
    {
      Id = Guid.NewGuid(),
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      EventType = success ? "LOGIN_SUCCESS" : "LOGIN_FAILURE",
      Username = email,
      IpAddress = ip,
      Success = success
    });
    await _context.SaveChangesAsync();

    if (!success)
    {
      _logger.LogWarning("Login failure for {Email} from {Ip}", email, ip);

      DateTime since = DateTime.UtcNow - AlertWindow;
      int failures = await _context.SecurityEvents.CountAsync(e =>
        e.EventType == "LOGIN_FAILURE" && e.CreatedAt >= since && (e.Username == email || e.IpAddress == ip));

      if (failures >= AlertThreshold)
      {
        _context.SecurityEvents.Add(new SecurityEventEntity()
        {
          Id = Guid.NewGuid(),
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow,
          EventType = "BRUTE_FORCE_ALERT",
          Username = email,
          IpAddress = ip,
          Success = false,
          Details = $"{failures} failed logins in {AlertWindow.TotalMinutes} minutes"
        });
        await _context.SaveChangesAsync();

        _logger.LogCritical("ALERT brute force suspected: {Failures} failed logins for {Email} from {Ip}", failures, email, ip);
      }

      throw AppException.Unauthorized("Invalid credentials");
    }

    _logger.LogInformation("Login success for {Email} from {Ip}", email, ip);
    return Ok(ApiResponse.Ok("login successful"));
  }

  /// <summary>Latest security events, so failures and alerts produced by the secure login can be seen.</summary>
  [HttpGet]
  [Route("/lab/secure/logging/events")]
  public async Task<IActionResult> GetEvents([FromQuery] int take = 50)
  {
    var events = await _context.SecurityEvents
      .OrderByDescending(e => e.CreatedAt)
      .Take(Math.Clamp(take, 1, 200))
      .ToListAsync();

    return Ok(ApiResponse.Ok(events));
  }

  /// <summary>Removes CR/LF so user input cannot start a new log line, and caps the length.</summary>
  private static string Sanitize(string? value)
  {
    string clean = (value ?? string.Empty).Replace('\r', '_').Replace('\n', '_');
    return clean.Length > 100 ? clean[..100] : clean;
  }
}
