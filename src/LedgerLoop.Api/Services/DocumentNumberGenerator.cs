using LedgerLoop.Api.Data;
using LedgerLoop.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LedgerLoop.Api.Services;

public sealed class DocumentNumberGenerator
{
    private readonly LedgerLoopDbContext _db;

    public DocumentNumberGenerator(LedgerLoopDbContext db)
    {
        _db = db;
    }

    public async Task<string> NextAsync(DocumentKind kind, DateOnly documentDate, CancellationToken ct)
    {
        var next = await _db.Database
            .SqlQueryRaw<long>("SELECT nextval('document_number_seq') AS \"Value\"")
            .SingleAsync(ct);

        var prefix = kind == DocumentKind.CreditNote ? "CN" : "INV";
        return $"{prefix}-{documentDate.Year:D4}-{next:D5}";
    }
}
