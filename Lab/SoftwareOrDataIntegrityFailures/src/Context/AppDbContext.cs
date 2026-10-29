using Microsoft.EntityFrameworkCore;
using Models.Entity;

public class AppDbContext : DbContext
{

  public DbSet<UserEntity> Users { get; set; } = null!;

  /// <summary>Orders marked paid by the payment webhook case.</summary>
  public DbSet<OrderEntity> Orders { get; set; } = null!;

  /// <summary>Trusted plugin manifest (official name/version/hash).</summary>
  public DbSet<TrustedPluginEntity> TrustedPlugins { get; set; } = null!;

  /// <summary>Plugins installed through the plugin install case.</summary>
  public DbSet<InstalledPluginEntity> InstalledPlugins { get; set; } = null!;

  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<OrderEntity>(e =>
    {
      e.Property(o => o.Amount).HasPrecision(18, 2);
      e.HasIndex(o => o.UserId);
      e.HasOne<UserEntity>().WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<TrustedPluginEntity>(e =>
    {
      e.HasIndex(p => new { p.Name, p.Version }).IsUnique();
    });
  }

}
