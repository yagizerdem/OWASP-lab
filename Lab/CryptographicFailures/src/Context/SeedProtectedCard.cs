using Newtonsoft.Json;
using Models.Crypto;
using Models.Entity;

/// <summary>
/// Seeds encrypted payment cards. The JSON file holds the fake plaintext numbers; they are encrypted through the key ring
/// at seed time, so no ciphertext and no key is ever committed to the repository.
/// </summary>
public class SeedProtectedCard
{
  private class CardRow
  {
    public string OwnerName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
  }

  /// <summary>Loads <c>Data/dummy.protectedcards.json</c>, encrypts every number with <paramref name="cipher"/> and inserts the rows when the table is empty.</summary>
  public static void Seed(AppDbContext context, IFieldCipher cipher)
  {
    if (context.ProtectedCards.Any()) return;

    var path = Path.Combine(AppContext.BaseDirectory, "Data", "dummy.protectedcards.json");
    if (!File.Exists(path)) return;

    var rows = JsonConvert.DeserializeObject<List<CardRow>>(File.ReadAllText(path));
    if (rows == null || rows.Count == 0) return;

    var entities = rows.Select(r =>
    {
      EncryptedField field = cipher.Encrypt(r.CardNumber);
      return new ProtectedCardEntity
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        OwnerName = r.OwnerName,
        Last4 = r.CardNumber[^4..],
        KeyId = field.KeyId,
        Nonce = field.Nonce,
        CipherText = field.CipherText
      };
    }).ToList();

    Seed(context, entities);
  }

  /// <summary>Inserts the given rows when the table is empty.</summary>
  public static void Seed(AppDbContext context, List<ProtectedCardEntity> entities)
  {
    if (context.ProtectedCards.Any()) return;

    context.ProtectedCards.AddRange(entities);
    context.SaveChanges();
  }
}
