using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entity;

/// <summary>
/// Internal risk assessment of a customer. Should only be visible to administrators.
/// </summary>
public class CustomerRiskProfileEntity : BaseEntity
{
  [ForeignKey("UserId")]
  public UserEntity User { get; set; } = null!;

  public Guid UserId { get; set; }

  /// <summary>
  /// Risk score of the customer (0-100).
  /// </summary>
  public int RiskScore { get; set; }

  /// <summary>
  /// KYC verification status, e.g. Pending, Verified, Rejected.
  /// </summary>
  [Required]
  public string KycStatus { get; set; } = "Pending";

  /// <summary>
  /// Internal note written by bank staff.
  /// </summary>
  [Required]
  public string InternalNote { get; set; } = string.Empty;

  /// <summary>
  /// Whether the customer is flagged for review.
  /// </summary>
  public bool IsFlagged { get; set; }
}
