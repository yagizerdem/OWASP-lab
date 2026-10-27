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
      Email = Faker.Internet.Email()
    };

    users.Add(user);
  }

  return users;
}

// Notes/roles refer to user ids, so existing users are reused unless --regen-users is passed.
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

// users[0] is the admin. Everybody else is a plain user.
var roles = users.Select((u, i) => new { UserId = u.Id, Role = i == 0 ? Roles.Admin : Roles.User }).ToList();

// users[0] is exactly at the per-user note limit, users[1] and users[2] are below it (limit tests).
int[] fixedCounts = { NoteEntity.MaxPerUser, 2, 2 };
var notes = new List<object>();
for (int i = 0; i < Math.Min(users.Count, 20); i++)
{
  int count = i < fixedCounts.Length ? fixedCounts[i] : Random.Shared.Next(0, NoteEntity.MaxPerUser);
  for (int n = 0; n < count; n++)
  {
    notes.Add(new
    {
      OwnerId = users[i].Id,
      Title = Faker.Lorem.Sentence(3).TrimEnd('.'),
      Content = Faker.Lorem.Sentence(12)
    });
  }
}

// Notes for the vulnerable endpoints: 3 per user for the first 10 users, no limit logic involved.
var legacyNotes = new List<object>();
for (int i = 0; i < Math.Min(users.Count, 10); i++)
{
  for (int n = 0; n < 3; n++)
  {
    legacyNotes.Add(new
    {
      Id = Guid.NewGuid(),
      OwnerId = users[i].Id,
      Title = Faker.Lorem.Sentence(3).TrimEnd('.'),
      Content = Faker.Lorem.Sentence(12),
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    });
  }
}

File.WriteAllText("dummy.legacynotes.json", JsonConvert.SerializeObject(legacyNotes, Formatting.Indented));
File.WriteAllText("dummy.roles.json", JsonConvert.SerializeObject(roles, Formatting.Indented));
File.WriteAllText("dummy.notes.json", JsonConvert.SerializeObject(notes, Formatting.Indented));

Console.WriteLine($"users: {users.Count}, roles: {roles.Count}, notes: {notes.Count}");
