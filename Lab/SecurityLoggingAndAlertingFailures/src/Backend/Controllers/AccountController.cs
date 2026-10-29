using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// Case 2 - sensitive change without an audit trail. Changing an account's email is the first step of
/// many account takeovers; the vulnerable endpoint leaves no trace of who did it or what the old value
/// was, the secure endpoint writes an append-only audit record.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
  private readonly AppDbContext _context;
  private readonly ILogger<AccountController> _logger;

  public AccountController(AppDbContext context, ILogger<AccountController> logger)
  {
    _context = context;
    _logger = logger;
  }

  /// <summary>
  /// VULNERABLE: the email is overwritten and nothing is recorded. After the change there is no way to
  /// tell who changed it, from where, or what the address used to be (so it cannot be restored).
  /// </summary>
  [HttpPut]
  [Route("/lab/vulnerable/logging/accounts/email")]
  public async Task<IActionResult> ChangeEmailVulnerable([FromBody] ChangeEmailDTO request)
  {
    UserEntity target = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.targetUserId)
      ?? throw AppException.NotFound("User not found");

    target.Email = request.newEmail;
    target.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.Ok("email changed"));
  }

  /// <summary>
  /// SECURE: the change is written to the audit log together with actor, old value, new value and IP,
  /// and a warning is logged when someone changes another user's email. There is deliberately no
  /// endpoint that updates or deletes audit rows.
  /// </summary>
  [HttpPut]
  [Route("/lab/secure/logging/accounts/email")]
  public async Task<IActionResult> ChangeEmailSecure([FromBody] ChangeEmailDTO request)
  {
    if (!await _context.Users.AnyAsync(u => u.Id == request.actorUserId))
    {
      throw AppException.Unauthorized("Unknown actor");
    }

    UserEntity target = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.targetUserId)
      ?? throw AppException.NotFound("User not found");

    string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    string oldEmail = target.Email;

    target.Email = request.newEmail;
    target.UpdatedAt = DateTime.UtcNow;

    _context.AuditLogs.Add(new AuditLogEntity()
    {
      Id = Guid.NewGuid(),
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      ActorUserId = request.actorUserId,
      Action = "EMAIL_CHANGED",
      TargetUserId = target.Id,
      OldValue = oldEmail,
      NewValue = request.newEmail,
      IpAddress = ip
    });

    // The change and its audit record are saved together: no change without a record.
    await _context.SaveChangesAsync();

    if (request.actorUserId != request.targetUserId)
    {
      _logger.LogWarning("User {Actor} changed the email of user {Target} from {Ip}", request.actorUserId, target.Id, ip);
    }

    return Ok(ApiResponse.Ok("email changed"));
  }

  /// <summary>Audit history of one account, newest first.</summary>
  [HttpGet]
  [Route("/lab/secure/logging/audit/{targetUserId:guid}")]
  public async Task<IActionResult> GetAudit(Guid targetUserId)
  {
    var logs = await _context.AuditLogs
      .Where(a => a.TargetUserId == targetUserId)
      .OrderByDescending(a => a.CreatedAt)
      .ToListAsync();

    return Ok(ApiResponse.Ok(logs));
  }
}
