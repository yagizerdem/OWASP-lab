using Newtonsoft.Json;
using Models.Entity;
public class SeedUser
{
  public static void Seed(AppDbContext context)
  {
    if (context.Users.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.users.json");

    List<UserEntity>? users = null;
    if (File.Exists(path))
    {
      var json = File.ReadAllText(path);
      users = JsonConvert.DeserializeObject<List<UserEntity>>(json, new JsonSerializerSettings
      {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
      });
    }

    if (users == null || users.Count == 0)
    {
      users = GenerateRandomUsers(1);
    }

    Seed(context, users);
  }

  public static void Seed(AppDbContext context, List<UserEntity> entitys)
  {
    if (context.Users.Any()) return;

    context.Users.AddRange(entitys);
    context.SaveChanges();
  }

  public static List<UserEntity> GenerateRandomUsers(int count = 100)
  {
    List<UserEntity> users = new List<UserEntity>();
    for (int i = 0; i < count; i++)
    {
      UserEntity user = new UserEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Username = Faker.Internet.UserName(),
        Password = $"pass{Faker.RandomNumber.Next(10000, 1000000)}",
        Email = Faker.Internet.Email(),
        age = Faker.RandomNumber.Next(18, 70)
      };

      users.Add(user);
    }

    return users;
  }
}
