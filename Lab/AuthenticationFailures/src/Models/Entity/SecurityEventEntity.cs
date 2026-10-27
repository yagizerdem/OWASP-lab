namespace Models.Entity;

/// <summary>Event type names.</summary>
public static class SecurityEventTypes
{
  public const string LoginSucceeded = "LoginSucceeded";
  public const string LoginFailed = "LoginFailed";
  public const string LoginBlocked = "LoginBlocked";
  public const string Logout = "Logout";
  public const string LogoutAll = "LogoutAll";
  public const string RefreshFailed = "RefreshFailed";
  public const string RefreshTokenReuse = "RefreshTokenReuse";
  public const string RevokedTokenUsed = "RevokedTokenUsed";
  public const string Unauthenticated = "Unauthenticated";
  public const string Forbidden = "Forbidden";
  public const string OwnershipViolation = "OwnershipViolation";
  public const string BlockedPath = "BlockedPath";
  public const string RateLimited = "RateLimited";
  public const string RepeatedFailures = "RepeatedFailures";
}

/// <summary>Severity levels: Info (audit trail), Warning (failure), Alert (needs an admin's attention).</summary>
public static class SecurityEventSeverity
{
  public const string Info = "Info";
  public const string Warning = "Warning";
  public const string Alert = "Alert";
}

/// <summary>
/// Audit trail for authentication and access control events. Alert-severity rows are what an admin should look at first.
/// </summary>
public class SecurityEventEntity : BaseEntity
{
  /// <summary>Event type, see <see cref="SecurityEventTypes"/>.</summary>
  public string Type { get; set; } = null!;

  /// <summary>Severity, see <see cref="SecurityEventSeverity"/>.</summary>
  public string Severity { get; set; } = SecurityEventSeverity.Warning;

  /// <summary>The user that triggered the event, when known.</summary>
  public Guid? UserId { get; set; }

  /// <summary>Identifier the caller claimed (e.g. the e-mail used at login).</summary>
  public string? Subject { get; set; }

  /// <summary>Client IP; the key of the repeated-failure alert.</summary>
  public string? IpAddress { get; set; }

  /// <summary>HTTP method.</summary>
  public string? Method { get; set; }

  /// <summary>Request path (control characters stripped).</summary>
  public string? Path { get; set; }

  /// <summary>Human readable description.</summary>
  public string? Detail { get; set; }
}
