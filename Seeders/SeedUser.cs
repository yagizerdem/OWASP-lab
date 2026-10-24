using System.Collections.Frozen;

public class SeedUser
{
  public static void Seed(AppDbContext context)
  {
    if (!context.Users.Any())
    {
      context.Users.Add(GenerateRandomUsers(1).First());
      context.SaveChanges();
    }
  }

  public static void Seed(AppDbContext context, UserEntity entity)
  {
    if (!context.Users.Any())
    {
      context.Users.Add(entity);
      context.SaveChanges();
    }
  }

  public static void Seed(AppDbContext context, List<UserEntity> entitys)
  {
    if (!context.Users.Any())
    {
      entitys.ForEach(e => context.Users.Add(e));
    }
    context.SaveChanges();
  }

  public static List<UserEntity> GenerateRandomUsers(int count = 100)
  {
    List<UserEntity> users = new List<UserEntity>();
    for (int i = 0; i < count; i++)
    {
      UserEntity user = new UserEntity()
      {
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Username = Faker.Internet.UserName(),
        Password = $"pass{Faker.RandomNumber.Next(10000, 1000000)}",
        Email = Faker.Internet.Email()
      };

      users.Add(user);
    }

    return users;
  }
}
