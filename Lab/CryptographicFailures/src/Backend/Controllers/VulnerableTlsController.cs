using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// VULNERABLE BY DESIGN. Outgoing HTTPS calls that do not validate the server certificate.
/// <para>
/// Checklist item covered: the received server certificate and trust chain are not validated (expired, self-signed, wrong host and
/// untrusted-root certificates are all accepted, which allows a man in the middle).
/// </para>
/// The target is chosen from a fixed allow-list (no SSRF) and needs internet access to the public <c>badssl.com</c> test hosts.
/// Compliant counterpart: <see cref="SecureTlsController"/>.
/// </summary>
[ApiController]
[Route("lab/vulnerable/crypto/tls")]
public class VulnerableTlsController : ControllerBase
{
  /// <summary>Allow-listed test targets shared by both controllers.</summary>
  public static readonly Dictionary<string, string> Targets = new(StringComparer.OrdinalIgnoreCase)
  {
    ["valid"] = "https://example.com/",
    ["expired"] = "https://expired.badssl.com/",
    ["self-signed"] = "https://self-signed.badssl.com/",
    ["wrong-host"] = "https://wrong.host.badssl.com/",
    ["untrusted-root"] = "https://untrusted-root.badssl.com/",
    // This app's own https endpoint (launchSettings https profile). Its ASP.NET Core dev certificate is untrusted on many machines.
    ["local-dev-cert"] = "https://localhost:5021/lab/secure/crypto/transport/info"
  };

  private readonly IHttpClientFactory _factory;

  /// <summary>The HTTP client factory comes from DI.</summary>
  public VulnerableTlsController(IHttpClientFactory factory)
  {
    _factory = factory;
  }

  /// <summary>
  /// Fetches a test target with certificate validation switched off (<c>DangerousAcceptAnyServerCertificateValidator</c>).
  /// <para>Expectation: every target, including expired, self-signed, wrong-host and untrusted-root, answers 200.</para>
  /// </summary>
  /// <param name="target">valid, expired, self-signed, wrong-host, untrusted-root or local-dev-cert.</param>
  [HttpGet("fetch")]
  public async Task<IActionResult> Fetch([FromQuery] string target = "self-signed")
  {
    if (!Targets.TryGetValue(target, out string? url)) throw new AppException("Unknown target. Allowed: " + string.Join(", ", Targets.Keys));

    try
    {
      HttpClient client = _factory.CreateClient("insecure-tls");
      using HttpResponseMessage response = await client.GetAsync(url);
      return Ok(ApiResponse.Ok(new { target, url, status = (int)response.StatusCode, validated = false }, "Certificate was NOT validated"));
    }
    catch (HttpRequestException ex)
    {
      // Network problems (no internet, target not listening) are not certificate problems.
      throw new AppException("Upstream unreachable: " + (ex.InnerException?.Message ?? ex.Message), System.Net.HttpStatusCode.BadGateway);
    }
  }
}
