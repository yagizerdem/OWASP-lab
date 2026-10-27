using System.ComponentModel.DataAnnotations;

namespace Models.DTO;

/// <summary>Refresh request body.</summary>
public class RefreshRequestDto
{
  [Required]
  /// <summary>Refresh token obtained from login/refresh.</summary>
  public string RefreshToken { get; set; } = null!;
}

/// <summary>Logout request body (optional).</summary>
public class LogoutRequestDto
{
  /// <summary>The refresh token to revoke.</summary>
  public string? RefreshToken { get; set; }
}
