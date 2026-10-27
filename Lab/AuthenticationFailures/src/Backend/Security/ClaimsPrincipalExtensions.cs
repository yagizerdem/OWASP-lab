namespace backend.Security;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

/// <summary>Short access to the validated token claims; the single source for ownership checks (rule 2).</summary>
public static class ClaimsPrincipalExtensions
{
  /// <summary>The caller's id, taken from the validated token. Never read it from the request body.</summary>
  public static Guid GetUserId(this ClaimsPrincipal user)
  {
    string? sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
    return Guid.TryParse(sub, out Guid id) ? id : throw AppException.Unauthorized("Invalid token");
  }

  /// <summary>The jti of the token (for the logout deny-list).</summary>
  public static string GetTokenId(this ClaimsPrincipal user) =>
    user.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? string.Empty;

  /// <summary>Expiry time of the token (UTC).</summary>
  public static DateTime GetTokenExpiry(this ClaimsPrincipal user) =>
    long.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Exp), out long exp)
      ? DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime
      : DateTime.UtcNow;
}
