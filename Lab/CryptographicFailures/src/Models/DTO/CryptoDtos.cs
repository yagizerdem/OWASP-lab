namespace Models.DTO;

/// <summary>Username and password pair.</summary>
public class CredentialsDto
{
  /// <summary>Login name.</summary>
  public string UserName { get; set; } = null!;

  /// <summary>Plaintext password.</summary>
  public string Password { get; set; } = null!;
}

/// <summary>Data to hash or sign.</summary>
public class MessageDto
{
  /// <summary>Message text.</summary>
  public string Message { get; set; } = null!;

  /// <summary>Algorithm name chosen by the client (the vulnerable endpoints honour it).</summary>
  public string? Algorithm { get; set; }
}

/// <summary>Message plus a signature to verify.</summary>
public class VerifyDto : MessageDto
{
  /// <summary>Signature to check.</summary>
  public string? Signature { get; set; }
}

/// <summary>Text to encrypt or a token to decrypt.</summary>
public class PlainTextDto
{
  /// <summary>Plaintext (encrypt) or ciphertext/token (decrypt).</summary>
  public string Text { get; set; } = null!;
}

/// <summary>Text protected with a key derived from a password.</summary>
public class PasswordProtectDto
{
  /// <summary>Password the key is (insecurely or securely) derived from.</summary>
  public string Password { get; set; } = null!;

  /// <summary>Plaintext (encrypt) or ciphertext (decrypt).</summary>
  public string Text { get; set; } = null!;
}

/// <summary>Start of a password reset.</summary>
public class ResetRequestDto
{
  /// <summary>Account to reset.</summary>
  public string UserName { get; set; } = null!;
}

/// <summary>Completion of a password reset.</summary>
public class ResetConfirmDto
{
  /// <summary>Account to reset.</summary>
  public string UserName { get; set; } = null!;

  /// <summary>Token or code received out of band.</summary>
  public string Token { get; set; } = null!;

  /// <summary>New password.</summary>
  public string NewPassword { get; set; } = null!;
}

/// <summary>New payment card.</summary>
public class CardCreateDto
{
  /// <summary>Owner login name.</summary>
  public string OwnerName { get; set; } = null!;

  /// <summary>Full card number.</summary>
  public string CardNumber { get; set; } = null!;
}

/// <summary>Session request.</summary>
public class SessionRequestDto
{
  /// <summary>User the session is for.</summary>
  public string UserName { get; set; } = null!;
}
