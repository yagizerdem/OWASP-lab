using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>An order that becomes "Paid" when the payment provider calls the webhook.</summary>
public class OrderEntity : BaseEntity
{
  [Required]
  public Guid UserId { get; set; }

  public decimal Amount { get; set; }

  [Required]
  [MaxLength(32)]
  public string Status { get; set; } = "Pending";
}
