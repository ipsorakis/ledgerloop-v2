# LedgerLoop

LedgerLoop is a billing back office for small European accounting practices. Bookkeepers
capture customer records, prepare draft invoices, check the totals, post documents to the
ledger, issue credit notes and hand a period extract to the auditors.

The product has been in service for several years. The current release is 2.4 and it still
serves the 2021 desktop client alongside the web editor.

> **The tax rules in this repository are synthetic.** Rates, effective dates, reverse-charge
> conditions and adjustments were invented for this application. They resemble European VAT
> only in shape. Do not use LedgerLoop, its figures or its rules for real tax, accounting or
> legal decisions.

## Capabilities

- Customer management, including country, tax identifier and billing currency
- Draft invoices with multiple line categories, per-line discounts and an invoice discount
- Inclusive and exclusive pricing
- Immediate totals in the editor and an authoritative preview from the ledger
- Posting, which freezes the figures and writes the ledger entry
- Credit notes for a whole invoice or for selected lines
- Manual tax amounts for the accounting role
- Period export for auditors
- Documents in historical periods, with the rates and adjustments of those periods

## Architecture

```
src/LedgerLoop.Api     ASP.NET Core Web API: domain, EF Core data access, billing services
src/LedgerLoop.Web     React editor (Vite), talks to the API through /api
database               DBA scripts: posting routine, reconciliation query, container init
fixtures/golden-invoices  Golden documents with the figures the ledger is expected to produce
tests/LedgerLoop.UnitTests        Calculation and fixture regression tests
tests/LedgerLoop.IntegrationTests API tests against a real PostgreSQL database
```

The API is a conventional controller/service/DbContext application. Controllers translate
requests, services own the billing behaviour and EF Core maps the `customers`, `invoices`,
`invoice_lines`, `tax_rates` and `audit_events` tables. Document numbering and posting run
through raw SQL against database objects that are older than the EF Core model.

`TaxResult` is the shared result shape. The preview endpoint returns it, posting stores the
figures it carries and the audit export re-emits it, so the field names are part of the
contract with the finance data warehouse feed and the archive reprint tool.

### An evolved application

LedgerLoop was not designed in one pass, and the repository shows it. Billing behaviour sits
in the places where it was needed at the time: some in the API services, some in the
database, some in the editor so that figures appear while an operator types. Two totals
endpoints exist because the desktop client submits the figures it already displayed and
cannot be changed quickly. Configuration is partly in `appsettings.json`, partly in seeded
reference data and partly in code constants.

These constraints are deliberate. Changing them casually breaks consumers that are not in
this repository, so treat the current behaviour as the specification and the golden documents
as the record of it.

## Technology

| Area | Choice |
| --- | --- |
| API | C# 12, .NET 8, ASP.NET Core, Serilog, Swashbuckle |
| Data | PostgreSQL 16, EF Core 8 (Npgsql), raw SQL for numbering and posting |
| Web | React 18, TypeScript 5.5, Vite 5 |
| Tests | xUnit, FluentAssertions, Testcontainers, Vitest, Testing Library |
| CI | GitHub Actions |

Monetary calculations in the API use `decimal`. The editor uses JavaScript numbers, which is
why the ledger figure is the one that is stored.

## Prerequisites

- .NET SDK 8.0
- Node.js 20 or newer
- PostgreSQL 16, or Docker for the compose stack
- Linux, macOS or WSL2

## Start everything with Docker

```bash
docker compose up --build
```

| Service | URL |
| --- | --- |
| Editor | http://localhost:8080 |
| API | http://localhost:5080 |
| Swagger | http://localhost:5080/swagger |
| Health | http://localhost:5080/health |

The API container migrates the database and seeds development data on startup.

## Run it locally without Docker

```bash
# 1. database
createdb ledgerloop
psql -d ledgerloop -c "CREATE ROLE ledgerloop LOGIN PASSWORD 'ledgerloop'; GRANT ALL ON DATABASE ledgerloop TO ledgerloop;"

# 2. API (migrates and seeds in Development)
dotnet restore
dotnet run --project src/LedgerLoop.Api

# 3. editor, in a second shell
cd src/LedgerLoop.Web
npm install
npm run dev
```

The editor is served on http://localhost:5173 and proxies `/api` and `/health` to the API. Set
`LEDGERLOOP_API_URL` if the API is not on its default port.

### Migrations and seed data

`Database:AutoMigrate` and `Database:Seed` are enabled in `appsettings.Development.json`, so a
development run brings the schema up to date and inserts the reference rates, demo customers
and demo documents. Both are off in `appsettings.json`.

To manage the schema by hand:

```bash
dotnet tool restore
dotnet dotnet-ef database update --project src/LedgerLoop.Api
dotnet dotnet-ef migrations add <Name> --project src/LedgerLoop.Api --output-dir Data/Migrations
```

## Tests

```bash
# calculation and golden-document regression tests
dotnet test tests/LedgerLoop.UnitTests

# API and posting tests against PostgreSQL
#   with a server you already run:
LEDGERLOOP_TEST_CONNECTION="Host=localhost;Port=5432;Database=ledgerloop;Username=ledgerloop;Password=ledgerloop" \
  dotnet test tests/LedgerLoop.IntegrationTests
#   without one, a throwaway container is started through Testcontainers:
dotnet test tests/LedgerLoop.IntegrationTests

# everything
dotnet test

# editor
cd src/LedgerLoop.Web
npm run lint
npm test
```

The integration tests create their own database on the server named by
`LEDGERLOOP_TEST_CONNECTION` and drop it afterwards, so they can run against a development
server without touching its data.

### Golden documents

`fixtures/golden-invoices/` holds fifteen documents with the ledger figures they must produce:
line rates and totals, the document net, tax and gross, the posting adjustment, and the
estimate the editor shows for the same document. The unit tests replay them through the
billing services, the integration tests post them through the API and the editor tests check
them against the estimator.

Some documents record an editor estimate that differs from the ledger by one cent. That is the
current behaviour of the product: the editor keeps line figures at full precision and rounds
the totals once, the ledger rounds every line. The stored figure is always the ledger figure.

## Example requests

```bash
API=http://localhost:5080

# health
curl -s $API/health

# create a customer
CUSTOMER=$(curl -s -X POST $API/api/customers \
  -H 'content-type: application/json' \
  -d '{"name":"Cabinet Marchand","countryCode":"FR","taxId":"FR12345678901","currency":"EUR"}' \
  | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')

# preview an unsaved document (0 = exclusive prices, 1 = professional services)
curl -s -X POST $API/api/invoices/preview \
  -H 'content-type: application/json' \
  -d "{\"customerId\":\"$CUSTOMER\",\"invoiceDate\":\"2024-02-01\",\"priceMode\":0,
       \"lines\":[{\"description\":\"Consulting\",\"category\":1,\"quantity\":3,\"unitPrice\":99.99}]}"

# save the draft, then post it
INVOICE=$(curl -s -X POST $API/api/invoices \
  -H 'content-type: application/json' \
  -d "{\"customerId\":\"$CUSTOMER\",\"invoiceDate\":\"2024-02-01\",\"priceMode\":0,
       \"lines\":[{\"description\":\"Consulting\",\"category\":1,\"quantity\":3,\"unitPrice\":99.99}]}" \
  | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')

curl -s -X POST $API/api/invoices/$INVOICE/post

# credit it (a manual tax amount and a credit note need the accounting role)
curl -s -X POST $API/api/credit-notes \
  -H 'content-type: application/json' \
  -H 'X-LedgerLoop-Role: accounting' \
  -d "{\"invoiceId\":\"$INVOICE\",\"reason\":\"Engagement cancelled\"}"

# period extract for the auditors
curl -s "$API/api/audit/export?from=2024-01-01&to=2024-12-31"
```

The full API is described at `/swagger`.

### Roles

Requests carry `X-LedgerLoop-Role` and `X-LedgerLoop-User` headers; there is no identity
provider in this repository. Entering a manual tax amount requires the `accounting` role.
