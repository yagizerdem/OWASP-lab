using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// Case 2 - shipping components with known vulnerabilities. The SBOM lists image-resize 0.9.2 (Critical)
/// and yaml-reader 1.4.0 (High). The vulnerable pipeline deploys without looking; the secure pipeline
/// matches the SBOM against advisories and blocks on Critical/High findings.
/// </summary>
[ApiController]
[Route("[controller]")]
public class DeployController : ControllerBase
{
  private static readonly string[] BlockingSeverities = { "Critical", "High" };

  private readonly AppDbContext _context;

  public DeployController(AppDbContext context)
  {
    _context = context;
  }

  /// <summary>VULNERABLE: deploys whatever is in the SBOM; advisories are never consulted.</summary>
  [HttpPost]
  [Route("/lab/vulnerable/supplychain/deploy")]
  public async Task<IActionResult> DeployVulnerable()
  {
    var components = await _context.Components
      .OrderBy(c => c.Name)
      .Select(c => new { c.Name, c.Version })
      .ToListAsync();

    return Ok(ApiResponse.Ok(new { components }, "deployed"));
  }

  /// <summary>
  /// SECURE: every component is checked against the advisories. Any Critical or High finding blocks the
  /// deployment with 409 and the list of findings; lower severities are returned as warnings.
  /// </summary>
  [HttpPost]
  [Route("/lab/secure/supplychain/deploy")]
  public async Task<IActionResult> DeploySecure()
  {
    var findings = await GetFindings();
    var blocking = findings.Where(f => BlockingSeverities.Contains(f.Severity)).ToList();

    if (blocking.Count > 0)
    {
      return StatusCode(StatusCodes.Status409Conflict,
        ApiResponse.Fail("Deployment blocked: components with Critical/High advisories", HttpStatusCode.Conflict, blocking));
    }

    return Ok(ApiResponse.Ok(new { warnings = findings }, "deployed"));
  }

  /// <summary>SBOM with the advisories that affect each component.</summary>
  [HttpGet]
  [Route("/lab/supplychain/sbom")]
  public async Task<IActionResult> GetSbom()
  {
    var components = await _context.Components
      .OrderBy(c => c.Name)
      .Select(c => new { c.Name, c.Version })
      .ToListAsync();

    return Ok(ApiResponse.Ok(new { components, findings = await GetFindings() }));
  }

  private async Task<List<Finding>> GetFindings()
  {
    List<ComponentEntity> components = await _context.Components.ToListAsync();
    List<AdvisoryEntity> advisories = await _context.Advisories.ToListAsync();

    return components
      .SelectMany(c => advisories
        .Where(a => a.PackageName == c.Name && ParseVersion(c.Version) < ParseVersion(a.FixedVersion))
        .Select(a => new Finding(c.Name, c.Version, a.AdvisoryId, a.Severity, a.FixedVersion, a.Summary)))
      .OrderBy(f => f.Component)
      .ToList();
  }

  private static Version ParseVersion(string value) => Version.TryParse(value, out Version? v) ? v : new Version(0, 0);

  private record Finding(string Component, string Version, string AdvisoryId, string Severity, string FixedVersion, string? Summary);
}
