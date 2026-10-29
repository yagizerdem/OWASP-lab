using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.DTO;
using Models.Entity;

namespace Backend.Controllers;

/// <summary>
/// Case 2 - code installed without an integrity check. A plugin package (e.g. downloaded from a mirror
/// or CDN) is installed as received. If the package was modified on the way, the modified code runs.
/// </summary>
[ApiController]
[Route("[controller]")]
public class PluginController : ControllerBase
{
  private readonly AppDbContext _context;

  public PluginController(AppDbContext context)
  {
    _context = context;
  }

  /// <summary>
  /// VULNERABLE: any name/version/content is installed. The package is never compared with the trusted
  /// manifest, so a tampered or entirely unknown package is accepted.
  /// </summary>
  [HttpPost]
  [Route("/lab/vulnerable/integrity/plugins/install")]
  public async Task<IActionResult> InstallVulnerable([FromBody] InstallPluginDTO request)
  {
    byte[] content = DecodeBase64(request.contentBase64);

    var plugin = new InstalledPluginEntity()
    {
      Id = Guid.NewGuid(),
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      Name = request.name,
      Version = request.version,
      Sha256 = Sha256Hex(content),
      ContentBase64 = request.contentBase64,
      Verified = false
    };
    _context.InstalledPlugins.Add(plugin);
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.Ok(new { plugin.Name, plugin.Version, plugin.Sha256, plugin.Verified }, "plugin installed"));
  }

  /// <summary>
  /// SECURE: the name/version must exist in the trusted manifest and the SHA-256 of the received bytes
  /// must equal the manifest hash. Unknown or modified packages are rejected before anything is stored.
  /// </summary>
  [HttpPost]
  [Route("/lab/secure/integrity/plugins/install")]
  public async Task<IActionResult> InstallSecure([FromBody] InstallPluginDTO request)
  {
    byte[] content = DecodeBase64(request.contentBase64);

    TrustedPluginEntity trusted = await _context.TrustedPlugins
      .FirstOrDefaultAsync(p => p.Name == request.name && p.Version == request.version)
      ?? throw AppException.Forbidden("Plugin is not in the trusted manifest");

    string hash = Sha256Hex(content);
    if (hash != trusted.Sha256)
    {
      throw AppException.Forbidden("Plugin hash does not match the trusted manifest");
    }

    var plugin = new InstalledPluginEntity()
    {
      Id = Guid.NewGuid(),
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      Name = trusted.Name,
      Version = trusted.Version,
      Sha256 = hash,
      ContentBase64 = request.contentBase64,
      Verified = true
    };
    _context.InstalledPlugins.Add(plugin);
    await _context.SaveChangesAsync();

    return Ok(ApiResponse.Ok(new { plugin.Name, plugin.Version, plugin.Sha256, plugin.Verified }, "plugin installed"));
  }

  /// <summary>The trusted manifest including the official package content (the "official download").</summary>
  [HttpGet]
  [Route("/lab/integrity/plugins/trusted")]
  public async Task<IActionResult> GetTrusted()
  {
    var plugins = await _context.TrustedPlugins
      .Select(p => new { p.Name, p.Version, p.Sha256, p.ContentBase64 })
      .ToListAsync();

    return Ok(ApiResponse.Ok(plugins));
  }

  /// <summary>Everything installed so far, with whether it was verified.</summary>
  [HttpGet]
  [Route("/lab/integrity/plugins/installed")]
  public async Task<IActionResult> GetInstalled()
  {
    var plugins = await _context.InstalledPlugins
      .OrderByDescending(p => p.CreatedAt)
      .Select(p => new { p.Name, p.Version, p.Sha256, p.Verified, p.CreatedAt })
      .ToListAsync();

    return Ok(ApiResponse.Ok(plugins));
  }

  private static byte[] DecodeBase64(string value)
  {
    try
    {
      return Convert.FromBase64String(value);
    }
    catch (FormatException)
    {
      throw new AppException("contentBase64 is not valid base64");
    }
  }

  private static string Sha256Hex(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
}
