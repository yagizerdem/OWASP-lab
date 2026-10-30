using Microsoft.AspNetCore.Mvc;
using Models.Entity;
namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
public class IDORController : ControllerBase
{

  private readonly AppDbContext _context;

  public IDORController(AppDbContext context)
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
  [Route("/lab/vulnerable/idor/user/{userId:guid}")]
  public async Task<IActionResult> Get([FromRoute] Guid userId)
  {
    List<BankAccountEntity> accounts = _context.BankAccounts.Where(b => b.OwnerId == userId).ToList();
    return Ok(ApiResponse.Ok(accounts));

  }


  [HttpGet("GetUserAccountsByAccountNo/{accountNo}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  [Route("/lab/vulnerable/idor/account/{accountNo}")]
  public async Task<IActionResult> Get([FromRoute] string accountNo)
  {
    List<BankAccountEntity> accounts = _context.BankAccounts.Where(b => b.AccountNumber == accountNo).ToList();
    return Ok(ApiResponse.Ok(accounts));

  }

}
