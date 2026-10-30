namespace backend.Controllers;

using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

/// <summary>
/// VULNERABLE BY DESIGN – Example 3: session that never really ends.
/// <para>
/// Login returns a token valid for <c>JWT:vulnerableTokenMinutes</c> (30 days). Logout only answers 200; the token is
/// not invalidated on the server, so a stolen token keeps working on <see cref="Me"/> after the user logged out.
/// </para>
/// <para>Compare with <see cref="SecureSessionController"/>.</para>
/// </summary>
[ApiController]
[Route("[controller]")]
public class VulnerableSessionController : ControllerBase
{
  private readonly AppDbContext _db;
  private readonly TokenFactory _tokens;
  private readonly JwtService _jwt;
  private readonly int _tokenMinutes;

  public VulnerableSessionController(AppDbContext db, TokenFactory tokens, JwtService jwt, IConfiguration configuration)
  {
    _db = db;
    _tokens = tokens;
    _jwt = jwt;
    _tokenMinutes = configuration.GetValue("JWT:vulnerableTokenMinutes", 43200);
  }

  /// <summary>Returns a 30 day token.</summary>
  [HttpPost]
  [Route("/lab/vulnerable/session/login")]
  public async Task<IActionResult> Login([FromBody] LoginDto request)
  {
    UserEntity? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null || user.Password != request.Password)
      throw AppException.Unauthorized("Invalid password or email");

    return Ok(ApiResponse.Ok(new { token = _tokens.Create(user, _tokenMinutes) }, "Login successful"));
  }

  /// <summary>"Logs out" without touching the token.</summary>
  [HttpPost]
  [Route("/lab/vulnerable/session/logout")]
  public IActionResult Logout()
  {
    return Ok(ApiResponse.Ok(null, "Logged out"));
  }

  /// <summary>Returns the identity inside the bearer token; logged-out tokens are still accepted.</summary>
  [HttpGet]
  [Route("/lab/vulnerable/session/me")]
  public IActionResult Me()
  {
    JwtUserInfo info = _jwt.ValidateToken(BearerToken.From(Request));
    return Ok(ApiResponse.Ok(new { info.UserId, info.Email, info.Username, info.ExpiresAt }));
  }
}
