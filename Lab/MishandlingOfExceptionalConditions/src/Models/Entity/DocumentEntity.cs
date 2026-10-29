using System.ComponentModel.DataAnnotations;

namespace Models.Entity;

/// <summary>A document owned by a user. Private documents may only be read by their owner.</summary>
public class DocumentEntity : BaseEntity
{
  [Required]
  public Guid OwnerId { get; set; }

  [Required]
  [MinLength(2)]
  [MaxLength(200)]
  public string Title { get; set; } = null!;

  [Required]
  public string Content { get; set; } = null!;

  public bool IsPrivate { get; set; }
}
