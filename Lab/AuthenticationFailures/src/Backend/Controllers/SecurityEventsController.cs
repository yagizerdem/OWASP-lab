namespace backend.Controllers;

using backend.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Security event log (audit trail) for administrators.
/// <para>Scope: rule 6 ("alert admins"), rules 1 and 9 (declarative role based access: <c>AdminOnly</c> policy).</para>
/// <para>Expectation: only the Admin role gets in; a normal user gets 403, an anonymous caller 401, and those attempts are logged too.</para>
/// </summary>
[ApiController]
[Route("lab/secure/security-events")]
[Authorize(Policy = LabAuth.AdminPolicy)]
public class SecurityEventsController : ControllerBase
{
  private readonly AppDbContext _db;

  /// <summary>Dependencies come from DI.</summary>
  public SecurityEventsController(AppDbContext db) => _db = db;

  /// <summary>
  /// Lists events from newest to oldest.
  /// <para>
  /// Expectation: <c>severity=Alert</c> shows only repeated-failure alerts and refresh token reuse;
  /// <c>type</c> filters by event type; <c>take</c> is clamped to 1-500 (default 100).
  /// </para>
  /// </summary>
  /// <param name="severity">Info | Warning | Alert.</param>
  /// <param name="type">E.g. LoginFailed, OwnershipViolation, RepeatedFailures.</param>
  /// <param name="take">Maximum number of rows to return.</param>
  [HttpGet]
  public async Task<IActionResult> List([FromQuery] string? severity, [FromQuery] string? type, [FromQuery] int take = 100)
  {
    take = Math.Clamp(take, 1, 500);

    var query = _db.SecurityEvents.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(severity)) query = query.Where(e => e.Severity == severity);
    if (!string.IsNullOrWhiteSpace(type)) query = query.Where(e => e.Type == type);

    var events = await query.OrderByDescending(e => e.CreatedAt).Take(take).ToListAsync();
    return Ok(ApiResponse.Ok(events));
  }
}
