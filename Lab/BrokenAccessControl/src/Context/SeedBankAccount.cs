using System.Security.Cryptography;

using Newtonsoft.Json;
using Models.Entity;

public class SeedBankAccount
{

  private static readonly HashSet<string> GeneratedNumbers = new HashSet<string>();

  public static void Seed(AppDbContext context)
  {
    if (context.BankAccounts.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.bankaccounts.json");

    List<BankAccountEntity>? accounts = null;
    if (File.Exists(path))
    {
      var json = File.ReadAllText(path);
      accounts = JsonConvert.DeserializeObject<List<BankAccountEntity>>(json, new JsonSerializerSettings
      {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
      });
    }

    // Owners are seeded by SeedUser; the JSON only carries a copy of each owner,
    // so drop it to stop EF from inserting the user a second time.
    var userIds = context.Users.Select(u => u.Id).ToHashSet();
    accounts = accounts?.Where(a => userIds.Contains(a.OwnerId)).ToList();
    accounts?.ForEach(a => a.owner = null!);

    if (accounts == null || accounts.Count == 0)
    {
      accounts = GenerateRandomBankAccounts(context.Users.ToList());
    }

    Seed(context, accounts);
  }

  public static void Seed(AppDbContext context, List<BankAccountEntity> entitys)
  {
    if (context.BankAccounts.Any()) return;

    context.BankAccounts.AddRange(entitys);
    context.SaveChanges();
  }

  public static List<BankAccountEntity> GenerateRandomBankAccounts(List<UserEntity> users)
  {
    List<BankAccountEntity> bankAccounts = new List<BankAccountEntity>();
    foreach (var user in users)
    {
      BankAccountEntity bankAccount = new BankAccountEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        AccountNumber = GenerateAccountNumber(),
        Balance = Faker.RandomNumber.Next(0, 100000),
        OwnerId = user.Id
      };

      bankAccounts.Add(bankAccount);
    }

    return bankAccounts;
  }

  public static string GenerateAccountNumber()
  {
    string accountNo;
    do
    {
      accountNo = string.Concat(
        Enumerable.Range(0, 16)
            .Select(_ => RandomNumberGenerator.GetInt32(0, 10))
    );
    } while (GeneratedNumbers.Contains(accountNo));
    GeneratedNumbers.Add(accountNo);
    return accountNo;
  }
}
