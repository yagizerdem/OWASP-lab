
using System.ComponentModel.DataAnnotations;
public class LoginDTO
{
  [Required]
  [EmailAddress]
  public string email { get; set; } = null!;

  [Required]
  public string password { get; set; } = null!;
}
