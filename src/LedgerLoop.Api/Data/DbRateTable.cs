using LedgerLoop.Api.Tax;
using Microsoft.EntityFrameworkCore;

namespace LedgerLoop.Api.Data;

public sealed class DbRateTable : IRateTable
{
    private readonly LedgerLoopDbContext _db;
    private readonly ILogger<DbRateTable> _logger;
    private IReadOnlyList<RateTableEntry>? _entries;

    public DbRateTable(LedgerLoopDbContext db, ILogger<DbRateTable> logger)
    {
        _db = db;
        _logger = logger;
    }

    public IReadOnlyList<RateTableEntry> Entries
    {
        get
        {
            if (_entries is not null)
            {
                return _entries;
            }

            var rows = _db.TaxRates
                .AsNoTracking()
                .OrderBy(r => r.CountryCode)
                .ThenBy(r => r.RateKind)
                .ThenBy(r => r.EffectiveFrom)
                .ToList();

            if (rows.Count == 0)
            {
                _logger.LogWarning("Billing rate table is empty; falling back to the packaged schedule.");
                _entries = StaticRateTable.Instance.Entries;
                return _entries;
            }

            _entries = rows
                .Select(r => new RateTableEntry(r.CountryCode, r.RateKind, r.RatePercent, r.EffectiveFrom, r.EffectiveTo))
                .ToList();

            return _entries;
        }
    }
}
