using System.ComponentModel.DataAnnotations;


namespace Models.Entity;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class UserEntity : BaseEntity
{

  /// <summary>
  /// Username of the user.
  /// </summary>
  [Required]
  public string Username { get; set; } = null!;
  /// <summary>
  /// Password of the user.
  /// </summary>
  [Required]
  public string Password { get; set; } = null!;

  /// <summary>
  /// Email address of the user.
  /// </summary>
  [Required]
  public string Email { get; set; } = null!;

  public List<BankAccountEntity> BankAccounts { get; set; } = new List<BankAccountEntity>();
}
