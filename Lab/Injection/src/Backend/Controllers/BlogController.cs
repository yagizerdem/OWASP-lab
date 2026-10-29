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

  /// <summary>
  /// VULNERABLE (UNION based): the search term is concatenated into the LIKE clause, so a UNION SELECT can
  /// return rows from another table (e.g. Users) in the shape of BlogAuthorDTO.
  /// </summary>
  [HttpGet]
  [Route("/lab/vulnerable/searchBlogs")]
  public IActionResult Search([FromQuery] string title)
  {
    // ORDER BY uses an unqualified column so the query is still valid after a UNION.
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
    where b.Title LIKE '%{title}%'
    ORDER BY CreatedAt
    OFFSET 0 ROWS
    FETCH NEXT 100 ROWS ONLY
    """;

    var data =
     _context.Database.SqlQueryRaw<BlogAuthorDTO>(query).ToList();

    return Ok(ApiResponse.Ok(data));
  }

  /// <summary>
  /// VULNERABLE (ORDER BY injection): the sort column is concatenated into ORDER BY. Column names cannot be
  /// parameterized, so a whitelist is the only fix. A subquery in the sort value can leak data through the
  /// row order or through a conversion error.
  /// </summary>
  [HttpGet]
  [Route("/lab/vulnerable/getBlogsSorted")]
  public IActionResult GetSorted([FromQuery] string sort = "CreatedAt")
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
    ORDER BY {sort}
    OFFSET 0 ROWS
    FETCH NEXT 100 ROWS ONLY
    """;

    var data =
     _context.Database.SqlQueryRaw<BlogAuthorDTO>(query).ToList();

    return Ok(ApiResponse.Ok(data));
  }
}
