using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Case 1 - debug/diagnostics endpoint left enabled. Useful while developing, but when it ships it hands
/// out the database connection string (with the sa password), the environment and host details.
/// </summary>
[ApiController]
[Route("[controller]")]
public class DiagnosticsController : ControllerBase
{
  private readonly IConfiguration _configuration;
  private readonly IWebHostEnvironment _env;

  public DiagnosticsController(IConfiguration configuration, IWebHostEnvironment env)
  {
    _configuration = configuration;
    _env = env;
  }

  /// <summary>
  /// VULNERABLE: anonymous, enabled in every environment, and returns secrets and internals
  /// (connection string, environment, machine name, runtime, content root path).
  /// </summary>
  [HttpGet]
  [Route("/lab/vulnerable/misconfig/diagnostics")]
  public IActionResult GetVulnerable()
  {
    return Ok(ApiResponse.Ok(new
    {
      environment = _env.EnvironmentName,
      connectionString = _configuration.GetConnectionString("DefaultConnection"),
      allowedHosts = _configuration["AllowedHosts"],
      machineName = Environment.MachineName,
      os = RuntimeInformation.OSDescription,
      runtime = RuntimeInformation.FrameworkDescription,
      contentRoot = _env.ContentRootPath
    }));
  }

  /// <summary>
  /// SECURE: a plain health check that reveals nothing useful to an attacker. Detailed diagnostics only
  /// exist in Development, and even there secrets are never part of the response.
  /// </summary>
  [HttpGet]
  [Route("/lab/secure/misconfig/diagnostics")]
  public IActionResult GetSecure()
  {
    if (!_env.IsDevelopment())
    {
      return Ok(ApiResponse.Ok(new { status = "ok" }));
    }

    return Ok(ApiResponse.Ok(new
    {
      status = "ok",
      environment = _env.EnvironmentName,
      databaseConfigured = !string.IsNullOrEmpty(_configuration.GetConnectionString("DefaultConnection"))
    }));
  }
}
