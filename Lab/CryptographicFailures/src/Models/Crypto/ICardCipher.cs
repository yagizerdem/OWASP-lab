namespace Models.Crypto;

/// <summary>Encrypts card numbers before they are stored (implemented in the Backend, used by the seeder too).</summary>
public interface ICardCipher
{
  /// <summary>Encrypts with a fresh random nonce. Returns base64 nonce and base64 ciphertext with the GCM tag appended.</summary>
  (string Nonce, string CipherText) Encrypt(string plaintext);
}
