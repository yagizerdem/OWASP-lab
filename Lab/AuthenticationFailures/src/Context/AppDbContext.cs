using Microsoft.EntityFrameworkCore;
using Models.Entity;

/// <summary>Lab database context; the lab only needs users.</summary>
public class AppDbContext : DbContext
{

  /// <summary>Users that log in through the lab endpoints.</summary>
  public DbSet<UserEntity> Users { get; set; } = null!;

  /// <summary>EF Core options come from DI.</summary>
  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

}
