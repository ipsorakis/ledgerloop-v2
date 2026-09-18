using LedgerLoop.Api.Data;
using LedgerLoop.Api.Middleware;
using LedgerLoop.Api.Security;
using LedgerLoop.Api.Services;
using LedgerLoop.Api.Tax;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("application", "ledgerloop-api"));

var connectionString = builder.Configuration.GetConnectionString("LedgerLoop")
                       ?? "Host=localhost;Port=5432;Database=ledgerloop;Username=ledgerloop;Password=ledgerloop";

builder.Services.AddDbContext<LedgerLoopDbContext>(options => options.UseNpgsql(connectionString));

var taxSettings = builder.Configuration.GetSection(TaxSettings.SectionName).Get<TaxSettings>() ?? new TaxSettings();
builder.Services.AddSingleton(taxSettings);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ActorContext>();
builder.Services.AddScoped<IRateTable, DbRateTable>();
builder.Services.AddScoped<IRateResolver, RateResolver>();
builder.Services.AddScoped<IInvoiceTaxCalculator, InvoiceTaxCalculator>();
builder.Services.AddScoped<DocumentNumberGenerator>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<InvoicePostingService>();
builder.Services.AddScoped<CreditNoteService>();
builder.Services.AddScoped<AuditExportService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LedgerLoop API",
        Version = "v1",
        Description = "Invoice preparation and posting for LedgerLoop. Tax figures are synthetic."
    });
});

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(
        builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
        ?? new[] { "http://localhost:5173", "http://localhost:4173" })
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "LedgerLoop API v1"));

app.MapControllers();

if (app.Configuration.GetValue("Database:AutoMigrate", false))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LedgerLoopDbContext>();

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (NpgsqlException ex) when (attempt < 10)
        {
            Log.Warning(ex, "Database not reachable yet (attempt {Attempt}), retrying", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    if (app.Configuration.GetValue("Database:Seed", false))
    {
        await SeedData.EnsureSeededAsync(scope.ServiceProvider);
    }
}

app.Run();

public partial class Program
{
}
