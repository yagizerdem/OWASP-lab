using Newtonsoft.Json;
using Models.Entity;

public class SeedCustomerRiskProfile
{
  public static void Seed(AppDbContext context)
  {
    if (context.CustomerRiskProfiles.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.customerriskprofiles.json");

    List<CustomerRiskProfileEntity>? riskProfiles = null;
    if (File.Exists(path))
    {
      var json = File.ReadAllText(path);
      riskProfiles = JsonConvert.DeserializeObject<List<CustomerRiskProfileEntity>>(json, new JsonSerializerSettings
      {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
      });
    }

    // Users are seeded by SeedUser; the JSON only carries a copy of each user,
    // so drop it to stop EF from inserting the user a second time.
    var userIds = context.Users.Select(u => u.Id).ToHashSet();
    riskProfiles = riskProfiles?.Where(r => userIds.Contains(r.UserId)).ToList();
    riskProfiles?.ForEach(r => r.User = null!);

    if (riskProfiles == null || riskProfiles.Count == 0)
    {
      riskProfiles = GenerateRandomCustomerRiskProfiles(context.Users.ToList());
    }

    Seed(context, riskProfiles);
  }

  public static void Seed(AppDbContext context, List<CustomerRiskProfileEntity> entitys)
  {
    if (context.CustomerRiskProfiles.Any()) return;

    context.CustomerRiskProfiles.AddRange(entitys);
    context.SaveChanges();
  }

  public static List<CustomerRiskProfileEntity> GenerateRandomCustomerRiskProfiles(List<UserEntity> users)
  {
    List<CustomerRiskProfileEntity> riskProfiles = new List<CustomerRiskProfileEntity>();
    foreach (var user in users)
    {
      CustomerRiskProfileEntity riskProfile = new CustomerRiskProfileEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        UserId = user.Id,
        RiskScore = Faker.RandomNumber.Next(1, 5),
        IsFlagged = Faker.RandomNumber.Next(0, 10) > 7,
        InternalNote = Faker.Lorem.Sentence()
      };

      riskProfiles.Add(riskProfile);
    }

    return riskProfiles;
  }
}
