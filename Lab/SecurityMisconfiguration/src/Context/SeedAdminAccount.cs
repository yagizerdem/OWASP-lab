using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads back-office accounts from Data/dummy.admins.json. Does nothing when the table already has rows.
/// </summary>
public class SeedAdminAccount
{
  private record AdminSeed(string Username, string Password, string Role, bool IsDefaultPassword);

  public static void Seed(AppDbContext context)
  {
    if (context.AdminAccounts.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.admins.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<AdminSeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    foreach (var s in seeds)
    {
      context.AdminAccounts.Add(new AdminAccountEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Username = s.Username,
        Password = s.Password,
        Role = s.Role,
        IsDefaultPassword = s.IsDefaultPassword
      });
    }

    context.SaveChanges();
  }
}
