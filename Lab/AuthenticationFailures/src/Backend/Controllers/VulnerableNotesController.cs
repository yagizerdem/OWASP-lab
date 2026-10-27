namespace backend.Controllers;

using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Models.Entity;

/// <summary>
/// VULNERABLE BY DESIGN. Insecure counterpart of <see cref="NotesController"/>; do not copy.
/// <para>
/// Rules it violates: (1) reads are anonymous, (2) copy-pasted login check and wide-open CORS,
/// (3) no record ownership (IDOR / BOLA, mass assignment), (4) the per-user limit does not exist,
/// (6) no access control failure is ever logged, (7) no rate limit.
/// </para>
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableCors("Vulnerable")]
[DisableRateLimiting]
[Route("lab/vulnerable/notes")]
public class VulnerableNotesController : ControllerBase
{
  private readonly AppDbContext _db;
  private readonly JwtService _jwt;

  /// <summary>Dependencies come from DI.</summary>
  public VulnerableNotesController(AppDbContext db, JwtService jwt)
  {
    _db = db;
    _jwt = jwt;
  }

  /// <summary>
  /// Lists notes.
  /// <para>
  /// Weaknesses (rules 1, 3): no authentication and no owner filter, so every note of every user is returned to
  /// anybody. Compare with <see cref="NotesController.List"/>.
  /// </para>
  /// </summary>
  [HttpGet]
  public async Task<IActionResult> List() =>
    Ok(ApiResponse.Ok(await _db.LegacyNotes.AsNoTracking().ToListAsync()));

  /// <summary>
  /// Lists the notes of the user whose id is passed in the query string.
  /// <para>
  /// Weakness (rule 3): the owner comes from the client, not from the token (BOLA). Change <c>ownerId</c> to read
  /// anybody's notes.
  /// </para>
  /// </summary>
  /// <param name="ownerId">User id chosen by the caller.</param>
  [HttpGet("by-owner")]
  public async Task<IActionResult> ByOwner([FromQuery] Guid ownerId) =>
    Ok(ApiResponse.Ok(await _db.LegacyNotes.AsNoTracking().Where(n => n.OwnerId == ownerId).ToListAsync()));

  /// <summary>
  /// Gets one note by id.
  /// <para>
  /// Weaknesses (rules 1, 3, 6): no authentication, no ownership check (IDOR), and a missing note answers 404 while an
  /// existing one answers 200, so ids can be enumerated. Nothing is logged.
  /// </para>
  /// </summary>
  /// <param name="id">Note id.</param>
  [HttpGet("{id:guid}")]
  public async Task<IActionResult> Get(Guid id)
  {
    LegacyNoteEntity note = await _db.LegacyNotes.FirstOrDefaultAsync(n => n.Id == id)
      ?? throw AppException.NotFound("Note not found");
    return Ok(ApiResponse.Ok(note));
  }

  /// <summary>
  /// Creates a note.
  /// <para>
  /// Weaknesses (rules 1, 3, 4): no authentication; the request body is bound straight to the entity (mass
  /// assignment), so the client chooses <c>OwnerId</c>, <c>Id</c> and timestamps and can create notes for other
  /// users; there is no per-user limit, so a client can create unlimited rows. Compare with
  /// <see cref="NotesController.Create"/> and <see cref="NoteEntity.Create"/>.
  /// </para>
  /// </summary>
  /// <param name="note">Entity bound directly from the body (insecure on purpose).</param>
  [HttpPost]
  public async Task<IActionResult> Create([FromBody] LegacyNoteEntity note)
  {
    _db.LegacyNotes.Add(note);
    await _db.SaveChangesAsync();
    return StatusCode(StatusCodes.Status201Created, ApiResponse.Created(note));
  }

  /// <summary>
  /// Replaces a note.
  /// <para>
  /// Weaknesses (rules 2, 3): the "is logged in" check is copy-pasted instead of using the shared policy, and it only
  /// checks that <em>some</em> valid token exists, not that the caller owns the note. The whole entity is overwritten
  /// from the body, including <c>OwnerId</c>, so a note can also be stolen.
  /// </para>
  /// </summary>
  /// <param name="id">Note id.</param>
  /// <param name="update">Replacement values (insecure on purpose).</param>
  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, [FromBody] LegacyNoteEntity update)
  {
    RequireAnyLogin();

    LegacyNoteEntity note = await _db.LegacyNotes.FirstOrDefaultAsync(n => n.Id == id)
      ?? throw AppException.NotFound("Note not found");

    note.OwnerId = update.OwnerId;
    note.Title = update.Title;
    note.Content = update.Content;
    note.UpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();

    return Ok(ApiResponse.Ok(note, "Updated"));
  }

  /// <summary>
  /// Deletes a note.
  /// <para>
  /// Weakness (rules 2, 3): any logged-in user can delete any note (same copy-pasted, ownership-blind check).
  /// </para>
  /// </summary>
  /// <param name="id">Note id.</param>
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    RequireAnyLogin();

    LegacyNoteEntity note = await _db.LegacyNotes.FirstOrDefaultAsync(n => n.Id == id)
      ?? throw AppException.NotFound("Note not found");

    _db.LegacyNotes.Remove(note);
    await _db.SaveChangesAsync();
    return Ok(ApiResponse.Ok(null, "Deleted"));
  }

  /// <summary>
  /// Hand written authentication check, duplicated per controller instead of being enforced once (rule 2).
  /// It only proves that the caller holds some valid token; it never says whose resource may be touched.
  /// </summary>
  private void RequireAnyLogin()
  {
    string header = Request.Headers.Authorization.ToString();
    if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
      throw AppException.Unauthorized("Login required");

    _jwt.ValidateToken(header["Bearer ".Length..]);
  }
}
