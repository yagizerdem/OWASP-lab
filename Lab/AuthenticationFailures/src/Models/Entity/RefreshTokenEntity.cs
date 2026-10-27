namespace Models.Entity;

/// <summary>
/// Server-side record of an issued refresh token. Only the SHA-256 hash of the token is stored.
/// </summary>
public class RefreshTokenEntity : BaseEntity
{
  /// <summary>Owner of the token.</summary>
  public Guid UserId { get; set; }

  /// <summary>SHA-256 (hex) digest; the raw token is never stored.</summary>
  public string TokenHash { get; set; } = null!;

  /// <summary>Expiry time (UTC).</summary>
  public DateTime ExpiresAt { get; set; }

  /// <summary>Revocation time; when set the token was used or revoked.</summary>
  public DateTime? RevokedAt { get; set; }

  /// <summary>Hash of the token that replaced this one during rotation.</summary>
  public string? ReplacedByTokenHash { get; set; }

  /// <summary>Client IP the token was issued to.</summary>
  public string? CreatedByIp { get; set; }

  /// <summary>True when not revoked and not expired.</summary>
  public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
}
