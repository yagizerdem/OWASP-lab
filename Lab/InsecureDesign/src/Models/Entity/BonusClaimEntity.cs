using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>
/// INSECURE DESIGN: each row is one "signup bonus" credit. The business rule "one bonus per account"
/// exists only as an idea, never as an enforced constraint (no unique index on UserId, no check before
/// inserting), so the claim endpoint can be called any number of times to mint unlimited balance.
/// </summary>
public class BonusClaimEntity : BaseEntity
{
  [Required]
  public Guid UserId { get; set; }

  public decimal Amount { get; set; }
}
