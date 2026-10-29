using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
  [Route("/lab/vulnerable/getBlogs")]
  public IActionResult Get()
  {
    List<BlogEntity> blogs =
     _context.Blogs.FromSqlRaw("Select * from Blogs b left join Users u on b.AuthorId = u.Id").Take(100).ToList();

    return Ok(ApiResponse.Ok(blogs));
  }
}
