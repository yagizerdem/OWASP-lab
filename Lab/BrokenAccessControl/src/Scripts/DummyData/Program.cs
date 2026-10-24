using System.Security.Cryptography;

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

List<BankAccountEntity> GenerateRandomBankAccounts(List<UserEntity> users)
{
  List<BankAccountEntity> bankAccounts = new List<BankAccountEntity>();
  for (int i = 0; i < users.Count; i++)
  {
    var user = users[i];
    BankAccountEntity bankAccount = new BankAccountEntity()
    {
      Id = Guid.NewGuid(),
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      AccountNumber = BankAccountGenerator.GenerateAccountNumber(),
      Balance = Faker.RandomNumber.Next(0, 100000),
      OwnerId = user.Id,
      owner = user
    };

    bankAccounts.Add(bankAccount);
  }

  return bankAccounts;
}

var bankAccounts = GenerateRandomBankAccounts(users);

var bankAccountsJson = JsonConvert.SerializeObject(bankAccounts);

// Console.WriteLine(bankAccountsJson);
File.WriteAllText("dummy.bankaccounts.json", bankAccountsJson);


public static class BankAccountGenerator
{
  public static string GenerateAccountNumber()
  {
    return string.Concat(
        Enumerable.Range(0, 16)
            .Select(_ => RandomNumberGenerator.GetInt32(0, 10))
    );
  }
}
