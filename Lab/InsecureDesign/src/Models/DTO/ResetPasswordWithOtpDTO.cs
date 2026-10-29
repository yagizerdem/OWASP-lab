

public class ResetPasswordWithOtpDTO
{
  public string email { get; set; } = null!;
  public string otp { get; set; } = null!;
  public string newPassword { get; set; } = null!;
}
