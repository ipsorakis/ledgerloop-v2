using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Security;
using LedgerLoop.Api.Services;
using LedgerLoop.Api.Tax;
using Microsoft.AspNetCore.Mvc;

namespace LedgerLoop.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly InvoiceService _invoices;
    private readonly InvoicePostingService _posting;
    private readonly ActorContext _actor;
    private readonly TaxSettings _settings;

    public InvoicesController(
        InvoiceService invoices,
        InvoicePostingService posting,
        ActorContext actor,
        TaxSettings settings)
    {
        _invoices = invoices;
        _posting = posting;
        _actor = actor;
        _settings = settings;
    }

    [HttpGet]
    public async Task<ActionResult<List<InvoiceSummaryResponse>>> List([FromQuery] string? status, CancellationToken ct) =>
        Ok(await _invoices.ListAsync(status, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDetailResponse>> Get(Guid id, CancellationToken ct)
    {
        var invoice = await _invoices.GetAsync(id, ct);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDetailResponse>> CreateDraft([FromBody] DraftInvoiceRequest request, CancellationToken ct)
    {
        var created = await _invoices.CreateDraftAsync(request, _actor.Role, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoiceDetailResponse>> UpdateDraft(Guid id, [FromBody] DraftInvoiceRequest request, CancellationToken ct) =>
        Ok(await _invoices.UpdateDraftAsync(id, request, _actor.Role, ct));

    /// <summary>
    /// Authoritative preview for a document that has not been saved yet.
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<TaxResult>> Preview([FromBody] DraftInvoiceRequest request, CancellationToken ct) =>
        Ok(await _invoices.PreviewDraftAsync(request, ct));

    [HttpGet("{id:guid}/preview")]
    public async Task<ActionResult<TaxResult>> PreviewSaved(Guid id, CancellationToken ct) =>
        Ok(await _invoices.PreviewAsync(id, ct));

    /// <summary>
    /// Totals endpoint used by the 2021 desktop client, which sends the figures
    /// it already showed to the operator. Server figures win; the submitted
    /// values are only checked for drift.
    /// </summary>
    [HttpPost("{id:guid}/totals-legacy")]
    public async Task<ActionResult<LegacyTotalsResponse>> LegacyTotals(Guid id, [FromBody] LegacyTotalsRequest request, CancellationToken ct)
    {
        var totals = await _invoices.PreviewAsync(id, ct);

        var clientTotal = request.ClientTaxTotal ?? request.Lines.Sum(l => l.TaxAmount);

        foreach (var line in request.Lines)
        {
            var server = totals.Lines.FirstOrDefault(l => l.LineNumber == line.LineNumber);
            if (server is null)
            {
                throw new DomainValidationException($"Line {line.LineNumber} is not part of {totals.DocumentNumber}.");
            }

            if (Math.Abs(server.TaxAmount - line.TaxAmount) > _settings.ClientTaxTolerance)
            {
                throw new DomainValidationException(
                    $"Line {line.LineNumber} tax {line.TaxAmount:0.00} differs from the ledger value {server.TaxAmount:0.00}.");
            }
        }

        return Ok(new LegacyTotalsResponse
        {
            ClientValuesAccepted = request.Lines.Count > 0,
            ClientTaxTotal = clientTotal,
            ServerTaxTotal = totals.TaxAmount,
            Totals = totals
        });
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<TaxResult>> Post(Guid id, CancellationToken ct) =>
        Ok(await _posting.PostAsync(id, _actor.Role, ct));
}
