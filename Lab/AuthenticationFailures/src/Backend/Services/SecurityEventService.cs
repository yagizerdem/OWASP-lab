namespace backend.Services;

using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Models.Entity;

/// <summary>
/// Single place that records authentication / access control events and raises an alert
/// when one client keeps failing (possible brute force, enumeration or privilege probing).
/// </summary>
public class SecurityEventService
{
  // Event types that count towards the "repeated failures" alert.
  private static readonly string[] FailureTypes =
  {
    SecurityEventTypes.LoginFailed, SecurityEventTypes.LoginBlocked, SecurityEventTypes.RefreshFailed,
    SecurityEventTypes.RefreshTokenReuse, SecurityEventTypes.RevokedTokenUsed, SecurityEventTypes.Unauthenticated,
    SecurityEventTypes.Forbidden, SecurityEventTypes.OwnershipViolation, SecurityEventTypes.BlockedPath,
    SecurityEventTypes.RateLimited,
  };

  private readonly AppDbContext _db;
  private readonly IHttpContextAccessor _accessor;
  private readonly IConfiguration _configuration;
  private readonly IMemoryCache _cache;
  private readonly ILogger<SecurityEventService> _logger;

  /// <summary>Dependencies come from DI.</summary>
  public SecurityEventService(AppDbContext db, IHttpContextAccessor accessor, IConfiguration configuration,
    IMemoryCache cache, ILogger<SecurityEventService> logger)
  {
    _db = db;
    _accessor = accessor;
    _configuration = configuration;
    _cache = cache;
    _logger = logger;
  }

  private int AlertThreshold => _configuration.GetValue("Security:AlertThreshold", 5);
  private TimeSpan AlertWindow => TimeSpan.FromMinutes(_configuration.GetValue("Security:AlertWindowMinutes", 10));
  private int MaxFailedLogins => _configuration.GetValue("Security:MaxFailedLogins", 5);
  private TimeSpan LockoutWindow => TimeSpan.FromMinutes(_configuration.GetValue("Security:LockoutMinutes", 15));

  /// <summary>
  /// Records the event (rule 6), logs non-Info events as warnings and checks the alert threshold for failure types.
  /// Never throws; the request continues even if recording fails. RateLimited events are written once per 30 s per IP.
  /// </summary>
  public async Task RecordAsync(string type, string severity, string? detail = null, Guid? userId = null, string? subject = null)
  {
    try
    {
      var ctx = _accessor.HttpContext;
      string? ip = ctx?.Connection.RemoteIpAddress?.ToString();

      // A flood of rate-limited requests must not turn into a flood of DB writes.
      if (type == SecurityEventTypes.RateLimited)
      {
        if (_cache.TryGetValue($"rl:{ip}", out _)) return;
        _cache.Set($"rl:{ip}", true, TimeSpan.FromSeconds(30));
      }

      _db.SecurityEvents.Add(new SecurityEventEntity
      {
        Id = Guid.NewGuid(),
        Type = type,
        Severity = severity,
        UserId = userId,
        Subject = Clean(subject, 256),
        IpAddress = ip,
        Method = ctx?.Request.Method,
        Path = Clean(ctx?.Request.Path.Value, 512),
        Detail = Clean(detail, 1024),
      });
      await _db.SaveChangesAsync();

      if (severity != SecurityEventSeverity.Info)
        _logger.LogWarning("SECURITY {Type} ip={Ip} user={UserId} subject={Subject} path={Path} detail={Detail}",
          type, ip, userId, Clean(subject, 256), Clean(ctx?.Request.Path.Value, 512), Clean(detail, 200));

      if (FailureTypes.Contains(type) && ip != null)
        await RaiseAlertIfNeededAsync(ip);
    }
    catch (Exception ex)
    {
      // Auditing must never take the request down.
      _logger.LogError(ex, "Could not record security event {Type}", type);
    }
  }

  /// <summary>
  /// When the same IP reaches the failure threshold inside the window, writes one Alert event and a Critical log line (alert admins).
  /// </summary>
  private async Task RaiseAlertIfNeededAsync(string ip)
  {
    DateTime since = DateTime.UtcNow - AlertWindow;

    int failures = await _db.SecurityEvents.CountAsync(e =>
      e.IpAddress == ip && e.CreatedAt >= since && FailureTypes.Contains(e.Type));
    if (failures < AlertThreshold) return;

    bool alreadyAlerted = await _db.SecurityEvents.AnyAsync(e =>
      e.IpAddress == ip && e.CreatedAt >= since && e.Type == SecurityEventTypes.RepeatedFailures);
    if (alreadyAlerted) return;

    _db.SecurityEvents.Add(new SecurityEventEntity
    {
      Id = Guid.NewGuid(),
      Type = SecurityEventTypes.RepeatedFailures,
      Severity = SecurityEventSeverity.Alert,
      IpAddress = ip,
      Detail = $"{failures} failures in the last {AlertWindow.TotalMinutes:0} minutes",
    });
    await _db.SaveChangesAsync();

    // Hook for a real alerting channel (mail, SIEM, chat). Critical level is what log based alerts should match.
    _logger.LogCritical("SECURITY ALERT: {Failures} access/authentication failures from {Ip} in {Minutes} minutes",
      failures, ip, AlertWindow.TotalMinutes);
  }

  /// <summary>
  /// True when the given login subject has too many failed attempts since its last successful login.
  /// </summary>
  public async Task<bool> IsLockedOutAsync(string subject)
  {
    DateTime since = DateTime.UtcNow - LockoutWindow;

    DateTime? lastSuccess = await _db.SecurityEvents
      .Where(e => e.Subject == subject && e.Type == SecurityEventTypes.LoginSucceeded && e.CreatedAt >= since)
      .MaxAsync(e => (DateTime?)e.CreatedAt);

    DateTime from = lastSuccess ?? since;

    int failures = await _db.SecurityEvents.CountAsync(e =>
      e.Subject == subject && e.Type == SecurityEventTypes.LoginFailed && e.CreatedAt > from);

    return failures >= MaxFailedLogins;
  }

  /// <summary>Strips control characters (log forging) and truncates.</summary>
  private static string? Clean(string? value, int max)
  {
    if (value == null) return null;
    var sb = new StringBuilder(Math.Min(value.Length, max));
    foreach (char c in value)
    {
      if (sb.Length >= max) break;
      sb.Append(char.IsControl(c) ? ' ' : c);
    }
    return sb.ToString();
  }
}
