using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Data;
using LedgerLoop.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LedgerLoop.Api.Services;

public sealed class CreditNoteService
{
    private readonly LedgerLoopDbContext _db;
    private readonly InvoiceService _invoices;
    private readonly DocumentNumberGenerator _numbers;

    public CreditNoteService(LedgerLoopDbContext db, InvoiceService invoices, DocumentNumberGenerator numbers)
    {
        _db = db;
        _invoices = invoices;
        _numbers = numbers;
    }

    public async Task<InvoiceDetailResponse> CreateAsync(CreditNoteRequest request, string? actorRole, CancellationToken ct)
    {
        var source = await _invoices.LoadAsync(request.InvoiceId, ct)
            ?? throw new DocumentNotFoundException(request.InvoiceId);

        if (source.DocumentKind == DocumentKind.CreditNote)
        {
            throw new DomainValidationException($"{source.InvoiceNumber} is already a credit note.");
        }

        if (source.Status != InvoiceStatus.Posted)
        {
            throw new DomainValidationException($"{source.InvoiceNumber} must be posted before it can be credited.");
        }

        var lines = source.Lines
            .Where(l => request.LineNumbers.Count == 0 || request.LineNumbers.Contains(l.LineNumber))
            .OrderBy(l => l.LineNumber)
            .ToList();

        if (lines.Count == 0)
        {
            throw new DomainValidationException("None of the requested line numbers exist on the source invoice.");
        }

        var creditNote = new Invoice
        {
            CustomerId = source.CustomerId,
            Customer = source.Customer,
            DocumentKind = DocumentKind.CreditNote,
            Status = InvoiceStatus.Draft,
            PriceMode = source.PriceMode,
            InvoiceDate = request.CreditNoteDate ?? source.InvoiceDate,
            Currency = source.Currency,
            InvoiceDiscountPercent = source.InvoiceDiscountPercent,
            OriginalInvoiceId = source.Id,
            Notes = request.Reason
        };

        creditNote.InvoiceNumber = await _numbers.NextAsync(DocumentKind.CreditNote, creditNote.InvoiceDate, ct);

        var number = 1;
        foreach (var line in lines)
        {
            creditNote.Lines.Add(new InvoiceLine
            {
                InvoiceId = creditNote.Id,
                LineNumber = number++,
                Description = line.Description,
                Category = line.Category,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineDiscountPercent = line.LineDiscountPercent
            });
        }

        _invoices.ApplyTotals(creditNote);

        _db.Invoices.Add(creditNote);
        _db.AuditEvents.Add(new AuditEvent
        {
            InvoiceId = creditNote.Id,
            EventType = "creditnote.created",
            Detail = $"{creditNote.InvoiceNumber} credits {source.InvoiceNumber}",
            ActorRole = actorRole
        });

        await _db.SaveChangesAsync(ct);

        return _invoices.Describe(creditNote);
    }
}
