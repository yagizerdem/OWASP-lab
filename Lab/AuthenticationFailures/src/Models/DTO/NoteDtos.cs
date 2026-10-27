using System.ComponentModel.DataAnnotations;
using Models.Entity;

namespace Models.DTO;

/// <summary>
/// Request body for create/update. Owner and id are never accepted from the client (no mass assignment).
/// </summary>
public class NoteRequestDto
{
  [Required]
  [StringLength(NoteEntity.MaxTitleLength, MinimumLength = 1)]
  /// <summary>Note title (1-100 characters).</summary>
  public string Title { get; set; } = null!;

  [Required]
  [StringLength(NoteEntity.MaxContentLength, MinimumLength = 1)]
  /// <summary>Note content (1-2000 characters).</summary>
  public string Content { get; set; } = null!;
}

/// <summary>Note response; OwnerId is intentionally not included.</summary>
public record NoteResponseDto(Guid Id, string Title, string Content, DateTime CreatedAt, DateTime UpdatedAt)
{
  /// <summary>Builds the response DTO from the entity.</summary>
  public static NoteResponseDto From(NoteEntity n) => new(n.Id, n.Title, n.Content, n.CreatedAt, n.UpdatedAt);
}
