using Microsoft.EntityFrameworkCore;
using Models.Entity;
public class AppDbContext : DbContext
{


  public DbSet<UserEntity> Users { get; set; } = null!;
  public DbSet<BlogEntity> Blogs { get; set; } = null!;

  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

  /// <summary>The blog's Author navigation uses the existing UserId column as its foreign key.</summary>
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<BlogEntity>(e =>
    {
      e.HasOne(b => b.Author).WithMany().HasForeignKey(b => b.AuthorId).OnDelete(DeleteBehavior.Cascade);
    });
  }

}
