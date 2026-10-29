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

// The hash of a package is the SHA-256 of a stand-in content string, so every package version and
// source has its own stable hash.
string Hash(string name, string version, string source) =>
  Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{source}:{name}@{version}"))).ToLowerInvariant();

object Package(string name, string version, string source) =>
  new { Name = name, Version = version, Source = source, Sha256 = Hash(name, version, source) };

// acme-* are internal company packages. Someone published look-alikes on the public registry:
// acme-logger with a huge version number and acme-auth with the same version but different content.
var packages = new List<object>
{
  Package("acme-logger", "1.2.0", "internal"),
  Package("acme-logger", "1.3.0", "internal"),
  Package("acme-logger", "99.0.0", "public"),
  Package("acme-auth", "2.0.1", "internal"),
  Package("acme-auth", "2.0.1", "public"),
  Package("json-parser", "3.1.0", "public"),
  Package("json-parser", "3.2.0", "public")
};

// The lockfile pins reviewed versions and the hash of the internal / reviewed build.
var lockEntries = new List<object>
{
  new { Name = "acme-logger", Version = "1.3.0", Sha256 = Hash("acme-logger", "1.3.0", "internal") },
  new { Name = "acme-auth", Version = "2.0.1", Sha256 = Hash("acme-auth", "2.0.1", "internal") },
  new { Name = "json-parser", Version = "3.1.0", Sha256 = Hash("json-parser", "3.1.0", "public") }
};

// The application's SBOM.
var components = new List<object>
{
  new { Name = "acme-logger", Version = "1.3.0" },
  new { Name = "json-parser", Version = "3.1.0" },
  new { Name = "image-resize", Version = "0.9.2" },
  new { Name = "http-client", Version = "4.0.0" },
  new { Name = "yaml-reader", Version = "1.4.0" }
};

// Fictional advisories (LAB-* ids, not real CVEs).
var advisories = new List<object>
{
  new { AdvisoryId = "LAB-2026-0001", PackageName = "image-resize", FixedVersion = "1.0.0", Severity = "Critical", Summary = "Crafted image file executes code during decoding" },
  new { AdvisoryId = "LAB-2026-0002", PackageName = "yaml-reader", FixedVersion = "1.5.0", Severity = "High", Summary = "Unsafe type loading when reading YAML documents" },
  new { AdvisoryId = "LAB-2026-0003", PackageName = "http-client", FixedVersion = "4.0.1", Severity = "Low", Summary = "Verbose error messages include request headers" },
  new { AdvisoryId = "LAB-2026-0004", PackageName = "json-parser", FixedVersion = "3.0.0", Severity = "Medium", Summary = "Deeply nested input causes excessive memory use" }
};

File.WriteAllText("dummy.packages.json", JsonConvert.SerializeObject(packages, Formatting.Indented));
File.WriteAllText("dummy.lock.json", JsonConvert.SerializeObject(lockEntries, Formatting.Indented));
File.WriteAllText("dummy.components.json", JsonConvert.SerializeObject(components, Formatting.Indented));
File.WriteAllText("dummy.advisories.json", JsonConvert.SerializeObject(advisories, Formatting.Indented));

Console.WriteLine($"users: {users.Count}, packages: {packages.Count}, lock: {lockEntries.Count}, components: {components.Count}, advisories: {advisories.Count}");
