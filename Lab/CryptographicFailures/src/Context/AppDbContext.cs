using Microsoft.EntityFrameworkCore;
using Models.Entity;

/// <summary>Lab database context; all EF Core entities and constraints are defined here.</summary>
public class AppDbContext : DbContext
{

  /// <summary>Users whose password is stored with a weak hash (cryptographic failure demo).</summary>
  public DbSet<WeakHashEntity> WeakHashes { get; set; } = null!;

  /// <summary>EF Core options come from DI.</summary>
  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

}
