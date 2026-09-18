namespace LedgerLoop.Api.Domain;

public class AuditEvent
{
    public long Id { get; set; }

    public Guid InvoiceId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string? ActorRole { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
