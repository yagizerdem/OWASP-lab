using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>
/// INSECURE DESIGN: the reset flow is weak by design, not by a coding mistake. The OTP is handed back
/// to the caller in the API response instead of going out-of-band (email/SMS), it never expires, and
/// nothing marks a row as "used" once it resets a password, so the same OTP can be replayed forever.
/// </summary>
public class PasswordResetEntity : BaseEntity
{
  [Required]
  public Guid UserId { get; set; }

  [Required]
  [MaxLength(10)]
  public string Otp { get; set; } = null!;
}
