using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Domain;
using LedgerLoop.Api.Security;
using LedgerLoop.UnitTests.Fixtures;

namespace LedgerLoop.IntegrationTests.Support;

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected ApiTestBase(ApiFixture fixture)
    {
        Fixture = fixture;
    }

    protected ApiFixture Fixture { get; }

    protected HttpClient Client => Fixture.Client;

    protected static HttpRequestMessage Request(HttpMethod method, string path, object? body = null, string? role = null)
    {
        var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        if (role is not null)
        {
            request.Headers.Add(ActorContext.RoleHeader, role);
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return request;
    }

    protected async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null, string? role = null)
    {
        var response = await Client.SendAsync(Request(method, path, body, role));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(Json)
               ?? throw new InvalidOperationException($"{method} {path} returned an empty payload.");
    }

    protected Task<CustomerResponse> CreateCustomerAsync(
        string name,
        string country = "FR",
        string? taxId = "FR12345678901",
        string? taxOverride = null,
        string currency = "EUR") =>
        SendAsync<CustomerResponse>(HttpMethod.Post, "/api/customers", new CustomerRequest
        {
            Name = name,
            CountryCode = country,
            TaxId = taxId,
            TaxOverride = taxOverride,
            Currency = currency
        });

    protected async Task<CustomerResponse> CreateCustomerFromFixtureAsync(GoldenInvoice fixture) =>
        await CreateCustomerAsync(
            $"{fixture.Customer.Name} ({fixture.Id})",
            fixture.Customer.CountryCode,
            fixture.Customer.TaxId,
            fixture.Customer.TaxOverride,
            fixture.Customer.Currency);

    protected static DraftInvoiceRequest DraftFrom(GoldenInvoice fixture, Guid customerId) => new()
    {
        CustomerId = customerId,
        InvoiceDate = fixture.Invoice.Date,
        PriceMode = fixture.Invoice.PriceMode,
        Currency = fixture.Invoice.Currency,
        InvoiceDiscountPercent = fixture.Invoice.InvoiceDiscountPercent,
        ManualTaxAmount = fixture.Invoice.ManualTaxAmount,
        Notes = fixture.Description,
        Lines = fixture.Invoice.Lines.Select(line => new InvoiceLineRequest
        {
            LineNumber = line.LineNumber,
            Description = line.Description,
            Category = line.Category,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            LineDiscountPercent = line.LineDiscountPercent
        }).ToList()
    };

    protected Task<InvoiceDetailResponse> CreateDraftAsync(DraftInvoiceRequest request, string? role = "billing") =>
        SendAsync<InvoiceDetailResponse>(HttpMethod.Post, "/api/invoices", request, role);

    protected Task<TaxResult> PostInvoiceAsync(Guid id, string? role = "billing") =>
        SendAsync<TaxResult>(HttpMethod.Post, $"/api/invoices/{id}/post", role: role);

    protected static DraftInvoiceRequest SimpleDraft(Guid customerId, DateOnly? date = null, decimal unitPrice = 100m) => new()
    {
        CustomerId = customerId,
        InvoiceDate = date ?? new DateOnly(2024, 5, 1),
        PriceMode = PriceMode.Exclusive,
        Currency = "EUR",
        Lines = new List<InvoiceLineRequest>
        {
            new()
            {
                LineNumber = 1,
                Description = "Advisory day",
                Category = LineCategory.ProfessionalServices,
                Quantity = 1,
                UnitPrice = unitPrice
            }
        }
    };
}
