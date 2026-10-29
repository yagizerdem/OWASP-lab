using Microsoft.EntityFrameworkCore;

using Models.Entity;

public class AppDbContext : DbContext
{

  public DbSet<UserEntity> Users { get; set; } = null!;
  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

}
