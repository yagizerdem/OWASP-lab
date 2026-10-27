namespace Models.Entity;

/// <summary>Role names used in the system.</summary>
public static class Roles
{
  public const string Admin = "Admin";
  public const string User = "User";
}

/// <summary>
/// Role assignment kept outside <see cref="UserEntity"/>. A user without any role has no privileges.
/// </summary>
public class UserRoleEntity : BaseEntity
{
  /// <summary>User the role is assigned to.</summary>
  public Guid UserId { get; set; }

  /// <summary>Role name, see <see cref="Roles"/>.</summary>
  public string Role { get; set; } = null!;
}
