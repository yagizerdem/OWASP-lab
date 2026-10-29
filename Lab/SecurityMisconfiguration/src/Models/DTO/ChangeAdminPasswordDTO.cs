namespace Models.DTO;

public class ChangeAdminPasswordDTO
{
  public string username { get; set; } = null!;
  public string currentPassword { get; set; } = null!;
  public string newPassword { get; set; } = null!;
}
