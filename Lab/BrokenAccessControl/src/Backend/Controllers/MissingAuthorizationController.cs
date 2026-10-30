using backend.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class MissingAuthorizationController : ControllerBase
{

  private readonly AppDbContext _context;

  private readonly IConfiguration _configuration;

  public MissingAuthorizationController(AppDbContext context, IConfiguration configuration)
  {
    _context = context;
    _configuration = configuration;
  }

  [HttpGet]
  [Route("/lab/vulnerable/missing-authorization/customer-risk-profiles/{id:guid}")]
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

  /// <summary>
  /// Lists every customer risk profile. No authentication or role check.
  /// </summary>
  [HttpGet]
  [Route("/lab/vulnerable/missing-authorization/customer-risk-profiles")]
  public IActionResult GetAllCustomerRiskProfiles()
  {
    var customerRiskProfiles = _context.CustomerRiskProfiles.ToList();

    return Ok(ApiResponse.Ok(customerRiskProfiles, "Customer risk profiles retrieved successfully"));
  }

  /// <summary>
  /// Lists only the flagged customers. No authentication or role check.
  /// </summary>
  [HttpGet]
  [Route("/lab/vulnerable/missing-authorization/customer-risk-profiles/flagged")]
  public IActionResult GetFlaggedCustomerRiskProfiles()
  {
    var flaggedProfiles = _context.CustomerRiskProfiles.Where(crp => crp.IsFlagged).ToList();

    return Ok(ApiResponse.Ok(flaggedProfiles, "Flagged customer risk profiles retrieved successfully"));
  }

  /// <summary>
  /// Changes the flag of a customer risk profile. No authentication or role check.
  /// </summary>
  [HttpPut]
  [Route("/lab/vulnerable/missing-authorization/customer-risk-profiles/{id:guid}/flag")]
  public IActionResult SetCustomerRiskProfileFlag(Guid id, [FromQuery] bool isFlagged)
  {
    var customerRiskProfile = _context.CustomerRiskProfiles.FirstOrDefault(crp => crp.Id == id) ??
      throw AppException.NotFound("Customer risk profile not found");

    customerRiskProfile.IsFlagged = isFlagged;
    customerRiskProfile.UpdatedAt = DateTime.UtcNow;
    _context.SaveChanges();

    return Ok(ApiResponse.Ok(customerRiskProfile, "Customer risk profile flag updated successfully"));
  }

  /// <summary>
  /// Deletes a customer risk profile. No authentication or role check.
  /// </summary>
  [HttpDelete]
  [Route("/lab/vulnerable/missing-authorization/customer-risk-profiles/{id:guid}")]
  public IActionResult DeleteCustomerRiskProfile(Guid id)
  {
    var customerRiskProfile = _context.CustomerRiskProfiles.FirstOrDefault(crp => crp.Id == id) ??
      throw AppException.NotFound("Customer risk profile not found");

    _context.CustomerRiskProfiles.Remove(customerRiskProfile);
    _context.SaveChanges();

    return Ok(ApiResponse.Ok(null, "Customer risk profile deleted successfully"));
  }

  /// <summary>
  /// Lists every user, including the passwords. No authentication or role check.
  /// </summary>
  [HttpGet]
  [Route("/lab/vulnerable/missing-authorization/users")]
  public IActionResult GetAllUsers()
  {
    var users = _context.Users.ToList();

    return Ok(ApiResponse.Ok(users, "Users retrieved successfully"));
  }

  /// <summary>
  /// Overwrites the balance of any bank account. No authentication or role check.
  /// </summary>
  [HttpPut]
  [Route("/lab/vulnerable/missing-authorization/bank-accounts/{accountNo}/balance")]
  public IActionResult SetBankAccountBalance(string accountNo, [FromQuery] decimal balance)
  {
    var bankAccount = _context.BankAccounts.FirstOrDefault(b => b.AccountNumber == accountNo) ??
      throw AppException.NotFound("Bank account not found");

    bankAccount.Balance = balance;
    bankAccount.UpdatedAt = DateTime.UtcNow;
    _context.SaveChanges();

    return Ok(ApiResponse.Ok(bankAccount, "Bank account balance updated successfully"));
  }

}
