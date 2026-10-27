using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

public static class RateLimitExtension
{
  public static IServiceCollection UseRateLimit(this IServiceCollection services)
  {

    services.AddRateLimiter(options =>
    {
      // Rejected requests get 429 instead of the default 503.
      options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

      // Named policy: applied only to endpoints decorated with [EnableRateLimiting("login")].
      // One counter per client IP, so a single attacker cannot lock everyone out.
      options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
              PermitLimit = 4,
              Window = TimeSpan.FromSeconds(120),
              QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
              QueueLimit = 0
            }));

    });

    return services;
  }

}
