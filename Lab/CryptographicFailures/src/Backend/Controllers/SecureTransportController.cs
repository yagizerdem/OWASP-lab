using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Models.DTO;

namespace Backend.Controllers;

/// <summary>
/// COMPLIANT counterpart of <see cref="VulnerableTransportController"/>.
/// <para>
/// Checklist items covered: encryption enforced (HTTPS only, HSTS), security headers on every <c>/lab/secure</c> response
/// (see <see cref="Backend.Security.SecurityHeadersMiddleware"/>), cookies with <c>Secure</c>/<c>HttpOnly</c>/<c>SameSite=Strict</c>,
/// unpredictable session ids, secrets only in headers or bodies.
/// </para>
/// </summary>
[ApiController]
[Route("lab/secure/crypto/transport")]
public class SecureTransportController : ControllerBase
{
  /// <summary>
  /// Creates a session cookie.
  /// <para>
  /// Expectation over <c>https://localhost:5021</c>: <c>Set-Cookie: sid=&lt;43 random chars&gt;; secure; samesite=strict; httponly</c> plus
  /// <c>Cache-Control: no-store</c>. Over plain HTTP the middleware answers 400 "HTTPS required" before any cookie is issued.
  /// </para>
  /// </summary>
  /// <param name="dto">User name.</param>
  [HttpPost("session")]
  public IActionResult CreateSession([FromBody] SessionRequestDto dto)
  {
    string sid = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    Response.Cookies.Append("sid", sid, new CookieOptions
    {
      HttpOnly = true,
      Secure = true,
      SameSite = SameSiteMode.Strict,
      MaxAge = TimeSpan.FromMinutes(15)
    });

    return Ok(ApiResponse.Ok(new { user = dto.UserName }, "Session created"));
  }

  /// <summary>
  /// Echoes the transport properties the server sees, to inspect the headers added by the middleware.
  /// <para>
  /// Expectation: response headers contain <c>Strict-Transport-Security</c>, <c>X-Content-Type-Options: nosniff</c>, <c>X-Frame-Options: DENY</c>,
  /// <c>Content-Security-Policy</c>, <c>Referrer-Policy</c> and <c>Cache-Control: no-store</c>.
  /// </para>
  /// </summary>
  [HttpGet("info")]
  public IActionResult Info()
  {
    return Ok(ApiResponse.Ok(new { scheme = Request.Scheme, isHttps = Request.IsHttps }));
  }
}
