using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Models.DTO;

namespace Backend.Controllers;

/// <summary>
/// VULNERABLE BY DESIGN. Broken encryption: DES, AES-ECB, a static IV, no authentication (malleable ciphertext), a padding oracle,
/// a hard-coded key and a password used directly as a key.
/// <para>
/// Checklist items covered: (1) old algorithms (DES), (2) default/hard-coded and re-used key, (3) key in source control,
/// (6) IV reused, ECB and no authenticated encryption, (7) password as key without a KDF, (10) padding oracle error messages.
/// </para>
/// Compliant counterpart: <see cref="SecureEncryptionController"/>.
/// </summary>
[ApiController]
[Route("lab/vulnerable/crypto/encryption")]
public class VulnerableEncryptionController : ControllerBase
{
  /// <summary>Static IV, identical for every encryption (checklist item: IV reused).</summary>
  private static readonly byte[] StaticIv = new byte[16];

  private readonly byte[] _key;

  /// <summary>The hard-coded key is read from the committed <c>appsettings.json</c> (<c>Crypto:LegacyKey</c>) with a literal fallback in code.</summary>
  public VulnerableEncryptionController(IConfiguration config)
  {
    _key = Encoding.UTF8.GetBytes((config["Crypto:LegacyKey"] ?? "legacy-key-1234!").PadRight(16)[..16]);
  }

  /// <summary>
  /// Encrypts with AES-ECB.
  /// <para>Weakness: equal plaintext blocks give equal ciphertext blocks, so patterns leak.</para>
  /// <para>Expectation: encrypting 48 identical characters returns three identical 32 hex character blocks.</para>
  /// </summary>
  /// <param name="dto">Plaintext in <c>Text</c>.</param>
  [HttpPost("ecb/encrypt")]
  public IActionResult EcbEncrypt([FromBody] PlainTextDto dto)
  {
    using var aes = Aes.Create();
    aes.Key = _key;
    byte[] cipher = aes.EncryptEcb(Encoding.UTF8.GetBytes(dto.Text), PaddingMode.PKCS7);
    return Ok(ApiResponse.Ok(new { mode = "AES-128-ECB", cipherHex = Convert.ToHexString(cipher).ToLowerInvariant() }));
  }

  /// <summary>Decrypts an ECB hex ciphertext produced by <see cref="EcbEncrypt"/>. Same weaknesses; also leaks the raw exception message.</summary>
  /// <param name="dto">Hex ciphertext in <c>Text</c>.</param>
  [HttpPost("ecb/decrypt")]
  public IActionResult EcbDecrypt([FromBody] PlainTextDto dto)
  {
    using var aes = Aes.Create();
    aes.Key = _key;
    return Ok(ApiResponse.Ok(new { plaintext = Encoding.UTF8.GetString(Run(() => aes.DecryptEcb(FromHex(dto.Text), PaddingMode.PKCS7))) }));
  }

  /// <summary>
  /// Encrypts with single DES.
  /// <para>Weakness: 56-bit key, brute-forceable in hours; deprecated for decades.</para>
  /// </summary>
  /// <param name="dto">Plaintext in <c>Text</c>.</param>
  [HttpPost("des/encrypt")]
  public IActionResult DesEncrypt([FromBody] PlainTextDto dto)
  {
    using var des = DES.Create();
    des.Key = _key[..8];
    byte[] cipher = des.EncryptCbc(Encoding.UTF8.GetBytes(dto.Text), StaticIv[..8], PaddingMode.PKCS7);
    return Ok(ApiResponse.Ok(new { mode = "DES-CBC (56-bit key)", cipherHex = Convert.ToHexString(cipher).ToLowerInvariant() }));
  }

  /// <summary>
  /// Encrypts with AES-CBC, a static IV and no MAC.
  /// <para>Weaknesses: the same plaintext always gives the same ciphertext (IV reuse) and nothing detects tampering.</para>
  /// <para>Expectation: two identical requests return identical <c>cipherHex</c>.</para>
  /// </summary>
  /// <param name="dto">Plaintext in <c>Text</c>.</param>
  [HttpPost("cbc/encrypt")]
  public IActionResult CbcEncrypt([FromBody] PlainTextDto dto)
  {
    return Ok(ApiResponse.Ok(new { mode = "AES-128-CBC, static IV, no MAC", cipherHex = Hex(CbcEncryptCore(_key, Encoding.UTF8.GetBytes(dto.Text))) }));
  }

  /// <summary>
  /// Issues a "session token": the hex of <c>IV || AES-CBC("admin=0;uid=NNNN")</c>.
  /// <para>Weakness: unauthenticated CBC where the IV travels in the token. See <see cref="WhoAmI"/> for the bit-flipping attack.</para>
  /// </summary>
  /// <param name="uid">Numeric user id 0-9999.</param>
  [HttpPost("session/issue")]
  public IActionResult IssueSession([FromQuery] int uid = 1)
  {
    byte[] cipher = CbcEncryptCore(_key, Encoding.UTF8.GetBytes($"admin=0;uid={Math.Clamp(uid, 0, 9999):D4}"));
    return Ok(ApiResponse.Ok(new { token = Hex(StaticIv.Concat(cipher).ToArray()) }));
  }

  /// <summary>
  /// Reads a session token.
  /// <para>
  /// Weaknesses: (a) no integrity check, so flipping bits of the IV flips the same bits of the first plaintext block
  /// (XOR byte 6 of the token with 0x01 turns <c>admin=0</c> into <c>admin=1</c>); (b) a padding oracle: bad padding answers
  /// 400 "Padding is invalid", good padding with a bad payload answers 422 "Malformed session payload", so an attacker can decrypt
  /// any ciphertext byte by byte.
  /// </para>
  /// </summary>
  /// <param name="token">Hex token from <see cref="IssueSession"/>, possibly tampered with.</param>
  [HttpGet("session/whoami")]
  public IActionResult WhoAmI([FromQuery] string token)
  {
    byte[] raw = FromHex(token);
    if (raw.Length < 32 || raw.Length % 16 != 0) throw new AppException("Token has an invalid length");

    byte[] plain = Run(() =>
    {
      using var aes = Aes.Create();
      aes.Key = _key;
      return aes.DecryptCbc(raw[16..], raw[..16], PaddingMode.PKCS7);
    });

    string payload = Encoding.UTF8.GetString(plain);
    string[] parts = payload.Split(';');
    if (parts.Length != 2 || !parts[0].StartsWith("admin=") || !parts[1].StartsWith("uid="))
      throw new AppException("Malformed session payload", System.Net.HttpStatusCode.UnprocessableEntity);

    return Ok(ApiResponse.Ok(new { isAdmin = parts[0] == "admin=1", payload }));
  }

  /// <summary>
  /// Encrypts with a key taken straight from the password (zero padded to 16 bytes, no KDF, no salt) and a static IV.
  /// <para>Weakness: the key space is the password space, so a dictionary attack decrypts instantly.</para>
  /// </summary>
  /// <param name="dto">Password and plaintext.</param>
  [HttpPost("password-key/encrypt")]
  public IActionResult PasswordKeyEncrypt([FromBody] PasswordProtectDto dto)
  {
    return Ok(ApiResponse.Ok(new { cipherHex = Hex(CbcEncryptCore(PasswordAsKey(dto.Password), Encoding.UTF8.GetBytes(dto.Text))) }));
  }

  /// <summary>
  /// Decrypts a password-keyed ciphertext.
  /// <para>Weakness: a wrong password normally ends in a padding exception whose message is returned, which tells the attacker the guess was wrong.</para>
  /// </summary>
  /// <param name="dto">Password and hex ciphertext.</param>
  [HttpPost("password-key/decrypt")]
  public IActionResult PasswordKeyDecrypt([FromBody] PasswordProtectDto dto)
  {
    byte[] plain = Run(() =>
    {
      using var aes = Aes.Create();
      aes.Key = PasswordAsKey(dto.Password);
      return aes.DecryptCbc(FromHex(dto.Text), StaticIv, PaddingMode.PKCS7);
    });

    return Ok(ApiResponse.Ok(new { plaintext = Encoding.UTF8.GetString(plain) }));
  }

  private static byte[] PasswordAsKey(string password) => Encoding.UTF8.GetBytes(password.PadRight(16, '\0'))[..16];

  private static byte[] CbcEncryptCore(byte[] key, byte[] data)
  {
    using var aes = Aes.Create();
    aes.Key = key;
    return aes.EncryptCbc(data, StaticIv, PaddingMode.PKCS7);
  }

  /// <summary>Runs a crypto operation and returns the raw exception text to the caller (an information leak on purpose).</summary>
  private static T Run<T>(Func<T> op)
  {
    try
    {
      return op();
    }
    catch (CryptographicException ex)
    {
      throw new AppException(ex.Message);
    }
  }

  private static string Hex(byte[] data) => Convert.ToHexString(data).ToLowerInvariant();

  private static byte[] FromHex(string hex)
  {
    try
    {
      return Convert.FromHexString(hex);
    }
    catch (FormatException)
    {
      throw new AppException("Not a hex string");
    }
  }
}
