namespace Models.Entity;

/// <summary>Compliant credential record: PBKDF2 hash, per-user salt and the work factor used (counterpart of <see cref="WeakHashEntity"/>).</summary>
public class SecureCredentialEntity : BaseEntity
{
  /// <summary>Login name (unique).</summary>
  public string UserName { get; set; } = null!;

  /// <summary>Base64 PBKDF2 hash.</summary>
  public string PasswordHash { get; set; } = null!;

  /// <summary>Base64 random salt, unique per user.</summary>
  public string Salt { get; set; } = null!;

  /// <summary>PBKDF2 iteration count at the time the hash was created.</summary>
  public int Iterations { get; set; }

  /// <summary>Algorithm label, e.g. <c>PBKDF2-SHA256</c>.</summary>
  public string Algorithm { get; set; } = null!;
}
