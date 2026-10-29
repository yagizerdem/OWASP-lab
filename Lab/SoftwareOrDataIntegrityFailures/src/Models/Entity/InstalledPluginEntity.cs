using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>A plugin package that has been installed, with the hash of what was actually installed.</summary>
public class InstalledPluginEntity : BaseEntity
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

  [Required]
  public string ContentBase64 { get; set; } = null!;

  /// <summary>True when the package was checked against the trusted manifest before installing.</summary>
  public bool Verified { get; set; }
}
