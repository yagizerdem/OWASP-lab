using Newtonsoft.Json;
using Models.Entity;

/// <summary>Seeds users whose password is stored as an unsalted MD5 hash.</summary>
public class SeedWeakHash
{
  /// <summary>Loads <c>Data/dummy.weakhashes.json</c> (or generates one row) and inserts it when the table is empty.</summary>
  public static void Seed(AppDbContext context)
  {
    if (context.WeakHashes.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.weakhashes.json");

    List<WeakHashEntity>? entities = null;
    if (File.Exists(path))
    {
      var json = File.ReadAllText(path);
      entities = JsonConvert.DeserializeObject<List<WeakHashEntity>>(json, new JsonSerializerSettings
      {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
      });
    }

    if (entities == null || entities.Count == 0)
    {
      entities = GenerateRandomWeakHashEntities(1);
    }

    Seed(context, entities);
  }

  /// <summary>Inserts the given rows when the table is empty.</summary>
  public static void Seed(AppDbContext context, List<WeakHashEntity> entities)
  {
    if (context.WeakHashes.Any()) return;

    context.WeakHashes.AddRange(entities);
    context.SaveChanges();
  }

  /// <summary>Fallback generator: unsalted MD5 (weak on purpose) of a random password.</summary>
  public static List<WeakHashEntity> GenerateRandomWeakHashEntities(int count = 100)
  {
    List<WeakHashEntity> users = new List<WeakHashEntity>();
    for (int i = 0; i < count; i++)
    {
      string password = $"pass{Faker.RandomNumber.Next(10000, 1000000)}";
      users.Add(new WeakHashEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        UserName = Faker.Internet.UserName(),
        PlaintextPassword = password,
        HashedPassword = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(password))).ToLowerInvariant()
      });
    }

    return users;
  }
}
