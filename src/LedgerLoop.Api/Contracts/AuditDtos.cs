namespace LedgerLoop.Api.Contracts;

public sealed class AuditExportResponse
{
    public string ExportId { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public int DocumentCount { get; set; }

    public decimal NetTotal { get; set; }

    public decimal TaxTotal { get; set; }

    public decimal GrossTotal { get; set; }

    public List<AuditExportDocument> Documents { get; set; } = new();
}

public sealed class AuditExportDocument
{
    public Guid InvoiceId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerCountry { get; set; } = string.Empty;

    public string? CustomerTaxId { get; set; }

    public DateTime? PostedAt { get; set; }

    public TaxResult Totals { get; set; } = new();
}
