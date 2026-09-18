using LedgerLoop.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LedgerLoop.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly LedgerLoopDbContext _db;

    public HealthController(LedgerLoopDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var database = "unavailable";

        try
        {
            database = await _db.Database.CanConnectAsync(ct) ? "up" : "down";
        }
        catch (Exception)
        {
            database = "down";
        }

        var payload = new
        {
            status = database == "up" ? "healthy" : "degraded",
            database,
            version = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            timestamp = DateTime.UtcNow
        };

        return database == "up" ? Ok(payload) : StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
    }
}
