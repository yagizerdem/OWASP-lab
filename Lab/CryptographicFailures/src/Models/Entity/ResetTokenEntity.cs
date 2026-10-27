namespace Models.Entity;

/// <summary>
/// Password reset token. Vulnerable flow: <see cref="TokenValue"/> is the plaintext 6 digit code, no expiry, never consumed.
/// Secure flow: <see cref="TokenValue"/> is the SHA-256 of a CSPRNG token, with expiry and single use.
/// </summary>
public class ResetTokenEntity : BaseEntity
{
  /// <summary>Account the token belongs to.</summary>
  public string UserName { get; set; } = null!;

  /// <summary>Plaintext code (vulnerable) or SHA-256 hex of the token (secure).</summary>
  public string TokenValue { get; set; } = null!;

  /// <summary>True when the row was created by the secure flow.</summary>
  public bool IssuedSecurely { get; set; }

  /// <summary>Expiry time; <c>null</c> means the token never expires (vulnerable flow).</summary>
  public DateTime? ExpiresAt { get; set; }

  /// <summary>When the token was consumed; <c>null</c> means unused (the vulnerable flow never sets it).</summary>
  public DateTime? UsedAt { get; set; }
}
