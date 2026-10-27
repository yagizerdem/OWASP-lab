using System.Security.Cryptography;
using System.Text;
using Backend.Crypto;
using Microsoft.AspNetCore.Mvc;
using Models.Crypto;
using Models.DTO;

namespace Backend.Controllers;

/// <summary>
/// COMPLIANT counterpart of <see cref="VulnerableEncryptionController"/>.
/// <para>
/// Checklist items covered: (1) AES-256 only, (2) keys from the <see cref="KeyRing"/> (random, versioned, not in source control),
/// (6) AES-GCM with a fresh random nonce per encryption (authenticated, tamper evident), (7) password based keys go through
/// PBKDF2-HMAC-SHA256 with a random salt, (10) every failure looks the same, so there is no padding oracle.
/// </para>
/// </summary>
[ApiController]
[Route("lab/secure/crypto/encryption")]
public class SecureEncryptionController : ControllerBase
{
  private const int VaultKeySize = 32;
  private const int VaultNonceSize = 12;
  private const int TagSize = 16;

  private readonly KeyRing _keys;

  /// <summary>The key ring comes from DI.</summary>
  public SecureEncryptionController(KeyRing keys)
  {
    _keys = keys;
  }

  /// <summary>
  /// Encrypts text with AES-256-GCM.
  /// <para>Expectation: encrypting the same text twice returns two different tokens (random nonce); the token starts with the key id.</para>
  /// </summary>
  /// <param name="dto">Plaintext in <c>Text</c>.</param>
  [HttpPost("encrypt")]
  public IActionResult Encrypt([FromBody] PlainTextDto dto)
  {
    return Ok(ApiResponse.Ok(new { mode = "AES-256-GCM", token = _keys.Encrypt(dto.Text).ToToken() }));
  }

  /// <summary>
  /// Decrypts a token from <see cref="Encrypt"/>.
  /// <para>Expectation: any modification of the token (even one bit) answers 400 "Invalid token"; the message never says why.</para>
  /// </summary>
  /// <param name="dto">Token in <c>Text</c>.</param>
  [HttpPost("decrypt")]
  public IActionResult Decrypt([FromBody] PlainTextDto dto)
  {
    return Ok(ApiResponse.Ok(new { plaintext = DecryptOrFail(dto.Text) }));
  }

  /// <summary>
  /// Issues a session token: AES-GCM of <c>admin=0;uid=NNNN</c>.
  /// <para>Expectation: unlike the vulnerable CBC token, flipping a bit makes <see cref="WhoAmI"/> reject the token.</para>
  /// </summary>
  /// <param name="uid">Numeric user id 0-9999.</param>
  [HttpPost("session/issue")]
  public IActionResult IssueSession([FromQuery] int uid = 1)
  {
    return Ok(ApiResponse.Ok(new { token = _keys.Encrypt($"admin=0;uid={Math.Clamp(uid, 0, 9999):D4}").ToToken() }));
  }

  /// <summary>
  /// Reads a session token.
  /// <para>Expectation: tampered, truncated or foreign tokens all answer the same 401 "Invalid session" (no padding oracle, no bit flipping).</para>
  /// </summary>
  /// <param name="token">Token from <see cref="IssueSession"/>.</param>
  [HttpGet("session/whoami")]
  public IActionResult WhoAmI([FromQuery] string token)
  {
    string payload;
    try
    {
      payload = DecryptOrFail(token);
    }
    catch (AppException)
    {
      throw AppException.Unauthorized("Invalid session");
    }

    return Ok(ApiResponse.Ok(new { isAdmin = payload.StartsWith("admin=1"), payload }));
  }

  /// <summary>
  /// Encrypts with a key derived from a password: PBKDF2-HMAC-SHA256 (600000 iterations, random 16 byte salt) then AES-256-GCM.
  /// <para>Expectation: output is <c>v1.iterations.salt.nonce.cipher</c>; two calls with the same inputs differ.</para>
  /// </summary>
  /// <param name="dto">Password and plaintext.</param>
  [HttpPost("password-key/encrypt")]
  public IActionResult PasswordKeyEncrypt([FromBody] PasswordProtectDto dto)
  {
    int iterations = Pbkdf2PasswordHasher.DefaultIterations;
    byte[] salt = RandomNumberGenerator.GetBytes(16);
    byte[] nonce = RandomNumberGenerator.GetBytes(VaultNonceSize);
    byte[] data = Encoding.UTF8.GetBytes(dto.Text);
    byte[] cipher = new byte[data.Length];
    byte[] tag = new byte[TagSize];

    using var gcm = new AesGcm(DeriveKey(dto.Password, salt, iterations), TagSize);
    gcm.Encrypt(nonce, data, cipher, tag);

    string token = string.Join('.', "v1", iterations, B64(salt), B64(nonce), B64(cipher.Concat(tag).ToArray()));
    return Ok(ApiResponse.Ok(new { token }));
  }

  /// <summary>
  /// Decrypts a <c>password-key</c> token.
  /// <para>Expectation: a wrong password and a damaged token answer the same 400 "Invalid password or token".</para>
  /// </summary>
  /// <param name="dto">Password and token.</param>
  [HttpPost("password-key/decrypt")]
  public IActionResult PasswordKeyDecrypt([FromBody] PasswordProtectDto dto)
  {
    try
    {
      string[] parts = dto.Text.Split('.');
      if (parts.Length != 5 || parts[0] != "v1") throw new CryptographicException();

      int iterations = int.Parse(parts[1]);
      // Reject attacker-chosen work factors that are too low (downgrade) or absurdly high (CPU exhaustion).
      if (iterations < Pbkdf2PasswordHasher.DefaultIterations || iterations > Pbkdf2PasswordHasher.DefaultIterations * 2)
        throw new CryptographicException();

      byte[] salt = Un64(parts[2]);
      byte[] nonce = Un64(parts[3]);
      byte[] all = Un64(parts[4]);
      if (all.Length < TagSize || nonce.Length != VaultNonceSize) throw new CryptographicException();

      byte[] plain = new byte[all.Length - TagSize];
      using var gcm = new AesGcm(DeriveKey(dto.Password, salt, iterations), TagSize);
      gcm.Decrypt(nonce, all[..^TagSize], all[^TagSize..], plain);

      return Ok(ApiResponse.Ok(new { plaintext = Encoding.UTF8.GetString(plain) }));
    }
    catch (Exception ex) when (ex is CryptographicException or FormatException or OverflowException)
    {
      throw new AppException("Invalid password or token");
    }
  }

  private string DecryptOrFail(string? token)
  {
    try
    {
      EncryptedField field = EncryptedField.FromToken(token) ?? throw new CryptographicException();
      return _keys.Decrypt(field);
    }
    catch (CryptographicException)
    {
      throw new AppException("Invalid token");
    }
  }

  private static byte[] DeriveKey(string password, byte[] salt, int iterations) =>
    Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, VaultKeySize);

  private static string B64(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

  private static byte[] Un64(string s)
  {
    string b64 = s.Replace('-', '+').Replace('_', '/');
    return Convert.FromBase64String(b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '='));
  }
}
