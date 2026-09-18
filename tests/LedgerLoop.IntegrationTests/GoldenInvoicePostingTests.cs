using FluentAssertions;
using LedgerLoop.Api.Domain;
using LedgerLoop.IntegrationTests.Support;
using LedgerLoop.UnitTests.Fixtures;

namespace LedgerLoop.IntegrationTests;

/// <summary>
/// Runs every golden document through the API so the stored figures, the
/// ledger adjustment and the calculated preview stay in step.
/// </summary>
public class GoldenInvoicePostingTests : ApiTestBase
{
    public GoldenInvoicePostingTests(ApiFixture fixture) : base(fixture)
    {
    }

    public static IEnumerable<object[]> Fixtures() =>
        GoldenInvoiceLibrary.All
            .Where(fixture => fixture.Invoice.DocumentKind == DocumentKind.Invoice)
            .Select(fixture => new object[] { fixture });

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task Golden_documents_post_with_the_recorded_figures(GoldenInvoice fixture)
    {
        var customer = await CreateCustomerFromFixtureAsync(fixture);
        var role = fixture.RequiredRole ?? "billing";

        var draft = await CreateDraftAsync(DraftFrom(fixture, customer.Id), role);

        draft.Totals.TaxTreatment.Should().Be(fixture.Expected.TaxTreatment, fixture.Description);
        draft.Totals.NetAmount.Should().Be(fixture.Expected.NetAmount, fixture.Description);
        draft.Totals.TaxAmount.Should().Be(fixture.Expected.TaxAmount, fixture.Description);

        var posted = await PostInvoiceAsync(draft.Id, role);

        posted.PostingAdjustment.Should().Be(fixture.Expected.PostingAdjustment, fixture.Description);
        posted.GrossAmount.Should().Be(fixture.Expected.PostedGrossAmount, fixture.Description);
    }
}
