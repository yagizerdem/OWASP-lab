namespace backend.Controllers;

using System.Security.Cryptography;
using System.Text;
using backend.Security;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

/// <summary>
/// Hardened authentication flow (login / refresh / logout / me).
/// <para>
/// Rules covered: (6) authentication/access failures are logged and repeated failures raise an alert,
/// (7) rate limiting, (8) short-lived access token + refresh token + server side invalidation after logout,
/// (9) declarative [Authorize]/[AllowAnonymous] usage.
/// </para>
/// <para>
/// Expectation: only login and refresh are anonymous; everything else needs a token (deny by default).
/// The existing weak <c>AuthController</c> (lab/Auth) is left as is for comparison.
/// </para>
/// </summary>
[ApiController]
[Route("lab/secure/auth")]
public class SecureAuthController : ControllerBase
{
  // Compared against when the e-mail is unknown so both failure paths cost about the same.
  private const string DummyPassword = "not-a-real-password";

  private readonly AppDbContext _db;
  private readonly TokenService _tokens;
  private readonly SecurityEventService _events;

  /// <summary>Dependencies come from DI.</summary>
  public SecureAuthController(AppDbContext db, TokenService tokens, SecurityEventService events)
  {
    _db = db;
    _tokens = tokens;
    _events = events;
  }

  /// <summary>
  /// Authenticates the user with e-mail + password and returns an access + refresh token pair.
  /// <para>Scope: rules 6, 7, 8. One of the two anonymous endpoints.</para>
  /// <para>
  /// Expectation: (a) wrong e-mail and wrong password give the same 401 + same message (no user enumeration);
  /// (b) after <c>Security:MaxFailedLogins</c> (5) failures since the last successful login the account answers 429 even with the
  /// correct password (lockout); (c) more than 10 requests per minute per IP get 429 ("auth" rate limit);
  /// (d) every failed/successful attempt is written to the SecurityEvents table; (e) the password is compared in constant time.
  /// </para>
  /// </summary>
  /// <param name="request">E-mail and password.</param>
  /// <returns>200 + token pair; 401 bad credentials; 429 lockout or rate limit.</returns>
  [HttpPost("login")]
  [AllowAnonymous]
  [EnableRateLimiting("auth")]
  public async Task<IActionResult> Login([FromBody] LoginDto request)
  {
    string subject = request.Email.Trim().ToLowerInvariant();

    if (await _events.IsLockedOutAsync(subject))
    {
      await _events.RecordAsync(SecurityEventTypes.LoginBlocked, SecurityEventSeverity.Warning,
        "Login attempt while locked out", subject: subject);
      throw new AppException("Too many failed attempts. Try again later.", System.Net.HttpStatusCode.TooManyRequests);
    }

    UserEntity? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

    bool valid = FixedTimeEquals(user?.Password ?? DummyPassword, request.Password) && user != null;
    if (!valid)
    {
      await _events.RecordAsync(SecurityEventTypes.LoginFailed, SecurityEventSeverity.Warning,
        "Invalid credentials", user?.Id, subject);
      // Same status and message whether the e-mail exists or not.
      throw AppException.Unauthorized("Invalid password or email");
    }

    IssuedTokens tokens = await _tokens.IssueAsync(user!);
    await _events.RecordAsync(SecurityEventTypes.LoginSucceeded, SecurityEventSeverity.Info, null, user!.Id, subject);

    return Ok(ApiResponse.Ok(ToResponse(tokens), "Login successful"));
  }

  /// <summary>
  /// Exchanges a valid refresh token for a new access + refresh token pair (rotation).
  /// <para>Scope: rule 8 (OAuth style refresh), 6, 7. [AllowAnonymous] because the token itself is the credential.</para>
  /// <para>
  /// Expectation: an old refresh token is single use; presenting it a second time is treated as theft, all refresh tokens
  /// of the user are revoked, an Alert level "RefreshTokenReuse" event is written and 401 is returned.
  /// An unknown or expired token gets the same 401 message.
  /// </para>
  /// </summary>
  /// <param name="request">Refresh token previously obtained from login/refresh.</param>
  /// <returns>200 + new token pair; 401 invalid token; 429 rate limit.</returns>
  [HttpPost("refresh")]
  [AllowAnonymous]
  [EnableRateLimiting("auth")]
  public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
  {
    IssuedTokens tokens = await _tokens.RotateAsync(request.RefreshToken);
    return Ok(ApiResponse.Ok(ToResponse(tokens), "Token refreshed"));
  }

  /// <summary>
  /// Ends the current session: the access token's jti is deny-listed and, if supplied, the refresh token is revoked.
  /// <para>Scope: rule 8 (server side invalidation after logout), 9 ([Authorize]).</para>
  /// <para>
  /// Expectation: after logout every request with the same access token gets 401 (and a "RevokedTokenUsed" event is written);
  /// refreshing with the revoked refresh token fails. Sending somebody else's refresh token has no effect.
  /// </para>
  /// </summary>
  /// <param name="request">Optional; the refresh token to revoke. The body may be empty.</param>
  /// <returns>200 success; 401 missing/invalid token.</returns>
  [HttpPost("logout")]
  [Authorize]
  public async Task<IActionResult> Logout([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] LogoutRequestDto? request)
  {
    Guid userId = User.GetUserId();

    await _tokens.RevokeAccessTokenAsync(User.GetTokenId(), userId, User.GetTokenExpiry());
    if (!string.IsNullOrEmpty(request?.RefreshToken))
      await _tokens.RevokeRefreshTokenAsync(request.RefreshToken, userId);

    await _events.RecordAsync(SecurityEventTypes.Logout, SecurityEventSeverity.Info, null, userId);
    return Ok(ApiResponse.Ok(null, "Logged out"));
  }

  /// <summary>
  /// Revokes all refresh tokens of the caller and the current access token ("sign out everywhere").
  /// <para>Scope: rule 8. Used to end all device sessions when something looks suspicious.</para>
  /// <para>
  /// Expectation: afterwards no refresh token works. Access tokens on other devices are not tracked one by one, so they
  /// die on their own within <c>JWT:accessTokenMinutes</c> (15 min) at the latest; this is a deliberate limit.
  /// </para>
  /// </summary>
  /// <returns>200 success; 401 missing/invalid token.</returns>
  [HttpPost("logout-all")]
  [Authorize]
  public async Task<IActionResult> LogoutAll()
  {
    Guid userId = User.GetUserId();

    await _tokens.RevokeAccessTokenAsync(User.GetTokenId(), userId, User.GetTokenExpiry());
    await _tokens.RevokeAllRefreshTokensAsync(userId);

    await _events.RecordAsync(SecurityEventTypes.LogoutAll, SecurityEventSeverity.Info, null, userId);
    return Ok(ApiResponse.Ok(null, "Logged out everywhere"));
  }

  /// <summary>
  /// Returns the identity decoded from the token and the current roles (read from the database).
  /// <para>Scope: rules 1 and 9; shows that identity and roles are re-evaluated on the server for every request.</para>
  /// <para>Expectation: 401 without a token; with a valid token id, e-mail, username, roles and token expiry.</para>
  /// </summary>
  /// <returns>200 + identity summary; 401 not authenticated.</returns>
  [HttpGet("me")]
  [Authorize]
  public IActionResult Me()
  {
    return Ok(ApiResponse.Ok(new
    {
      id = User.GetUserId(),
      email = User.FindFirst("email")?.Value,
      username = User.FindFirst("name")?.Value,
      roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value),
      tokenExpiresAt = User.GetTokenExpiry(),
    }));
  }

  /// <summary>Maps the token pair to the response shape returned to the client.</summary>
  private static object ToResponse(IssuedTokens t) => new
  {
    accessToken = t.AccessToken,
    accessTokenExpiresAt = t.AccessTokenExpiresAt,
    refreshToken = t.RefreshToken,
    refreshTokenExpiresAt = t.RefreshTokenExpiresAt,
  };

  /// <summary>Hashes both strings and compares them in constant time (prevents timing attacks, does not leak length).</summary>
  private static bool FixedTimeEquals(string a, string b) =>
    CryptographicOperations.FixedTimeEquals(
      SHA256.HashData(Encoding.UTF8.GetBytes(a)),
      SHA256.HashData(Encoding.UTF8.GetBytes(b)));
}
