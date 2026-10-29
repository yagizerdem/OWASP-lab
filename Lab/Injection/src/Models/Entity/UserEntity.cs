using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

public class UserEntity : BaseEntity
{
  [Required]
  [MinLength(2)]
  [MaxLength(100)]
  public string Username { get; set; } = null!;

  [Required]
  [EmailAddress]
  public string Email { get; set; } = null!;

  [Required]
  [MinLength(8)]
  [MaxLength(100)]
  public string Password { get; set; } = null!;

  [Range(0, 150)]
  public int age { get; set; }

  [Required]
  public string SecurityAnswer { get; set; } = null!;

}
