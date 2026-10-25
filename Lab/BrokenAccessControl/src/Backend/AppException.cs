using System.Net;

/// <summary>
/// Expected, business-level error that maps directly to an HTTP status code.
/// </summary>
public class AppException : Exception
{
  public HttpStatusCode Status { get; }

  public bool isOperational { get; }

  public AppException(string message,
  HttpStatusCode status = HttpStatusCode.BadRequest,
  bool isOperational = true)
      : base(message)
  {
    Status = status;
    this.isOperational = isOperational;
  }

  public static AppException NotFound(string message = "Not found")
      => new(message, HttpStatusCode.NotFound);

  public static AppException Unauthorized(string message = "Unauthorized")
      => new(message, HttpStatusCode.Unauthorized);

  public static AppException Forbidden(string message = "Forbidden")
      => new(message, HttpStatusCode.Forbidden);

  public static AppException InternalServerError(string message = "Internal server error")
      => new(message, HttpStatusCode.InternalServerError, false);
}
