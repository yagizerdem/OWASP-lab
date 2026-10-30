namespace backend.Services;

/// <summary>Reads the token from the "Authorization: Bearer" header.</summary>
public static class BearerToken
{
  /// <summary>Returns the bearer token, or an empty string when the header is missing.</summary>
  public static string From(HttpRequest request)
  {
    string header = request.Headers.Authorization.ToString();
    return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..].Trim() : string.Empty;
  }
}
