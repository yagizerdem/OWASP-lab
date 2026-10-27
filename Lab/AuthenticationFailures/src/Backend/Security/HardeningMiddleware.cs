namespace backend.Security;

using System.Text.RegularExpressions;
using backend.Services;
using Models.Entity;

/// <summary>
/// Answers 404 (and logs) for requests that look for repository metadata, backups or config files,
/// and adds basic response headers. The app serves no static files and no directory listing.
/// </summary>
public partial class HardeningMiddleware
{
  // /.git/..., /.env, /x/.DS_Store  and  *.bak, *.old, *.orig, *.swp, *.sql, *.config, *.zip, *~
  [GeneratedRegex(@"(^|/)\.(?!well-known)[^/]+|(\.(bak|old|orig|swp|sql|config|zip|tar|gz|7z)|~)$", RegexOptions.IgnoreCase)]
  private static partial Regex SensitivePath();

  private readonly RequestDelegate _next;

  /// <summary>Next step in the pipeline.</summary>
  public HardeningMiddleware(RequestDelegate next) => _next = next;

  /// <summary>Adds security headers (nosniff, DENY, no-store) and cuts sensitive path probes with 404 + log (rule 5).</summary>
  public async Task InvokeAsync(HttpContext context, SecurityEventService events)
  {
    // The vulnerable static file server is exempt on purpose (rule 5 violation demo).
    if (context.Request.Path.StartsWithSegments(VulnerableStaticFiles.RequestPath))
    {
      await _next(context);
      return;
    }

    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers.CacheControl = "no-store";

    string path = Uri.UnescapeDataString(context.Request.Path.Value ?? string.Empty);
    if (SensitivePath().IsMatch(path))
    {
      await events.RecordAsync(SecurityEventTypes.BlockedPath, SecurityEventSeverity.Warning, "Sensitive path probe");
      context.Response.StatusCode = StatusCodes.Status404NotFound;
      return;
    }

    await _next(context);
  }
}
