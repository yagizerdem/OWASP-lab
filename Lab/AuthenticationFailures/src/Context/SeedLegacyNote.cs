using Newtonsoft.Json;
using Models.Entity;

/// <summary>
/// Loads sample notes for the vulnerable endpoints from Data/dummy.legacynotes.json.
/// Does nothing when the table already has rows.
/// </summary>
public class SeedLegacyNote
{
  /// <summary>Inserts the notes whose owner exists; rows for unknown users are skipped.</summary>
  public static void Seed(AppDbContext context)
  {
    if (context.LegacyNotes.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.legacynotes.json");
    if (!File.Exists(path)) return;

    var notes = JsonConvert.DeserializeObject<List<LegacyNoteEntity>>(File.ReadAllText(path));
    if (notes == null || notes.Count == 0) return;

    var userIds = context.Users.Select(u => u.Id).ToHashSet();
    context.LegacyNotes.AddRange(notes.Where(n => userIds.Contains(n.OwnerId)));
    context.SaveChanges();
  }
}
