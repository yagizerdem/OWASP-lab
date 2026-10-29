using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// Case 2 - half-finished operation after an exception. The vulnerable transfer commits the debit before
/// the step that can fail, so an exception leaves the money gone from the sender and never credited.
/// </summary>
[ApiController]
[Route("[controller]")]
public class WalletController : ControllerBase
{
  private readonly AppDbContext _context;

  public WalletController(AppDbContext context)
  {
    _context = context;
  }

  /// <summary>
  /// VULNERABLE (no rollback): the debit is saved on its own, then the receiver lookup throws when
  /// toUserId has no wallet. The client gets a 500, the sender's balance is already reduced, and the
  /// receiver got nothing.
  /// </summary>
  [HttpPost]
  [Route("/lab/vulnerable/exceptions/wallets/transfer")]
  public async Task<IActionResult> TransferVulnerable([FromBody] TransferDTO request)
  {
    if (request.amount <= 0)
    {
      throw new AppException("Amount must be positive");
    }

    WalletEntity from = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.fromUserId)
      ?? throw AppException.NotFound("Sender wallet not found");

    if (from.Balance < request.amount)
    {
      throw new AppException("Insufficient balance");
    }

    from.Balance -= request.amount;
    from.UpdatedAt = DateTime.UtcNow;
    // VULNERABLE: the debit is committed before the rest of the operation has succeeded.
    await _context.SaveChangesAsync();

    // Throws InvalidOperationException when the receiver has no wallet; nothing undoes the debit above.
    WalletEntity to = await _context.Wallets.FirstAsync(w => w.UserId == request.toUserId);
    to.Balance += request.amount;
    to.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.Ok(new { fromBalance = from.Balance, toBalance = to.Balance }, "Transfer completed"));
  }

  /// <summary>
  /// SECURE (all or nothing): everything that can fail is checked before any change, and both balance
  /// updates are committed in one transaction, so an error leaves both wallets untouched.
  /// </summary>
  [HttpPost]
  [Route("/lab/secure/exceptions/wallets/transfer")]
  public async Task<IActionResult> TransferSecure([FromBody] TransferDTO request)
  {
    if (request.amount <= 0)
    {
      throw new AppException("Amount must be positive");
    }

    if (request.fromUserId == request.toUserId)
    {
      throw new AppException("Sender and receiver must be different");
    }

    WalletEntity from = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.fromUserId)
      ?? throw AppException.NotFound("Sender wallet not found");
    WalletEntity to = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.toUserId)
      ?? throw AppException.NotFound("Receiver wallet not found");

    if (from.Balance < request.amount)
    {
      throw new AppException("Insufficient balance");
    }

    await using var transaction = await _context.Database.BeginTransactionAsync();

    from.Balance -= request.amount;
    to.Balance += request.amount;
    from.UpdatedAt = DateTime.UtcNow;
    to.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    // If anything above throws, disposing the transaction without Commit rolls everything back.
    await transaction.CommitAsync();

    return Ok(ApiResponse.Ok(new { fromBalance = from.Balance, toBalance = to.Balance }, "Transfer completed"));
  }

  /// <summary>Shows a user's balance so the effect of a failed transfer can be observed.</summary>
  [HttpGet]
  [Route("/lab/exceptions/wallets/{userId:guid}")]
  public async Task<IActionResult> GetBalance(Guid userId)
  {
    WalletEntity wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId)
      ?? throw AppException.NotFound("Wallet not found");

    return Ok(ApiResponse.Ok(new { wallet.UserId, wallet.Balance }));
  }
}
