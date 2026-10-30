using Microsoft.EntityFrameworkCore;
using Models.Entity;

/// <summary>Lab database context; all EF Core entities and constraints are defined here.</summary>
public class AppDbContext : DbContext
{

  /// <summary>Users whose password is stored with a weak hash (cryptographic failure demo).</summary>
  public DbSet<WeakHashEntity> WeakHashes { get; set; } = null!;

  /// <summary>Users whose password is stored with PBKDF2 and a per-user salt (compliant counterpart).</summary>
  public DbSet<SecureCredentialEntity> SecureCredentials { get; set; } = null!;

  /// <summary>Password reset tokens (weak plaintext codes and secure hashed tokens).</summary>
  public DbSet<ResetTokenEntity> ResetTokens { get; set; } = null!;

  /// <summary>Payment cards stored in plaintext (vulnerable).</summary>
  public DbSet<LegacyCardEntity> LegacyCards { get; set; } = null!;

  /// <summary>Payment cards stored with AES-GCM field encryption (compliant).</summary>
  public DbSet<ProtectedCardEntity> ProtectedCards { get; set; } = null!;

  /// <summary>EF Core options come from DI.</summary>
  public AppDbContext(DbContextOptions<AppDbContext> options)
       : base(options)
  {

  }

  /// <summary>Length limits and unique indexes of the cryptographic failures lab tables.</summary>
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<SecureCredentialEntity>(e =>
    {
      e.Property(c => c.UserName).HasMaxLength(128).IsRequired();
      e.Property(c => c.PasswordHash).HasMaxLength(128).IsRequired();
      e.Property(c => c.Salt).HasMaxLength(64).IsRequired();
      e.Property(c => c.Algorithm).HasMaxLength(32).IsRequired();
      e.HasIndex(c => c.UserName).IsUnique();
    });

    modelBuilder.Entity<ResetTokenEntity>(e =>
    {
      e.Property(t => t.UserName).HasMaxLength(128).IsRequired();
      e.Property(t => t.TokenValue).HasMaxLength(128).IsRequired();
      e.HasIndex(t => new { t.UserName, t.TokenValue });
    });

    modelBuilder.Entity<LegacyCardEntity>(e =>
    {
      e.Property(c => c.OwnerName).HasMaxLength(128).IsRequired();
      e.Property(c => c.CardNumber).HasMaxLength(32).IsRequired();
      e.HasIndex(c => c.OwnerName);
    });

    modelBuilder.Entity<ProtectedCardEntity>(e =>
    {
      e.Property(c => c.OwnerName).HasMaxLength(128).IsRequired();
      e.Property(c => c.Last4).HasMaxLength(4).IsRequired();
      e.Property(c => c.Nonce).HasMaxLength(32).IsRequired();
      e.Property(c => c.CipherText).HasMaxLength(256).IsRequired();
      e.HasIndex(c => c.OwnerName);
    });
  }

}
