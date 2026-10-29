using Microsoft.EntityFrameworkCore;
using Models.Entity;

public class AppDbContext : DbContext
{

  public DbSet<UserEntity> Users { get; set; } = null!;

  /// <summary>Login successes/failures and brute force alerts (secure login case).</summary>
  public DbSet<SecurityEventEntity> SecurityEvents { get; set; } = null!;

  /// <summary>Append-only audit trail of account changes (secure email change case).</summary>
  public DbSet<AuditLogEntity> AuditLogs { get; set; } = null!;

  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<SecurityEventEntity>(e =>
    {
      e.HasIndex(s => new { s.EventType, s.CreatedAt });
    });

    modelBuilder.Entity<AuditLogEntity>(e =>
    {
      e.HasIndex(a => a.TargetUserId);
    });
  }

}
