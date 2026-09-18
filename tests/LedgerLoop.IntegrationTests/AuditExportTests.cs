using FluentAssertions;
using LedgerLoop.Api.Contracts;
using LedgerLoop.IntegrationTests.Support;
using LedgerLoop.UnitTests.Fixtures;

namespace LedgerLoop.IntegrationTests;

public class AuditExportTests : ApiTestBase
{
    public AuditExportTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Exports_posted_documents_with_the_expected_result_shape()
    {
        var fixture = GoldenInvoiceLibrary.ById("03-reduced-rate");
        var customer = await CreateCustomerFromFixtureAsync(fixture);
        var draft = await CreateDraftAsync(DraftFrom(fixture, customer.Id));
        await PostInvoiceAsync(draft.Id);

        var export = await SendAsync<AuditExportResponse>(
            HttpMethod.Get,
            $"/api/audit/export?from={fixture.Invoice.InvoiceDate}&to={fixture.Invoice.InvoiceDate}");

        export.ExportId.Should().StartWith("AX-");
        export.DocumentCount.Should().BeGreaterThan(0);

        var document = export.Documents.Single(d => d.InvoiceId == draft.Id);

        document.CustomerCountry.Should().Be(fixture.Customer.CountryCode);
        document.PostedAt.Should().NotBeNull();
        document.Totals.CalculatorVersion.Should().NotBeNullOrWhiteSpace();
        document.Totals.NetAmount.Should().Be(fixture.Expected.NetAmount);
        document.Totals.TaxAmount.Should().Be(fixture.Expected.TaxAmount);
        document.Totals.GrossAmount.Should().Be(fixture.Expected.PostedGrossAmount);
        document.Totals.Lines.Should().HaveCount(fixture.Invoice.Lines.Count);
        document.Totals.Lines[0].TreatmentCode.Should().Be(fixture.Expected.TaxTreatment);
    }

    [Fact]
    public async Task The_export_carries_the_period_adjustment_of_older_documents()
    {
        var fixture = GoldenInvoiceLibrary.ById("12-historical-reduced-rate");
        var customer = await CreateCustomerFromFixtureAsync(fixture);
        var draft = await CreateDraftAsync(DraftFrom(fixture, customer.Id));
        await PostInvoiceAsync(draft.Id);

        var export = await SendAsync<AuditExportResponse>(HttpMethod.Get, "/api/audit/export?to=2022-12-31");

        var document = export.Documents.Single(d => d.InvoiceId == draft.Id);

        document.Totals.PostingAdjustment.Should().Be(fixture.Expected.PostingAdjustment);
        document.Totals.GrossAmount.Should().Be(fixture.Expected.PostedGrossAmount);
        export.GrossTotal.Should().Be(export.Documents.Sum(d => d.Totals.GrossAmount));
    }

    [Fact]
    public async Task Draft_documents_are_not_exported()
    {
        var customer = await CreateCustomerAsync("Unposted Export SAS");
        var draft = await CreateDraftAsync(SimpleDraft(customer.Id, new DateOnly(2023, 2, 2)));

        var export = await SendAsync<AuditExportResponse>(HttpMethod.Get, "/api/audit/export?from=2023-02-02&to=2023-02-02");

        export.Documents.Should().NotContain(d => d.InvoiceId == draft.Id);
    }
}
