namespace backend.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Models.Entity;

/// <summary>Signs JWTs for the lab users; the lifetime is chosen by the caller.</summary>
public class TokenFactory
{
  private readonly IConfiguration _configuration;

  public TokenFactory(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  /// <summary>Creates a signed HS256 JWT that expires after <paramref name="minutes"/>.</summary>
  public string Create(UserEntity user, int minutes)
  {
    string secret = _configuration["JWT:secret"] ?? throw AppException.InternalServerError("Internal server err");
    string issuer = _configuration["JWT:issuer"] ?? throw AppException.InternalServerError("Internal server err");
    string audience = _configuration["JWT:audience"] ?? throw AppException.InternalServerError("Internal server err");

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
