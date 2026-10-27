using System.Security.Cryptography;
using System.Text;
using Backend.Crypto;
using Microsoft.AspNetCore.Mvc;
using Models.DTO;

namespace Backend.Controllers;

/// <summary>
/// COMPLIANT counterpart of <see cref="VulnerableAlgorithmsController"/>.
/// <para>
/// Checklist items covered: (1) only modern algorithms, (9) SHA-2 for hashing and HMAC-SHA256 for integrity,
/// (11) the algorithm is fixed by the server and cannot be downgraded, (2) signing keys come from the key ring (HKDF sub-key, rotation aware).
/// </para>
/// </summary>
[ApiController]
[Route("lab/secure/crypto/algorithms")]
public class SecureAlgorithmsController : ControllerBase
{
  private readonly KeyRing _keys;

  /// <summary>The key ring comes from DI.</summary>
  public SecureAlgorithmsController(KeyRing keys)
  {
    _keys = keys;
  }

  /// <summary>
  /// Hashes text with an allow-listed SHA-2 algorithm.
  /// <para>Expectation: sha256/sha384/sha512 succeed; md5, sha1, fnv and anything else answer 400.</para>
  /// <para>Note: this is a plain digest for integrity checks of public data; never use it to store passwords (see <see cref="SecurePasswordController"/>).</para>
  /// </summary>
  /// <param name="text">Text to hash.</param>
  /// <param name="algorithm">sha256, sha384 or sha512.</param>
  [HttpGet("hash")]
  public IActionResult Hash([FromQuery] string text, [FromQuery] string algorithm = "sha256")
  {
    byte[] data = Encoding.UTF8.GetBytes(text);
    byte[] digest = algorithm.ToLowerInvariant() switch
    {
      "sha256" => SHA256.HashData(data),
      "sha384" => SHA384.HashData(data),
      "sha512" => SHA512.HashData(data),
      _ => throw new AppException("Unsupported algorithm. Allowed: sha256, sha384, sha512")
    };

    return Ok(ApiResponse.Ok(new { algorithm = algorithm.ToLowerInvariant(), hash = Convert.ToHexString(digest).ToLowerInvariant() }));
  }

  /// <summary>
  /// Signs a message with HMAC-SHA256.
  /// <para>Expectation: any <c>algorithm</c> value other than empty or <c>HMAC-SHA256</c> answers 400; the signature is <c>keyId.hex</c>.</para>
  /// </summary>
  /// <param name="dto">Message (the algorithm field is only accepted when it matches the server choice).</param>
  [HttpPost("sign")]
  public IActionResult Sign([FromBody] MessageDto dto)
  {
    EnsureServerAlgorithm(dto.Algorithm);
    return Ok(ApiResponse.Ok(new { algorithm = "HMAC-SHA256", signature = _keys.Sign(dto.Message) }));
  }

  /// <summary>
  /// Verifies a signature in constant time.
  /// <para>Expectation: <c>algorithm=none</c> answers 400; a tampered message or signature answers 401 with one uniform message.</para>
  /// </summary>
  /// <param name="dto">Message and signature.</param>
  [HttpPost("verify")]
  public IActionResult Verify([FromBody] VerifyDto dto)
  {
    EnsureServerAlgorithm(dto.Algorithm);

    if (!_keys.VerifySignature(dto.Message, dto.Signature))
      throw AppException.Unauthorized("Invalid signature");

    return Ok(ApiResponse.Ok(new { valid = true, algorithm = "HMAC-SHA256" }));
  }

  private static void EnsureServerAlgorithm(string? requested)
  {
    if (!string.IsNullOrEmpty(requested) && !requested.Equals("HMAC-SHA256", StringComparison.OrdinalIgnoreCase))
      throw new AppException("The algorithm is chosen by the server");
  }
}
