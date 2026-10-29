using System.ComponentModel.DataAnnotations;

public class RequestPasswordResetDTO
{
  [Required]
  [EmailAddress]
  public string email { get; set; } = null!;
}
