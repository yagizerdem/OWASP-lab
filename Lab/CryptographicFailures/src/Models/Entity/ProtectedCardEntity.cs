namespace Models.Entity;

/// <summary>Payment card stored the compliant way: AES-256-GCM ciphertext and its nonce.</summary>
public class ProtectedCardEntity : BaseEntity
{
  /// <summary>Card owner.</summary>
  public string OwnerName { get; set; } = null!;

  /// <summary>Last four digits, kept in clear so lists can be masked without decrypting.</summary>
  public string Last4 { get; set; } = null!;

  /// <summary>Base64 GCM nonce, unique per row and per encryption.</summary>
  public string Nonce { get; set; } = null!;

  /// <summary>Base64 ciphertext with the GCM tag appended.</summary>
  public string CipherText { get; set; } = null!;
}
