using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads wallets from Data/dummy.wallets.json. Does nothing when the table already has rows.
/// </summary>
public class SeedWallet
{
  private record WalletSeed(Guid UserId, decimal Balance);

  /// <summary>Inserts the wallets; rows that belong to unknown users are skipped.</summary>
  public static void Seed(AppDbContext context)
  {
    if (context.Wallets.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.wallets.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<WalletSeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    // Seed data may refer to users that do not exist (e.g. users were regenerated); skip those.
    var userIds = context.Users.Select(u => u.Id).ToHashSet();

    foreach (var s in seeds.Where(s => userIds.Contains(s.UserId)))
    {
      context.Wallets.Add(new WalletEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        UserId = s.UserId,
        Balance = s.Balance
      });
    }

    context.SaveChanges();
  }
}
