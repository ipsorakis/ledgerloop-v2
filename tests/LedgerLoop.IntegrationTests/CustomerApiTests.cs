using System.Net;
using FluentAssertions;
using LedgerLoop.Api.Contracts;
using LedgerLoop.IntegrationTests.Support;

namespace LedgerLoop.IntegrationTests;

public class CustomerApiTests : ApiTestBase
{
    public CustomerApiTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Creates_and_updates_a_customer()
    {
        var created = await CreateCustomerAsync("Atelier Perrin");

        created.TaxTreatment.Should().Be("DOMESTIC");

        var updated = await SendAsync<CustomerResponse>(HttpMethod.Put, $"/api/customers/{created.Id}", new CustomerRequest
        {
            Name = "Atelier Perrin SARL",
            CountryCode = "DE",
            TaxId = "DE123456789",
            Currency = "EUR"
        });

        updated.Name.Should().Be("Atelier Perrin SARL");
        updated.TaxTreatment.Should().Be("REVERSE_CHARGE");
    }

    [Fact]
    public async Task Records_an_exemption_through_the_customer_flag()
    {
        var created = await CreateCustomerAsync("Association Test", taxOverride: "1");

        created.TaxOverride.Should().Be("1");
        created.TaxTreatment.Should().Be("EXEMPT");
    }

    [Fact]
    public async Task Rejects_a_country_that_is_not_enabled_for_billing()
    {
        var response = await Client.SendAsync(Request(HttpMethod.Post, "/api/customers", new CustomerRequest
        {
            Name = "Warsaw Trading",
            CountryCode = "PL",
            Currency = "PLN"
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("not enabled for billing");
    }
}
