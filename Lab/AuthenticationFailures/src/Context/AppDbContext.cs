using Microsoft.EntityFrameworkCore;
using Models.Entity;

/// <summary>Lab database context; all EF Core entities and constraints are defined here.</summary>
public class AppDbContext : DbContext
{

  /// <summary>Users (existing model, unchanged).</summary>
  public DbSet<UserEntity> Users { get; set; } = null!;

  /// <summary>Notes that belong to a user (for rules 3 and 4).</summary>
  public DbSet<NoteEntity> Notes { get; set; } = null!;

  /// <summary>User-role assignments (Admin/User).</summary>
  public DbSet<UserRoleEntity> UserRoles { get; set; } = null!;

  /// <summary>Unprotected notes used by the vulnerable endpoints (no ownership FK, no limits).</summary>
  public DbSet<LegacyNoteEntity> LegacyNotes { get; set; } = null!;

  /// <summary>Refresh token records (hash only).</summary>
  public DbSet<RefreshTokenEntity> RefreshTokens { get; set; } = null!;

  /// <summary>Deny-list of access tokens (jti) revoked by logout.</summary>
  public DbSet<RevokedTokenEntity> RevokedTokens { get; set; } = null!;

  /// <summary>Authentication / access control event log (rule 6).</summary>
  public DbSet<SecurityEventEntity> SecurityEvents { get; set; } = null!;

  /// <summary>EF Core options come from DI.</summary>
  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

  /// <summary>
  /// Length limits, unique indexes and foreign keys to Users (a deleted user's data is deleted too).
  /// The Users table itself is not touched.
  /// </summary>
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<NoteEntity>(e =>
    {
      e.Property(n => n.Title).HasMaxLength(NoteEntity.MaxTitleLength).IsRequired();
      e.Property(n => n.Content).HasMaxLength(NoteEntity.MaxContentLength).IsRequired();
      e.HasIndex(n => n.OwnerId);
      e.HasOne<UserEntity>().WithMany().HasForeignKey(n => n.OwnerId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<UserRoleEntity>(e =>
    {
      e.Property(r => r.Role).HasMaxLength(32).IsRequired();
      e.HasIndex(r => new { r.UserId, r.Role }).IsUnique();
      e.HasOne<UserEntity>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<RefreshTokenEntity>(e =>
    {
      e.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
      e.Property(t => t.ReplacedByTokenHash).HasMaxLength(64);
      e.Property(t => t.CreatedByIp).HasMaxLength(64);
      e.HasIndex(t => t.TokenHash).IsUnique();
      e.HasIndex(t => t.UserId);
      e.HasOne<UserEntity>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<RevokedTokenEntity>(e =>
    {
      e.Property(t => t.Jti).HasMaxLength(64).IsRequired();
      e.HasIndex(t => t.Jti).IsUnique();
      e.HasIndex(t => t.ExpiresAt);
    });

    modelBuilder.Entity<SecurityEventEntity>(e =>
    {
      e.Property(s => s.Type).HasMaxLength(48).IsRequired();
      e.Property(s => s.Severity).HasMaxLength(16).IsRequired();
      e.Property(s => s.Subject).HasMaxLength(256);
      e.Property(s => s.IpAddress).HasMaxLength(64);
      e.Property(s => s.Method).HasMaxLength(16);
      e.Property(s => s.Path).HasMaxLength(512);
      e.Property(s => s.Detail).HasMaxLength(1024);
      e.HasIndex(s => new { s.IpAddress, s.CreatedAt });
      e.HasIndex(s => new { s.Subject, s.Type, s.CreatedAt });
    });
  }

}
