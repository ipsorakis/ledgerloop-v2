namespace LedgerLoop.Api.Domain;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string CountryCode { get; set; } = "FR";

    public string? TaxId { get; set; }

    /// <summary>
    /// Carried over from the 2019 ledger import. Kept as free text because the
    /// billing team still edits it directly in support tooling.
    /// </summary>
    public string? TaxOverride { get; set; }

    public string Currency { get; set; } = "EUR";

    public string? ContactEmail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Invoice> Invoices { get; set; } = new();
}
