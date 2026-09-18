using LedgerLoop.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace LedgerLoop.IntegrationTests.Support;

/// <summary>
/// Provides a migrated PostgreSQL database. A server can be supplied through
/// LEDGERLOOP_TEST_CONNECTION (used by CI and by developers running a local
/// server); otherwise a throwaway container is started.
/// </summary>
public sealed class PostgresServer : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string? _adminConnectionString;
    private string? _databaseName;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("LEDGERLOOP_TEST_CONNECTION");

        if (!string.IsNullOrWhiteSpace(external))
        {
            _databaseName = $"ledgerloop_test_{Guid.NewGuid():N}";
            _adminConnectionString = new NpgsqlConnectionStringBuilder(external) { Database = "postgres" }.ConnectionString;

            await ExecuteAsync(_adminConnectionString, $"CREATE DATABASE \"{_databaseName}\"");

            ConnectionString = new NpgsqlConnectionStringBuilder(external) { Database = _databaseName }.ConnectionString;
        }
        else
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("ledgerloop")
                .WithUsername("ledgerloop")
                .WithPassword("ledgerloop")
                .Build();

            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
        }

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public LedgerLoopDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<LedgerLoopDbContext>().UseNpgsql(ConnectionString).Options);

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            return;
        }

        if (_adminConnectionString is null || _databaseName is null)
        {
            return;
        }

        NpgsqlConnection.ClearAllPools();
        await ExecuteAsync(_adminConnectionString, $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)");
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
