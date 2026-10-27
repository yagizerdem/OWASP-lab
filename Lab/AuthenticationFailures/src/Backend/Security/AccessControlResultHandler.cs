namespace backend.Security;

using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Models.Entity;
using Newtonsoft.Json;

/// <summary>
/// Runs for every request the authorization middleware rejects (deny by default hits this too):
/// logs the failure, then answers with the lab's standard JSON error.
/// </summary>
public class AccessControlResultHandler : IAuthorizationMiddlewareResultHandler
{
  private readonly AuthorizationMiddlewareResultHandler _default = new();

  /// <summary>Logs 401 (not authenticated) and 403 (not authorized) results and writes a JSON response; other results go to the default handler.</summary>
  public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
    PolicyAuthorizationResult authorizeResult)
  {
    if (!authorizeResult.Challenged && !authorizeResult.Forbidden)
    {
      await _default.HandleAsync(next, context, policy, authorizeResult);
      return;
    }

    var events = context.RequestServices.GetRequiredService<SecurityEventService>();
    Guid? userId = Guid.TryParse(context.User.FindFirst("sub")?.Value, out Guid id) ? id : null;

    ApiResponse body;
    if (authorizeResult.Challenged)
    {
      await events.RecordAsync(SecurityEventTypes.Unauthenticated, SecurityEventSeverity.Warning, "Missing or invalid token");
      context.Response.Headers.WWWAuthenticate = "Bearer";
      context.Response.StatusCode = StatusCodes.Status401Unauthorized;
      body = ApiResponse.Unauthorized("Authentication required");
    }
    else
    {
      await events.RecordAsync(SecurityEventTypes.Forbidden, SecurityEventSeverity.Warning, "Role requirement not met", userId);
      context.Response.StatusCode = StatusCodes.Status403Forbidden;
      body = ApiResponse.Forbidden("Access denied");
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonConvert.SerializeObject(body));
  }
}
