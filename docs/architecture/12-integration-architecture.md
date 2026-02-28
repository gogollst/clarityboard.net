# Integration Architecture

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Integration Overview

```
┌─────────────────────────────────────────────────────┐
│                  Clarity Board                       │
│                                                     │
│  ┌──────────────────────────────────────────────┐  │
│  │           Integration Layer                   │  │
│  │                                               │  │
│  │  ┌─────────┐ ┌─────────┐ ┌──────────────┐   │  │
│  │  │ Webhook │ │  Pull   │ │   Export     │   │  │
│  │  │ Inbound │ │ Adapter │ │  Outbound    │   │  │
│  │  └────┬────┘ └────┬────┘ └──────┬───────┘   │  │
│  │       │           │             │            │  │
│  │  ┌────▼───────────▼─────────────▼────────┐   │  │
│  │  │        Mapping Rules Engine            │   │  │
│  │  └───────────────────────────────────────┘   │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
└───────┬──────────┬──────────┬──────────┬───────────┘
        │          │          │          │
   ┌────▼───┐ ┌───▼───┐ ┌───▼───┐ ┌───▼────┐
   │ Billing│ │ CRM   │ │Banking│ │  HR    │
   │ Stripe │ │Hubspot│ │ Fints │ │Personio│
   │ Paddle │ │Salesf. │ │ API   │ │BambooHR│
   └────────┘ └───────┘ └───────┘ └────────┘

   ┌────────┐ ┌───────┐ ┌────────┐ ┌────────┐
   │Marketing│ │  ERP  │ │ DATEV  │ │GetMOSS │
   │Google  │ │Custom │ │ Export │ │Virtual │
   │Meta    │ │       │ │        │ │Cards   │
   └────────┘ └───────┘ └────────┘ └────────┘
```

---

## 2. Inbound Integration Patterns

### Webhook-Based (Push)

| Source | Events | Mapping |
|--------|--------|---------|
| **Stripe** | `invoice.created`, `invoice.paid`, `charge.refunded`, `subscription.*` | Revenue (8xxx), AR (1200), VAT (17xx) |
| **Paddle** | `transaction.completed`, `subscription.created`, `subscription.canceled` | Revenue, AR, VAT |
| **HubSpot** | `deal.propertyChange`, `contact.creation`, `company.creation` | Pipeline metrics, KPI source data |
| **Salesforce** | `Opportunity`, `Lead`, `Account` changes | Pipeline metrics, KPI source data |
| **Personio** | `employee.created`, `absence.created`, `attendance.created` | Personnel costs (4xxx), HR KPIs |
| **BambooHR** | Employee events, time-off, payroll | Personnel costs, HR KPIs |
| **Google Ads** | Campaign metrics, spend data | Marketing costs (46xx), Marketing KPIs |
| **Meta Ads** | Campaign metrics, conversion data | Marketing costs (46xx), Marketing KPIs |

### Pull-Based (Scheduled)

| Source | Method | Schedule | Mapping |
|--------|--------|----------|---------|
| **Banking (FinTS)** | FinTS 3.0 API | Every 4 hours | Bank (1200), cash flow |
| **Banking (PSD2)** | Open Banking API | Every 4 hours | Bank (1200), cash flow |
| **ECB Exchange Rates** | ECB SDMX API | Daily 16:00 CET | Exchange rate table |
| **GetMOSS** | GetMOSS API | On demand + daily | Expense receipts, booking suggestions |

---

## 3. DATEV Export Architecture

### Export Format: EXTF (Extended Transfer Format)

```
DATEV ASCII Export Structure:
├── Header Record (Line 1)
│   ├── EXTF version (700)
│   ├── Data category (21 = posting records, 20 = accounts)
│   ├── Format name
│   ├── Format version
│   ├── Created timestamp
│   ├── Consultant number
│   ├── Client number
│   ├── Fiscal year start
│   ├── Account length (4-8 digits)
│   └── Date range (from/to)
│
└── Data Records (Line 2+)
    ├── Transaction amount (Umsatz)
    ├── Debit/Credit indicator (S/H)
    ├── Account number (Konto)
    ├── Counter account (Gegenkonto)
    ├── BU-Schlüssel (tax code)
    ├── Booking date (DDMM)
    ├── Document field 1 (Belegfeld 1)
    ├── Document field 2 (Belegfeld 2)
    ├── Description (Buchungstext)
    ├── Cost center 1 (Kostenstelle 1)
    ├── Cost center 2 (Kostenstelle 2)
    └── Additional fields (up to 116 total)
```

### Export Service

```csharp
public class DatevExportService
{
    public async Task<DatevExportResult> GenerateExport(
        Guid entityId, int year, int month)
    {
        // 1. Load entity configuration (consultant number, client number, account length)
        var entity = await _entityRepo.GetWithDatevConfigAsync(entityId);

        // 2. Load all posted journal entries for the period
        var entries = await _accountingRepo.GetPostedEntriesAsync(entityId, year, month);

        // 3. Validate entries for DATEV compatibility
        var validationResult = _validator.Validate(entries, entity.DatevConfig);
        if (!validationResult.IsValid)
        {
            return DatevExportResult.Failed(validationResult.Errors);
        }

        // 4. Generate EXTF file
        var extfContent = _extfGenerator.Generate(entries, entity.DatevConfig);

        // 5. Generate account labels file (optional)
        var accountLabels = _extfGenerator.GenerateAccountLabels(
            await _accountRepo.GetActiveAsync(entityId),
            entity.DatevConfig
        );

        // 6. Package into ZIP
        var zipBytes = CreateZip(new Dictionary<string, byte[]>
        {
            [$"EXTF_Buchungen_{year}{month:D2}.csv"] = extfContent,
            [$"EXTF_Kontenbeschriftungen_{year}{month:D2}.csv"] = accountLabels,
        });

        // 7. Store in MinIO for download
        var downloadUrl = await _storage.StoreAsync(
            $"datev-exports/{entityId}/{year}/{month:D2}/export.zip",
            zipBytes
        );

        // 8. Log export event
        await _auditService.LogAsync(new AuditEntry
        {
            Action = "datev_export.generated",
            EntityId = entityId,
            ResourceType = "DatevExport",
            NewValues = new { Year = year, Month = month, RecordCount = entries.Count }
        });

        return DatevExportResult.Success(downloadUrl, entries.Count);
    }
}
```

### EXTF Line Generation

```csharp
public class ExtfGenerator
{
    public byte[] Generate(List<JournalEntry> entries, DatevConfig config)
    {
        var sb = new StringBuilder();

        // Header line
        sb.AppendLine(GenerateHeader(config));

        // Data lines
        foreach (var entry in entries)
        {
            foreach (var line in entry.Lines)
            {
                sb.AppendLine(GenerateDataLine(entry, line, config));
            }
        }

        return Encoding.GetEncoding(1252).GetBytes(sb.ToString());  // DATEV requires Windows-1252
    }

    private string GenerateDataLine(JournalEntry entry, JournalEntryLine line, DatevConfig config)
    {
        var amount = line.DebitAmount > 0 ? line.DebitAmount : line.CreditAmount;
        var indicator = line.DebitAmount > 0 ? "S" : "H";

        // DATEV EXTF CSV format (semicolon-separated)
        return string.Join(";",
            FormatAmount(amount),           // Col 1: Umsatz
            indicator,                       // Col 2: S/H
            "EUR",                           // Col 3: WKZ (currency)
            "",                              // Col 4: Kurs (exchange rate)
            "",                              // Col 5: Basis-Umsatz
            line.Account.AccountNumber,      // Col 6: Konto
            GetCounterAccount(entry, line),  // Col 7: Gegenkonto
            line.VatCode ?? "",              // Col 8: BU-Schlüssel
            entry.EntryDate.ToString("ddMM"),// Col 9: Belegdatum
            entry.EntryNumber.ToString(),    // Col 10: Belegfeld 1
            "",                              // Col 11: Belegfeld 2
            Truncate(entry.Description, 60), // Col 12: Buchungstext
            line.CostCenter ?? "",           // Col 13: Kostenstelle 1
            ""                               // Col 14: Kostenstelle 2
            // ... remaining fields
        );
    }
}
```

### Export Validation Rules

| Rule | Check | Error Code |
|------|-------|-----------|
| Balanced entries | Debit sum = Credit sum per entry | DATEV-001 |
| Valid accounts | All accounts exist in chart | DATEV-002 |
| Valid BU-Schlüssel | Tax code in allowed range | DATEV-003 |
| Period consistency | All entries within export period | DATEV-004 |
| Account length | Matches DATEV config (4-8 digits) | DATEV-005 |
| Character encoding | All text Windows-1252 compatible | DATEV-006 |
| Amount precision | Max 2 decimal places | DATEV-007 |
| Description length | Max 60 characters | DATEV-008 |

---

## 4. GetMOSS Integration

### Virtual Credit Card Workflow

```
1. Employee initiates expense
   → GetMOSS creates virtual card with budget limit

2. Employee makes purchase
   → GetMOSS captures receipt automatically

3. GetMOSS webhook → Clarity Board
   Event: transaction.completed
   Payload: { amount, vendor, category, receipt_url, card_id }

4. Clarity Board processes:
   a. Download receipt image from GetMOSS
   b. AI extraction (verify against transaction data)
   c. Generate booking suggestion
   d. Create cash flow entry
   e. Notify user for approval

5. On approval:
   → Create journal entry (expense account, VAT, bank/liability)
   → Update KPIs
```

### GetMOSS API Integration

```csharp
public class GetMossAdapter : IPullAdapter
{
    public string SourceType => "getmoss";

    public async Task<IEnumerable<WebhookEvent>> FetchNewEventsAsync(
        PullAdapterConfig config, DateTimeOffset since, CancellationToken ct)
    {
        var client = new GetMossClient(config.ApiKey, config.BaseUrl);

        // Fetch new transactions since last sync
        var transactions = await client.GetTransactionsAsync(since, ct);
        var events = new List<WebhookEvent>();

        foreach (var tx in transactions)
        {
            // Download receipt if available
            byte[]? receipt = null;
            if (tx.ReceiptUrl != null)
            {
                receipt = await client.DownloadReceiptAsync(tx.ReceiptUrl, ct);
            }

            events.Add(new WebhookEvent
            {
                SourceType = "getmoss",
                SourceId = config.SourceId,
                EventType = "transaction.completed",
                IdempotencyKey = tx.TransactionId,
                Payload = JsonSerializer.SerializeToDocument(new
                {
                    tx.TransactionId,
                    tx.Amount,
                    tx.Currency,
                    tx.Vendor,
                    tx.Category,
                    tx.CardId,
                    tx.EmployeeId,
                    tx.Timestamp,
                    HasReceipt = receipt != null
                })
            });
        }

        return events;
    }
}
```

---

## 5. Banking Integration

### FinTS 3.0 (German Banking Standard)

```csharp
public class FinTsAdapter : IPullAdapter
{
    public string SourceType => "banking";

    public async Task<IEnumerable<WebhookEvent>> FetchNewEventsAsync(
        PullAdapterConfig config, DateTimeOffset since, CancellationToken ct)
    {
        var client = new FinTsClient(new FinTsConfig
        {
            BankCode = config.GetSetting("bank_code"),
            Url = config.GetSetting("fints_url"),
            UserId = config.GetSetting("user_id"),
            Pin = await _vault.GetSecretAsync(config.GetSetting("pin_vault_ref")),
        });

        // Connect and authenticate
        await client.ConnectAsync(ct);

        // Fetch account statements (MT940 format)
        var statements = await client.GetStatementsAsync(
            config.GetSetting("account_number"),
            since.DateTime,
            DateTime.UtcNow,
            ct
        );

        return statements.SelectMany(s => s.Transactions.Select(tx => new WebhookEvent
        {
            SourceType = "banking",
            SourceId = config.SourceId,
            EventType = tx.Amount > 0 ? "transaction.credit" : "transaction.debit",
            IdempotencyKey = $"{tx.Date:yyyyMMdd}-{tx.Reference}",
            Payload = JsonSerializer.SerializeToDocument(new
            {
                tx.Date,
                tx.Amount,
                tx.Currency,
                tx.Reference,
                tx.RemittanceInfo,
                tx.CreditorName,
                tx.DebtorName,
                Balance = s.ClosingBalance
            })
        }));
    }
}
```

### Bank Transaction → Journal Entry Mapping

| Transaction Pattern | Debit Account | Credit Account | Auto |
|--------------------|---------------|----------------|------|
| Customer payment received | Bank (1200) | AR (1400) | Yes |
| Supplier payment sent | AP (1600) | Bank (1200) | Yes |
| Salary payment | Personnel (4xxx) | Bank (1200) | Yes (with payroll data) |
| Tax payment | Tax liability (17xx) | Bank (1200) | Semi-auto |
| Unknown inflow | Bank (1200) | Suspense (1590) | No (manual) |
| Unknown outflow | Suspense (1590) | Bank (1200) | No (manual) |

---

## 6. ECB Exchange Rate Service

```csharp
public class EcbExchangeRateService : IHostedService
{
    // Runs daily at 16:00 CET (ECB publishes rates ~16:00)
    private const string EcbUrl =
        "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";

    public async Task FetchRatesAsync(CancellationToken ct)
    {
        var response = await _httpClient.GetStringAsync(EcbUrl, ct);
        var rates = ParseEcbXml(response);

        foreach (var rate in rates)
        {
            await _rateRepo.UpsertAsync(new ExchangeRate
            {
                BaseCurrency = "EUR",
                TargetCurrency = rate.Currency,
                Rate = rate.Rate,
                RateDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Source = "ECB"
            });

            // Update Redis cache
            await _cache.SetAsync(
                $"clarityboard:fx:EUR:{rate.Currency}:{DateOnly.FromDateTime(DateTime.UtcNow)}",
                rate.Rate,
                TimeSpan.FromHours(25)
            );
        }
    }
}
```

---

## 7. Integration Health Monitoring

### Source Health Dashboard

| Metric | Per Source | Alert |
|--------|-----------|-------|
| Last event received | Timestamp | > expected interval |
| Events/hour | Counter | < 50% of baseline |
| Error rate | Percentage | > 5% |
| Processing latency P95 | Duration | > 10 seconds |
| Mapping coverage | Percentage | < 90% events mapped |
| Auth failures | Counter | > 0 in 1 hour |

### Webhook Configuration UI

```
Source Configuration:
┌─────────────────────────────────────────────────┐
│ Source: Stripe                                   │
│ Type: billing                                    │
│ Status: ● Active                                │
│                                                  │
│ Endpoint: /api/v1/webhooks/billing/stripe-main  │
│ Webhook Secret: ●●●●●●●● [Regenerate]          │
│ IP Allowlist: [Optional]                         │
│                                                  │
│ Entity Mapping: Clarity GmbH                    │
│ Rate Limit: 1000/min                            │
│ Priority: Normal                                │
│                                                  │
│ Events (last 24h):                              │
│   Received: 342     │  Processed: 340           │
│   Failed: 2         │  DLQ: 0                   │
│   Avg Latency: 1.2s │  Error Rate: 0.6%        │
│                                                  │
│ [Test Connection] [View Events] [Disable]       │
└─────────────────────────────────────────────────┘
```

---

## 8. Integration Error Recovery

| Scenario | Detection | Recovery |
|----------|-----------|---------|
| Source offline | No events for > expected interval | Alert admin, queue events on source side |
| Auth failure | 401 responses | Alert admin, check secret rotation |
| Mapping error | Transform exception | DLQ, admin reviews, fix mapping rule |
| Duplicate events | Idempotency check | Skip silently (log for audit) |
| Out-of-order events | Timestamp check | Process by business date, not received date |
| Schema change | Parsing exception | DLQ, alert admin, update transformer |
| Rate limit exceeded | 429 responses | Back-off, increase limits if needed |

---

## Document Navigation

- Previous: [Security Architecture](./11-security-architecture.md)
- [Back to Index](./README.md)
