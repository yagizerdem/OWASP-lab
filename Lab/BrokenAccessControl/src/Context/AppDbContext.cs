using Microsoft.EntityFrameworkCore;
using Models.Entity;

public class AppDbContext : DbContext
{

  public DbSet<UserEntity> Users { get; set; } = null!;
  public DbSet<BankAccountEntity> BankAccounts { get; set; } = null!;
  public DbSet<CustomerRiskProfileEntity> CustomerRiskProfiles { get; set; } = null!;

  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<BankAccountEntity>(entity =>
    {
      entity.Property(b => b.AccountNumber).HasMaxLength(32);
      entity.HasIndex(b => b.AccountNumber).IsUnique();
    });
  }

}
