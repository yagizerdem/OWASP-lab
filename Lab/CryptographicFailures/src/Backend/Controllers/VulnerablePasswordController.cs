using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class VulnerablePasswordController : ControllerBase
{

  private readonly AppDbContext _context;
  public VulnerablePasswordController(AppDbContext context)
  {
    _context = context;
  }

  [HttpPost]

  public IActionResult BruteForcePassword([FromBody] BruteForcePasswordDTO dto)
  {
    var user = _context.WeakHashes.FirstOrDefault(u => u.UserName == dto.UserName);
    if (user == null)
    {
      throw AppException.NotFound("User not found");
    }


    string hash = MD5.HashData(System.Text.Encoding.UTF8.GetBytes(dto.Password)).ToString() ??
      throw new AppException("Failed to hash password");

    if (hash != user.HashedPassword)
    {
      throw AppException.Unauthorized("Invalid password");
    }

    return Ok(ApiResponse.Ok(user, "Password verified successfully"));
  }

}
