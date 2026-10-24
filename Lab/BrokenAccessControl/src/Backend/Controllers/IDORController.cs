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

  /// <summary>
  /// Retrieves a list of all users for the specified userId, demonstrating broken access control.
  /// </summary>
  /// <returns></returns>
  [HttpGet("GetUserAccountsByUserId/{userId:guid}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  public async Task<IActionResult> Get([FromRoute] Guid userId)
  {

    Console.WriteLine($"Retrieving accounts for userId: {userId}");

    List<BankAccountEntity> accounts = _context.BankAccounts.Where(b => b.OwnerId == userId).ToList();
    return Ok(ApiResponse.Ok(accounts));

  }


  [HttpGet("GetUserAccountsByAccountNo/{accountNo}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  public async Task<IActionResult> Get([FromRoute] string accountNo)
  {
    List<BankAccountEntity> accounts = _context.BankAccounts.Where(b => b.AccountNumber == accountNo).ToList();
    return Ok(ApiResponse.Ok(accounts));

  }

}
