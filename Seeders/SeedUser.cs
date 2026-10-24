public class SeedUser
{
  public static void Seed(AppDbContext context)
  {
    if (!context.Users.Any())
    {
      context.Users.Add(new UserEntity
      {
        Username = "admin",
        Password = "admin123",
        Email = "admin@example.com",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow

      });
      context.SaveChanges();
    }
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
