using Microsoft.EntityFrameworkCore;
using Models.Entity;

public class AppDbContext : DbContext
{

  public DbSet<UserEntity> Users { get; set; } = null!;

  /// <summary>Documents for the fail-open authorization case.</summary>
  public DbSet<DocumentEntity> Documents { get; set; } = null!;

  /// <summary>Wallets for the half-finished transfer case.</summary>
  public DbSet<WalletEntity> Wallets { get; set; } = null!;

  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<DocumentEntity>(e =>
    {
      e.HasIndex(d => d.OwnerId);
      e.HasOne<UserEntity>().WithMany().HasForeignKey(d => d.OwnerId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<WalletEntity>(e =>
    {
      e.Property(w => w.Balance).HasPrecision(18, 2);
      e.HasIndex(w => w.UserId).IsUnique();
      e.HasOne<UserEntity>().WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
    });
  }

}
