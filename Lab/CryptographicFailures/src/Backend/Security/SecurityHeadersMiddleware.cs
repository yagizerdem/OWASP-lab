namespace Backend.Security;

/// <summary>
/// Enforces encryption in transit and adds browser security headers for the compliant (<c>/lab/secure</c>) endpoints.
/// <list type="bullet">
/// <item>Plain HTTP requests are rejected with 400 so secrets are never accepted over an unencrypted channel.</item>
/// <item>Responses carry HSTS, nosniff, frame denial, a strict CSP, no referrer and no-store.</item>
/// </list>
/// </summary>
public class SecurityHeadersMiddleware
{
  private readonly RequestDelegate _next;

  /// <summary>The next middleware comes from the pipeline.</summary>
  public SecurityHeadersMiddleware(RequestDelegate next)
  {
    _next = next;
  }

  /// <summary>Applies the rules to <c>/lab/secure</c> requests only; every other path is left alone.</summary>
  public async Task InvokeAsync(HttpContext context)
  {
    if (!context.Request.Path.StartsWithSegments("/lab/secure"))
    {
      await _next(context);
      return;
    }

    if (!context.Request.IsHttps)
    {
      context.Response.StatusCode = StatusCodes.Status400BadRequest;
      context.Response.ContentType = "application/json";
      await context.Response.WriteAsync("{\"message\":\"HTTPS required\",\"status\":400}");
      return;
    }

    context.Response.OnStarting(() =>
    {
      IHeaderDictionary h = context.Response.Headers;
      h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
      h["X-Content-Type-Options"] = "nosniff";
      h["X-Frame-Options"] = "DENY";
      h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
      h["Referrer-Policy"] = "no-referrer";
      h["Cache-Control"] = "no-store";
      h["Pragma"] = "no-cache";
      return Task.CompletedTask;
    });

    await _next(context);
  }
}
