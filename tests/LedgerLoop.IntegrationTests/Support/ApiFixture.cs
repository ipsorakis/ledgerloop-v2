using LedgerLoop.Api.Data;

namespace LedgerLoop.IntegrationTests.Support;

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgresServer _server = new();
    private LedgerLoopApiFactory? _factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _server.InitializeAsync();
        _factory = new LedgerLoopApiFactory(_server.ConnectionString);
        Client = _factory.CreateClient();
    }

    public LedgerLoopDbContext CreateContext() => _server.CreateContext();

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _server.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "ledgerloop-api";
}
