

public class ChangePasswordDTO
{
  public string email { get; set; } = null!;
  public string securityAnswer { get; set; } = null!;
  public string newPassword { get; set; } = null!;
}
