using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// COMPLIANT counterpart of <see cref="VulnerableTlsController"/>.
/// <para>
/// Checklist item covered: the default certificate validation (chain, expiry, host name) stays on, so any bad certificate fails the call.
/// The error detail is logged server side only.
/// </para>
/// </summary>
[ApiController]
[Route("lab/secure/crypto/tls")]
public class SecureTlsController : ControllerBase
{
  private readonly IHttpClientFactory _factory;
  private readonly ILogger<SecureTlsController> _logger;

  /// <summary>Dependencies come from DI.</summary>
  public SecureTlsController(IHttpClientFactory factory, ILogger<SecureTlsController> logger)
  {
    _factory = factory;
    _logger = logger;
  }

  /// <summary>
  /// Fetches a test target with normal certificate validation.
  /// <para>
  /// Expectation: <c>valid</c> answers 200; <c>expired</c>, <c>self-signed</c>, <c>wrong-host</c> and <c>untrusted-root</c> answer 502
  /// "The upstream TLS certificate was rejected" and the reason appears only in the server log.
  /// </para>
  /// </summary>
  /// <param name="target">valid, expired, self-signed, wrong-host, untrusted-root or local-dev-cert.</param>
  [HttpGet("fetch")]
  public async Task<IActionResult> Fetch([FromQuery] string target = "self-signed")
  {
    if (!VulnerableTlsController.Targets.TryGetValue(target, out string? url))
      throw new AppException("Unknown target. Allowed: " + string.Join(", ", VulnerableTlsController.Targets.Keys));

    try
    {
      HttpClient client = _factory.CreateClient("default-tls");
      using HttpResponseMessage response = await client.GetAsync(url);
      return Ok(ApiResponse.Ok(new { target, url, status = (int)response.StatusCode, validated = true }));
    }
    catch (HttpRequestException ex)
    {
      _logger.LogWarning("TLS validation failed for {Target}: {Reason}", target, ex.InnerException?.Message ?? ex.Message);
      throw new AppException("The upstream TLS certificate was rejected", HttpStatusCode.BadGateway);
    }
  }
}
