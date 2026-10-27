using System.Net;

namespace Models.Entity;

/// <summary>
/// A private note. Every note belongs to exactly one user (<see cref="OwnerId"/>).
/// The per-user limit is a business invariant, so it is enforced here and not in the controller.
/// </summary>
public class NoteEntity : BaseEntity
{
  /// <summary>Maximum number of notes allowed per user (business rule).</summary>
  public const int MaxPerUser = 5;
  public const int MaxTitleLength = 100;
  public const int MaxContentLength = 2000;

  /// <summary>Owner of the note. Only assigned through <see cref="Create"/>, from the user in the token.</summary>
  public Guid OwnerId { get; private set; }

  /// <summary>Title (at most <see cref="MaxTitleLength"/> characters).</summary>
  public string Title { get; private set; } = null!;

  /// <summary>Content (at most <see cref="MaxContentLength"/> characters).</summary>
  public string Content { get; private set; } = null!;

  /// <summary>For EF Core; create notes from outside with <see cref="Create"/>.</summary>
  private NoteEntity() { }

  /// <summary>
  /// Creates a note for <paramref name="ownerId"/>. Throws 409 when the owner already holds <see cref="MaxPerUser"/> notes.
  /// </summary>
  public static NoteEntity Create(Guid ownerId, string title, string content, int ownerCurrentNoteCount)
  {
    if (ownerCurrentNoteCount >= MaxPerUser)
      throw new AppException($"Note limit reached ({MaxPerUser} per user)", HttpStatusCode.Conflict);

    var note = new NoteEntity
    {
      Id = Guid.NewGuid(),
      OwnerId = ownerId,
    };
    note.Update(title, content);
    return note;
  }

  /// <summary>Validates and updates title/content; throws 400 when invalid.</summary>
  public void Update(string title, string content)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length > MaxTitleLength)
      throw new AppException($"Title is required and must be at most {MaxTitleLength} characters");

    if (string.IsNullOrWhiteSpace(content) || content.Length > MaxContentLength)
      throw new AppException($"Content is required and must be at most {MaxContentLength} characters");

    Title = title.Trim();
    Content = content;
    UpdatedAt = DateTime.UtcNow;
  }

  /// <summary>Returns whether the note belongs to the given user.</summary>
  public bool IsOwnedBy(Guid userId) => OwnerId == userId;
}
