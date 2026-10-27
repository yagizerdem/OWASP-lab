namespace backend.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Models.DTO;
using Models.Entity;

/// <summary>
/// VULNERABLE BY DESIGN. Insecure counterpart of <see cref="SecureAuthController"/>; do not copy.
/// <para>
/// Rules it violates: (1) everything is anonymous, (2) wide-open CORS with credentials, (6) nothing is logged,
/// (7) no rate limit and no lockout, (8) 30 day token, logout does nothing on the server, no refresh/rotation.
/// Authentication failures it demonstrates: user enumeration, plaintext password compare with timing difference,
/// token accepted from the query string, token in a script-readable cookie.
/// </para>
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableCors("Vulnerable")]
[DisableRateLimiting]
[Route("lab/vulnerable/auth")]
public class VulnerableAuthController : ControllerBase
{
  private readonly AppDbContext _db;
  private readonly IConfiguration _configuration;
  private readonly JwtService _jwt;

  /// <summary>Dependencies come from DI.</summary>
  public VulnerableAuthController(AppDbContext db, IConfiguration configuration, JwtService jwt)
  {
    _db = db;
    _configuration = configuration;
    _jwt = jwt;
  }

  /// <summary>
  /// Logs a user in with e-mail and password.
  /// <para>
  /// Weaknesses (rules 6, 7, 8): unknown e-mail answers 404 with a different message than a wrong password (user
  /// enumeration); there is no lockout and no rate limit, so passwords can be brute forced; failures are not logged;
  /// the token lives for <c>JWT:expiresInMinutes</c> (30 days); the token is also set in a cookie without
  /// <c>HttpOnly</c>/<c>Secure</c>.
  /// </para>
  /// <para>Compare with <see cref="SecureAuthController.Login"/>.</para>
  /// </summary>
  /// <param name="request">E-mail and password.</param>
  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginDto request)
  {
    UserEntity user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email)
      ?? throw AppException.NotFound("No account exists for this e-mail");

    if (user.Password != request.Password)
      throw AppException.Unauthorized("Wrong password for this account");

    string token = CreateLongLivedToken(user);

    Response.Cookies.Append("token", token, new CookieOptions
    {
      Expires = DateTimeOffset.UtcNow.AddDays(30),
      HttpOnly = false,
      Secure = false,
    });

    return Ok(ApiResponse.Ok(new { token }, "Login successful"));
  }

  /// <summary>
  /// "Logs out" the caller.
  /// <para>
  /// Weakness (rule 8): only the cookie is removed in the response. The token is not invalidated on the server, so
  /// the same token keeps working on <see cref="Me"/> and everywhere else until it expires.
  /// </para>
  /// <para>Compare with <see cref="SecureAuthController.Logout"/>.</para>
  /// </summary>
  [HttpPost("logout")]
  public IActionResult Logout()
  {
    Response.Cookies.Delete("token");
    return Ok(ApiResponse.Ok(null, "Logged out"));
  }

  /// <summary>
  /// Returns the identity inside the token.
  /// <para>
  /// Weaknesses (rules 2, 8): the token is accepted from the query string (ends up in logs, referrers and browser
  /// history), from the header or from the cookie, with hand written parsing that is not shared with other endpoints;
  /// there is no revocation check, so logged-out tokens still work.
  /// </para>
  /// </summary>
  /// <param name="token">Token passed in the URL (insecure on purpose).</param>
  [HttpGet("me")]
  public IActionResult Me([FromQuery] string? token)
  {
    token ??= Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
    if (string.IsNullOrWhiteSpace(token)) token = Request.Cookies["token"];

    JwtUserInfo info = _jwt.ValidateToken(token ?? string.Empty);
    return Ok(ApiResponse.Ok(new { info.UserId, info.Email, info.Username, info.ExpiresAt }));
  }

  /// <summary>Builds a signed JWT that is valid for the (very long) configured legacy lifetime.</summary>
  private string CreateLongLivedToken(UserEntity user)
  {
    string secret = _configuration["JWT:secret"] ?? throw AppException.InternalServerError("Internal server err");
    string issuer = _configuration["JWT:issuer"] ?? throw AppException.InternalServerError("Internal server err");
    string audience = _configuration["JWT:audience"] ?? throw AppException.InternalServerError("Internal server err");
    int minutes = _configuration.GetValue("JWT:expiresInMinutes", 43200);

    var credentials = new SigningCredentials(
      new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
      new(JwtRegisteredClaimNames.Email, user.Email),
      new(JwtRegisteredClaimNames.Name, user.Username),
      new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    var jwt = new JwtSecurityToken(issuer, audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(minutes), credentials);
    return new JwtSecurityTokenHandler().WriteToken(jwt);
  }
}
