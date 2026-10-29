using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads advisories from Data/dummy.advisories.json. Does nothing when the table already has rows.
/// </summary>
public class SeedAdvisory
{
  private record AdvisorySeed(string AdvisoryId, string PackageName, string FixedVersion, string Severity, string? Summary);

  public static void Seed(AppDbContext context)
  {
    if (context.Advisories.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.advisories.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<AdvisorySeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    foreach (var s in seeds)
    {
      context.Advisories.Add(new AdvisoryEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        AdvisoryId = s.AdvisoryId,
        PackageName = s.PackageName,
        FixedVersion = s.FixedVersion,
        Severity = s.Severity,
        Summary = s.Summary
      });
    }

    context.SaveChanges();
  }
}
