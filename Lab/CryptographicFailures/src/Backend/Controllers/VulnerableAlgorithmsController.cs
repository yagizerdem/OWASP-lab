using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Models.DTO;

namespace Backend.Controllers;

/// <summary>
/// VULNERABLE BY DESIGN. Weak hashes, client-chosen algorithms and an integrity check that can be bypassed.
/// <para>
/// Checklist items covered: (1) old or weak algorithms, (9) deprecated or non-cryptographic hash functions,
/// (11) algorithm downgrade or bypass, (2) hard-coded key committed in <c>appsettings.json</c>.
/// </para>
/// Compliant counterpart: <see cref="SecureAlgorithmsController"/>.
/// </summary>
[ApiController]
[Route("lab/vulnerable/crypto/algorithms")]
public class VulnerableAlgorithmsController : ControllerBase
{
  private readonly IConfiguration _config;

  /// <summary>Configuration provides the legacy signing secret that lives in the committed <c>appsettings.json</c>.</summary>
  public VulnerableAlgorithmsController(IConfiguration config)
  {
    _config = config;
  }

  /// <summary>
  /// Hashes text with an algorithm chosen by the caller.
  /// <para>Weakness: MD5, SHA-1 and a non-cryptographic FNV-1a checksum are offered, so collisions and fast cracking are possible.</para>
  /// <para>Expectation: <c>algorithm=md5</c> returns <c>5f4dcc3b5aa765d61d8327deb882cf99</c> for <c>password</c> (public in every rainbow table).</para>
  /// </summary>
  /// <param name="text">Text to hash.</param>
  /// <param name="algorithm">md5, sha1, fnv or sha256.</param>
  [HttpGet("hash")]
  public IActionResult Hash([FromQuery] string text, [FromQuery] string algorithm = "md5")
  {
    return Ok(ApiResponse.Ok(new { algorithm, hash = Compute(algorithm, text) }));
  }

  /// <summary>
  /// Signs a message.
  /// <para>
  /// Weaknesses: the algorithm comes from the client, the signature is <c>H(secret || message)</c> instead of an HMAC (length extension
  /// with MD5/SHA-1), and the secret is the legacy key committed to source control.
  /// </para>
  /// </summary>
  /// <param name="dto">Message and algorithm name.</param>
  [HttpPost("sign")]
  public IActionResult Sign([FromBody] MessageDto dto)
  {
    string algorithm = dto.Algorithm ?? "md5";
    return Ok(ApiResponse.Ok(new { algorithm, signature = Compute(algorithm, Secret + dto.Message) }));
  }

  /// <summary>
  /// Verifies a signature.
  /// <para>
  /// Weaknesses: <c>algorithm=none</c> skips verification entirely (downgrade/bypass), and the comparison is a normal string compare
  /// (timing side channel).
  /// </para>
  /// <para>Expectation: <c>{"message":"amount=1000","algorithm":"none"}</c> is reported as valid without any signature.</para>
  /// </summary>
  /// <param name="dto">Message, signature and algorithm name.</param>
  [HttpPost("verify")]
  public IActionResult Verify([FromBody] VerifyDto dto)
  {
    string algorithm = dto.Algorithm ?? "md5";
    if (algorithm.Equals("none", StringComparison.OrdinalIgnoreCase))
      return Ok(ApiResponse.Ok(new { valid = true, algorithm }, "Signature accepted"));

    bool valid = Compute(algorithm, Secret + dto.Message) == dto.Signature;
    return Ok(ApiResponse.Ok(new { valid, algorithm }));
  }

  private string Secret => _config["Crypto:LegacyKey"] ?? "legacy-key-1234!";

  private static string Compute(string algorithm, string text)
  {
    byte[] data = Encoding.UTF8.GetBytes(text);
    return algorithm.ToLowerInvariant() switch
    {
      "md5" => Convert.ToHexString(MD5.HashData(data)).ToLowerInvariant(),
      "sha1" => Convert.ToHexString(SHA1.HashData(data)).ToLowerInvariant(),
      "sha256" => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant(),
      "fnv" => Fnv1a(data).ToString("x8"),
      _ => throw new AppException("Unsupported algorithm")
    };
  }

  /// <summary>32-bit FNV-1a: a fast non-cryptographic hash, trivially collidable, shown as a wrong choice for integrity.</summary>
  private static uint Fnv1a(byte[] data)
  {
    uint hash = 2166136261;
    foreach (byte b in data)
    {
      hash ^= b;
      hash *= 16777619;
    }

    return hash;
  }
}
