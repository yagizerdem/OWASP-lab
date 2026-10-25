
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entity;

public class BankAccountEntity : BaseEntity
{
  public string AccountNumber { get; set; } = null!;
  public decimal Balance { get; set; }

  [ForeignKey("OwnerId")]
  public UserEntity owner { get; set; } = null!;

  public Guid OwnerId { get; set; }

}
