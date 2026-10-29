using System.Security.Cryptography;
using System.Text;
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

// Orders refer to user ids, so existing users are reused unless --regen-users is passed.
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

// 2 pending orders for each of the first 20 users.
var orders = new List<object>();
for (int i = 0; i < Math.Min(users.Count, 20); i++)
{
  for (int n = 0; n < 2; n++)
  {
    orders.Add(new
    {
      UserId = users[i].Id,
      Amount = (decimal)Faker.RandomNumber.Next(10, 500)
    });
  }
}

// Official plugin packages; the manifest stores the SHA-256 of the exact bytes.
object TrustedPlugin(string name, string version, string content)
{
  byte[] bytes = Encoding.UTF8.GetBytes(content);
  return new
  {
    Name = name,
    Version = version,
    Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
    ContentBase64 = Convert.ToBase64String(bytes)
  };
}

var trustedPlugins = new List<object>
{
  TrustedPlugin("report-export", "1.0.0", "export function run(data) { return toCsv(data); }"),
  TrustedPlugin("theme-dark", "2.1.0", "export const theme = { background: '#111', text: '#eee' };")
};

File.WriteAllText("dummy.orders.json", JsonConvert.SerializeObject(orders, Formatting.Indented));
File.WriteAllText("dummy.trustedplugins.json", JsonConvert.SerializeObject(trustedPlugins, Formatting.Indented));

Console.WriteLine($"users: {users.Count}, orders: {orders.Count}, trusted plugins: {trustedPlugins.Count}");
