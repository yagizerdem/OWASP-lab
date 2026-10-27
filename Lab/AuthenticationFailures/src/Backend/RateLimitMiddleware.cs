using System.Net;
using System.Threading.RateLimiting;
using backend.Services;
using Microsoft.AspNetCore.RateLimiting;
using Models.Entity;
using Newtonsoft.Json;

/// <summary>
/// Rate limit configuration (rule 7): global 100 requests/min/IP, "auth" 10 requests/min/IP (secure login/refresh),
/// "login" 4 requests/120 s/IP (existing lab endpoint). Rejected requests get 429 + Retry-After and are logged.
/// </summary>
public static class RateLimitExtension
{
  /// <summary>Registers the rate limiter services and policies.</summary>
  public static IServiceCollection UseRateLimit(this IServiceCollection services)
  {

    services.AddRateLimiter(options =>
    {
      // Rejected requests get 429 instead of the default 503.
      options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

      // Log every rejection and answer with the lab's JSON error instead of an empty body.
      options.OnRejected = async (context, cancellationToken) =>
      {
        var http = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
          http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

        await http.RequestServices.GetRequiredService<SecurityEventService>()
          .RecordAsync(SecurityEventTypes.RateLimited, SecurityEventSeverity.Warning, "Rate limit exceeded");

        http.Response.ContentType = "application/json";
        await http.Response.WriteAsync(
          JsonConvert.SerializeObject(ApiResponse.Fail("Too many requests", HttpStatusCode.TooManyRequests)), cancellationToken);
      };

      // Applies to every endpoint, so nothing is left without a limit: 100 requests / minute / IP.
      // The vulnerable lab endpoints are deliberately exempt (rule 7 violation).
      options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        httpContext.Request.Path.StartsWithSegments("/lab/vulnerable")
        ? RateLimitPartition.GetNoLimiter("vulnerable")
        : RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = 100,
              Window = TimeSpan.FromMinutes(1),
              QueueLimit = 0
            }));

      // Stricter limit for the secure login / refresh endpoints: 10 requests / minute / IP.
      options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = 10,
              Window = TimeSpan.FromMinutes(1),
              QueueLimit = 0
            }));

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
