using System.Text.Json;
using FluentAssertions;
using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Tax;
using LedgerLoop.UnitTests.Fixtures;

namespace LedgerLoop.UnitTests;

/// <summary>
/// Characterization harness for the French domestic documents. Every FR-domestic
/// golden fixture is run through the calculator seam and the whole
/// <see cref="TaxResult"/> is compared against a committed snapshot, so any change
/// of behaviour on that path is a test failure rather than a silent drift.
/// </summary>
public class FrenchDomesticCharacterizationTests
{
    private const string SnapshotFileName = "fr-domestic-tax-results.json";

    private static readonly JsonSerializerOptions SnapshotOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void The_french_domestic_selection_is_not_empty()
    {
        FrenchDomesticFixtures.All.Should().NotBeEmpty();
        FrenchDomesticFixtures.All.Should()
            .OnlyContain(fixture => fixture.Expected.TaxTreatment == TreatmentCodes.Domestic);
    }

    [Fact]
    public void The_french_domestic_selection_is_covered_by_the_snapshot()
    {
        var snapshot = ReadSnapshot();

        snapshot.Keys.Should().BeEquivalentTo(FrenchDomesticFixtures.All.Select(fixture => fixture.Id));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Documents_match_the_recorded_calculator_behaviour(GoldenInvoice fixture)
    {
        var snapshot = ReadSnapshot();
        snapshot.Should().ContainKey(fixture.Id,
            $"{fixture.Id} is FR-domestic and must be recorded in {SnapshotFileName}");

        var actual = Describe(InvoiceTaxCalculator.Default.Calculate(fixture.ToInvoice()));

        actual.Should().BeEquivalentTo(snapshot[fixture.Id], fixture.Description);
    }

    public static IEnumerable<object[]> Fixtures() => FrenchDomesticFixtures.AsTheoryData();

    private static TaxResultSnapshot Describe(TaxResult result) => new()
    {
        DocumentNumber = result.DocumentNumber,
        DocumentKind = result.DocumentKind,
        Currency = result.Currency,
        InvoiceDate = result.InvoiceDate.ToString("yyyy-MM-dd"),
        TaxTreatment = result.TaxTreatment,
        CalculatorVersion = result.CalculatorVersion,
        ManualOverrideApplied = result.ManualOverrideApplied,
        NetAmount = result.NetAmount,
        TaxAmount = result.TaxAmount,
        GrossAmount = result.GrossAmount,
        PostingAdjustment = result.PostingAdjustment,
        Lines = result.Lines
            .OrderBy(line => line.LineNumber)
            .Select(line => new TaxResultLineSnapshot
            {
                LineNumber = line.LineNumber,
                Description = line.Description,
                Category = line.Category,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                RatePercent = line.RatePercent,
                NetAmount = line.NetAmount,
                TaxAmount = line.TaxAmount,
                GrossAmount = line.GrossAmount,
                TreatmentCode = line.TreatmentCode
            })
            .ToList()
    };

    private static IReadOnlyDictionary<string, TaxResultSnapshot> ReadSnapshot()
    {
        var path = Path.Combine(SnapshotLibrary.Directory, SnapshotFileName);

        return JsonSerializer.Deserialize<Dictionary<string, TaxResultSnapshot>>(File.ReadAllText(path), SnapshotOptions)
               ?? throw new InvalidOperationException($"{path} could not be read.");
    }

    public sealed class TaxResultSnapshot
    {
        public string DocumentNumber { get; set; } = string.Empty;

        public string DocumentKind { get; set; } = string.Empty;

        public string Currency { get; set; } = string.Empty;

        public string InvoiceDate { get; set; } = string.Empty;

        public string TaxTreatment { get; set; } = string.Empty;

        public string CalculatorVersion { get; set; } = string.Empty;

        public bool ManualOverrideApplied { get; set; }

        public decimal NetAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal GrossAmount { get; set; }

        public decimal PostingAdjustment { get; set; }

        public List<TaxResultLineSnapshot> Lines { get; set; } = new();
    }

    public sealed class TaxResultLineSnapshot
    {
        public int LineNumber { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal RatePercent { get; set; }

        public decimal NetAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal GrossAmount { get; set; }

        public string TreatmentCode { get; set; } = string.Empty;
    }
}
