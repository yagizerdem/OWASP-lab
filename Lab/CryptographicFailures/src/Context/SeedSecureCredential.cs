using Newtonsoft.Json;
using Models.Entity;

/// <summary>Seeds PBKDF2 credentials (compliant counterpart of <see cref="SeedWeakHash"/>) from <c>Data/dummy.securecredentials.json</c>.</summary>
public class SeedSecureCredential
{
  /// <summary>Loads the JSON file and inserts it when the table is empty.</summary>
  public static void Seed(AppDbContext context)
  {
    if (context.SecureCredentials.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.securecredentials.json");
    if (!File.Exists(path)) return;

    var entities = JsonConvert.DeserializeObject<List<SecureCredentialEntity>>(File.ReadAllText(path), new JsonSerializerSettings
    {
      MissingMemberHandling = MissingMemberHandling.Ignore,
      NullValueHandling = NullValueHandling.Ignore,
    });

    if (entities == null || entities.Count == 0) return;

    Seed(context, entities);
  }

  /// <summary>Inserts the given rows when the table is empty.</summary>
  public static void Seed(AppDbContext context, List<SecureCredentialEntity> entities)
  {
    if (context.SecureCredentials.Any()) return;

    context.SecureCredentials.AddRange(entities);
    context.SaveChanges();
  }
}
