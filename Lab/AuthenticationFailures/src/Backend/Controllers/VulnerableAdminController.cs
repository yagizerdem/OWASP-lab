namespace backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// VULNERABLE BY DESIGN. Insecure counterpart of <see cref="SecurityEventsController"/>; do not copy.
/// <para>
/// Rules it violates: (1) an "admin" route that is anonymous (missing function level access control,
/// security by URL obscurity), (6) no audit trail, (7) no rate limit.
/// </para>
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableCors("Vulnerable")]
[DisableRateLimiting]
[Route("lab/vulnerable/admin")]
public class VulnerableAdminController : ControllerBase
{
  private readonly AppDbContext _db;

  /// <summary>Dependencies come from DI.</summary>
  public VulnerableAdminController(AppDbContext db) => _db = db;

  /// <summary>
  /// Lists all users including their passwords.
  /// <para>
  /// Weaknesses (rules 1, 6): no authentication and no role check, so the route is "protected" only by its name.
  /// The response also exposes the stored plaintext passwords (sensitive data exposure). Compare with
  /// <see cref="SecurityEventsController.List"/>, which needs the <c>AdminOnly</c> policy.
  /// </para>
  /// </summary>
  [HttpGet("users")]
  public async Task<IActionResult> Users() =>
    Ok(ApiResponse.Ok(await _db.Users.AsNoTracking().ToListAsync()));
}
