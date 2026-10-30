namespace backend.Controllers;

using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Models.DTO;
using Models.Entity;

/// <summary>
/// SECURE – Example 3: short session with server side logout.
/// <para>
/// Login returns a token valid for <c>JWT:accessTokenMinutes</c> (15). Logout puts the token's <c>jti</c> on a deny-list
/// until the token would expire anyway, and <see cref="Me"/> rejects deny-listed tokens with 401.
/// </para>
/// </summary>
[ApiController]
[Route("[controller]")]
public class SecureSessionController : ControllerBase
{
  private readonly AppDbContext _db;
  private readonly TokenFactory _tokens;
  private readonly JwtService _jwt;
  private readonly IMemoryCache _cache;
  private readonly int _tokenMinutes;

  public SecureSessionController(AppDbContext db, TokenFactory tokens, JwtService jwt, IMemoryCache cache, IConfiguration configuration)
  {
    _db = db;
    _tokens = tokens;
    _jwt = jwt;
    _cache = cache;
    _tokenMinutes = configuration.GetValue("JWT:accessTokenMinutes", 15);
  }

  /// <summary>Returns a 15 minute token.</summary>
  [HttpPost]
  [Route("/lab/secure/session/login")]
  public async Task<IActionResult> Login([FromBody] LoginDto request)
  {
    UserEntity? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null || user.Password != request.Password)
      throw AppException.Unauthorized("Invalid password or email");

    return Ok(ApiResponse.Ok(new { token = _tokens.Create(user, _tokenMinutes) }, "Login successful"));
  }

  /// <summary>Revokes the caller's token on the server.</summary>
  [HttpPost]
  [Route("/lab/secure/session/logout")]
  public IActionResult Logout()
  {
    JwtUserInfo info = Authenticate();
    _cache.Set(RevokedKey(info.TokenId), true, new DateTimeOffset(info.ExpiresAt, TimeSpan.Zero));
    return Ok(ApiResponse.Ok(null, "Logged out"));
  }

  /// <summary>Returns the identity inside the bearer token; 401 once the token was logged out.</summary>
  [HttpGet]
  [Route("/lab/secure/session/me")]
  public IActionResult Me()
  {
    JwtUserInfo info = Authenticate();
    return Ok(ApiResponse.Ok(new { info.UserId, info.Email, info.Username, info.ExpiresAt }));
  }

  /// <summary>Validates the bearer token and checks it was not revoked by logout.</summary>
  private JwtUserInfo Authenticate()
  {
    JwtUserInfo info = _jwt.ValidateToken(BearerToken.From(Request));
    if (_cache.TryGetValue(RevokedKey(info.TokenId), out _))
      throw AppException.Unauthorized("Invalid token");
    return info;
  }

  private static string RevokedKey(string jti) => "revoked-jti:" + jti;
}
