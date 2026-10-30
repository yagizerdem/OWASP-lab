namespace backend.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

/// <summary>
/// VULNERABLE BY DESIGN – Example 2: brute force.
/// <para>
/// Failed logins are not counted, so an attacker can try passwords forever. After 1000 wrong guesses the right
/// password still works.
/// </para>
/// <para>Compare with <see cref="SecureBruteForceController"/>.</para>
/// </summary>
[ApiController]
[Route("[controller]")]
public class VulnerableBruteForceController : ControllerBase
{
  private readonly AppDbContext _db;

  public VulnerableBruteForceController(AppDbContext db)
  {
    _db = db;
  }

  /// <summary>Checks e-mail and password with no attempt limit.</summary>
  [HttpPost]
  [Route("/lab/vulnerable/bruteforce/login")]
  public async Task<IActionResult> Login([FromBody] LoginDto request)
  {
    UserEntity? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null || user.Password != request.Password)
      throw AppException.Unauthorized("Invalid password or email");

    return Ok(ApiResponse.Ok(new { user.Id, user.Username }, "Login successful"));
  }
}
