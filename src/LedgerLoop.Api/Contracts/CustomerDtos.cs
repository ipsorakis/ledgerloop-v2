namespace LedgerLoop.Api.Contracts;

public sealed class CustomerRequest
{
    public string Name { get; set; } = string.Empty;

    public string CountryCode { get; set; } = "FR";

    public string? TaxId { get; set; }

    public string? TaxOverride { get; set; }

    public string Currency { get; set; } = "EUR";

    public string? ContactEmail { get; set; }
}

public sealed class CustomerResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string? TaxId { get; set; }

    public string? TaxOverride { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string? ContactEmail { get; set; }

    public string TaxTreatment { get; set; } = string.Empty;
}
