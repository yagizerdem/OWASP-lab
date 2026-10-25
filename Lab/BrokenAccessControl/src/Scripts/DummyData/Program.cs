using System.Security.Cryptography;
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


List<CustomerRiskProfileEntity> GenerateRandomCustomerRiskProfiles(List<UserEntity> users)
{
  List<CustomerRiskProfileEntity> riskProfiles = new List<CustomerRiskProfileEntity>();
  for (int i = 0; i < users.Count; i++)
  {
    var user = users[i];
    CustomerRiskProfileEntity riskProfile = new CustomerRiskProfileEntity()
    {
      Id = Guid.NewGuid(),
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
      UserId = user.Id,
      User = user,
      RiskScore = Faker.RandomNumber.Next(1, 5),  // Assuming risk level is an integer between 1 and 4
      IsFlagged = Faker.RandomNumber.Next(0, 10) > 7,  // Randomly flagged as true or false
      InternalNote = Faker.Lorem.Sentence()
    }
    ;

    riskProfiles.Add(riskProfile);
  }

  return riskProfiles;
}

var customerRiskProfiles = GenerateRandomCustomerRiskProfiles(users);

var customerRiskProfilesJson = JsonConvert.SerializeObject(customerRiskProfiles);
File.WriteAllText("dummy.customerriskprofiles.json", customerRiskProfilesJson);


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
