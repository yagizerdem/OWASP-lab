using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads the application's SBOM from Data/dummy.components.json. Does nothing when the table already has rows.
/// </summary>
public class SeedComponent
{
  private record ComponentSeed(string Name, string Version);

  public static void Seed(AppDbContext context)
  {
    if (context.Components.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.components.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<ComponentSeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    foreach (var s in seeds)
    {
      context.Components.Add(new ComponentEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Name = s.Name,
        Version = s.Version
      });
    }

    context.SaveChanges();
  }
}
