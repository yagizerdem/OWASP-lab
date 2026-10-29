using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads the lockfile from Data/dummy.lock.json. Does nothing when the table already has rows.
/// </summary>
public class SeedLockEntry
{
  private record LockSeed(string Name, string Version, string Sha256);

  public static void Seed(AppDbContext context)
  {
    if (context.LockEntries.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.lock.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<LockSeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    foreach (var s in seeds)
    {
      context.LockEntries.Add(new LockEntryEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Name = s.Name,
        Version = s.Version,
        Sha256 = s.Sha256
      });
    }

    context.SaveChanges();
  }
}
