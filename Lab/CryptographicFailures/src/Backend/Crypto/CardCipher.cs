using System.Security.Cryptography;
using System.Text;
using Models.Crypto;

namespace Backend.Crypto;

/// <summary>
/// AES-256-GCM encryption for card numbers. The key is never in the source code or in a committed config file: it comes
/// from <c>Crypto__CardKey</c> (base64, 32 bytes), and in Development it is generated once into the git-ignored
/// <c>App_Data/card-key.txt</c>.
/// </summary>
public sealed class CardCipher : ICardCipher
{
  private const int KeySize = 32;
  private const int NonceSize = 12;
  private const int TagSize = 16;

  private readonly byte[] _key;

  public CardCipher(IConfiguration config, IHostEnvironment env)
  {
    string? key = config["Crypto:CardKey"];

    if (string.IsNullOrWhiteSpace(key))
    {
      if (!env.IsDevelopment())
      {
        throw new InvalidOperationException("Crypto__CardKey is not set.");
      }

      string path = Path.Combine(env.ContentRootPath, "App_Data", "card-key.txt");
      if (!File.Exists(path))
      {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Convert.ToBase64String(RandomNumberGenerator.GetBytes(KeySize)));
      }

      key = File.ReadAllText(path).Trim();
    }

    _key = Convert.FromBase64String(key);
    if (_key.Length != KeySize)
    {
      throw new InvalidOperationException($"The card key must be {KeySize} bytes.");
    }
  }

  public (string Nonce, string CipherText) Encrypt(string plaintext)
  {
    byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
    byte[] data = Encoding.UTF8.GetBytes(plaintext);
    byte[] cipher = new byte[data.Length];
    byte[] tag = new byte[TagSize];

    using var gcm = new AesGcm(_key, TagSize);
    gcm.Encrypt(nonce, data, cipher, tag);

    return (Convert.ToBase64String(nonce), Convert.ToBase64String([.. cipher, .. tag]));
  }
}
