using LedgerLoop.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LedgerLoop.Api.Data;

public class LedgerLoopDbContext : DbContext
{
    public LedgerLoopDbContext(DbContextOptions<LedgerLoopDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<TaxRate> TaxRates => Set<TaxRate>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(c => c.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsRequired();
            entity.Property(c => c.TaxId).HasColumnName("tax_id").HasMaxLength(32);
            entity.Property(c => c.TaxOverride).HasColumnName("tax_override").HasMaxLength(16);
            entity.Property(c => c.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
            entity.Property(c => c.ContactEmail).HasColumnName("contact_email").HasMaxLength(200);
            entity.Property(c => c.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(c => c.Name);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).HasColumnName("id");
            entity.Property(i => i.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(32).IsRequired();
            entity.HasIndex(i => i.InvoiceNumber).IsUnique();
            entity.Property(i => i.CustomerId).HasColumnName("customer_id");
            entity.Property(i => i.DocumentKind).HasColumnName("document_kind").HasConversion<int>();
            entity.Property(i => i.Status).HasColumnName("status").HasConversion<int>();
            entity.Property(i => i.PriceMode).HasColumnName("price_mode").HasConversion<int>();
            entity.Property(i => i.InvoiceDate).HasColumnName("invoice_date");
            entity.Property(i => i.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
            entity.Property(i => i.InvoiceDiscountPercent).HasColumnName("invoice_discount_percent").HasPrecision(5, 2);
            entity.Property(i => i.ManualTaxAmount).HasColumnName("manual_tax_amount").HasPrecision(18, 2);
            entity.Property(i => i.ManualTaxReason).HasColumnName("manual_tax_reason").HasMaxLength(400);
            entity.Property(i => i.OriginalInvoiceId).HasColumnName("original_invoice_id");
            entity.Property(i => i.Notes).HasColumnName("notes").HasMaxLength(1000);
            entity.Property(i => i.NetAmount).HasColumnName("net_amount").HasPrecision(18, 2);
            entity.Property(i => i.TaxAmount).HasColumnName("tax_amount").HasPrecision(18, 2);
            entity.Property(i => i.GrossAmount).HasColumnName("gross_amount").HasPrecision(18, 2);
            entity.Property(i => i.PostingAdjustment).HasColumnName("posting_adjustment").HasPrecision(18, 2);
            entity.Property(i => i.TaxTreatment).HasColumnName("tax_treatment").HasMaxLength(32);
            entity.Property(i => i.PostedAt).HasColumnName("posted_at");
            entity.Property(i => i.CreatedAt).HasColumnName("created_at");
            entity.Property(i => i.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(i => i.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.ToTable("invoice_lines");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).HasColumnName("id");
            entity.Property(l => l.InvoiceId).HasColumnName("invoice_id");
            entity.Property(l => l.LineNumber).HasColumnName("line_number");
            entity.Property(l => l.Description).HasColumnName("description").HasMaxLength(400).IsRequired();
            entity.Property(l => l.Category).HasColumnName("category").HasConversion<int>();
            entity.Property(l => l.Quantity).HasColumnName("quantity").HasPrecision(18, 4);
            entity.Property(l => l.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 4);
            entity.Property(l => l.LineDiscountPercent).HasColumnName("line_discount_percent").HasPrecision(5, 2);
            entity.Property(l => l.RatePercent).HasColumnName("rate_percent").HasPrecision(5, 2);
            entity.Property(l => l.NetAmount).HasColumnName("net_amount").HasPrecision(18, 2);
            entity.Property(l => l.TaxAmount).HasColumnName("tax_amount").HasPrecision(18, 2);
            entity.Property(l => l.GrossAmount).HasColumnName("gross_amount").HasPrecision(18, 2);
            entity.Property(l => l.TreatmentCode).HasColumnName("treatment_code").HasMaxLength(32);

            entity.HasOne(l => l.Invoice)
                .WithMany(i => i.Lines)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(l => new { l.InvoiceId, l.LineNumber }).IsUnique();
        });

        modelBuilder.Entity<TaxRate>(entity =>
        {
            entity.ToTable("tax_rates");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasColumnName("id");
            entity.Property(r => r.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsRequired();
            entity.Property(r => r.RateKind).HasColumnName("rate_kind").HasConversion<int>();
            entity.Property(r => r.RatePercent).HasColumnName("rate_percent").HasPrecision(5, 2);
            entity.Property(r => r.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(r => r.EffectiveTo).HasColumnName("effective_to");
            entity.HasIndex(r => new { r.CountryCode, r.RateKind, r.EffectiveFrom });

            entity.HasData(TaxRateSeed.Rows);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasColumnName("id");
            entity.Property(a => a.InvoiceId).HasColumnName("invoice_id");
            entity.Property(a => a.EventType).HasColumnName("event_type").HasMaxLength(64).IsRequired();
            entity.Property(a => a.Detail).HasColumnName("detail").HasMaxLength(2000);
            entity.Property(a => a.ActorRole).HasColumnName("actor_role").HasMaxLength(64);
            entity.Property(a => a.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(a => a.InvoiceId);
        });
    }
}
