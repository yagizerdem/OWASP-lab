using Microsoft.EntityFrameworkCore;

using Models.Entity;

public class AppDbContext : DbContext
{

  public DbSet<UserEntity> Users { get; set; } = null!;

  /// <summary>INSECURE DESIGN case: OTPs that never expire and are never marked used.</summary>
  public DbSet<PasswordResetEntity> PasswordResets { get; set; } = null!;

  /// <summary>INSECURE DESIGN case: one row per bonus claim, with no "already claimed" enforcement.</summary>
  public DbSet<BonusClaimEntity> BonusClaims { get; set; } = null!;

  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<BonusClaimEntity>(e =>
    {
      e.Property(b => b.Amount).HasPrecision(18, 2);
    });
  }

}
