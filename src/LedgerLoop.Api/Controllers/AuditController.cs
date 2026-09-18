using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LedgerLoop.Api.Controllers;

[ApiController]
[Route("api/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly AuditExportService _export;

    public AuditController(AuditExportService export)
    {
        _export = export;
    }

    [HttpGet("export")]
    public async Task<ActionResult<AuditExportResponse>> Export(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct) =>
        Ok(await _export.ExportAsync(from, to, ct));
}
