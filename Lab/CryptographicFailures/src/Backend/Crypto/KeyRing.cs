using System.Security.Cryptography;
using System.Text;
using Models.Crypto;
using Newtonsoft.Json;

namespace Backend.Crypto;

/// <summary>
/// Compliant key management for the secure endpoints (checklist: default/weak/reused keys, keys in source control, missing rotation).
/// <list type="bullet">
/// <item>Keys are 256-bit random values and are never part of the source code or of a committed configuration file.</item>
/// <item>Outside Development they must be supplied from the environment (<c>Crypto__Keys__k1</c>, ...) or the app refuses to start.</item>
/// <item>In Development they are generated once into <c>App_Data/dev-keys.json</c>, which is git-ignored.</item>
/// <item>Every ciphertext carries its key id, so a new key can be activated while old data stays readable (rotation).</item>
/// </list>
/// </summary>
public sealed class KeyRing : IFieldCipher
{
  private const int KeySize = 32;
  private const int NonceSize = 12;
  private const int TagSize = 16;

  private readonly Dictionary<string, byte[]> _keys = new();

  /// <inheritdoc />
  public string ActiveKeyId { get; private set; }

  /// <summary>Ids of all keys that can decrypt.</summary>
  public IReadOnlyCollection<string> KeyIds => _keys.Keys;

  /// <summary>Secret that authorises administrative actions such as re-encryption (never committed either).</summary>
  public string AdminToken { get; }

  /// <summary>Loads keys from configuration/environment, or from the git-ignored development key file.</summary>
  public KeyRing(IConfiguration config, IHostEnvironment env, ILogger<KeyRing> logger)
  {
    foreach (IConfigurationSection section in config.GetSection("Crypto:Keys").GetChildren())
    {
      if (!string.IsNullOrWhiteSpace(section.Value)) AddKey(section.Key, section.Value);
    }

    string? adminToken = config["Crypto:AdminToken"];

    if (_keys.Count == 0)
    {
      if (!env.IsDevelopment())
        throw new InvalidOperationException("No encryption keys configured. Provide Crypto__Keys__k1 (base64, 32 bytes) via the environment.");

      (adminToken) = LoadOrCreateDevelopmentKeys(env, logger, adminToken);
    }

    AdminToken = adminToken ?? throw new InvalidOperationException("Crypto:AdminToken is required.");
    ActiveKeyId = config["Crypto:ActiveKeyId"] ?? _keys.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
    if (!_keys.ContainsKey(ActiveKeyId))
      throw new InvalidOperationException($"Active key '{ActiveKeyId}' is not in the key ring.");
  }

  /// <summary>Switches the key used for new encryptions (rotation); old keys stay available for decryption.</summary>
  public void Activate(string keyId)
  {
    if (!_keys.ContainsKey(keyId)) throw new AppException("Unknown key id");
    ActiveKeyId = keyId;
  }

  /// <inheritdoc />
  public EncryptedField Encrypt(string plaintext)
  {
    byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
    byte[] data = Encoding.UTF8.GetBytes(plaintext);
    byte[] cipher = new byte[data.Length];
    byte[] tag = new byte[TagSize];

    using var gcm = new AesGcm(_keys[ActiveKeyId], TagSize);
    gcm.Encrypt(nonce, data, cipher, tag, Encoding.UTF8.GetBytes(ActiveKeyId));

    return new EncryptedField(ActiveKeyId, Convert.ToBase64String(nonce), Convert.ToBase64String(cipher.Concat(tag).ToArray()));
  }

  /// <inheritdoc />
  public string Decrypt(EncryptedField field)
  {
    if (!_keys.TryGetValue(field.KeyId, out byte[]? key)) throw new CryptographicException("Decryption failed");

    try
    {
      byte[] nonce = Convert.FromBase64String(field.Nonce);
      byte[] all = Convert.FromBase64String(field.CipherText);
      if (nonce.Length != NonceSize || all.Length < TagSize) throw new CryptographicException("Decryption failed");

      byte[] cipher = all[..^TagSize];
      byte[] tag = all[^TagSize..];
      byte[] plain = new byte[cipher.Length];

      using var gcm = new AesGcm(key, TagSize);
      gcm.Decrypt(nonce, cipher, tag, plain, Encoding.UTF8.GetBytes(field.KeyId));
      return Encoding.UTF8.GetString(plain);
    }
    catch (FormatException)
    {
      // Same exception type and message for every failure: no oracle for an attacker.
      throw new CryptographicException("Decryption failed");
    }
  }

  /// <summary>HMAC-SHA256 signature <c>keyId.hex</c> using a sub-key derived with HKDF (the encryption key itself is never used as an HMAC key).</summary>
  public string Sign(string message)
  {
    return $"{ActiveKeyId}.{Convert.ToHexString(Mac(ActiveKeyId, message)).ToLowerInvariant()}";
  }

  /// <summary>Constant-time signature check.</summary>
  public bool VerifySignature(string message, string? signature)
  {
    string[]? parts = signature?.Split('.');
    if (parts is not { Length: 2 } || !_keys.ContainsKey(parts[0])) return false;

    try
    {
      return CryptographicOperations.FixedTimeEquals(Mac(parts[0], message), Convert.FromHexString(parts[1]));
    }
    catch (FormatException)
    {
      return false;
    }
  }

  private byte[] Mac(string keyId, string message)
  {
    byte[] subKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, _keys[keyId], KeySize, info: Encoding.UTF8.GetBytes("lab-signing"));
    return HMACSHA256.HashData(subKey, Encoding.UTF8.GetBytes(message));
  }

  private void AddKey(string id, string base64)
  {
    byte[] key = Convert.FromBase64String(base64);
    if (key.Length != KeySize) throw new InvalidOperationException($"Key '{id}' must be {KeySize} bytes (AES-256).");
    _keys[id] = key;
  }

  private string LoadOrCreateDevelopmentKeys(IHostEnvironment env, ILogger logger, string? adminToken)
  {
    string path = Path.Combine(env.ContentRootPath, "App_Data", "dev-keys.json");

    if (!File.Exists(path))
    {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      var created = new DevKeyFile
      {
        Keys = new Dictionary<string, string>
        {
          ["k1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(KeySize)),
          ["k2"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(KeySize)),
        },
        AdminToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()
      };
      File.WriteAllText(path, JsonConvert.SerializeObject(created, Formatting.Indented));
      logger.LogWarning("Generated development keys in {Path} (git-ignored). Use environment variables outside Development.", path);
    }

    DevKeyFile file = JsonConvert.DeserializeObject<DevKeyFile>(File.ReadAllText(path))
      ?? throw new InvalidOperationException("dev-keys.json is invalid.");
    foreach (var (id, value) in file.Keys) AddKey(id, value);
    return adminToken ?? file.AdminToken;
  }

  private class DevKeyFile
  {
    public Dictionary<string, string> Keys { get; set; } = new();
    public string AdminToken { get; set; } = null!;
  }
}
