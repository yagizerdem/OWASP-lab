namespace Models.Entity;

/// <summary>
/// VULNERABLE BY DESIGN. Counterpart of <see cref="NoteEntity"/> used by the <c>lab/vulnerable/*</c> endpoints.
/// Everything the secure model protects is missing here: public setters (anyone binding a request body can set
/// <see cref="OwnerId"/> / <see cref="BaseEntity.Id"/> / timestamps), no per-user limit, no length validation,
/// no foreign key to the owner.
/// </summary>
public class LegacyNoteEntity : BaseEntity
{
  /// <summary>Owner of the note. Taken from the client in the vulnerable controller (mass assignment).</summary>
  public Guid OwnerId { get; set; }

  /// <summary>Title, unvalidated.</summary>
  public string Title { get; set; } = null!;

  /// <summary>Content, unvalidated.</summary>
  public string Content { get; set; } = null!;
}
