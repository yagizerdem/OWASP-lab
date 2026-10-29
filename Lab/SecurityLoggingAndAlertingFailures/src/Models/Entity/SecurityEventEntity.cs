using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>A security relevant event (login success/failure, brute force alert) kept for detection.</summary>
public class SecurityEventEntity : BaseEntity
{
  [Required]
  [MaxLength(64)]
  public string EventType { get; set; } = null!;

  [MaxLength(100)]
  public string? Username { get; set; }

  [MaxLength(64)]
  public string? IpAddress { get; set; }

  public bool Success { get; set; }

  [MaxLength(500)]
  public string? Details { get; set; }
}
