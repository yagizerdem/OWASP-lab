using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("lab/[controller]")]
public class WeatherForecastController : ControllerBase
{
  private static readonly string[] Summaries =
  [
      "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
  ];

  [HttpGet(Name = "GetWeatherForecast")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [EndpointSummary("This is a summary.")]
  [EndpointDescription("This is a description.")]
  public ActionResult<IEnumerable<WeatherForecast>> Get([FromRoute] int a, [FromQuery] int b)
  {
    var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
    {
      Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
      TemperatureC = Random.Shared.Next(-20, 55),
      Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    })
    .ToArray();


    if (Random.Shared.Next(0, 2) == 1)
    {
      return NotFound();
    }

    return Ok(forecasts);


  }
}
