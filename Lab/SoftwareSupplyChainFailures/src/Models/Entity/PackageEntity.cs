using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>A package version available from a registry: "internal" (company feed) or "public".</summary>
public class PackageEntity : BaseEntity
{
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = null!;

  [Required]
  [MaxLength(32)]
  public string Version { get; set; } = null!;

  [Required]
  [MaxLength(16)]
  public string Source { get; set; } = null!;

  [Required]
  [MaxLength(64)]
  public string Sha256 { get; set; } = null!;
}
