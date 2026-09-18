using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Data;
using LedgerLoop.Api.Domain;
using LedgerLoop.Api.Tax;
using Microsoft.EntityFrameworkCore;

namespace LedgerLoop.Api.Services;

public sealed class InvoiceService
{
    private readonly LedgerLoopDbContext _db;
    private readonly IInvoiceTaxCalculator _calculator;
    private readonly DocumentNumberGenerator _numbers;
    private readonly TaxSettings _settings;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        LedgerLoopDbContext db,
        IInvoiceTaxCalculator calculator,
        DocumentNumberGenerator numbers,
        TaxSettings settings,
        ILogger<InvoiceService> logger)
    {
        _db = db;
        _calculator = calculator;
        _numbers = numbers;
        _settings = settings;
        _logger = logger;
    }

    public async Task<List<InvoiceSummaryResponse>> ListAsync(string? status, CancellationToken ct)
    {
        var query = _db.Invoices.AsNoTracking().Include(i => i.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var parsed))
        {
            query = query.Where(i => i.Status == parsed);
        }

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenBy(i => i.InvoiceNumber)
            .ToListAsync(ct);

        return invoices.Select(i => new InvoiceSummaryResponse
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            CustomerName = i.Customer?.Name ?? string.Empty,
            DocumentKind = i.DocumentKind.ToString(),
            Status = i.Status.ToString(),
            InvoiceDate = i.InvoiceDate,
            Currency = i.Currency,
            NetAmount = i.NetAmount,
            TaxAmount = i.TaxAmount,
            GrossAmount = i.GrossAmount,
            TaxTreatment = i.TaxTreatment
        }).ToList();
    }

    public async Task<Invoice?> LoadAsync(Guid id, CancellationToken ct) =>
        await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<InvoiceDetailResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var invoice = await LoadAsync(id, ct);
        return invoice is null ? null : Describe(invoice);
    }

    public async Task<TaxResult> PreviewAsync(Guid id, CancellationToken ct)
    {
        var invoice = await LoadAsync(id, ct)
            ?? throw new DocumentNotFoundException(id);

        return _calculator.Calculate(invoice);
    }

    public async Task<TaxResult> PreviewDraftAsync(DraftInvoiceRequest request, CancellationToken ct)
    {
        var customer = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct)
            ?? throw new DomainValidationException($"Customer {request.CustomerId} does not exist.");

        var invoice = MapToInvoice(request, customer, DocumentKind.Invoice);
        invoice.InvoiceNumber = "PREVIEW";

        return _calculator.Calculate(invoice);
    }

    public async Task<InvoiceDetailResponse> CreateDraftAsync(DraftInvoiceRequest request, string? actorRole, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct)
            ?? throw new DomainValidationException($"Customer {request.CustomerId} does not exist.");

        ValidateLines(request);
        EnsureOverrideAllowed(request.ManualTaxAmount, actorRole);

        var invoice = MapToInvoice(request, customer, DocumentKind.Invoice);
        invoice.InvoiceNumber = await _numbers.NextAsync(DocumentKind.Invoice, invoice.InvoiceDate, ct);

        ApplyTotals(invoice);

        _db.Invoices.Add(invoice);
        _db.AuditEvents.Add(new AuditEvent
        {
            InvoiceId = invoice.Id,
            EventType = "draft.created",
            Detail = $"{invoice.InvoiceNumber} for {customer.Name}",
            ActorRole = actorRole
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Created draft {InvoiceNumber} for customer {CustomerId}", invoice.InvoiceNumber, customer.Id);

        return Describe(invoice);
    }

    public async Task<InvoiceDetailResponse> UpdateDraftAsync(Guid id, DraftInvoiceRequest request, string? actorRole, CancellationToken ct)
    {
        var invoice = await LoadAsync(id, ct) ?? throw new DocumentNotFoundException(id);

        if (invoice.Status == InvoiceStatus.Posted)
        {
            throw new DomainValidationException($"Invoice {invoice.InvoiceNumber} is posted and can no longer be edited.");
        }

        ValidateLines(request);
        EnsureOverrideAllowed(request.ManualTaxAmount, actorRole);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct)
            ?? throw new DomainValidationException($"Customer {request.CustomerId} does not exist.");

        invoice.CustomerId = customer.Id;
        invoice.Customer = customer;
        invoice.InvoiceDate = request.InvoiceDate ?? invoice.InvoiceDate;
        invoice.PriceMode = request.PriceMode;
        invoice.Currency = request.Currency ?? customer.Currency;
        invoice.InvoiceDiscountPercent = request.InvoiceDiscountPercent;
        invoice.ManualTaxAmount = request.ManualTaxAmount;
        invoice.ManualTaxReason = request.ManualTaxReason;
        invoice.Notes = request.Notes;
        invoice.UpdatedAt = DateTime.UtcNow;

        _db.InvoiceLines.RemoveRange(invoice.Lines);
        invoice.Lines = BuildLines(request);
        foreach (var line in invoice.Lines)
        {
            line.InvoiceId = invoice.Id;
            _db.InvoiceLines.Add(line);
        }

        ApplyTotals(invoice);

        await _db.SaveChangesAsync(ct);

        return Describe(invoice);
    }

    public void ApplyTotals(Invoice invoice)
    {
        var result = _calculator.Calculate(invoice);

        invoice.NetAmount = result.NetAmount;
        invoice.TaxAmount = result.TaxAmount;
        invoice.GrossAmount = result.GrossAmount;
        invoice.TaxTreatment = result.TaxTreatment;

        foreach (var line in result.Lines)
        {
            var target = invoice.Lines.First(l => l.LineNumber == line.LineNumber);
            target.RatePercent = line.RatePercent;
            target.NetAmount = line.NetAmount;
            target.TaxAmount = line.TaxAmount;
            target.GrossAmount = line.GrossAmount;
            target.TreatmentCode = line.TreatmentCode;
        }
    }

    public InvoiceDetailResponse Describe(Invoice invoice)
    {
        var totals = _calculator.Calculate(invoice);
        totals.PostingAdjustment = invoice.PostingAdjustment;

        if (invoice.Status == InvoiceStatus.Posted)
        {
            totals.GrossAmount = invoice.GrossAmount;
        }

        return new InvoiceDetailResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.Customer?.Name ?? string.Empty,
            CustomerCountry = invoice.Customer?.CountryCode ?? string.Empty,
            DocumentKind = invoice.DocumentKind.ToString(),
            Status = invoice.Status.ToString(),
            PriceMode = invoice.PriceMode.ToString(),
            InvoiceDate = invoice.InvoiceDate,
            Currency = invoice.Currency,
            InvoiceDiscountPercent = invoice.InvoiceDiscountPercent,
            ManualTaxAmount = invoice.ManualTaxAmount,
            Notes = invoice.Notes,
            PostingAdjustment = invoice.PostingAdjustment,
            PostedAt = invoice.PostedAt,
            OriginalInvoiceId = invoice.OriginalInvoiceId,
            Totals = totals,
            Lines = invoice.Lines.OrderBy(l => l.LineNumber).Select(l => new InvoiceLineResponse
            {
                LineNumber = l.LineNumber,
                Description = l.Description,
                Category = l.Category.ToString(),
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineDiscountPercent = l.LineDiscountPercent
            }).ToList()
        };
    }

    private void EnsureOverrideAllowed(decimal? manualTaxAmount, string? actorRole)
    {
        if (!manualTaxAmount.HasValue)
        {
            return;
        }

        if (!string.Equals(actorRole, _settings.ManualOverrideRole, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenOperationException("A manual tax amount may only be set by the accounting role.");
        }
    }

    private static void ValidateLines(DraftInvoiceRequest request)
    {
        if (request.Lines.Count == 0)
        {
            throw new DomainValidationException("An invoice needs at least one line.");
        }

        if (request.InvoiceDiscountPercent is < 0 or > 100)
        {
            throw new DomainValidationException("Invoice discount must be between 0 and 100.");
        }

        foreach (var line in request.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.Description))
            {
                throw new DomainValidationException("Every line needs a description.");
            }

            if (line.LineDiscountPercent is < 0 or > 100)
            {
                throw new DomainValidationException("Line discount must be between 0 and 100.");
            }
        }
    }

    private Invoice MapToInvoice(DraftInvoiceRequest request, Customer customer, DocumentKind kind)
    {
        var invoice = new Invoice
        {
            CustomerId = customer.Id,
            Customer = customer,
            DocumentKind = kind,
            Status = InvoiceStatus.Draft,
            PriceMode = request.PriceMode,
            InvoiceDate = request.InvoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Currency = request.Currency ?? customer.Currency ?? _settings.DefaultCurrency,
            InvoiceDiscountPercent = request.InvoiceDiscountPercent,
            ManualTaxAmount = request.ManualTaxAmount,
            ManualTaxReason = request.ManualTaxReason,
            Notes = request.Notes,
            Lines = BuildLines(request)
        };

        foreach (var line in invoice.Lines)
        {
            line.InvoiceId = invoice.Id;
        }

        return invoice;
    }

    private static List<InvoiceLine> BuildLines(DraftInvoiceRequest request)
    {
        var lines = new List<InvoiceLine>();
        var number = 1;

        foreach (var line in request.Lines)
        {
            lines.Add(new InvoiceLine
            {
                LineNumber = line.LineNumber ?? number,
                Description = line.Description,
                Category = line.Category,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineDiscountPercent = line.LineDiscountPercent
            });
            number++;
        }

        return lines;
    }
}
