namespace backend.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// User metadata carried inside a validated JWT.
/// </summary>
public record JwtUserInfo(Guid UserId, string Email, string Username, string TokenId, DateTime ExpiresAt);

public class JwtService
{
  private readonly IConfiguration _configuration;

  public JwtService(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  /// <summary>
  /// Validates the signature, issuer, audience and lifetime of a JWT and returns the user metadata inside it.
  /// Throws <see cref="AppException"/> (401) if the token is invalid.
  /// </summary>
  public JwtUserInfo ValidateToken(string token)
  {
    if (string.IsNullOrWhiteSpace(token))
      throw AppException.Unauthorized("Invalid token");

    string secretKey = _configuration["JWT:secret"] ??
      throw AppException.InternalServerError("Internal server err");

    string issuer = _configuration["JWT:issuer"] ??
      throw AppException.InternalServerError("Internal server err");

    string audience = _configuration["JWT:audience"] ??
      throw AppException.InternalServerError("Internal server err");

    var validationParameters = new TokenValidationParameters
    {
      ValidateIssuerSigningKey = true,
      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
      ValidateIssuer = true,
      ValidIssuer = issuer,
      ValidateAudience = true,
      ValidAudience = audience,
      ValidateLifetime = true,
      RequireExpirationTime = true,
      ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
      ClockSkew = TimeSpan.Zero
    };

    // MapInboundClaims = false keeps claim names as written in the token ("sub", "email", ...).
    var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

    try
    {
      var principal = handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
      var jwt = (JwtSecurityToken)validatedToken;

      string? sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
      if (!Guid.TryParse(sub, out Guid userId))
        throw AppException.Unauthorized("Invalid token");

      return new JwtUserInfo(
        userId,
        principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty,
        principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? string.Empty,
        principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty,
        jwt.ValidTo);
    }
    catch (SecurityTokenException)
    {
      throw AppException.Unauthorized("Invalid token");
    }
    catch (ArgumentException)
    {
      // Malformed token (not a well-formed JWS)
      throw AppException.Unauthorized("Invalid token");
    }
  }
}
