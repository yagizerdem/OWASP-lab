using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>One wallet per user; transfers move balance between wallets.</summary>
public class WalletEntity : BaseEntity
{
  [Required]
  public Guid UserId { get; set; }

  public decimal Balance { get; set; }
}
