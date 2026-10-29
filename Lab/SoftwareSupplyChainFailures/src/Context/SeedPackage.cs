using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads registry packages from Data/dummy.packages.json. Does nothing when the table already has rows.
/// </summary>
public class SeedPackage
{
  private record PackageSeed(string Name, string Version, string Source, string Sha256);

  public static void Seed(AppDbContext context)
  {
    if (context.Packages.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.packages.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<PackageSeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    foreach (var s in seeds)
    {
      context.Packages.Add(new PackageEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Name = s.Name,
        Version = s.Version,
        Source = s.Source,
        Sha256 = s.Sha256
      });
    }

    context.SaveChanges();
  }
}
