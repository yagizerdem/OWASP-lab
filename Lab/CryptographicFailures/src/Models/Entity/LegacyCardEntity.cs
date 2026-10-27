namespace Models.Entity;

/// <summary>Payment card stored the insecure way: the full card number in plaintext (vulnerable endpoints only).</summary>
public class LegacyCardEntity : BaseEntity
{
  /// <summary>Card owner (matches a <see cref="WeakHashEntity.UserName"/>).</summary>
  public string OwnerName { get; set; } = null!;

  /// <summary>Full card number, plaintext.</summary>
  public string CardNumber { get; set; } = null!;
}
