using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>
/// A (fictional, lab-only) security advisory: every version of PackageName below FixedVersion is affected.
/// </summary>
public class AdvisoryEntity : BaseEntity
{
  [Required]
  [MaxLength(32)]
  public string AdvisoryId { get; set; } = null!;

  [Required]
  [MaxLength(100)]
  public string PackageName { get; set; } = null!;

  [Required]
  [MaxLength(32)]
  public string FixedVersion { get; set; } = null!;

  [Required]
  [MaxLength(16)]
  public string Severity { get; set; } = null!;

  [MaxLength(300)]
  public string? Summary { get; set; }
}
