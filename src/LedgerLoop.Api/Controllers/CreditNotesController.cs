using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Security;
using LedgerLoop.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LedgerLoop.Api.Controllers;

[ApiController]
[Route("api/credit-notes")]
public sealed class CreditNotesController : ControllerBase
{
    private readonly CreditNoteService _creditNotes;
    private readonly ActorContext _actor;

    public CreditNotesController(CreditNoteService creditNotes, ActorContext actor)
    {
        _creditNotes = creditNotes;
        _actor = actor;
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDetailResponse>> Create([FromBody] CreditNoteRequest request, CancellationToken ct)
    {
        var created = await _creditNotes.CreateAsync(request, _actor.Role, ct);
        return Created($"/api/invoices/{created.Id}", created);
    }
}
