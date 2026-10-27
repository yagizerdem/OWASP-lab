using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads sample notes from Data/dummy.notes.json. Notes are added through <c>NoteEntity.Create</c>,
/// so the per-user limit also applies to seed data. Does nothing when the table already has rows.
/// </summary>
public class SeedNote
{
  private record NoteSeed(Guid OwnerId, string Title, string Content);

  /// <summary>Inserts the notes; rows that belong to unknown users are skipped.</summary>
  public static void Seed(AppDbContext context)
  {
    if (context.Notes.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.notes.json");
    if (!File.Exists(path)) return;

    var seeds = JsonConvert.DeserializeObject<List<NoteSeed>>(File.ReadAllText(path));
    if (seeds == null || seeds.Count == 0) return;

    // Seed data may refer to users that do not exist (e.g. users were regenerated); skip those.
    var userIds = context.Users.Select(u => u.Id).ToHashSet();
    var perOwner = new Dictionary<Guid, int>();

    foreach (var s in seeds.Where(s => userIds.Contains(s.OwnerId)))
    {
      perOwner.TryGetValue(s.OwnerId, out int count);
      // Goes through the domain model, so the per-user limit also applies to seed data.
      context.Notes.Add(NoteEntity.Create(s.OwnerId, s.Title, s.Content, count));
      perOwner[s.OwnerId] = count + 1;
    }

    context.SaveChanges();
  }
}
