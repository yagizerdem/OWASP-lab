using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Context.DTO;
using Models.Entity;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]

public class BlogController : ControllerBase
{
  private readonly AppDbContext _context;

  public BlogController(AppDbContext context)
  {
    _context = context;
  }


  [HttpGet]
  [Route("/lab/vulnerable/getUserBlogs")]
  public IActionResult Get([FromQuery] string userName, [FromQuery] string password)
  {

    string query = $"""
    SELECT 
    b.Id,
    b.Title,
    b.Content,
    b.CreatedAt,
    b.UpdatedAt,
    u.Id AS AuthorId,
    u.Username AS AuthorUsername
    FROM Blogs b
    LEFT JOIN Users u ON b.AuthorId = u.Id
    where u.Username = '{userName}' AND u.Password = '{password}'
    ORDER BY b.CreatedAt
    OFFSET 0 ROWS
    FETCH NEXT 100 ROWS ONLY
    """;

    var data =
     _context.Database.SqlQueryRaw<BlogAuthorDTO>(query).ToList();


    return Ok(ApiResponse.Ok(data));
  }
}
