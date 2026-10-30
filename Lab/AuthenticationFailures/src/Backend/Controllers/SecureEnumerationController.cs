namespace backend.Controllers;

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

/// <summary>
/// SECURE – Example 1: no user enumeration.
/// <para>
/// Unknown e-mail and wrong password give the same 401 with the same message. The password is compared in
/// constant time, and an unknown e-mail is compared against a dummy value, so both paths also take about the same time.
/// </para>
/// </summary>
[ApiController]
[Route("[controller]")]
public class SecureEnumerationController : ControllerBase
{
  // Compared against when the e-mail is unknown so both failure paths cost the same.
  private const string DummyPassword = "not-a-real-password";

  private readonly AppDbContext _db;

  public SecureEnumerationController(AppDbContext db)
  {
    _db = db;
  }

  /// <summary>Checks e-mail and password; every failure looks identical.</summary>
  [HttpPost]
  [Route("/lab/secure/enumeration/login")]
  public async Task<IActionResult> Login([FromBody] LoginDto request)
  {
    UserEntity? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

    bool valid = FixedTimeEquals(user?.Password ?? DummyPassword, request.Password) && user != null;
    if (!valid)
      throw AppException.Unauthorized("Invalid password or email");

    return Ok(ApiResponse.Ok(new { user!.Id, user.Username }, "Login successful"));
  }

  /// <summary>Hashes both strings and compares them in constant time (no timing or length leak).</summary>
  private static bool FixedTimeEquals(string a, string b) =>
    CryptographicOperations.FixedTimeEquals(
      SHA256.HashData(Encoding.UTF8.GetBytes(a)),
      SHA256.HashData(Encoding.UTF8.GetBytes(b)));
}
