using System.Net;
using FluentAssertions;
using LedgerLoop.Api.Contracts;
using LedgerLoop.IntegrationTests.Support;
using LedgerLoop.UnitTests.Fixtures;

namespace LedgerLoop.IntegrationTests;

public class CreditNoteTests : ApiTestBase
{
    public CreditNoteTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task A_credit_note_mirrors_the_posted_invoice_with_reversed_amounts()
    {
        var fixture = GoldenInvoiceLibrary.ById("11-credit-note");
        var customer = await CreateCustomerFromFixtureAsync(fixture);

        var invoice = await CreateDraftAsync(DraftFrom(fixture, customer.Id));
        await PostInvoiceAsync(invoice.Id);

        var creditNote = await SendAsync<InvoiceDetailResponse>(HttpMethod.Post, "/api/credit-notes", new CreditNoteRequest
        {
            InvoiceId = invoice.Id,
            Reason = "Workshop cancelled by the customer"
        }, role: "accounting");

        creditNote.DocumentKind.Should().Be("CreditNote");
        creditNote.InvoiceNumber.Should().StartWith("CN-2022-");
        creditNote.OriginalInvoiceId.Should().Be(invoice.Id);
        creditNote.Totals.NetAmount.Should().Be(fixture.Expected.NetAmount);
        creditNote.Totals.TaxAmount.Should().Be(fixture.Expected.TaxAmount);

        var posted = await PostInvoiceAsync(creditNote.Id, "accounting");

        posted.TaxAmount.Should().Be(fixture.Expected.TaxAmount);
        posted.PostingAdjustment.Should().Be(fixture.Expected.PostingAdjustment);
        posted.GrossAmount.Should().Be(fixture.Expected.PostedGrossAmount);
    }

    [Fact]
    public async Task Only_selected_lines_are_credited()
    {
        var customer = await CreateCustomerAsync("Partial Credit SAS");
        var draft = SimpleDraft(customer.Id);
        draft.Lines.Add(new InvoiceLineRequest
        {
            LineNumber = 2,
            Description = "Travel",
            Category = LedgerLoop.Api.Domain.LineCategory.Shipping,
            Quantity = 1,
            UnitPrice = 60m
        });

        var invoice = await CreateDraftAsync(draft);
        await PostInvoiceAsync(invoice.Id);

        var creditNote = await SendAsync<InvoiceDetailResponse>(HttpMethod.Post, "/api/credit-notes", new CreditNoteRequest
        {
            InvoiceId = invoice.Id,
            LineNumbers = new List<int> { 2 }
        }, role: "accounting");

        creditNote.Lines.Should().HaveCount(1);
        creditNote.Totals.NetAmount.Should().Be(-60m);
        creditNote.Totals.TaxAmount.Should().Be(-12m);
    }

    [Fact]
    public async Task A_draft_invoice_cannot_be_credited()
    {
        var customer = await CreateCustomerAsync("Draft Credit SAS");
        var invoice = await CreateDraftAsync(SimpleDraft(customer.Id));

        var response = await Client.SendAsync(Request(HttpMethod.Post, "/api/credit-notes", new CreditNoteRequest
        {
            InvoiceId = invoice.Id
        }, role: "accounting"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("must be posted");
    }
}
