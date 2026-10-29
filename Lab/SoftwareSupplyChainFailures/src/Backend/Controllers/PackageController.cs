using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// Case 1 - dependency confusion. The build resolves a dependency by name across every registry. An
/// attacker who publishes "acme-logger" 99.0.0 on the public registry wins over the internal 1.3.0.
/// </summary>
[ApiController]
[Route("[controller]")]
public class PackageController : ControllerBase
{
  /// <summary>Names with this prefix belong to the company and may only come from the internal registry.</summary>
  private const string InternalScope = "acme-";

  private readonly AppDbContext _context;

  public PackageController(AppDbContext context)
  {
    _context = context;
  }

  /// <summary>
  /// VULNERABLE: takes the highest version found in any registry. For acme-logger this returns the public
  /// 99.0.0 look-alike; nothing checks the source, the lockfile or the hash.
  /// </summary>
  [HttpGet]
  [Route("/lab/vulnerable/supplychain/packages/resolve")]
  public async Task<IActionResult> ResolveVulnerable([FromQuery] string name)
  {
    List<PackageEntity> candidates = await _context.Packages.Where(p => p.Name == name).ToListAsync();
    if (candidates.Count == 0)
    {
      throw AppException.NotFound("Package not found");
    }

    PackageEntity chosen = candidates.OrderByDescending(p => ParseVersion(p.Version)).First();

    return Ok(ApiResponse.Ok(new { chosen.Name, chosen.Version, chosen.Source, chosen.Sha256 }));
  }

  /// <summary>
  /// SECURE: the package must be in the lockfile; the exact pinned version is used; acme-* packages are only
  /// taken from the internal registry; and the package hash must equal the hash in the lockfile.
  /// </summary>
  [HttpGet]
  [Route("/lab/secure/supplychain/packages/resolve")]
  public async Task<IActionResult> ResolveSecure([FromQuery] string name)
  {
    LockEntryEntity locked = await _context.LockEntries.FirstOrDefaultAsync(l => l.Name == name)
      ?? throw new AppException("Package is not in the lockfile");

    string allowedSource = name.StartsWith(InternalScope, StringComparison.Ordinal) ? "internal" : "public";

    PackageEntity package = await _context.Packages.FirstOrDefaultAsync(p =>
        p.Name == name && p.Version == locked.Version && p.Source == allowedSource)
      ?? throw AppException.NotFound("Pinned version is not available in the allowed registry");

    if (package.Sha256 != locked.Sha256)
    {
      throw AppException.Forbidden("Package hash does not match the lockfile");
    }

    return Ok(ApiResponse.Ok(new { package.Name, package.Version, package.Source, package.Sha256 }));
  }

  /// <summary>Every package in both registries, and the lockfile.</summary>
  [HttpGet]
  [Route("/lab/supplychain/packages")]
  public async Task<IActionResult> List()
  {
    var packages = await _context.Packages
      .OrderBy(p => p.Name).ThenBy(p => p.Source)
      .Select(p => new { p.Name, p.Version, p.Source, p.Sha256 })
      .ToListAsync();
    var lockfile = await _context.LockEntries
      .OrderBy(l => l.Name)
      .Select(l => new { l.Name, l.Version, l.Sha256 })
      .ToListAsync();

    return Ok(ApiResponse.Ok(new { packages, lockfile }));
  }

  private static Version ParseVersion(string value) => Version.TryParse(value, out Version? v) ? v : new Version(0, 0);
}
