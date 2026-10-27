namespace Models.Crypto;

/// <summary>An authenticated-encryption result: which key was used, the nonce and the ciphertext (with the GCM tag appended).</summary>
/// <param name="KeyId">Identifier of the key that produced the ciphertext (needed for rotation).</param>
/// <param name="Nonce">Base64 nonce, unique per encryption.</param>
/// <param name="CipherText">Base64 ciphertext followed by the authentication tag.</param>
public record EncryptedField(string KeyId, string Nonce, string CipherText)
{
  /// <summary>Serialises to <c>keyId.nonce.cipher</c> using URL-safe base64, so the value is safe in headers and URLs.</summary>
  public string ToToken() => $"{KeyId}.{ToUrlSafe(Nonce)}.{ToUrlSafe(CipherText)}";

  /// <summary>Parses a value produced by <see cref="ToToken"/>; returns <c>null</c> when it is malformed.</summary>
  public static EncryptedField? FromToken(string? token)
  {
    string[]? parts = token?.Split('.');
    if (parts is not { Length: 3 } || parts[0].Length == 0) return null;
    return new EncryptedField(parts[0], FromUrlSafe(parts[1]), FromUrlSafe(parts[2]));
  }

  private static string ToUrlSafe(string b64) => b64.TrimEnd('=').Replace('+', '-').Replace('/', '_');

  private static string FromUrlSafe(string s)
  {
    string b64 = s.Replace('-', '+').Replace('_', '/');
    return b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
  }
}

/// <summary>Field level authenticated encryption backed by a versioned key ring (implemented in the Backend).</summary>
public interface IFieldCipher
{
  /// <summary>Id of the key used for new encryptions.</summary>
  string ActiveKeyId { get; }

  /// <summary>Encrypts with the active key and a fresh random nonce.</summary>
  EncryptedField Encrypt(string plaintext);

  /// <summary>Decrypts and authenticates; throws <see cref="System.Security.Cryptography.CryptographicException"/> on any failure.</summary>
  string Decrypt(EncryptedField field);
}
