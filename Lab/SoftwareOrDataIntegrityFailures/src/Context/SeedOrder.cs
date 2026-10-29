using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads pending orders from Data/dummy.orders.json. Does nothing when the table already has rows.
/// </summary>
public class SeedOrder
{
  private record OrderSeed(Guid UserId, decimal Amount);

  /// <summary>Inserts the orders as Pending; rows that belong to unknown users are skipped.</summary>
  public static void Seed(AppDbContext context)
  {
    if (context.Orders.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.orders.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<OrderSeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    // Seed data may refer to users that do not exist (e.g. users were regenerated); skip those.
    var userIds = context.Users.Select(u => u.Id).ToHashSet();

    foreach (var s in seeds.Where(s => userIds.Contains(s.UserId)))
    {
      context.Orders.Add(new OrderEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        UserId = s.UserId,
        Amount = s.Amount,
        Status = "Pending"
      });
    }

    context.SaveChanges();
  }
}
