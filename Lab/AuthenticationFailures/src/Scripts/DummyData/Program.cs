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

var users = GenerateRandomUsers();

var json = JsonConvert.SerializeObject(users);

Console.WriteLine(json);
File.WriteAllText("dummy.users.json", json);
