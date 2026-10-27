namespace backend.Controllers;

using System.Data;
using backend.Security;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

/// <summary>
/// Record ownership example: a user can only read, write and delete their own notes.
/// <para>
/// Rules covered: (1) deny by default ([Authorize]), (3) ownership enforcement,
/// (4) per-user note limit (business rule lives in the <c>NoteEntity</c> domain model), (6) ownership violations are logged.
/// </para>
/// <para>Expectation: the owner is never taken from the request body, always from the token (no mass assignment).</para>
/// </summary>
[ApiController]
[Route("lab/secure/notes")]
[Authorize]
public class NotesController : ControllerBase
{
  private readonly AppDbContext _db;
  private readonly SecurityEventService _events;

  /// <summary>Dependencies come from DI.</summary>
  public NotesController(AppDbContext db, SecurityEventService events)
  {
    _db = db;
    _events = events;
  }

  /// <summary>
  /// Lists the caller's own notes.
  /// <para>Scope: rule 3. The query is filtered by <c>OwnerId == token.sub</c>; somebody else's note never shows up in the list.</para>
  /// <para>Expectation: 200 + only own notes (newest first); 401 without a token.</para>
  /// </summary>
  [HttpGet]
  public async Task<IActionResult> List()
  {
    Guid userId = User.GetUserId();

    var notes = await _db.Notes
      .Where(n => n.OwnerId == userId)
      .OrderByDescending(n => n.CreatedAt)
      .ToListAsync();

    return Ok(ApiResponse.Ok(notes.Select(NoteResponseDto.From)));
  }

  /// <summary>
  /// Gets a single note.
  /// <para>Scope: rules 3 and 6 (IDOR prevention + logging).</para>
  /// <para>
  /// Expectation: own note 200; a missing note and a note of another user get the exact same 404 response (ids cannot
  /// be probed); an attempt to access another user's note is logged as "OwnershipViolation".
  /// </para>
  /// </summary>
  /// <param name="id">Note id.</param>
  [HttpGet("{id:guid}")]
  public async Task<IActionResult> Get(Guid id)
  {
    NoteEntity note = await GetOwnedNoteAsync(id);
    return Ok(ApiResponse.Ok(NoteResponseDto.From(note)));
  }

  /// <summary>
  /// Creates a new note for the caller.
  /// <para>Scope: rule 3 (owner comes from the token), rule 4 (limit lives in the domain model).</para>
  /// <para>
  /// Expectation: at most <c>NoteEntity.MaxPerUser</c> (5) notes per user; the 6th request gets 409. The check and the insert
  /// run in a Serializable transaction, so parallel requests cannot exceed the limit. Extra body fields (OwnerId, Id) are ignored.
  /// Validation errors (empty/too long title or content) return 400.
  /// </para>
  /// </summary>
  /// <param name="request">Title and content.</param>
  /// <returns>201 + created note; 409 limit reached; 400 invalid data.</returns>
  [HttpPost]
  public async Task<IActionResult> Create([FromBody] NoteRequestDto request)
  {
    Guid userId = User.GetUserId();

    // Serializable so two parallel requests cannot both pass the limit check.
    await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

    int count = await _db.Notes.CountAsync(n => n.OwnerId == userId);
    NoteEntity note = NoteEntity.Create(userId, request.Title, request.Content, count);

    _db.Notes.Add(note);
    await _db.SaveChangesAsync();
    await tx.CommitAsync();

    return StatusCode(StatusCodes.Status201Created, ApiResponse.Created(NoteResponseDto.From(note)));
  }

  /// <summary>
  /// Updates the title and content of the caller's own note.
  /// <para>Scope: rules 3 and 6. Ownership is checked with <c>GetOwnedNoteAsync</c>.</para>
  /// <para>Expectation: 200 for the owner; 404 for somebody else's or a missing note (+ violation log); 400 for invalid data.</para>
  /// </summary>
  /// <param name="id">Note to update.</param>
  /// <param name="request">New title and content.</param>
  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, [FromBody] NoteRequestDto request)
  {
    NoteEntity note = await GetOwnedNoteAsync(id);
    note.Update(request.Title, request.Content);
    await _db.SaveChangesAsync();

    return Ok(ApiResponse.Ok(NoteResponseDto.From(note), "Updated"));
  }

  /// <summary>
  /// Kendi notunu siler.
  /// <para>Scope: rules 3 and 6.</para>
  /// <para>Expectation: 200 for the owner and the row is really deleted; for somebody else's note 404 + "OwnershipViolation" log.</para>
  /// </summary>
  /// <param name="id">Silinecek not.</param>
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    NoteEntity note = await GetOwnedNoteAsync(id);
    _db.Notes.Remove(note);
    await _db.SaveChangesAsync();

    return Ok(ApiResponse.Ok(null, "Deleted"));
  }

  /// <summary>
  /// Loads a note only if the caller owns it. A note of somebody else answers exactly like a missing note
  /// (404), so ids cannot be probed, but the attempt is logged.
  /// </summary>
  private async Task<NoteEntity> GetOwnedNoteAsync(Guid id)
  {
    Guid userId = User.GetUserId();

    NoteEntity? note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id);
    if (note == null)
      throw AppException.NotFound("Note not found");

    if (!note.IsOwnedBy(userId))
    {
      await _events.RecordAsync(SecurityEventTypes.OwnershipViolation, SecurityEventSeverity.Warning,
        $"Tried to access note {id} owned by another user", userId);
      throw AppException.NotFound("Note not found");
    }

    return note;
  }
}
