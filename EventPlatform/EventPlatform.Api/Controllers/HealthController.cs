using EventPlatform.Api.DbContexts;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(AppDbContext _context) : ControllerBase
{

    [HttpGet]
    public IActionResult Check()
    {
        var canConnect = _context.Database.CanConnect();
        return canConnect
            ? Ok("Connected to database")
            : StatusCode(500, "Cannot connect to database");
    }
}
