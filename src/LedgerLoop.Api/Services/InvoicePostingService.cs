using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Data;
using LedgerLoop.Api.Domain;
using LedgerLoop.Api.Tax;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LedgerLoop.Api.Services;

public sealed class InvoicePostingService
{
    private readonly LedgerLoopDbContext _db;
    private readonly InvoiceService _invoices;
    private readonly IInvoiceTaxCalculator _calculator;
    private readonly ILogger<InvoicePostingService> _logger;

    public InvoicePostingService(
        LedgerLoopDbContext db,
        InvoiceService invoices,
        IInvoiceTaxCalculator calculator,
        ILogger<InvoicePostingService> logger)
    {
        _db = db;
        _invoices = invoices;
        _calculator = calculator;
        _logger = logger;
    }

    public async Task<TaxResult> PostAsync(Guid id, string? actorRole, CancellationToken ct)
    {
        var invoice = await _invoices.LoadAsync(id, ct) ?? throw new DocumentNotFoundException(id);

        if (invoice.Status == InvoiceStatus.Posted)
        {
            throw new DomainValidationException($"{invoice.InvoiceNumber} has already been posted.");
        }

        if (invoice.Lines.Count == 0)
        {
            throw new DomainValidationException($"{invoice.InvoiceNumber} has no lines to post.");
        }

        var result = _calculator.Calculate(invoice);

        invoice.NetAmount = result.NetAmount;
        invoice.TaxAmount = result.TaxAmount;
        invoice.GrossAmount = result.GrossAmount;
        invoice.TaxTreatment = result.TaxTreatment;
        invoice.Status = InvoiceStatus.Posted;
        invoice.PostedAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;

        foreach (var line in result.Lines)
        {
            var target = invoice.Lines.First(l => l.LineNumber == line.LineNumber);
            target.RatePercent = line.RatePercent;
            target.NetAmount = line.NetAmount;
            target.TaxAmount = line.TaxAmount;
            target.GrossAmount = line.GrossAmount;
            target.TreatmentCode = line.TreatmentCode;
        }

        await _db.SaveChangesAsync(ct);

        var adjustment = await ApplyLedgerPostingAsync(invoice.Id, ct);

        await _db.Entry(invoice).ReloadAsync(ct);

        _db.AuditEvents.Add(new AuditEvent
        {
            InvoiceId = invoice.Id,
            EventType = "invoice.posted",
            Detail = $"{invoice.InvoiceNumber} posted with adjustment {adjustment:0.00} {invoice.Currency}",
            ActorRole = actorRole
        });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Posted {InvoiceNumber} net={Net} tax={Tax} adjustment={Adjustment}",
            invoice.InvoiceNumber, invoice.NetAmount, invoice.TaxAmount, adjustment);

        result.PostingAdjustment = adjustment;
        result.GrossAmount = invoice.GrossAmount;

        return result;
    }

    /// <summary>
    /// Ledger hand-off. Runs inside the database so the period adjustment stays
    /// aligned with the nightly ledger batch that shares the same routine.
    /// </summary>
    private async Task<decimal> ApplyLedgerPostingAsync(Guid invoiceId, CancellationToken ct)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT ledgerloop_post_invoice(@invoice_id)";
        var parameter = new NpgsqlParameter("invoice_id", invoiceId);
        command.Parameters.Add(parameter);

        var raw = await command.ExecuteScalarAsync(ct);

        return raw is null or DBNull ? 0m : Convert.ToDecimal(raw);
    }
}
