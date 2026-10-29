using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads the trusted plugin manifest from Data/dummy.trustedplugins.json. Does nothing when the table
/// already has rows.
/// </summary>
public class SeedTrustedPlugin
{
  private record TrustedPluginSeed(string Name, string Version, string Sha256, string ContentBase64);

  public static void Seed(AppDbContext context)
  {
    if (context.TrustedPlugins.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.trustedplugins.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<TrustedPluginSeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    foreach (var s in seeds)
    {
      context.TrustedPlugins.Add(new TrustedPluginEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Name = s.Name,
        Version = s.Version,
        Sha256 = s.Sha256,
        ContentBase64 = s.ContentBase64
      });
    }

    context.SaveChanges();
  }
}
