using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads role assignments from Data/dummy.roles.json (first user is Admin, the others are User).
/// Does nothing when the table already has rows.
/// </summary>
public class SeedUserRole
{
  private record RoleSeed(Guid UserId, string Role);

  /// <summary>Inserts the roles; rows that belong to unknown users are skipped.</summary>
  public static void Seed(AppDbContext context)
  {
    if (context.UserRoles.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.roles.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<RoleSeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    var userIds = context.Users.Select(u => u.Id).ToHashSet();

    context.UserRoles.AddRange(seeds
      .Where(s => userIds.Contains(s.UserId))
      .Select(s => new UserRoleEntity { Id = Guid.NewGuid(), UserId = s.UserId, Role = s.Role }));

    context.SaveChanges();
  }
}
