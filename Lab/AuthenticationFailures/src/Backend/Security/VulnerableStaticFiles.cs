namespace backend.Security;

/// <summary>
/// VULNERABLE BY DESIGN. Prepares the folder served (with directory listing) at <see cref="RequestPath"/>.
/// <para>
/// Violates rule 5: directory listing is enabled and the folder contains repository metadata (<c>.git</c>), a
/// configuration backup and a database dump. All content is fake lab data generated at startup, so nothing sensitive
/// is ever committed to the repository.
/// </para>
/// </summary>
public static class VulnerableStaticFiles
{
  /// <summary>URL prefix of the vulnerable static file server.</summary>
  public const string RequestPath = "/vuln-public";

  /// <summary>Creates the folder and the fake sensitive files (idempotent).</summary>
  /// <param name="root">Physical folder to serve.</param>
  public static void Prepare(string root)
  {
    Directory.CreateDirectory(Path.Combine(root, ".git"));

    File.WriteAllText(Path.Combine(root, ".git", "HEAD"), "ref: refs/heads/main\n");
    File.WriteAllText(Path.Combine(root, ".git", "config"),
      "[core]\n\trepositoryformatversion = 0\n[remote \"origin\"]\n\turl = https://example.invalid/owasp-lab.git\n");
    File.WriteAllText(Path.Combine(root, "appsettings.json.bak"),
      "{ \"ConnectionStrings\": { \"DefaultConnection\": \"Server=db;Database=lab;User Id=sa;Password=fake-backup-password\" } }\n");
    File.WriteAllText(Path.Combine(root, "db-backup.sql.old"), "-- fake dump\nINSERT INTO Users VALUES ('demo');\n");
    File.WriteAllText(Path.Combine(root, "readme.txt"), "Public folder.\n");
  }
}
