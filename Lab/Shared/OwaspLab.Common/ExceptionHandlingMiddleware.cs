using Newtonsoft.Json;
using System.Net;

namespace Middlewares.Exceptions
{
  public class ExceptionHandlingMiddleware : IMiddleware
  {
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
      try
      {
        await next(httpContext);
      }
      catch (Exception ex)
      {
        await HandleException(httpContext, ex);
      }
    }

    private Task HandleException(HttpContext context, Exception ex)
    {

      if (ex is AppException appException)
      {
        context.Response.StatusCode = (int)appException.Status;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(JsonConvert.SerializeObject(
          new ApiResponse()
          {
            Data = null,
            Message = appException.isOperational ? appException.Message : "Internal server error",
            Status = appException.Status
          }

        ));
      }

      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
      context.Response.ContentType = "application/json";
      return context.Response.WriteAsync(JsonConvert.SerializeObject(
        new ApiResponse()
        {
          Data = null,
          Message = "Internal server error",
          Status = HttpStatusCode.InternalServerError
        }
      ));

    }

  }

}
