using System.Security.Cryptography;
using System.Text;
using Models.Entity;
using Newtonsoft.Json;

// A small shared password pool: several users end up with the same password, so the unsalted MD5 hashes collide
// (that is the weakness the lab demonstrates).
List<string> passwords = Enumerable.Range(1, 10)
  .Select(_ => $"pass{Faker.RandomNumber.Next(10000, 1000000)}")
  .ToList();

List<WeakHashEntity> GenerateRandomWeakHashEntities(int count = 100)
{
  List<WeakHashEntity> users = new List<WeakHashEntity>();
  for (int i = 0; i < count; i++)
  {
    // Weak on purpose: unsalted MD5, lowercase hex.
    string password = passwords[Random.Shared.Next(passwords.Count)];
    string hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();

    users.Add(new WeakHashEntity()
    {
      Id = Guid.NewGuid(),
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      UserName = Faker.Internet.UserName(),
      HashedPassword = hash,
      PlaintextPassword = password
    });
  }

  return users;
}

List<WeakHashEntity> weakHashEntities = GenerateRandomWeakHashEntities();
File.WriteAllText("dummy.weakhashes.json", JsonConvert.SerializeObject(weakHashEntities));

Console.WriteLine($"dummy.weakhashes.json written ({weakHashEntities.Count} rows). Copy it to Context/Data.");
Console.WriteLine("Plaintext password pool (answer key for the lab): " + string.Join(", ", passwords));
