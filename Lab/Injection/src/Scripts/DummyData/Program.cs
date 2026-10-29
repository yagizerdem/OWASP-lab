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
      age = Faker.RandomNumber.Next(18, 70),
      SecurityAnswer = Faker.Name.Last().ToLowerInvariant()
    };

    users.Add(user);
  }

  return users;
}

// Blogs refer to user ids, so existing users are reused unless --regen-users is passed.
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

// 1 to 5 blog posts for each of the first 20 users.
var blogs = new List<object>();
for (int i = 0; i < Math.Min(users.Count, 20); i++)
{
  int count = Random.Shared.Next(1, 6);
  for (int n = 0; n < count; n++)
  {
    blogs.Add(new
    {
      UserId = users[i].Id,
      Title = Faker.Lorem.Sentence(3).TrimEnd('.'),
      Content = Faker.Lorem.Sentence(30)
    });
  }
}

File.WriteAllText("dummy.blogs.json", JsonConvert.SerializeObject(blogs, Formatting.Indented));

Console.WriteLine($"users: {users.Count}, blogs: {blogs.Count}");
