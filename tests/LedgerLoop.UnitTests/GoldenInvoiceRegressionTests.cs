using FluentAssertions;
using LedgerLoop.Api.Tax;
using LedgerLoop.UnitTests.Fixtures;

namespace LedgerLoop.UnitTests;

public class GoldenInvoiceRegressionTests
{
    [Fact]
    public void The_fixture_library_is_loaded() =>
        GoldenInvoiceLibrary.All.Should().HaveCountGreaterThanOrEqualTo(10);

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Documents_match_the_recorded_ledger_figures(GoldenInvoice fixture)
    {
        var result = InvoiceTaxCalculator.Default.Calculate(fixture.ToInvoice());

        result.TaxTreatment.Should().Be(fixture.Expected.TaxTreatment, fixture.Description);
        result.ManualOverrideApplied.Should().Be(fixture.Expected.ManualOverrideApplied);
        result.NetAmount.Should().Be(fixture.Expected.NetAmount, fixture.Description);
        result.TaxAmount.Should().Be(fixture.Expected.TaxAmount, fixture.Description);
        result.GrossAmount.Should().Be(fixture.Expected.GrossAmount, fixture.Description);

        result.Lines.Should().HaveCount(fixture.Expected.Lines.Count);

        foreach (var expected in fixture.Expected.Lines)
        {
            var line = result.Lines.Single(l => l.LineNumber == expected.LineNumber);

            line.RatePercent.Should().Be(expected.RatePercent, $"{fixture.Id} line {expected.LineNumber}");
            line.NetAmount.Should().Be(expected.NetAmount, $"{fixture.Id} line {expected.LineNumber}");
            line.TaxAmount.Should().Be(expected.TaxAmount, $"{fixture.Id} line {expected.LineNumber}");
            line.GrossAmount.Should().Be(expected.GrossAmount, $"{fixture.Id} line {expected.LineNumber}");
        }
    }

    [Fact]
    public void The_editor_estimate_recorded_for_the_one_cent_case_is_still_one_cent_below_the_ledger()
    {
        var fixture = GoldenInvoiceLibrary.ById("15-one-cent-sensitive-total");

        (fixture.Expected.TaxAmount - fixture.Expected.EditorEstimate.TaxAmount).Should().Be(0.01m);
    }

    public static IEnumerable<object[]> Fixtures() => GoldenInvoiceLibrary.AsTheoryData();
}
