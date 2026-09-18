using System.Net;
using FluentAssertions;
using LedgerLoop.Api.Domain;
using LedgerLoop.IntegrationTests.Support;
using LedgerLoop.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace LedgerLoop.IntegrationTests;

public class InvoicePostingTests : ApiTestBase
{
    public InvoicePostingTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Posting_stores_the_ledger_figures_on_the_document()
    {
        var fixture = GoldenInvoiceLibrary.ById("09-per-line-discount");
        var customer = await CreateCustomerFromFixtureAsync(fixture);
        var draft = await CreateDraftAsync(DraftFrom(fixture, customer.Id));

        var posted = await PostInvoiceAsync(draft.Id);

        posted.NetAmount.Should().Be(fixture.Expected.NetAmount);
        posted.TaxAmount.Should().Be(fixture.Expected.TaxAmount);
        posted.PostingAdjustment.Should().Be(fixture.Expected.PostingAdjustment);
        posted.GrossAmount.Should().Be(fixture.Expected.PostedGrossAmount);

        await using var db = Fixture.CreateContext();
        var stored = await db.Invoices.Include(i => i.Lines).SingleAsync(i => i.Id == draft.Id);

        stored.Status.Should().Be(InvoiceStatus.Posted);
        stored.PostedAt.Should().NotBeNull();
        stored.GrossAmount.Should().Be(fixture.Expected.PostedGrossAmount);
        stored.Lines.Should().OnlyContain(line => line.TreatmentCode == "DOMESTIC");
    }

    [Fact]
    public async Task Documents_from_older_periods_receive_the_ledger_adjustment()
    {
        var fixture = GoldenInvoiceLibrary.ById("12-historical-reduced-rate");
        var customer = await CreateCustomerFromFixtureAsync(fixture);
        var draft = await CreateDraftAsync(DraftFrom(fixture, customer.Id));

        var posted = await PostInvoiceAsync(draft.Id);

        posted.TaxAmount.Should().Be(fixture.Expected.TaxAmount);
        posted.PostingAdjustment.Should().Be(fixture.Expected.PostingAdjustment);
        posted.PostingAdjustment.Should().NotBe(0m);
        posted.GrossAmount.Should().Be(fixture.Expected.PostedGrossAmount);

        await using var db = Fixture.CreateContext();
        var stored = await db.Invoices.SingleAsync(i => i.Id == draft.Id);
        stored.PostingAdjustment.Should().Be(fixture.Expected.PostingAdjustment);
    }

    [Fact]
    public async Task Recent_documents_are_posted_without_an_adjustment()
    {
        var customer = await CreateCustomerAsync("Recent Period SAS");
        var draft = await CreateDraftAsync(SimpleDraft(customer.Id, new DateOnly(2024, 9, 30), 480m));

        var posted = await PostInvoiceAsync(draft.Id);

        posted.PostingAdjustment.Should().Be(0m);
        posted.GrossAmount.Should().Be(576m);
    }

    [Fact]
    public async Task A_document_cannot_be_posted_twice()
    {
        var customer = await CreateCustomerAsync("Single Posting SAS");
        var draft = await CreateDraftAsync(SimpleDraft(customer.Id));

        await PostInvoiceAsync(draft.Id);

        var response = await Client.SendAsync(Request(HttpMethod.Post, $"/api/invoices/{draft.Id}/post"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("already been posted");
    }

    [Fact]
    public async Task A_posted_document_can_no_longer_be_edited()
    {
        var customer = await CreateCustomerAsync("Locked Document SAS");
        var draft = await CreateDraftAsync(SimpleDraft(customer.Id));
        await PostInvoiceAsync(draft.Id);

        var response = await Client.SendAsync(Request(
            HttpMethod.Put,
            $"/api/invoices/{draft.Id}",
            SimpleDraft(customer.Id, unitPrice: 200m)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Posting_an_unknown_document_reports_not_found()
    {
        var response = await Client.SendAsync(Request(HttpMethod.Post, $"/api/invoices/{Guid.NewGuid()}/post"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
