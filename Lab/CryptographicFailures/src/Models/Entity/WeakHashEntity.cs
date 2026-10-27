using Models.Entity;

public class WeakHashEntity : BaseEntity
{
  public string UserName { get; set; } = null!;

  public string HashedPassword { get; set; } = null!;

  public string PlaintextPassword { get; set; } = null!;

}
