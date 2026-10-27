namespace Models.Entity;

/// <summary>
/// Deny-list entry for an access token (by its jti) that was logged out before it expired.
/// Rows can be purged once <see cref="ExpiresAt"/> has passed.
/// </summary>
public class RevokedTokenEntity : BaseEntity
{
  /// <summary>Unique id of the revoked access token.</summary>
  public string Jti { get; set; } = null!;

  /// <summary>Token sahibi.</summary>
  public Guid UserId { get; set; }

  /// <summary>Natural expiry of the token; the row can be deleted afterwards.</summary>
  public DateTime ExpiresAt { get; set; }
}
