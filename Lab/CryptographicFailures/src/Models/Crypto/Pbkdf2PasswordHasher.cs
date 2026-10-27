using System.Security.Cryptography;
using System.Text;

namespace Models.Crypto;

/// <summary>
/// Compliant password hashing: PBKDF2-HMAC-SHA256 with a random per-user salt and a work factor that is stored with the hash
/// so it can be raised later. Shared by the secure endpoints and the dummy data generator.
/// </summary>
public static class Pbkdf2PasswordHasher
{
  /// <summary>Algorithm label stored next to the hash.</summary>
  public const string AlgorithmName = "PBKDF2-SHA256";

  /// <summary>Current work factor (OWASP password storage cheat sheet recommendation for PBKDF2-HMAC-SHA256).</summary>
  public const int DefaultIterations = 600_000;

  private const int SaltSize = 16;
  private const int HashSize = 32;

  /// <summary>Hashes a password with a fresh random salt.</summary>
  /// <param name="password">Plaintext password.</param>
  /// <param name="iterations">Work factor.</param>
  /// <returns>Base64 hash, base64 salt and the iteration count used.</returns>
  public static (string Hash, string Salt, int Iterations) Hash(string password, int iterations = DefaultIterations)
  {
    byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
    byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, HashSize);
    return (Convert.ToBase64String(hash), Convert.ToBase64String(salt), iterations);
  }

  /// <summary>Verifies a password in constant time.</summary>
  public static bool Verify(string password, string hash, string salt, int iterations)
  {
    byte[] expected = Convert.FromBase64String(hash);
    byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
      Encoding.UTF8.GetBytes(password), Convert.FromBase64String(salt), iterations, HashAlgorithmName.SHA256, expected.Length);
    return CryptographicOperations.FixedTimeEquals(expected, actual);
  }
}
