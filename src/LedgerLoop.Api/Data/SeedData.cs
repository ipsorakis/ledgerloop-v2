using LedgerLoop.Api.Contracts;
using LedgerLoop.Api.Domain;
using LedgerLoop.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LedgerLoop.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var db = services.GetRequiredService<LedgerLoopDbContext>();
        var invoices = services.GetRequiredService<InvoiceService>();
        var posting = services.GetRequiredService<InvoicePostingService>();
        var logger = services.GetRequiredService<ILogger<LedgerLoopDbContext>>();

        if (await db.Customers.AnyAsync(ct))
        {
            return;
        }

        var customers = new[]
        {
            new Customer
            {
                Name = "Boulangerie Lemaire",
                CountryCode = "FR",
                TaxId = "FR12345678901",
                Currency = "EUR",
                ContactEmail = "compta@lemaire.example"
            },
            new Customer
            {
                Name = "Studio Delacroix SARL",
                CountryCode = "FR",
                TaxId = "FR98765432109",
                TaxOverride = "STD",
                Currency = "EUR",
                ContactEmail = "billing@delacroix.example"
            },
            new Customer
            {
                Name = "Nordwind Logistik GmbH",
                CountryCode = "DE",
                TaxId = "DE123456789",
                Currency = "EUR",
                ContactEmail = "rechnung@nordwind.example"
            },
            new Customer
            {
                Name = "Hansa Kliniken GmbH",
                CountryCode = "DE",
                TaxId = "DE1234",
                Currency = "EUR",
                ContactEmail = "einkauf@hansa-kliniken.example"
            },
            new Customer
            {
                Name = "Iberia Servicios SL",
                CountryCode = "ES",
                TaxId = "ESB12345678",
                Currency = "EUR",
                ContactEmail = "facturas@iberia-servicios.example"
            },
            new Customer
            {
                Name = "Thames Ledger Ltd",
                CountryCode = "GB",
                TaxId = "GB123456789",
                Currency = "GBP",
                ContactEmail = "accounts@thamesledger.example"
            },
            new Customer
            {
                Name = "Association Solidarite Ouest",
                CountryCode = "FR",
                TaxOverride = "1",
                Currency = "EUR",
                ContactEmail = "tresorier@solidarite-ouest.example"
            }
        };

        db.Customers.AddRange(customers);
        await db.SaveChangesAsync(ct);

        var drafts = new List<(DraftInvoiceRequest Request, bool Post)>
        {
            (new DraftInvoiceRequest
            {
                CustomerId = customers[0].Id,
                InvoiceDate = new DateOnly(2022, 3, 14),
                PriceMode = PriceMode.Exclusive,
                Notes = "Quarterly bakery software subscription",
                Lines =
                {
                    new InvoiceLineRequest { Description = "LedgerLoop Standard subscription", Category = LineCategory.DigitalServices, Quantity = 3, UnitPrice = 49.90m },
                    new InvoiceLineRequest { Description = "Onboarding workshop", Category = LineCategory.ProfessionalServices, Quantity = 1, UnitPrice = 320m }
                }
            }, true),
            (new DraftInvoiceRequest
            {
                CustomerId = customers[1].Id,
                InvoiceDate = new DateOnly(2022, 11, 2),
                PriceMode = PriceMode.Inclusive,
                Notes = "Retail bundle sold at shelf prices",
                Lines =
                {
                    new InvoiceLineRequest { Description = "Printed handbook", Category = LineCategory.PrintedBooks, Quantity = 12, UnitPrice = 14.99m },
                    new InvoiceLineRequest { Description = "Delivery", Category = LineCategory.Shipping, Quantity = 1, UnitPrice = 9.90m }
                }
            }, true),
            (new DraftInvoiceRequest
            {
                CustomerId = customers[2].Id,
                InvoiceDate = new DateOnly(2023, 6, 30),
                PriceMode = PriceMode.Exclusive,
                Notes = "Cross-border fleet integration",
                Lines =
                {
                    new InvoiceLineRequest { Description = "Integration engineering", Category = LineCategory.ProfessionalServices, Quantity = 24, UnitPrice = 118m },
                    new InvoiceLineRequest { Description = "Connector licence", Category = LineCategory.DigitalServices, Quantity = 1, UnitPrice = 1450m, LineDiscountPercent = 5m }
                }
            }, true),
            (new DraftInvoiceRequest
            {
                CustomerId = customers[3].Id,
                InvoiceDate = new DateOnly(2024, 1, 18),
                PriceMode = PriceMode.Exclusive,
                InvoiceDiscountPercent = 3m,
                Notes = "Clinic supplies, tax identifier still pending verification",
                Lines =
                {
                    new InvoiceLineRequest { Description = "Sterile dressing packs", Category = LineCategory.MedicalSupplies, Quantity = 40, UnitPrice = 12.35m },
                    new InvoiceLineRequest { Description = "Cold chain transport", Category = LineCategory.Shipping, Quantity = 1, UnitPrice = 85m }
                }
            }, false),
            (new DraftInvoiceRequest
            {
                CustomerId = customers[4].Id,
                InvoiceDate = new DateOnly(2024, 4, 5),
                PriceMode = PriceMode.Exclusive,
                Notes = "Reverse charge services for Spanish reseller",
                Lines =
                {
                    new InvoiceLineRequest { Description = "Implementation support", Category = LineCategory.ProfessionalServices, Quantity = 16, UnitPrice = 97.50m }
                }
            }, true),
            (new DraftInvoiceRequest
            {
                CustomerId = customers[5].Id,
                InvoiceDate = new DateOnly(2024, 5, 21),
                PriceMode = PriceMode.Exclusive,
                Currency = "GBP",
                Notes = "Export of licences to the United Kingdom",
                Lines =
                {
                    new InvoiceLineRequest { Description = "Annual licence", Category = LineCategory.DigitalServices, Quantity = 5, UnitPrice = 240m }
                }
            }, false),
            (new DraftInvoiceRequest
            {
                CustomerId = customers[6].Id,
                InvoiceDate = new DateOnly(2024, 6, 11),
                PriceMode = PriceMode.Exclusive,
                Notes = "Charity programme, exemption on file",
                Lines =
                {
                    new InvoiceLineRequest { Description = "Community food parcels", Category = LineCategory.FoodStaples, Quantity = 250, UnitPrice = 3.4m },
                    new InvoiceLineRequest { Description = "Volunteer training", Category = LineCategory.ProfessionalServices, Quantity = 2, UnitPrice = 180m }
                }
            }, false)
        };

        var posted = new List<Guid>();

        foreach (var (request, post) in drafts)
        {
            var created = await invoices.CreateDraftAsync(request, "billing", ct);

            if (post)
            {
                await posting.PostAsync(created.Id, "billing", ct);
                posted.Add(created.Id);
            }
        }

        if (posted.Count > 0)
        {
            var creditNotes = services.GetRequiredService<CreditNoteService>();
            var creditNote = await creditNotes.CreateAsync(new CreditNoteRequest
            {
                InvoiceId = posted[0],
                Reason = "Workshop cancelled by the customer",
                LineNumbers = { 2 }
            }, "accounting", ct);

            await posting.PostAsync(creditNote.Id, "accounting", ct);
        }

        logger.LogInformation("Seeded {Customers} customers and {Documents} documents", customers.Length, drafts.Count + 1);
    }
}
