using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>Append-only record of who changed what on which account, with the old and new values.</summary>
public class AuditLogEntity : BaseEntity
{
  [Required]
  public Guid ActorUserId { get; set; }

  [Required]
  [MaxLength(64)]
  public string Action { get; set; } = null!;

  [Required]
  public Guid TargetUserId { get; set; }

  [MaxLength(256)]
  public string? OldValue { get; set; }

  [MaxLength(256)]
  public string? NewValue { get; set; }

  [MaxLength(64)]
  public string? IpAddress { get; set; }
}
