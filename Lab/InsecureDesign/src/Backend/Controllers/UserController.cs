using Microsoft.AspNetCore.Mvc;
using Models.Entity;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
  private readonly AppDbContext _context;
  public UserController(AppDbContext context)
  {
    _context = context;
  }


  [HttpPost]
  [Route("/lab/secure/login")]
  public IActionResult Login([FromBody] LoginDTO request)
  {
    var user = this._context.Users.FirstOrDefault(u => u.Email == request.email);
    if (user == null)
    {
      throw AppException.NotFound("User not found");
    }

    if (user.Password != request.password)
    {
      throw AppException.Unauthorized("Invalid password");
    }

    return Ok(ApiResponse.Ok("login successful"));
  }

  [HttpPost]
  [Route("/lab/vulnerable/change-password")]
  public IActionResult ChangePassword([FromBody] ChangePasswordDTO request)
  {
    var user = this._context.Users.FirstOrDefault(u => u.Email == request.email);
    if (user == null)
    {
      throw AppException.NotFound("User not found");
    }

    if (user.SecurityAnswer != request.securityAnswer)
    {
      throw AppException.Unauthorized("Invalid security answer");
    }

    user.Password = request.newPassword;
    this._context.SaveChanges();

    return Ok(ApiResponse.Ok("password changed successfully"));
  }

  /// <summary>
  /// INSECURE DESIGN (weak password reset flow): a proper design never hands the OTP back to the
  /// caller - it goes out-of-band through email/SMS. Here the OTP is returned directly in the response
  /// "for convenience", so anyone who can reach this endpoint for a given email can read the OTP needed
  /// to take over that account, no interception required.
  /// </summary>
  [HttpPost]
  [Route("/lab/vulnerable/request-password-reset")]
  public IActionResult RequestPasswordReset([FromBody] RequestPasswordResetDTO request)
  {
    var user = this._context.Users.FirstOrDefault(u => u.Email == request.email);
    if (user == null)
    {
      throw AppException.NotFound("User not found");
    }

    string otp = Random.Shared.Next(0, 10000).ToString("D4");

    this._context.PasswordResets.Add(new PasswordResetEntity()
    {
      Id = Guid.NewGuid(),
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      UserId = user.Id,
      Otp = otp
    });
    this._context.SaveChanges();

    // INSECURE: a real design would email/SMS this and never put it in the HTTP response.
    return Ok(ApiResponse.Ok(new { otp }, "OTP generated (would normally be delivered out-of-band)"));
  }

  /// <summary>
  /// INSECURE DESIGN (weak password reset flow continued): the OTP has no expiry and is never marked
  /// used after a successful reset, so the exact same OTP can reset the password again and again,
  /// indefinitely, instead of being valid for one use within a short window.
  /// </summary>
  [HttpPost]
  [Route("/lab/vulnerable/reset-password-with-otp")]
  public IActionResult ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpDTO request)
  {
    var user = this._context.Users.FirstOrDefault(u => u.Email == request.email);
    if (user == null)
    {
      throw AppException.NotFound("User not found");
    }

    // No expiry check and no "already used" check - any OTP ever issued to this user still works.
    var reset = this._context.PasswordResets
      .FirstOrDefault(r => r.UserId == user.Id && r.Otp == request.otp);
    if (reset == null)
    {
      throw AppException.Unauthorized("Invalid OTP");
    }

    user.Password = request.newPassword;
    this._context.SaveChanges();

    return Ok(ApiResponse.Ok("password reset successfully"));
  }

  /// <summary>
  /// INSECURE DESIGN (missing business-rule enforcement): the idea "one signup bonus per account" was
  /// never turned into a server-side control - no unique constraint on UserId, no check for an existing
  /// claim before inserting. Calling this endpoint repeatedly keeps minting more balance for the same
  /// account.
  /// </summary>
  [HttpPost]
  [Route("/lab/vulnerable/claim-signup-bonus")]
  public IActionResult ClaimSignupBonus([FromBody] ClaimSignupBonusDTO request)
  {
    var user = this._context.Users.FirstOrDefault(u => u.Email == request.email);
    if (user == null)
    {
      throw AppException.NotFound("User not found");
    }

    this._context.BonusClaims.Add(new BonusClaimEntity()
    {
      Id = Guid.NewGuid(),
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      UserId = user.Id,
      Amount = 50
    });
    this._context.SaveChanges();

    decimal balance = this._context.BonusClaims
      .Where(b => b.UserId == user.Id)
      .Sum(b => b.Amount);

    return Ok(ApiResponse.Ok(new { balance }, "signup bonus claimed"));
  }

  /// <summary>Helper endpoint so the balance from repeated claim-signup-bonus calls can be observed.</summary>
  [HttpGet]
  [Route("/lab/vulnerable/wallet-balance")]
  public IActionResult GetWalletBalance([FromQuery] string email)
  {
    var user = this._context.Users.FirstOrDefault(u => u.Email == email);
    if (user == null)
    {
      throw AppException.NotFound("User not found");
    }

    decimal balance = this._context.BonusClaims
      .Where(b => b.UserId == user.Id)
      .Sum(b => b.Amount);

    return Ok(ApiResponse.Ok(new { balance }));
  }

}
