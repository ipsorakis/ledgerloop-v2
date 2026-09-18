using LedgerLoop.Api.Tax;

namespace LedgerLoop.UnitTests.Fixtures;

/// <summary>
/// Selects the golden documents that the French domestic extraction covers:
/// the seller country is FR and the customer profile resolves to DOMESTIC.
/// </summary>
public static class FrenchDomesticFixtures
{
    private static readonly TaxSettings Settings = new();

    public static bool IsFrenchDomestic(GoldenInvoice fixture) =>
        string.Equals(Settings.SellerCountry, "FR", StringComparison.OrdinalIgnoreCase)
        && CustomerTaxProfile.Resolve(fixture.ToCustomer(), Settings.SellerCountry) == TreatmentCodes.Domestic;

    public static IReadOnlyList<GoldenInvoice> All { get; } =
        GoldenInvoiceLibrary.All.Where(IsFrenchDomestic).ToList();

    public static IEnumerable<object[]> AsTheoryData() => All.Select(fixture => new object[] { fixture });
}
