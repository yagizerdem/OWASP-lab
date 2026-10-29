using System.ComponentModel.DataAnnotations;
namespace Models.Entity;

public class BlogEntity : BaseEntity
{
  [Required]
  [MinLength(2)]
  [MaxLength(200)]
  public string Title { get; set; } = null!;

  [Required]
  public string Content { get; set; } = null!;

  [Required]
  public Guid AuthorId { get; set; }

  public UserEntity Author { get; set; } = null!;
}
