using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LedgerLoop.Api.Domain;

namespace LedgerLoop.UnitTests.Fixtures;

public sealed class GoldenInvoice
{
    public string Id { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public string? RequiredRole { get; set; }

    public GoldenCustomer Customer { get; set; } = new();

    public GoldenDocument Invoice { get; set; } = new();

    public GoldenExpectation Expected { get; set; } = new();

    public override string ToString() => Id;
}

public sealed class GoldenCustomer
{
    public string Name { get; set; } = string.Empty;

    public string CountryCode { get; set; } = "FR";

    public string? TaxId { get; set; }

    public string? TaxOverride { get; set; }

    public string Currency { get; set; } = "EUR";
}

public sealed class GoldenDocument
{
    public string DocumentNumber { get; set; } = "GOLD-0000";

    public DocumentKind DocumentKind { get; set; }

    public string InvoiceDate { get; set; } = "2024-01-01";

    public PriceMode PriceMode { get; set; }

    public string Currency { get; set; } = "EUR";

    public decimal InvoiceDiscountPercent { get; set; }

    public decimal? ManualTaxAmount { get; set; }

    public List<GoldenLine> Lines { get; set; } = new();

    public DateOnly Date => DateOnly.ParseExact(InvoiceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public sealed class GoldenLine
{
    public int LineNumber { get; set; }

    public string Description { get; set; } = string.Empty;

    public LineCategory Category { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineDiscountPercent { get; set; }
}

public sealed class GoldenExpectation
{
    public string TaxTreatment { get; set; } = string.Empty;

    public bool ManualOverrideApplied { get; set; }

    public List<GoldenExpectedLine> Lines { get; set; } = new();

    public decimal NetAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal PostingAdjustment { get; set; }

    public decimal PostedGrossAmount { get; set; }

    public GoldenEditorEstimate EditorEstimate { get; set; } = new();
}

public sealed class GoldenExpectedLine
{
    public int LineNumber { get; set; }

    public decimal RatePercent { get; set; }

    public decimal NetAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrossAmount { get; set; }
}

public sealed class GoldenEditorEstimate
{
    public string Treatment { get; set; } = string.Empty;

    public decimal NetAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrossAmount { get; set; }
}

public static class GoldenInvoiceLibrary
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Directory { get; } = Locate();

    public static IReadOnlyList<GoldenInvoice> All { get; } = Load();

    public static IEnumerable<object[]> AsTheoryData() => All.Select(fixture => new object[] { fixture });

    public static GoldenInvoice ById(string id) =>
        All.First(fixture => fixture.Id == id);

    public static Customer ToCustomer(this GoldenInvoice fixture) => new()
    {
        Name = fixture.Customer.Name,
        CountryCode = fixture.Customer.CountryCode,
        TaxId = fixture.Customer.TaxId,
        TaxOverride = fixture.Customer.TaxOverride,
        Currency = fixture.Customer.Currency
    };

    public static Invoice ToInvoice(this GoldenInvoice fixture, Customer? customer = null)
    {
        customer ??= fixture.ToCustomer();

        var invoice = new Invoice
        {
            InvoiceNumber = fixture.Invoice.DocumentNumber,
            Customer = customer,
            CustomerId = customer.Id,
            DocumentKind = fixture.Invoice.DocumentKind,
            PriceMode = fixture.Invoice.PriceMode,
            InvoiceDate = fixture.Invoice.Date,
            Currency = fixture.Invoice.Currency,
            InvoiceDiscountPercent = fixture.Invoice.InvoiceDiscountPercent,
            ManualTaxAmount = fixture.Invoice.ManualTaxAmount
        };

        foreach (var line in fixture.Invoice.Lines)
        {
            invoice.Lines.Add(new InvoiceLine
            {
                InvoiceId = invoice.Id,
                LineNumber = line.LineNumber,
                Description = line.Description,
                Category = line.Category,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineDiscountPercent = line.LineDiscountPercent
            });
        }

        return invoice;
    }

    private static IReadOnlyList<GoldenInvoice> Load() =>
        System.IO.Directory.GetFiles(Directory, "*.json")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => JsonSerializer.Deserialize<GoldenInvoice>(File.ReadAllText(path), Options)
                            ?? throw new InvalidOperationException($"Fixture {path} could not be read."))
            .ToList();

    private static string Locate()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "fixtures", "golden-invoices");
            if (System.IO.Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("fixtures/golden-invoices could not be located from the test output directory.");
    }
}
