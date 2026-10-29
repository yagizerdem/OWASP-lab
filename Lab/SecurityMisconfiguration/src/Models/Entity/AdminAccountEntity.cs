using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>
/// Back-office account. Some accounts ship with a vendor default password and IsDefaultPassword = true
/// until someone changes it.
/// </summary>
public class AdminAccountEntity : BaseEntity
{
  [Required]
  [MinLength(2)]
  [MaxLength(100)]
  public string Username { get; set; } = null!;

  [Required]
  [MaxLength(100)]
  public string Password { get; set; } = null!;

  [Required]
  [MaxLength(32)]
  public string Role { get; set; } = null!;

  public bool IsDefaultPassword { get; set; }
}
