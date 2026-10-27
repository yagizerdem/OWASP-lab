
using System.Net;

public class ApiResponse
{
  public string Message { get; set; } = string.Empty;
  public object? Data { get; set; }
  public HttpStatusCode Status { get; set; }

  public static ApiResponse Ok(object? data = null, string message = "Success")
      => new() { Message = message, Data = data, Status = HttpStatusCode.OK };

  public static ApiResponse Created(object? data = null, string message = "Created")
      => new() { Message = message, Data = data, Status = HttpStatusCode.Created };

  public static ApiResponse Fail(string message, HttpStatusCode status = HttpStatusCode.BadRequest, object? data = null)
      => new() { Message = message, Data = data, Status = status };

  public static ApiResponse NotFound(string message = "Not found")
      => Fail(message, HttpStatusCode.NotFound);

  public static ApiResponse Unauthorized(string message = "Unauthorized")
      => Fail(message, HttpStatusCode.Unauthorized);

  public static ApiResponse Forbidden(string message = "Forbidden")
      => Fail(message, HttpStatusCode.Forbidden);

  public static ApiResponse InternalServerError(string message = "Internal server error")
      => Fail(message, HttpStatusCode.InternalServerError);
}
