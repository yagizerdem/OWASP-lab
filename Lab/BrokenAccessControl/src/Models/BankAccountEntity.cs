

using System.ComponentModel.DataAnnotations.Schema;

public class BankAccountEntity : BaseEntity
{
  public string AccountNumber { get; set; } = null!;
  public decimal Balance { get; set; }

  [ForeignKey("OwnerId")]
  public UserEntity owner { get; set; } = null!;

  public string OwnerId { get; set; } = null!;

}
