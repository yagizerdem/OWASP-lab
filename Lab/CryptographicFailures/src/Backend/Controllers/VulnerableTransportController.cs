using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Models.DTO;

namespace Backend.Controllers;

/// <summary>
/// VULNERABLE BY DESIGN. Encryption in transit is not enforced and browser protections are missing.
/// <para>
/// Checklist items covered: encryption not enforced (plain HTTP accepted, no HSTS), missing security headers, cookies without
/// <c>Secure</c>/<c>HttpOnly</c>/<c>SameSite</c>, predictable session id, secrets in the URL, responses that may be cached.
/// </para>
/// The redirect to HTTPS is switched off for <c>/lab/vulnerable</c> in <c>Program.cs</c> so these calls work over <c>http://localhost:5020</c>.
/// Compliant counterpart: <see cref="SecureTransportController"/>.
/// </summary>
[ApiController]
[Route("lab/vulnerable/crypto/transport")]
public class VulnerableTransportController : ControllerBase
{
  private readonly IConfiguration _config;

  /// <summary>Configuration provides the legacy API key that lives in the committed <c>appsettings.json</c>.</summary>
  public VulnerableTransportController(IConfiguration config)
  {
    _config = config;
  }

  /// <summary>
  /// Creates a session cookie.
  /// <para>
  /// Weaknesses: the cookie has no <c>Secure</c> flag (sent over HTTP, sniffable), no <c>HttpOnly</c> (readable by script, XSS theft), no
  /// <c>SameSite</c>, and the value is the MD5 of the user name (predictable, forgeable). No <c>Cache-Control</c>, no HSTS.
  /// </para>
  /// <para>Expectation: the <c>Set-Cookie</c> header is <c>sid=&lt;md5&gt;; path=/</c> with no flags; the call works over plain HTTP.</para>
  /// </summary>
  /// <param name="dto">User name.</param>
  [HttpPost("session")]
  public IActionResult CreateSession([FromBody] SessionRequestDto dto)
  {
    string sid = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(dto.UserName))).ToLowerInvariant();
    Response.Cookies.Append("sid", sid, new CookieOptions { HttpOnly = false, Secure = false });
    return Ok(ApiResponse.Ok(new { sid }, "Session created"));
  }

  /// <summary>
  /// Returns a profile when the API key is correct.
  /// <para>
  /// Weakness: the secret is passed in the query string, so it ends up in server logs, proxy logs, browser history and the <c>Referer</c> header.
  /// The key itself is the legacy key committed to <c>appsettings.json</c>.
  /// </para>
  /// </summary>
  /// <param name="apiKey">Secret passed in the URL (insecure on purpose).</param>
  [HttpGet("profile")]
  public IActionResult Profile([FromQuery] string apiKey)
  {
    if (apiKey != (_config["Crypto:LegacyKey"] ?? "legacy-key-1234!"))
      throw AppException.Unauthorized("Wrong API key");

    return Ok(ApiResponse.Ok(new { name = "Lab Admin", email = "admin@lab.local", note = "Sent over HTTP with the key in the URL" }));
  }
}
