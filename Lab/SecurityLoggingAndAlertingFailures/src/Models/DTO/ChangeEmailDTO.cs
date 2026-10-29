namespace Models.DTO;

public class ChangeEmailDTO
{
  public Guid actorUserId { get; set; }
  public Guid targetUserId { get; set; }
  public string newEmail { get; set; } = null!;
}
