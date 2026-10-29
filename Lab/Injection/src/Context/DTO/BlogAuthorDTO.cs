
namespace Context.DTO;


public class BlogAuthorDTO
{
  public Guid Id { get; set; }
  public string Title { get; set; } = null!;
  public string Content { get; set; } = null!;
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public Guid AuthorId { get; set; }
  public string AuthorUsername { get; set; } = null!;
}
