using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
  private readonly AppDbContext _context;
  public UserController(AppDbContext context)
  {
    _context = context;
  }


  [HttpPost]
  [Route("/lab/secure/login")]
  public IActionResult Login([FromBody] LoginDTO request)
  {
    var user = this._context.Users.FirstOrDefault(u => u.Email == request.email);
    if (user == null)
    {
      throw AppException.NotFound("User not found");
    }

    if (user.Password != request.password)
    {
      throw AppException.Unauthorized("Invalid password");
    }

    return Ok(ApiResponse.Ok("login successful"));
  }

  [HttpPost]
  [Route("/lab/vulnerable/change-password")]
  public IActionResult ChangePassword([FromBody] ChangePasswordDTO request)
  {
    var user = this._context.Users.FirstOrDefault(u => u.Email == request.email);
    if (user == null)
    {
      throw AppException.NotFound("User not found");
    }

    if (user.SecurityAnswer != request.securityAnswer)
    {
      throw AppException.Unauthorized("Invalid security answer");
    }

    user.Password = request.newPassword;
    this._context.SaveChanges();

    return Ok(ApiResponse.Ok("password changed successfully"));
  }

}
