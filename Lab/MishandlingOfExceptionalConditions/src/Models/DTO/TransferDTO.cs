namespace Models.DTO;

public class TransferDTO
{
  public Guid fromUserId { get; set; }
  public Guid toUserId { get; set; }
  public decimal amount { get; set; }
}
