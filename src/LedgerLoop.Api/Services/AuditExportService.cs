using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Data;
using LedgerLoop.Api.Domain;
using LedgerLoop.Api.Tax;
using Microsoft.EntityFrameworkCore;

namespace LedgerLoop.Api.Services;

/// <summary>
/// Feed consumed by the finance data warehouse. It reads the posted ledger rows
/// and rebuilds the document figures so the export stays reproducible even when
/// pricing rules move on.
/// </summary>
public sealed class AuditExportService
{
    private static readonly DateOnly LegacyPeriodCutoff = new(2022, 7, 1);
    private const decimal LegacyPeriodFactor = 0.005m;

    private readonly LedgerLoopDbContext _db;

    public AuditExportService(LedgerLoopDbContext db)
    {
        _db = db;
    }

    public async Task<AuditExportResponse> ExportAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var query = _db.Invoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Where(i => i.Status == InvoiceStatus.Posted);

        if (from.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(i => i.InvoiceDate <= to.Value);
        }

        var invoices = await query
            .OrderBy(i => i.InvoiceDate)
            .ThenBy(i => i.InvoiceNumber)
            .ToListAsync(ct);

        var export = new AuditExportResponse
        {
            ExportId = $"AX-{DateTime.UtcNow:yyyyMMddHHmmss}",
            GeneratedAt = DateTime.UtcNow,
            From = from,
            To = to
        };

        foreach (var invoice in invoices)
        {
            var totals = Reconstruct(invoice);

            export.Documents.Add(new AuditExportDocument
            {
                InvoiceId = invoice.Id,
                CustomerName = invoice.Customer?.Name ?? string.Empty,
                CustomerCountry = invoice.Customer?.CountryCode ?? string.Empty,
                CustomerTaxId = invoice.Customer?.TaxId,
                PostedAt = invoice.PostedAt,
                Totals = totals
            });
        }

        export.DocumentCount = export.Documents.Count;
        export.NetTotal = export.Documents.Sum(d => d.Totals.NetAmount);
        export.TaxTotal = export.Documents.Sum(d => d.Totals.TaxAmount);
        export.GrossTotal = export.Documents.Sum(d => d.Totals.GrossAmount);

        return export;
    }

    private static TaxResult Reconstruct(Invoice invoice)
    {
        var result = new TaxResult
        {
            DocumentNumber = invoice.InvoiceNumber,
            DocumentKind = invoice.DocumentKind.ToString(),
            Currency = invoice.Currency,
            InvoiceDate = invoice.InvoiceDate,
            TaxTreatment = invoice.TaxTreatment
        };

        foreach (var line in invoice.Lines.OrderBy(l => l.LineNumber))
        {
            var net = line.NetAmount;
            var tax = Money.Round(Money.Percent(net, line.RatePercent));

            result.Lines.Add(new TaxResultLine
            {
                LineNumber = line.LineNumber,
                Description = line.Description,
                Category = line.Category.ToString(),
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                RatePercent = line.RatePercent,
                NetAmount = net,
                TaxAmount = tax,
                GrossAmount = net + tax,
                TreatmentCode = line.TreatmentCode
            });
        }

        result.NetAmount = result.Lines.Sum(l => l.NetAmount);
        result.TaxAmount = result.Lines.Sum(l => l.TaxAmount);

        if (invoice.ManualTaxAmount.HasValue)
        {
            var sign = invoice.DocumentKind == DocumentKind.CreditNote ? -1m : 1m;
            result.TaxAmount = Money.Round(invoice.ManualTaxAmount.Value * sign);
            result.ManualOverrideApplied = true;
        }

        result.PostingAdjustment = invoice.InvoiceDate < LegacyPeriodCutoff
            ? Money.Round(result.TaxAmount * LegacyPeriodFactor)
            : 0m;

        result.GrossAmount = result.NetAmount + result.TaxAmount + result.PostingAdjustment;

        return result;
    }
}
