using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads sample blog posts from Data/dummy.blogs.json. Does nothing when the table already has rows.
/// </summary>
public class SeedBlog
{
  private record BlogSeed(Guid UserId, string Title, string Content);

  /// <summary>Inserts the blogs; rows that belong to unknown users are skipped.</summary>
  public static void Seed(AppDbContext context)
  {
    if (context.Blogs.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.blogs.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<BlogSeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    // Seed data may refer to users that do not exist (e.g. users were regenerated); skip those.
    var userIds = context.Users.Select(u => u.Id).ToHashSet();

    foreach (var s in seeds.Where(s => userIds.Contains(s.UserId)))
    {
      context.Blogs.Add(new BlogEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        AuthorId = s.UserId,
        Title = s.Title,
        Content = s.Content
      });
    }

    context.SaveChanges();
  }
}
