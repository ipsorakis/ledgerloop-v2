using System.Net;
using FluentAssertions;
using LedgerLoop.Api.Contracts;
using LedgerLoop.IntegrationTests.Support;
using LedgerLoop.UnitTests.Fixtures;

namespace LedgerLoop.IntegrationTests;

public class InvoicePreviewTests : ApiTestBase
{
    public InvoicePreviewTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Previews_an_unsaved_document_with_the_ledger_figures()
    {
        var fixture = GoldenInvoiceLibrary.ById("02-mixed-rates");
        var customer = await CreateCustomerFromFixtureAsync(fixture);

        var totals = await SendAsync<TaxResult>(HttpMethod.Post, "/api/invoices/preview", DraftFrom(fixture, customer.Id));

        totals.DocumentNumber.Should().Be("PREVIEW");
        totals.NetAmount.Should().Be(fixture.Expected.NetAmount);
        totals.TaxAmount.Should().Be(fixture.Expected.TaxAmount);
        totals.GrossAmount.Should().Be(fixture.Expected.GrossAmount);
        totals.Lines.Select(l => l.RatePercent).Should().Equal(fixture.Expected.Lines.Select(l => l.RatePercent));
    }

    [Fact]
    public async Task Saves_a_draft_and_previews_it_from_the_ledger()
    {
        var fixture = GoldenInvoiceLibrary.ById("07-inclusive-pricing");
        var customer = await CreateCustomerFromFixtureAsync(fixture);

        var draft = await CreateDraftAsync(DraftFrom(fixture, customer.Id));

        draft.InvoiceNumber.Should().StartWith("INV-2024-");
        draft.Status.Should().Be("Draft");

        var totals = await SendAsync<TaxResult>(HttpMethod.Get, $"/api/invoices/{draft.Id}/preview");

        totals.NetAmount.Should().Be(fixture.Expected.NetAmount);
        totals.TaxAmount.Should().Be(fixture.Expected.TaxAmount);
    }

    [Fact]
    public async Task The_older_totals_endpoint_accepts_client_figures_within_tolerance()
    {
        var fixture = GoldenInvoiceLibrary.ById("01-domestic-standard-rate");
        var customer = await CreateCustomerFromFixtureAsync(fixture);
        var draft = await CreateDraftAsync(DraftFrom(fixture, customer.Id));

        var response = await SendAsync<LegacyTotalsResponse>(
            HttpMethod.Post,
            $"/api/invoices/{draft.Id}/totals-legacy",
            new LegacyTotalsRequest
            {
                ClientTaxTotal = fixture.Expected.TaxAmount - 0.01m,
                Lines = new List<LegacyTotalsLine>
                {
                    new() { LineNumber = 1, TaxAmount = fixture.Expected.Lines[0].TaxAmount - 0.01m }
                }
            });

        response.ClientValuesAccepted.Should().BeTrue();
        response.ServerTaxTotal.Should().Be(fixture.Expected.TaxAmount);
        response.Totals.TaxAmount.Should().Be(fixture.Expected.TaxAmount);
    }

    [Fact]
    public async Task The_older_totals_endpoint_rejects_client_figures_that_drift()
    {
        var fixture = GoldenInvoiceLibrary.ById("01-domestic-standard-rate");
        var customer = await CreateCustomerFromFixtureAsync(fixture);
        var draft = await CreateDraftAsync(DraftFrom(fixture, customer.Id));

        var response = await Client.SendAsync(Request(
            HttpMethod.Post,
            $"/api/invoices/{draft.Id}/totals-legacy",
            new LegacyTotalsRequest
            {
                Lines = new List<LegacyTotalsLine>
                {
                    new() { LineNumber = 1, TaxAmount = 10.00m }
                }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rejects_a_document_without_lines()
    {
        var customer = await CreateCustomerAsync("Empty Draft Ltd");

        var draft = SimpleDraft(customer.Id);
        draft.Lines.Clear();

        var response = await Client.SendAsync(Request(HttpMethod.Post, "/api/invoices", draft));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
