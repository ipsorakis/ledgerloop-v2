using System.Net;
using System.Text.Json;
using FluentAssertions;
using LedgerLoop.IntegrationTests.Support;

namespace LedgerLoop.IntegrationTests;

public class HealthEndpointTests : ApiTestBase
{
    public HealthEndpointTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Reports_the_database_as_reachable()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        payload.RootElement.GetProperty("status").GetString().Should().Be("healthy");
        payload.RootElement.GetProperty("database").GetString().Should().Be("up");
    }

    [Fact]
    public async Task Publishes_an_openapi_document()
    {
        var response = await Client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("/api/invoices/{id}/post");
    }
}
