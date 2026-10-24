using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("lab/[controller]")]
public class BrokenAccessController : ControllerBase
{

  private readonly AppDbContext _context;

  public BrokenAccessController(AppDbContext context)
  {
    _context = context;
  }

  [HttpGet(Name = "GetBrokenAccess")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  public async Task<IActionResult> Get()
  {

    List<UserEntity> users = await _context.Users.FromSql($"select * from Users").ToListAsync();

    return Ok(new { message = "This is a broken access control endpoint.", users });
  }

}
