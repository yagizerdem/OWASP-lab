using Newtonsoft.Json;
using Models.Entity;

/// <summary>Seeds plaintext payment cards for the vulnerable endpoints from <c>Data/dummy.legacycards.json</c> (fake, Luhn valid numbers).</summary>
public class SeedLegacyCard
{
  /// <summary>Loads the JSON file and inserts it when the table is empty.</summary>
  public static void Seed(AppDbContext context)
  {
    if (context.LegacyCards.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.legacycards.json");
    if (!File.Exists(path)) return;

    var entities = JsonConvert.DeserializeObject<List<LegacyCardEntity>>(File.ReadAllText(path), new JsonSerializerSettings
    {
      MissingMemberHandling = MissingMemberHandling.Ignore,
      NullValueHandling = NullValueHandling.Ignore,
    });

    if (entities == null || entities.Count == 0) return;

    Seed(context, entities);
  }

  /// <summary>Inserts the given rows when the table is empty.</summary>
  public static void Seed(AppDbContext context, List<LegacyCardEntity> entities)
  {
    if (context.LegacyCards.Any()) return;

    context.LegacyCards.AddRange(entities);
    context.SaveChanges();
  }
}
