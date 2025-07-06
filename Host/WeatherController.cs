using Host.Personal;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public partial class WeatherController : ControllerBase
{
    [HttpGet]
    [Route("weatherforecast")]
    public partial IActionResult Get()
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
        new
        {
            Time = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Temperature = Random.Shared.Next(-20, 55),
        })
        .ToArray();
        int value = 2;
        return Ok(new { Item = 2, Object = new UserDefiendObject(), value });
    }

    [HttpGet]
    [Route("get-some-object")]
    public partial IActionResult GetResult()
    {
        return Ok(new { Integer = 1, Object = new UserDefiendObject() });
    }

    [HttpGet]
    [Route("get-some-object2")]
    public partial IActionResult GetResult2()
    {
        var @object = new UserDefiendObject();
        return Ok(@object);
    }

    [HttpPost]
    [Route("test")]
    public partial IActionResult TestMethod()
    {
        if (Random.Shared.NextDouble() > 0.5)
        {
            return Ok(new
            {
                Item1 = 1,
                Item2 = 2,
                Schema = new UserDefiendObject()
            });
        }

        // when generate dto for badrequest,
        // should have no 'Schema' property
        return BadRequest(new
        {
            Item1 = 1,
            Item2 = 2,
        });
    }
}


namespace Host.Personal
{
    public record UserDefiendObject
    {
        public int Item { get; set; }
        public int Temperature { get; set; }
    }
}