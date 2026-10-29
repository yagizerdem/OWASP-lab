using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads sample documents from Data/dummy.documents.json. Does nothing when the table already has rows.
/// </summary>
public class SeedDocument
{
  private record DocumentSeed(Guid OwnerId, string Title, string Content, bool IsPrivate);

  /// <summary>Inserts the documents; rows that belong to unknown users are skipped.</summary>
  public static void Seed(AppDbContext context)
  {
    if (context.Documents.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.documents.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<DocumentSeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    // Seed data may refer to users that do not exist (e.g. users were regenerated); skip those.
    var userIds = context.Users.Select(u => u.Id).ToHashSet();

    foreach (var s in seeds.Where(s => userIds.Contains(s.OwnerId)))
    {
      context.Documents.Add(new DocumentEntity()
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        OwnerId = s.OwnerId,
        Title = s.Title,
        Content = s.Content,
        IsPrivate = s.IsPrivate
      });
    }

    context.SaveChanges();
  }
}
