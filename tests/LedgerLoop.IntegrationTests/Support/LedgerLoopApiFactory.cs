using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LedgerLoop.IntegrationTests.Support;

public sealed class LedgerLoopApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public LedgerLoopApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:LedgerLoop", _connectionString);
        builder.UseSetting("Database:AutoMigrate", "false");
        builder.UseSetting("Database:Seed", "false");
    }
}
