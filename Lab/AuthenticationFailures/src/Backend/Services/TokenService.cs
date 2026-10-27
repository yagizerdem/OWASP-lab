namespace backend.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Models.Entity;

/// <summary>Issued token pair and their expiry times.</summary>
public record IssuedTokens(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

/// <summary>
/// Issues short-lived access tokens plus rotating refresh tokens, and revokes both.
/// </summary>
public class TokenService
{
  private readonly AppDbContext _db;
  private readonly IConfiguration _configuration;
  private readonly IHttpContextAccessor _accessor;
  private readonly SecurityEventService _events;

  /// <summary>Dependencies come from DI.</summary>
  public TokenService(AppDbContext db, IConfiguration configuration, IHttpContextAccessor accessor, SecurityEventService events)
  {
    _db = db;
    _configuration = configuration;
    _accessor = accessor;
    _events = events;
  }

  /// <summary>Hashes a refresh token with SHA-256 (hex) so it can be stored in the DB.</summary>
  public static string Hash(string token) =>
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

  /// <summary>Creates a new access + refresh token for the user and stores the refresh token.</summary>
  public async Task<IssuedTokens> IssueAsync(UserEntity user)
  {
    var (accessToken, accessExpires) = CreateAccessToken(user);
    var (refreshToken, refreshExpires) = CreateRefreshToken(user.Id);
    await _db.SaveChangesAsync();
    return new IssuedTokens(accessToken, accessExpires, refreshToken, refreshExpires);
  }

  /// <summary>
  /// Rotates a refresh token. Presenting an already used (revoked) token means it was stolen or replayed,
  /// so every refresh token of that user is revoked.
  /// </summary>
  public async Task<IssuedTokens> RotateAsync(string refreshToken)
  {
    string hash = Hash(refreshToken);
    RefreshTokenEntity? stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);

    if (stored == null)
    {
      await _events.RecordAsync(SecurityEventTypes.RefreshFailed, SecurityEventSeverity.Warning, "Unknown refresh token");
      throw AppException.Unauthorized("Invalid refresh token");
    }

    if (stored.RevokedAt != null)
    {
      await RevokeAllRefreshTokensAsync(stored.UserId);
      await _events.RecordAsync(SecurityEventTypes.RefreshTokenReuse, SecurityEventSeverity.Alert,
        "Revoked refresh token was presented again; all sessions of the user were revoked", stored.UserId);
      throw AppException.Unauthorized("Invalid refresh token");
    }

    if (stored.ExpiresAt <= DateTime.UtcNow)
    {
      await _events.RecordAsync(SecurityEventTypes.RefreshFailed, SecurityEventSeverity.Warning, "Expired refresh token", stored.UserId);
      throw AppException.Unauthorized("Invalid refresh token");
    }

    UserEntity? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == stored.UserId);
    if (user == null)
      throw AppException.Unauthorized("Invalid refresh token");

    var (accessToken, accessExpires) = CreateAccessToken(user);
    var (newRefresh, refreshExpires) = CreateRefreshToken(user.Id);

    stored.RevokedAt = DateTime.UtcNow;
    stored.ReplacedByTokenHash = Hash(newRefresh);
    await _db.SaveChangesAsync();

    return new IssuedTokens(accessToken, accessExpires, newRefresh, refreshExpires);
  }

  /// <summary>Adds the access token's jti to the deny-list until it would have expired anyway.</summary>
  public async Task RevokeAccessTokenAsync(string jti, Guid userId, DateTime expiresAt)
  {
    if (string.IsNullOrEmpty(jti)) return;
    if (await _db.RevokedTokens.AnyAsync(r => r.Jti == jti)) return;

    _db.RevokedTokens.Add(new RevokedTokenEntity { Id = Guid.NewGuid(), Jti = jti, UserId = userId, ExpiresAt = expiresAt });

    // Expired entries are useless (the token fails validation anyway), keep the table small.
    var expired = await _db.RevokedTokens.Where(r => r.ExpiresAt < DateTime.UtcNow).ToListAsync();
    _db.RevokedTokens.RemoveRange(expired);

    await _db.SaveChangesAsync();
  }

  /// <summary>Revokes one refresh token, only if it belongs to <paramref name="userId"/>.</summary>
  public async Task RevokeRefreshTokenAsync(string refreshToken, Guid userId)
  {
    string hash = Hash(refreshToken);
    RefreshTokenEntity? stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId);
    if (stored is { RevokedAt: null })
    {
      stored.RevokedAt = DateTime.UtcNow;
      await _db.SaveChangesAsync();
    }
  }

  /// <summary>Revokes all active refresh tokens of the user.</summary>
  public async Task RevokeAllRefreshTokensAsync(Guid userId)
  {
    var active = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
    foreach (var t in active) t.RevokedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();
  }

  /// <summary>Creates a short-lived (JWT:accessTokenMinutes) signed JWT; it carries no roles.</summary>
  private (string Token, DateTime ExpiresAt) CreateAccessToken(UserEntity user)
  {
    string secretKey = _configuration["JWT:secret"] ?? throw AppException.InternalServerError("Internal server err");
    string issuer = _configuration["JWT:issuer"] ?? throw AppException.InternalServerError("Internal server err");
    string audience = _configuration["JWT:audience"] ?? throw AppException.InternalServerError("Internal server err");
    int minutes = _configuration.GetValue("JWT:accessTokenMinutes", 15);

    var credentials = new SigningCredentials(
      new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), SecurityAlgorithms.HmacSha256);

    // Roles are intentionally not in the token: they are read from the DB on every request.
    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
      new(JwtRegisteredClaimNames.Email, user.Email),
      new(JwtRegisteredClaimNames.Name, user.Username),
      new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    DateTime expires = DateTime.UtcNow.AddMinutes(minutes);
    var token = new JwtSecurityToken(issuer, audience, claims, DateTime.UtcNow, expires, credentials);
    return (new JwtSecurityTokenHandler().WriteToken(token), expires);
  }

  /// <summary>Creates a 64 byte cryptographically random token; only its hash is added to the context (the caller calls SaveChanges).</summary>
  private (string Token, DateTime ExpiresAt) CreateRefreshToken(Guid userId)
  {
    string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    DateTime expires = DateTime.UtcNow.AddDays(_configuration.GetValue("JWT:refreshTokenDays", 7));

    _db.RefreshTokens.Add(new RefreshTokenEntity
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      TokenHash = Hash(token),
      ExpiresAt = expires,
      CreatedByIp = _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
    });

    return (token, expires);
  }
}
