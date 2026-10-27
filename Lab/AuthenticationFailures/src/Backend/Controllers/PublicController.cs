namespace backend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The only intentionally public resource of the secure API.
/// <para>Scope: rule 1 (deny by default except public resources; the exception is marked explicitly with [AllowAnonymous]).</para>
/// </summary>
[ApiController]
[Route("lab/secure/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
  /// <summary>
  /// Liveness check. Expectation: 200 and status=up without a token; contains no sensitive information.
  /// </summary>
  [HttpGet("health")]
  public IActionResult Health() => Ok(ApiResponse.Ok(new { status = "up" }));
}
