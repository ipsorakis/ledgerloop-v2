using System.Net;
using FluentAssertions;
using LedgerLoop.IntegrationTests.Support;
using LedgerLoop.UnitTests.Fixtures;

namespace LedgerLoop.IntegrationTests;

public class ManualOverrideAuthorizationTests : ApiTestBase
{
    public ManualOverrideAuthorizationTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task A_billing_operator_may_not_enter_a_manual_tax_amount()
    {
        var fixture = GoldenInvoiceLibrary.ById("13-manual-tax-amount");
        var customer = await CreateCustomerFromFixtureAsync(fixture);

        var response = await Client.SendAsync(Request(
            HttpMethod.Post,
            "/api/invoices",
            DraftFrom(fixture, customer.Id),
            role: "billing"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_request_without_a_role_may_not_enter_a_manual_tax_amount()
    {
        var fixture = GoldenInvoiceLibrary.ById("13-manual-tax-amount");
        var customer = await CreateCustomerFromFixtureAsync(fixture);

        var response = await Client.SendAsync(Request(HttpMethod.Post, "/api/invoices", DraftFrom(fixture, customer.Id)));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Accounting_may_enter_a_manual_tax_amount_and_it_survives_posting()
    {
        var fixture = GoldenInvoiceLibrary.ById("13-manual-tax-amount");
        var customer = await CreateCustomerFromFixtureAsync(fixture);

        var draft = await CreateDraftAsync(DraftFrom(fixture, customer.Id), role: fixture.RequiredRole);

        draft.Totals.ManualOverrideApplied.Should().BeTrue();
        draft.Totals.TaxAmount.Should().Be(fixture.Expected.TaxAmount);

        var posted = await PostInvoiceAsync(draft.Id, fixture.RequiredRole);

        posted.TaxAmount.Should().Be(fixture.Expected.TaxAmount);
        posted.GrossAmount.Should().Be(fixture.Expected.PostedGrossAmount);
    }
}
