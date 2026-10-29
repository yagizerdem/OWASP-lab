using Models.Entity;
using Newtonsoft.Json;

List<UserEntity> GenerateRandomUsers(int count = 100)
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

// Security events and audit logs are produced by using the lab, so only users are generated.
const string usersFile = "dummy.users.json";
bool regenUsers = args.Contains("--regen-users");

List<UserEntity> users;
if (!regenUsers && File.Exists(usersFile))
{
  users = JsonConvert.DeserializeObject<List<UserEntity>>(File.ReadAllText(usersFile)) ?? new();
}
else
{
  users = GenerateRandomUsers();
  File.WriteAllText(usersFile, JsonConvert.SerializeObject(users));
}

Console.WriteLine($"users: {users.Count}");
