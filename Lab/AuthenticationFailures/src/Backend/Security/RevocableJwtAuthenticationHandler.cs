namespace backend.Security;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models.Entity;

/// <summary>Authentication scheme and policy names.</summary>
public static class LabAuth
{
  public const string Scheme = "LabJwt";
  public const string AdminPolicy = "AdminOnly";
}

/// <summary>
/// Authenticates "Authorization: Bearer" tokens with the shared <see cref="JwtService"/>, then checks the server side
/// state that a stateless JWT cannot carry: logout deny-list, user still exists, current roles.
/// </summary>
public class RevocableJwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  private readonly JwtService _jwt;
  private readonly AppDbContext _db;
  private readonly SecurityEventService _events;

  /// <summary>Dependencies come from DI.</summary>
  public RevocableJwtAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, JwtService jwt, AppDbContext db, SecurityEventService events)
    : base(options, logger, encoder)
  {
    _jwt = jwt;
    _db = db;
    _events = events;
  }

  /// <summary>
  /// No bearer token: NoResult (anonymous); bad signature/issuer/audience/lifetime, a token revoked by logout or a deleted
  /// user: Fail. On success the roles are read from the DB and added to the principal (rules 2, 8, 9).
  /// </summary>
  protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    string? header = Request.Headers.Authorization;
    if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
      return AuthenticateResult.NoResult();

    JwtUserInfo info;
    try
    {
      info = _jwt.ValidateToken(header["Bearer ".Length..].Trim());
    }
    catch (AppException)
    {
      return AuthenticateResult.Fail("Invalid token");
    }

    if (await _db.RevokedTokens.AnyAsync(r => r.Jti == info.TokenId))
    {
      await _events.RecordAsync(SecurityEventTypes.RevokedTokenUsed, SecurityEventSeverity.Warning,
        "Logged-out access token was used", info.UserId);
      return AuthenticateResult.Fail("Token revoked");
    }

    if (!await _db.Users.AnyAsync(u => u.Id == info.UserId))
      return AuthenticateResult.Fail("Unknown user");

    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Sub, info.UserId.ToString()),
      new(JwtRegisteredClaimNames.Email, info.Email),
      new(JwtRegisteredClaimNames.Name, info.Username),
      new(JwtRegisteredClaimNames.Jti, info.TokenId),
      new(JwtRegisteredClaimNames.Exp, new DateTimeOffset(info.ExpiresAt, TimeSpan.Zero).ToUnixTimeSeconds().ToString()),
    };

    var roles = await _db.UserRoles.Where(r => r.UserId == info.UserId).Select(r => r.Role).ToListAsync();
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var identity = new ClaimsIdentity(claims, Scheme.Name, JwtRegisteredClaimNames.Name, ClaimTypes.Role);
    return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
  }
}
