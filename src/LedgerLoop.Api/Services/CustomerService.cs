using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Data;
using LedgerLoop.Api.Domain;
using LedgerLoop.Api.Tax;
using Microsoft.EntityFrameworkCore;

namespace LedgerLoop.Api.Services;

public sealed class CustomerService
{
    private static readonly string[] SupportedCountries = { "FR", "DE", "ES", "GB", "IE", "NL", "IT", "BE", "PT" };

    private readonly LedgerLoopDbContext _db;
    private readonly TaxSettings _settings;

    public CustomerService(LedgerLoopDbContext db, TaxSettings settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<List<CustomerResponse>> ListAsync(CancellationToken ct)
    {
        var customers = await _db.Customers.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
        return customers.Select(Describe).ToList();
    }

    public async Task<CustomerResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return customer is null ? null : Describe(customer);
    }

    public async Task<CustomerResponse> CreateAsync(CustomerRequest request, CancellationToken ct)
    {
        Validate(request);

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            CountryCode = request.CountryCode.ToUpperInvariant(),
            TaxId = Normalise(request.TaxId),
            TaxOverride = request.TaxOverride,
            Currency = request.Currency.ToUpperInvariant(),
            ContactEmail = request.ContactEmail
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        return Describe(customer);
    }

    public async Task<CustomerResponse?> UpdateAsync(Guid id, CustomerRequest request, CancellationToken ct)
    {
        Validate(request);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return null;
        }

        customer.Name = request.Name.Trim();
        customer.CountryCode = request.CountryCode.ToUpperInvariant();
        customer.TaxId = Normalise(request.TaxId);
        customer.TaxOverride = request.TaxOverride;
        customer.Currency = request.Currency.ToUpperInvariant();
        customer.ContactEmail = request.ContactEmail;

        await _db.SaveChangesAsync(ct);

        return Describe(customer);
    }

    private CustomerResponse Describe(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        CountryCode = customer.CountryCode,
        TaxId = customer.TaxId,
        TaxOverride = customer.TaxOverride,
        Currency = customer.Currency,
        ContactEmail = customer.ContactEmail,
        TaxTreatment = CustomerTaxProfile.Resolve(customer, _settings.SellerCountry)
    };

    private static string? Normalise(string? taxId) =>
        string.IsNullOrWhiteSpace(taxId) ? null : taxId.Replace(" ", string.Empty).ToUpperInvariant();

    private static void Validate(CustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new DomainValidationException("Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CountryCode) || request.CountryCode.Length != 2)
        {
            throw new DomainValidationException("Country code must be a two letter ISO code.");
        }

        if (!SupportedCountries.Contains(request.CountryCode.ToUpperInvariant()))
        {
            throw new DomainValidationException($"Country {request.CountryCode} is not enabled for billing.");
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3)
        {
            throw new DomainValidationException("Currency must be a three letter ISO code.");
        }
    }
}
