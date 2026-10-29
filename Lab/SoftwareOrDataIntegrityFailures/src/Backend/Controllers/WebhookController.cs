using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;
using Newtonsoft.Json;

namespace Backend.Controllers;

/// <summary>
/// Case 1 - unsigned webhook. The payment provider calls this endpoint to report that an order was
/// paid. Without verifying where the message came from, anyone can POST {"status":"paid"} and get goods
/// for free.
/// </summary>
[ApiController]
[Route("[controller]")]
public class WebhookController : ControllerBase
{
  private readonly AppDbContext _context;
  private readonly IConfiguration _configuration;
  private readonly IWebHostEnvironment _env;

  public WebhookController(AppDbContext context, IConfiguration configuration, IWebHostEnvironment env)
  {
    _context = context;
    _configuration = configuration;
    _env = env;
  }

  /// <summary>
  /// VULNERABLE: the body is trusted as-is. No signature, no check that the amount matches the order,
  /// and an already paid order can be "paid" again.
  /// </summary>
  [HttpPost]
  [Route("/lab/vulnerable/integrity/webhooks/payment")]
  public async Task<IActionResult> PaymentVulnerable([FromBody] PaymentWebhookDTO request)
  {
    OrderEntity order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.orderId)
      ?? throw AppException.NotFound("Order not found");

    if (request.status == "paid")
    {
      order.Status = "Paid";
      order.UpdatedAt = DateTime.UtcNow;
      await _context.SaveChangesAsync();
    }

    return Ok(ApiResponse.Ok(new { order.Id, order.Status }));
  }

  /// <summary>
  /// SECURE: the X-Signature header must be the HMAC-SHA256 (hex) of the exact raw request body with the
  /// shared webhook secret, compared in constant time. The amount must match the order, and only a
  /// Pending order can become Paid.
  /// </summary>
  [HttpPost]
  [Route("/lab/secure/integrity/webhooks/payment")]
  public async Task<IActionResult> PaymentSecure([FromBody] PaymentWebhookDTO request, [FromHeader(Name = "X-Signature")] string? signature)
  {
    // The raw body is buffered in Program.cs, so it can be read again after model binding.
    Request.Body.Position = 0;
    using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
    string rawBody = await reader.ReadToEndAsync();

    if (!IsValidSignature(rawBody, signature))
    {
      throw AppException.Unauthorized("Invalid webhook signature");
    }

    OrderEntity order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.orderId)
      ?? throw AppException.NotFound("Order not found");

    if (request.amount != order.Amount)
    {
      throw new AppException("Amount does not match the order");
    }

    if (request.status == "paid")
    {
      if (order.Status != "Pending")
      {
        throw new AppException("Order is not pending");
      }

      order.Status = "Paid";
      order.UpdatedAt = DateTime.UtcNow;
      await _context.SaveChangesAsync();
    }

    return Ok(ApiResponse.Ok(new { order.Id, order.Status }));
  }

  /// <summary>
  /// Development only: plays the payment provider and returns a raw body with its signature. Send the
  /// rawBody exactly as returned and put the signature in the X-Signature header.
  /// </summary>
  [HttpPost]
  [Route("/lab/secure/integrity/webhooks/provider-sign")]
  public IActionResult ProviderSign([FromBody] PaymentWebhookDTO request)
  {
    if (!_env.IsDevelopment())
    {
      throw AppException.NotFound();
    }

    string rawBody = JsonConvert.SerializeObject(request);
    return Ok(ApiResponse.Ok(new { rawBody, signature = Sign(rawBody) }));
  }

  /// <summary>Order ids, amounts and statuses, so the webhook cases can be tried.</summary>
  [HttpGet]
  [Route("/lab/integrity/orders")]
  public async Task<IActionResult> GetOrders()
  {
    var orders = await _context.Orders
      .OrderBy(o => o.CreatedAt)
      .Select(o => new { o.Id, o.UserId, o.Amount, o.Status })
      .Take(100)
      .ToListAsync();

    return Ok(ApiResponse.Ok(orders));
  }

  private bool IsValidSignature(string rawBody, string? signature)
  {
    if (string.IsNullOrWhiteSpace(signature))
    {
      return false;
    }

    byte[] expected = Encoding.ASCII.GetBytes(Sign(rawBody));
    byte[] actual = Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant());
    return CryptographicOperations.FixedTimeEquals(expected, actual);
  }

  private string Sign(string rawBody)
  {
    string secret = _configuration["Webhook:Secret"]
      ?? throw AppException.InternalServerError("Webhook secret is not configured");

    byte[] mac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(rawBody));
    return Convert.ToHexString(mac).ToLowerInvariant();
  }
}
