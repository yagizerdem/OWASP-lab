using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// Case 2 - default credentials. Back-office accounts are installed with well-known vendor passwords
/// (admin/admin, support/support123). The vulnerable login lets them in; the secure login refuses an
/// account until its default password has been changed.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AdminController : ControllerBase
{
  private static readonly string[] KnownDefaultPasswords = { "admin", "password", "123456", "support123", "changeme" };

  private readonly AppDbContext _context;

  public AdminController(AppDbContext context)
  {
    _context = context;
  }

  /// <summary>
  /// VULNERABLE: accounts still on their factory password log in normally and get their full role,
  /// so admin/admin is enough to get into the back office.
  /// </summary>
  [HttpPost]
  [Route("/lab/vulnerable/misconfig/admin/login")]
  public async Task<IActionResult> LoginVulnerable([FromBody] AdminLoginDTO request)
  {
    AdminAccountEntity? account = await _context.AdminAccounts.FirstOrDefaultAsync(a => a.Username == request.username);
    if (account == null || account.Password != request.password)
    {
      throw AppException.Unauthorized("Invalid credentials");
    }

    return Ok(ApiResponse.Ok(new { account.Username, account.Role }, "login successful"));
  }

  /// <summary>
  /// SECURE: an account still flagged IsDefaultPassword cannot log in; it has to go through
  /// change-password first.
  /// </summary>
  [HttpPost]
  [Route("/lab/secure/misconfig/admin/login")]
  public async Task<IActionResult> LoginSecure([FromBody] AdminLoginDTO request)
  {
    AdminAccountEntity? account = await _context.AdminAccounts.FirstOrDefaultAsync(a => a.Username == request.username);
    if (account == null || account.Password != request.password)
    {
      throw AppException.Unauthorized("Invalid credentials");
    }

    if (account.IsDefaultPassword)
    {
      throw AppException.Forbidden("The default password must be changed before this account can be used");
    }

    return Ok(ApiResponse.Ok(new { account.Username, account.Role }, "login successful"));
  }

  /// <summary>
  /// SECURE: replaces a default password. The new password must be at least 12 characters, differ from
  /// the current one and not be a known default.
  /// </summary>
  [HttpPost]
  [Route("/lab/secure/misconfig/admin/change-password")]
  public async Task<IActionResult> ChangePassword([FromBody] ChangeAdminPasswordDTO request)
  {
    AdminAccountEntity? account = await _context.AdminAccounts.FirstOrDefaultAsync(a => a.Username == request.username);
    if (account == null || account.Password != request.currentPassword)
    {
      throw AppException.Unauthorized("Invalid credentials");
    }

    if (request.newPassword is not { Length: >= 12 }
        || request.newPassword == request.currentPassword
        || KnownDefaultPasswords.Contains(request.newPassword, StringComparer.OrdinalIgnoreCase))
    {
      throw new AppException("New password must be at least 12 characters, new, and not a known default");
    }

    account.Password = request.newPassword;
    account.IsDefaultPassword = false;
    account.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.Ok("password changed"));
  }
}
