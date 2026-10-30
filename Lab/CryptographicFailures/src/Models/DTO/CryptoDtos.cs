namespace Models.DTO;

/// <summary>Username and password pair.</summary>
public class CredentialsDto
{
  public string UserName { get; set; } = null!;

  public string Password { get; set; } = null!;
}

/// <summary>Start of a password reset.</summary>
public class ResetRequestDto
{
  public string UserName { get; set; } = null!;
}

/// <summary>Completion of a password reset.</summary>
public class ResetConfirmDto
{
  public string UserName { get; set; } = null!;

  /// <summary>Code or token received out of band.</summary>
  public string Token { get; set; } = null!;

  public string NewPassword { get; set; } = null!;
}

/// <summary>New payment card.</summary>
public class CardCreateDto
{
  public string OwnerName { get; set; } = null!;

  public string CardNumber { get; set; } = null!;
}
