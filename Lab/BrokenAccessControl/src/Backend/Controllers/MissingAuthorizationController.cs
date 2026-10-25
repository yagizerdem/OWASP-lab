using backend.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("lab/[controller]")]
public class MissingAuthorizationController : ControllerBase
{

  private readonly AppDbContext _context;

  private readonly IConfiguration _configuration;

  public MissingAuthorizationController(AppDbContext context, IConfiguration configuration)
  {
    _context = context;
    _configuration = configuration;
  }

  [HttpGet("customer-risk-profiles/{id:guid}")]
  public IActionResult GetCustomerRiskProfile(Guid id)
  {
    JwtService jwtService = new JwtService(this._configuration);
    Request.Cookies.TryGetValue("token", out string token);

    // if (token != null)
    // {
    //   JwtUserInfo info = jwtService.ValidateToken(token);
    //   Console.WriteLine(info);
    // }

    var customerRiskProfile = _context.CustomerRiskProfiles.FirstOrDefault(crp => crp.Id == id);

    return Ok(ApiResponse.Ok(customerRiskProfile, "Customer risk profile retrieved successfully"));
  }

}
