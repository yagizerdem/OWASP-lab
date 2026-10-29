using Microsoft.EntityFrameworkCore;
using Models.Entity;

public class AppDbContext : DbContext
{

  public DbSet<UserEntity> Users { get; set; } = null!;

  /// <summary>Back-office accounts, some still on vendor default passwords.</summary>
  public DbSet<AdminAccountEntity> AdminAccounts { get; set; } = null!;

  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<AdminAccountEntity>(e =>
    {
      e.HasIndex(a => a.Username).IsUnique();
    });
  }

}
