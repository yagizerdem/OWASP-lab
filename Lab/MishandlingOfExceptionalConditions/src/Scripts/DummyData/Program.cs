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

// Documents and wallets refer to user ids, so existing users are reused unless --regen-users is passed.
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

// 3 documents for each of the first 20 users; the first one of every user is private.
var documents = new List<object>();
for (int i = 0; i < Math.Min(users.Count, 20); i++)
{
  for (int n = 0; n < 3; n++)
  {
    documents.Add(new
    {
      OwnerId = users[i].Id,
      Title = Faker.Lorem.Sentence(3).TrimEnd('.'),
      Content = Faker.Lorem.Sentence(20),
      IsPrivate = n == 0
    });
  }
}

// Every user gets a wallet with the same starting balance.
var wallets = users.Select(u => new { UserId = u.Id, Balance = 1000m }).ToList();

File.WriteAllText("dummy.documents.json", JsonConvert.SerializeObject(documents, Formatting.Indented));
File.WriteAllText("dummy.wallets.json", JsonConvert.SerializeObject(wallets, Formatting.Indented));

Console.WriteLine($"users: {users.Count}, documents: {documents.Count}, wallets: {wallets.Count}");
