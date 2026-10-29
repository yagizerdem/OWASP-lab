using Microsoft.EntityFrameworkCore;
using Models.Entity;

public class AppDbContext : DbContext
{

  public DbSet<UserEntity> Users { get; set; } = null!;

  /// <summary>Package versions offered by the internal and public registries.</summary>
  public DbSet<PackageEntity> Packages { get; set; } = null!;

  /// <summary>The project's lockfile (pinned versions and hashes).</summary>
  public DbSet<LockEntryEntity> LockEntries { get; set; } = null!;

  /// <summary>The application's SBOM (components it ships with).</summary>
  public DbSet<ComponentEntity> Components { get; set; } = null!;

  /// <summary>Known vulnerability advisories (fictional, lab-only ids).</summary>
  public DbSet<AdvisoryEntity> Advisories { get; set; } = null!;

  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<PackageEntity>(e =>
    {
      e.HasIndex(p => new { p.Name, p.Version, p.Source }).IsUnique();
    });

    modelBuilder.Entity<LockEntryEntity>(e =>
    {
      e.HasIndex(l => l.Name).IsUnique();
    });

    modelBuilder.Entity<AdvisoryEntity>(e =>
    {
      e.HasIndex(a => a.PackageName);
    });
  }

}
