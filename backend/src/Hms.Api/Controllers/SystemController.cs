using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SystemController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public SystemController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var nowUtc = DateTime.UtcNow;
        var dhakaZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka");
        var dhakaTime = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, dhakaZone);

        return Ok(new
        {
            Product = "Hospital Management System (HMS)",
            Version = "1.0.0",
            Environment = _env.EnvironmentName,
            Framework = ".NET 9.0",
            ServerTimeUtc = nowUtc.ToString("o"),
            ServerTimeDhaka = dhakaTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC+06:00'")
        });
    }
}
