using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>A third-party component the application ships with (one SBOM entry).</summary>
public class ComponentEntity : BaseEntity
{
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = null!;

  [Required]
  [MaxLength(32)]
  public string Version { get; set; } = null!;
}
