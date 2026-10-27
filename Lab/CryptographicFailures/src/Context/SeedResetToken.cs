using Newtonsoft.Json;
using Models.Entity;

/// <summary>Seeds a few sample reset tokens (weak, expired secure and used secure ones) from <c>Data/dummy.resettokens.json</c>.</summary>
public class SeedResetToken
{
  /// <summary>Loads the JSON file and inserts it when the table is empty.</summary>
  public static void Seed(AppDbContext context)
  {
    if (context.ResetTokens.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.resettokens.json");
    if (!File.Exists(path)) return;

    var entities = JsonConvert.DeserializeObject<List<ResetTokenEntity>>(File.ReadAllText(path), new JsonSerializerSettings
    {
      MissingMemberHandling = MissingMemberHandling.Ignore,
      NullValueHandling = NullValueHandling.Ignore,
    });

    if (entities == null || entities.Count == 0) return;

    Seed(context, entities);
  }

  /// <summary>Inserts the given rows when the table is empty.</summary>
  public static void Seed(AppDbContext context, List<ResetTokenEntity> entities)
  {
    if (context.ResetTokens.Any()) return;

    context.ResetTokens.AddRange(entities);
    context.SaveChanges();
  }
}
