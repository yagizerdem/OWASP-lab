using System.Security.Cryptography;
using System.Text;
using Models.Crypto;
using Models.Entity;
using Newtonsoft.Json;

// The weak hash users are reused when Context/Data/dummy.weakhashes.json already exists (the other tables refer to
// their user names). Pass --regen-users to generate a fresh set.
const string existingUsersFile = "../../Context/Data/dummy.weakhashes.json";
bool regenUsers = args.Contains("--regen-users");

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

List<WeakHashEntity> weakHashEntities;
if (!regenUsers && File.Exists(existingUsersFile))
{
  weakHashEntities = JsonConvert.DeserializeObject<List<WeakHashEntity>>(File.ReadAllText(existingUsersFile)) ?? new();
  Console.WriteLine($"Reusing {weakHashEntities.Count} users from {existingUsersFile}.");
}
else
{
  weakHashEntities = GenerateRandomWeakHashEntities();
  File.WriteAllText("dummy.weakhashes.json", JsonConvert.SerializeObject(weakHashEntities));
  Console.WriteLine($"dummy.weakhashes.json written ({weakHashEntities.Count} rows). Copy it to Context/Data.");
  Console.WriteLine("Plaintext password pool (answer key for the lab): " + string.Join(", ", passwords));
}

// Compliant credentials for the first 20 users (same password as the weak table, so both logins can be compared).
// PBKDF2 is slow on purpose, hence not all 100 users.
var users = weakHashEntities.Take(20).ToList();
var secureCredentials = users.Select(u =>
{
  var (hash, salt, iterations) = Pbkdf2PasswordHasher.Hash(u.PlaintextPassword);
  return new SecureCredentialEntity
  {
    Id = Guid.NewGuid(),
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    UserName = u.UserName,
    PasswordHash = hash,
    Salt = salt,
    Iterations = iterations,
    Algorithm = Pbkdf2PasswordHasher.AlgorithmName
  };
}).ToList();
File.WriteAllText("dummy.securecredentials.json", JsonConvert.SerializeObject(secureCredentials));

// Fake, Luhn valid 16 digit card numbers (4111... style test range) for the same 20 users.
string GenerateCardNumber()
{
  int[] digits = new int[16];
  digits[0] = 4;
  for (int i = 1; i < 15; i++) digits[i] = Random.Shared.Next(0, 10);

  int sum = 0;
  for (int i = 0; i < 15; i++)
  {
    int d = digits[14 - i];
    if (i % 2 == 0) { d *= 2; if (d > 9) d -= 9; }
    sum += d;
  }

  digits[15] = (10 - sum % 10) % 10;
  return string.Concat(digits);
}

var cards = users.Select(u => new { OwnerName = u.UserName, CardNumber = GenerateCardNumber() }).ToList();

// The same numbers go to both tables so the plaintext and the encrypted version can be compared.
File.WriteAllText("dummy.legacycards.json", JsonConvert.SerializeObject(cards.Select(c => new LegacyCardEntity
{
  Id = Guid.NewGuid(),
  CreatedAt = DateTime.UtcNow,
  UpdatedAt = DateTime.UtcNow,
  OwnerName = c.OwnerName,
  CardNumber = c.CardNumber
})));
File.WriteAllText("dummy.protectedcards.json", JsonConvert.SerializeObject(cards));

Console.WriteLine("Written: dummy.securecredentials.json, dummy.legacycards.json, dummy.protectedcards.json. Copy them to Context/Data.");
