namespace backend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

/// <summary>
/// VULNERABLE BY DESIGN – Example 1: user enumeration.
/// <para>
/// An unknown e-mail answers 404 "No account exists…", a wrong password answers 401 "Wrong password…".
/// An attacker can tell which e-mails are registered and then focus on those accounts.
/// </para>
/// <para>Compare with <see cref="SecureEnumerationController"/>.</para>
/// </summary>
[ApiController]
[Route("[controller]")]
public class VulnerableEnumerationController : ControllerBase
{
  private readonly AppDbContext _db;

  public VulnerableEnumerationController(AppDbContext db)
  {
    _db = db;
  }

  /// <summary>Checks e-mail and password and reveals which of the two was wrong.</summary>
  [HttpPost]
  [Route("/lab/vulnerable/enumeration/login")]
  public async Task<IActionResult> Login([FromBody] LoginDto request)
  {
    UserEntity user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email)
      ?? throw AppException.NotFound("No account exists for this e-mail");

    if (user.Password != request.Password)
      throw AppException.Unauthorized("Wrong password for this account");

    return Ok(ApiResponse.Ok(new { user.Id, user.Username }, "Login successful"));
  }
}
