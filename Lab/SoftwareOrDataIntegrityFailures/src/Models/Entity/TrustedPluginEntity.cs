using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>
/// Trusted plugin manifest: the official package for a name/version and its SHA-256. The package
/// content is kept here so the lab has an "official download" to install or tamper with.
/// </summary>
public class TrustedPluginEntity : BaseEntity
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
}
