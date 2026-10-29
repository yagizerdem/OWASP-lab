using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>One line of the lockfile: the exact version and hash the project has reviewed and pinned.</summary>
public class LockEntryEntity : BaseEntity
{
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = null!;

  [Required]
  [MaxLength(32)]
  public string Version { get; set; } = null!;

  [Required]
  [MaxLength(64)]
  public string Sha256 { get; set; } = null!;
}
