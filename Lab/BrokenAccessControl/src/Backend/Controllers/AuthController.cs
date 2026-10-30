using Microsoft.AspNetCore.Mvc;
namespace backend.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Models.DTO;
using Models.Entity;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{

  private readonly AppDbContext _context;

  private readonly IConfiguration _configuration;

  public AuthController(AppDbContext context, IConfiguration configuration)
  {
    _context = context;
    _configuration = configuration;
  }

  [HttpPost]
  [Route("/lab/login")]
  public IActionResult Login([FromBody] LoginDto request)
  {

    UserEntity user = _context.Users.FirstOrDefault(u => u.Email == request.Email) ??
    throw AppException.NotFound("Invalid password or email");

    if (user.Password != request.Password)
      throw AppException.Unauthorized("Invalid password or email");


    string secretKey = _configuration["JWT:secret"] ??
      throw AppException.InternalServerError("Internal server err");

    string issuer = _configuration["JWT:issuer"] ??
      throw AppException.InternalServerError("Internal server err");

    string audience = _configuration["JWT:audience"] ??
      throw AppException.InternalServerError("Internal server err");

    int expiresInMinutes = int.Parse(_configuration["JWT:expiresInMinutes"] ??
      throw AppException.InternalServerError("Internal server err"));

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


    var claims = new List<Claim>
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),      // Subject (User ID)
    new Claim(JwtRegisteredClaimNames.Email, user.Email),
    new Claim(JwtRegisteredClaimNames.Name, user.Username),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
  };


    var token = new JwtSecurityToken(
        issuer: issuer,              // Where it came from
        audience: audience,            // Who can use it
        claims: claims,
        notBefore: DateTime.UtcNow,
        expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),    // Expiration based on configuration
        signingCredentials: credentials
    );
    var handler = new JwtSecurityTokenHandler();
    string tokenString = handler.WriteToken(token);


    CookieOptions options = new CookieOptions
    {
      Expires = DateTimeOffset.UtcNow.AddDays(9),  // 9 days expiry
      HttpOnly = true,     // Avoid JavaScript access
      SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict  // CSRF protection
    };

    Response.Cookies.Append("token", tokenString, options);

    return Ok(ApiResponse.Ok(new
    {
      token = tokenString
    }, "Login successful"));
  }

}
