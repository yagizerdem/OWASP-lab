namespace Models.DTO;

public class PaymentWebhookDTO
{
  public Guid orderId { get; set; }
  public string status { get; set; } = null!;
  public decimal amount { get; set; }
}
