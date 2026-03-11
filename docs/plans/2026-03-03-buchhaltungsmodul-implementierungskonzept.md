# Implementierungskonzept: ClarityBoard Buchhaltungsmodul

**Version:** 1.0 | **Datum:** 03.03.2026 | **Status:** Final
**Scope:** Vollständige doppelte Buchführung, DATEV-Export, HR-Integration, GoBD-Compliance

---

## 1. Zusammenfassung & Scope

### Was wird 1:1 umgesetzt (aus Fachkonzept)

- Alle 9 DB-Schemas (`core`, `accounting`, `document`, `planning`, `cashflow`, `i18n`, `datev`, `audit`) exakt wie in §4
- SKR 04 als Default, alle 119 Aqua-Cloud-Individualkonten als Seeding
- GoBD-konforme Unveränderbarkeit: SHA-256-Verkettung aller `journal_entries`
- DATEV EXTF v700: Buchungsstapel + Kontenbeschriftungen + Debitoren/Kreditoren als Windows-1252 CSV
- Belegerfassungs-Pipeline: Upload → OCR/AI → Validierung → Buchungsvorschlag → Freigabe
- Szenario-Engine: Ist vs. Budget vs. Forecast vs. Best/Worst Case
- Cash-Flow direkt + indirekt + 13-Wochen-Forecast
- Materialized Views `mv_trial_balance` + `mv_bwa` für Performance
- Alle 8 KPIs aus §7.2 direkt auf dem Datenmodell

### Leicht angepasst

- **Schemas gehören zur bestehenden ClarityBoard-DB** — kein separates Datenbankschema, nur neue PG-Schemas in der existierenden Datenbank
- **`core.legal_entities`** ersetzt/erweitert die bestehende `public.entities`-Tabelle via View (`core.legal_entities` → View auf `public.entities` mit DATEV-Spalten in separater Extension-Tabelle)
- **`core.cost_centers`** ist die neue Quelle der Wahrheit — HR-Modul referenziert diese Tabelle rückwärts (HR hatte bisher `hr.cost_centers` → nicht duplizieren)
- **Monetäre Werte**: Fachkonzept nutzt `NUMERIC(18,2)` → ClarityBoard-Konvention ist `BIGINT` (Cent) + `CHAR(3)` (ISO-4217). **Für Buchhaltung: BIGINT-Cent beibehalten**, Anzeige-Konvertierung im Frontend

### Explizite HR-Integration

| Regel | Implementierung |
|-------|----------------|
| HR = Single Source of Truth für Personal | `core.cost_centers` (type=`employee`) hat `hr_employee_id UUID REFERENCES hr.employees(id)` — keine Datenduplizierung |
| Gehaltsbuchungen referenzieren HR | `accounting.journal_entries.source_ref` = `hr:salary:{salary_history_id}`, `journal_entry_lines.hr_employee_id` FK |
| Reisekosten → Buchungen automatisch | Event-Handler `TravelExpenseApproved` → `CreateJournalEntryFromTravelExpenseCommand` |
| Kostenstellen 1:1 HR-Mitarbeiter | `core.cost_centers` Typ `employee` wird beim HR-Onboarding auto-angelegt |
| Statistiken inkl. Personal | `mv_trial_balance` + KPI-Queries filtern nach `cost_center_id` → HR-Employee-Join |
| HR-Änderungen triggern Buchhaltung | `ISalaryChangedEventHandler`, `IEmployeeTerminatedEventHandler` in Application Layer |

---

## 2. Datenmodell

### 2.1 Migrations-Strategie

**EF Core Code-First, manuelle SQL-Migrations** (wie im bestehenden Projekt — kein `dotnet ef migrations add` auf Prod):

```
1. migration_YYYYMMDDHHMMSS_AddAccountingCore.sql
2. migration_YYYYMMDDHHMMSS_AddAccountingJournal.sql
3. migration_YYYYMMDDHHMMSS_AddDocumentSchema.sql
4. migration_YYYYMMDDHHMMSS_AddPlanningSchema.sql
5. migration_YYYYMMDDHHMMSS_AddCashflowSchema.sql
6. migration_YYYYMMDDHHMMSS_AddDatevAuditSchema.sql
7. migration_YYYYMMDDHHMMSS_AddHrAccountingLinks.sql  ← HR-Integration
8. migration_YYYYMMDDHHMMSS_SeedSkr04Accounts.sql
9. migration_YYYYMMDDHHMMSS_SeedAquaCloudData.sql
```

Jede Migration wird in `ClarityBoardContext.MigrateAsync()` auto-applied. Designer-Dateien (`.Designer.cs`) werden manuell erstellt.

### 2.2 C# Entities (vollständig)

#### core.legal_entities — Erweiterung von public.entities

```csharp
// ClarityBoard.Domain/Entities/Accounting/LegalEntityExtension.cs
// Kein neues Objekt — Extension-Tabelle zu public.entities

namespace ClarityBoard.Domain.Entities.Accounting;

public class LegalEntityExtension
{
    private LegalEntityExtension() { }

    public Guid EntityId { get; private set; }                    // FK → public.entities.id
    public string ChartOfAccounts { get; private set; } = "SKR04";
    public int FiscalYearStartMonth { get; private set; } = 1;
    public string BaseCurrency { get; private set; } = "EUR";
    public string? DatevConsultantNumber { get; private set; }    // 5-stellig
    public string? DatevClientNumber { get; private set; }        // 5-stellig
    public string? TaxNumber { get; private set; }
    public string? VatId { get; private set; }
    public string? TradeRegisterNumber { get; private set; }

    public static LegalEntityExtension Create(
        Guid entityId, string chartOfAccounts = "SKR04",
        string? datevConsultantNumber = null,
        string? datevClientNumber = null)
    {
        return new LegalEntityExtension
        {
            EntityId = entityId,
            ChartOfAccounts = chartOfAccounts,
            DatevConsultantNumber = datevConsultantNumber,
            DatevClientNumber = datevClientNumber,
        };
    }
}
```

#### core.cost_centers — Mit HR-Integration

```csharp
// ClarityBoard.Domain/Entities/Accounting/CostCenter.cs
namespace ClarityBoard.Domain.Entities.Accounting;

public class CostCenter
{
    private CostCenter() { }

    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string Code { get; private set; } = string.Empty;      // z.B. "210"
    public string ShortName { get; private set; } = string.Empty; // z.B. "Dev"
    public string? LongName { get; private set; }
    public CostCenterType Type { get; private set; }
    public Guid? ParentId { get; private set; }
    public Guid? HrEmployeeId { get; private set; }               // ← HR-Link (bei Type=Employee)
    public bool IsActive { get; private set; } = true;
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public CostCenter? Parent { get; private set; }
    public ICollection<CostCenter> Children { get; private set; } = [];

    public static CostCenter Create(
        Guid entityId, string code, string shortName,
        CostCenterType type, Guid? parentId = null,
        Guid? hrEmployeeId = null)
    {
        if (type == CostCenterType.Employee && hrEmployeeId is null)
            throw new ArgumentException("Employee cost centers require hrEmployeeId.");
        return new CostCenter
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            Code = code,
            ShortName = shortName,
            Type = type,
            ParentId = parentId,
            HrEmployeeId = hrEmployeeId,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Deactivate(DateOnly validTo)
    {
        IsActive = false;
        ValidTo = validTo;
    }
}

public enum CostCenterType { Department, Employee, Project }
```

#### accounting.accounts (Entity-spezifischer Kontenplan)

```csharp
// ClarityBoard.Domain/Entities/Accounting/Account.cs
namespace ClarityBoard.Domain.Entities.Accounting;

public class Account
{
    private Account() { }

    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;  // "5900", "4402"
    public Guid? StandardAccountId { get; private set; }
    public int AccountClass { get; private set; }                       // 0-9
    public AccountType AccountType { get; private set; }
    public DebitCreditIndicator ShIndicator { get; private set; }
    public bool IsIndividual { get; private set; }                     // I-Konto
    public int? VatAutoFunction { get; private set; }
    public int? HfType { get; private set; }
    public int? FunctionCode { get; private set; }
    public decimal? FeFactor { get; private set; }
    public string? VatDefaultCode { get; private set; }               // BU-Schlüssel
    public bool IsAutoAccount { get; private set; }
    public bool IsCostCenterRequired { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<AccountLabel> Labels { get; private set; } = [];

    public static Account Create(
        Guid entityId, string accountNumber, int accountClass,
        AccountType accountType, DebitCreditIndicator shIndicator,
        bool isIndividual = false, bool isCostCenterRequired = false)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            AccountNumber = accountNumber,
            AccountClass = accountClass,
            AccountType = accountType,
            ShIndicator = shIndicator,
            IsIndividual = isIndividual,
            IsCostCenterRequired = isCostCenterRequired,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}

public enum AccountType { Asset, Liability, Equity, Revenue, Expense, Statistical }
public enum DebitCreditIndicator { Debit, Credit }

public class AccountLabel
{
    public Guid AccountId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;  // "de", "en", "ru"
    public string Name { get; private set; } = string.Empty;
    public string? LongName { get; private set; }
}
```

#### accounting.journal_entries (GoBD-konform, mit HR-Links)

```csharp
// ClarityBoard.Domain/Entities/Accounting/JournalEntry.cs
namespace ClarityBoard.Domain.Entities.Accounting;

public class JournalEntry
{
    private JournalEntry() { }

    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public long EntryNumber { get; private set; }                     // Lückenlos, pro Entity
    public Guid FiscalPeriodId { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public DateOnly? DocumentDate { get; private set; }
    public DateOnly? ServiceDate { get; private set; }
    public DateTime PostingDate { get; private set; }
    public string Description { get; private set; } = string.Empty;  // Max 500, DATEV: 60
    public string? DocumentRef { get; private set; }                 // Belegfeld 1 (max 36)
    public string? DocumentRef2 { get; private set; }                // Belegfeld 2 (max 12)
    public Guid? DocumentId { get; private set; }
    public Guid? PartnerId { get; private set; }
    public JournalEntrySource SourceType { get; private set; }
    public string? SourceRef { get; private set; }                   // z.B. "hr:salary:uuid"
    public JournalEntryStatus Status { get; private set; } = JournalEntryStatus.Draft;
    public bool IsReversal { get; private set; }
    public Guid? ReversalOfId { get; private set; }
    public bool IsOpeningBalance { get; private set; }
    public bool IsClosing { get; private set; }
    // GoBD: Unveränderbarkeit
    public string Hash { get; private set; } = string.Empty;        // SHA-256
    public string? PreviousHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public Guid? PostedBy { get; private set; }
    public int Version { get; private set; } = 1;

    public ICollection<JournalEntryLine> Lines { get; private set; } = [];

    public static JournalEntry CreateDraft(
        Guid entityId, Guid fiscalPeriodId,
        DateOnly entryDate, string description,
        JournalEntrySource sourceType, Guid createdBy,
        string? documentRef = null, string? sourceRef = null)
    {
        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            FiscalPeriodId = fiscalPeriodId,
            EntryDate = entryDate,
            PostingDate = DateTime.UtcNow,
            Description = description[..Math.Min(description.Length, 500)],
            DocumentRef = documentRef?[..Math.Min(documentRef.Length, 36)],
            SourceType = sourceType,
            SourceRef = sourceRef,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };
    }

    public void Post(long entryNumber, string previousHash, Guid postedBy)
    {
        if (Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException($"Cannot post entry in status {Status}.");

        var linesBalance = Lines.Sum(l => l.DebitAmountCents) - Lines.Sum(l => l.CreditAmountCents);
        if (linesBalance != 0)
            throw new InvalidOperationException($"Journal entry is not balanced: {linesBalance} cents off.");

        EntryNumber = entryNumber;
        PreviousHash = previousHash;
        Hash = ComputeHash(entryNumber, previousHash);
        Status = JournalEntryStatus.Posted;
        PostedAt = DateTime.UtcNow;
        PostedBy = postedBy;
    }

    public void Reverse(Guid reversedBy)
    {
        if (Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("Only posted entries can be reversed.");
        Status = JournalEntryStatus.Reversed;
    }

    private string ComputeHash(long entryNumber, string previousHash)
    {
        var raw = $"{Id}|{EntityId}|{entryNumber}|{EntryDate}|{Description}|" +
                  $"{string.Join("|", Lines.OrderBy(l => l.LineNumber).Select(l => $"{l.AccountId}:{l.DebitAmountCents}:{l.CreditAmountCents}"))}|" +
                  $"{previousHash}";
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public class JournalEntryLine
{
    private JournalEntryLine() { }

    public Guid Id { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public short LineNumber { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? ContraAccountId { get; private set; }
    public long DebitAmountCents { get; private set; }              // BIGINT Cent (ClarityBoard-Konvention)
    public long CreditAmountCents { get; private set; }
    public string? TaxCode { get; private set; }                    // BU-Schlüssel
    public long TaxAmountCents { get; private set; }
    public Guid? TaxAccountId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? HrEmployeeId { get; private set; }                 // ← HR-Integration
    public Guid? HrTravelExpenseId { get; private set; }           // ← HR-Integration
    public string? Description { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public long? OriginalAmountCents { get; private set; }
    public decimal ExchangeRate { get; private set; } = 1.0m;
    public string? Dimension1 { get; private set; }
    public string? Dimension2 { get; private set; }
    public string Tags { get; private set; } = "[]";               // JSONB

    public static JournalEntryLine Create(
        Guid journalEntryId, short lineNumber,
        Guid accountId, long debitAmountCents, long creditAmountCents,
        Guid? costCenterId = null, string? taxCode = null,
        Guid? hrEmployeeId = null, Guid? hrTravelExpenseId = null)
    {
        if (debitAmountCents < 0 || creditAmountCents < 0)
            throw new ArgumentException("Amounts cannot be negative.");
        if (debitAmountCents > 0 && creditAmountCents > 0)
            throw new ArgumentException("A line cannot have both debit and credit.");

        return new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            JournalEntryId = journalEntryId,
            LineNumber = lineNumber,
            AccountId = accountId,
            DebitAmountCents = debitAmountCents,
            CreditAmountCents = creditAmountCents,
            CostCenterId = costCenterId,
            TaxCode = taxCode,
            HrEmployeeId = hrEmployeeId,
            HrTravelExpenseId = hrTravelExpenseId,
        };
    }
}

public enum JournalEntrySource { Manual, BankImport, Invoice, Recurring, AiSuggestion, Migration, HrSalary, HrTravel }
public enum JournalEntryStatus { Draft, Posted, Reversed }
```

#### FiscalPeriod

```csharp
// ClarityBoard.Domain/Entities/Accounting/FiscalPeriod.cs
public class FiscalPeriod
{
    private FiscalPeriod() { }

    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public short Year { get; private set; }
    public short Month { get; private set; }              // 0=Eröffnung, 1-12, 13=Abschluss
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public FiscalPeriodStatus Status { get; private set; } = FiscalPeriodStatus.Open;
    public DateTime? ClosedAt { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public int ExportCount { get; private set; }

    public static FiscalPeriod Create(Guid entityId, short year, short month)
    {
        var firstDay = new DateOnly(year, month == 0 ? 1 : month, 1);
        return new FiscalPeriod
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            Year = year,
            Month = month,
            StartDate = firstDay,
            EndDate = month == 0 ? firstDay : new DateOnly(year, month, DateTime.DaysInMonth(year, month)),
        };
    }

    public void SoftClose(Guid userId)
    {
        if (Status != FiscalPeriodStatus.Open)
            throw new InvalidOperationException("Period is not open.");
        Status = FiscalPeriodStatus.SoftClosed;
        ClosedAt = DateTime.UtcNow;
        ClosedBy = userId;
    }

    public void HardClose() => Status = FiscalPeriodStatus.HardClosed;
    public void IncrementExportCount() => ExportCount++;
}

public enum FiscalPeriodStatus { Open, SoftClosed, Exported, HardClosed }
```

#### document.Document

```csharp
// ClarityBoard.Domain/Entities/Accounting/Document.cs
public class AccountingDocument
{
    private AccountingDocument() { }

    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string? DocumentNumber { get; private set; }
    public string? ExternalRef { get; private set; }
    public Guid? PartnerId { get; private set; }
    public DateOnly? DocumentDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateOnly? ServicePeriodFrom { get; private set; }
    public DateOnly? ServicePeriodTo { get; private set; }
    public long NetAmountCents { get; private set; }
    public long TaxAmountCents { get; private set; }
    public long GrossAmountCents { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Open;
    public string? PaymentReference { get; private set; }
    // OCR/AI
    public string? OcrRawText { get; private set; }
    public string? AiExtraction { get; private set; }               // JSONB
    public decimal? AiConfidence { get; private set; }
    // Datei
    public string? FileStorageKey { get; private set; }
    public string? FileName { get; private set; }
    public string? FileMimeType { get; private set; }
    public long? FileSizeBytes { get; private set; }
    // Status
    public DocumentStatus Status { get; private set; } = DocumentStatus.Uploaded;
    public Guid? JournalEntryId { get; private set; }
    // GoBD
    public DateOnly? RetentionUntil { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static AccountingDocument Upload(
        Guid entityId, DocumentType documentType, string fileStorageKey,
        string fileName, string mimeType, long fileSizeBytes, Guid createdBy)
    {
        return new AccountingDocument
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            DocumentType = documentType,
            FileStorageKey = fileStorageKey,
            FileName = fileName,
            FileMimeType = mimeType,
            FileSizeBytes = fileSizeBytes,
            RetentionUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)), // GoBD 10 Jahre
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void SetAiExtraction(string jsonExtraction, decimal confidence)
    {
        AiExtraction = jsonExtraction;
        AiConfidence = confidence;
        Status = DocumentStatus.Extracted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void LinkJournalEntry(Guid journalEntryId)
    {
        JournalEntryId = journalEntryId;
        Status = DocumentStatus.Booked;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum DocumentType
{
    IncomingInvoice, OutgoingInvoice, CreditNote,
    BankStatement, CreditCardStatement, Receipt,
    Contract, Payroll, TaxNotice, Other
}
public enum DocumentStatus { Uploaded, Processing, Extracted, Validated, Booked, Archived, Rejected }
public enum PaymentStatus { Open, Partial, Paid, Overdue, Cancelled }
```

#### planning.Scenario + PlanEntry

```csharp
// ClarityBoard.Domain/Entities/Accounting/Scenario.cs
public class Scenario
{
    private Scenario() { }

    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ScenarioType ScenarioType { get; private set; }
    public Guid? ParentScenarioId { get; private set; }
    public int Version { get; private set; } = 1;
    public string? Description { get; private set; }
    public short Year { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsApproved { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string Parameters { get; private set; } = "{}";         // JSONB
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<PlanEntry> PlanEntries { get; private set; } = [];

    public static Scenario Create(
        Guid entityId, string name, ScenarioType type, short year, Guid createdBy)
    {
        return new Scenario
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            Name = name,
            ScenarioType = type,
            Year = year,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Lock() => IsLocked = true;
    public void Approve(Guid approvedBy) { IsApproved = true; ApprovedBy = approvedBy; ApprovedAt = DateTime.UtcNow; }
}

public enum ScenarioType { Actual, Budget, Forecast, BestCase, WorstCase, Custom }

public class PlanEntry
{
    private PlanEntry() { }

    public Guid Id { get; private set; }
    public Guid ScenarioId { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid AccountId { get; private set; }
    public short PeriodYear { get; private set; }
    public short PeriodMonth { get; private set; }
    public long AmountCents { get; private set; }                   // positiv=Soll-typisch
    public Guid? CostCenterId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string? Description { get; private set; }
    public PlanEntrySource Source { get; private set; } = PlanEntrySource.Manual;
    public string? Formula { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // HR-Integration: Plan-Eintrag aus HR-Gehaltsdaten generiert
    public Guid? HrEmployeeId { get; private set; }

    public static PlanEntry Create(
        Guid scenarioId, Guid entityId, Guid accountId,
        short year, short month, long amountCents,
        Guid? costCenterId = null, Guid? hrEmployeeId = null,
        PlanEntrySource source = PlanEntrySource.Manual)
    {
        return new PlanEntry
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            EntityId = entityId,
            AccountId = accountId,
            PeriodYear = year,
            PeriodMonth = month,
            AmountCents = amountCents,
            CostCenterId = costCenterId,
            HrEmployeeId = hrEmployeeId,
            Source = source,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}

public enum PlanEntrySource { Manual, Formula, Import, AiGenerated, HrSync }
```

#### datev.DatevExport

```csharp
// ClarityBoard.Domain/Entities/Accounting/DatevExport.cs
public class DatevExport
{
    private DatevExport() { }

    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid FiscalPeriodId { get; private set; }
    public DatevExportType ExportType { get; private set; }
    public DatevExportStatus Status { get; private set; } = DatevExportStatus.Pending;
    public int? FileCount { get; private set; }
    public int? RecordCount { get; private set; }
    public string? ValidationResult { get; private set; }          // JSONB
    public string Errors { get; private set; } = "[]";             // JSONB
    public string Warnings { get; private set; } = "[]";           // JSONB
    public string? Checksums { get; private set; }                 // JSONB
    public string? FileStorageKeys { get; private set; }           // JSONB
    public DateTime? GeneratedAt { get; private set; }
    public Guid GeneratedBy { get; private set; }
    public DateTime? DownloadedAt { get; private set; }
    public Guid? DownloadedBy { get; private set; }

    public static DatevExport Create(Guid entityId, Guid fiscalPeriodId,
        DatevExportType exportType, Guid generatedBy)
    {
        return new DatevExport
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            FiscalPeriodId = fiscalPeriodId,
            ExportType = exportType,
            GeneratedBy = generatedBy,
        };
    }

    public void SetGenerating() => Status = DatevExportStatus.Generating;
    public void SetReady(int fileCount, int recordCount, string checksums, string fileStorageKeys)
    {
        Status = DatevExportStatus.Ready;
        FileCount = fileCount;
        RecordCount = recordCount;
        Checksums = checksums;
        FileStorageKeys = fileStorageKeys;
        GeneratedAt = DateTime.UtcNow;
    }
    public void SetFailed(string errors) { Status = DatevExportStatus.Failed; Errors = errors; }
    public void MarkDownloaded(Guid downloadedBy) { DownloadedAt = DateTime.UtcNow; DownloadedBy = downloadedBy; }
}

public enum DatevExportType { Full, Delta, Correction }
public enum DatevExportStatus { Pending, Generating, Validating, Ready, Downloaded, Failed }
```

### 2.3 EF Core Konfigurationen

```csharp
// ClarityBoard.Infrastructure/Persistence/Configurations/Accounting/JournalEntryConfiguration.cs
public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries", "accounting");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntryNumber).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.DocumentRef).HasMaxLength(36);
        builder.Property(e => e.DocumentRef2).HasMaxLength(12);
        builder.Property(e => e.SourceType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.SourceRef).HasMaxLength(200);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Hash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.PreviousHash).HasMaxLength(64);
        builder.HasIndex(e => new { e.EntityId, e.EntryNumber }).IsUnique();
        builder.HasIndex(e => new { e.EntityId, e.EntryDate }).IsDescending(false, true);
        builder.HasIndex(e => new { e.EntityId, e.Status })
               .HasFilter("\"status\" = 'Draft'");
        builder.HasMany(e => e.Lines)
               .WithOne()
               .HasForeignKey(l => l.JournalEntryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("journal_entry_lines", "accounting");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.JournalEntryId, l.LineNumber }).IsUnique();
        builder.Property(l => l.Currency).HasMaxLength(3).IsRequired();
        builder.Property(l => l.TaxCode).HasMaxLength(10);
        builder.Property(l => l.Tags).HasColumnType("jsonb").HasDefaultValue("[]");
        // Check Constraints via HasCheckConstraint
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_line_debit_nonneg", "debit_amount_cents >= 0");
            t.HasCheckConstraint("ck_line_credit_nonneg", "credit_amount_cents >= 0");
            t.HasCheckConstraint("ck_line_not_both", "NOT (debit_amount_cents > 0 AND credit_amount_cents > 0)");
        });
        // Partial index auf HR-Employee (nur wenn gesetzt)
        builder.HasIndex(l => l.HrEmployeeId)
               .HasFilter("hr_employee_id IS NOT NULL");
        builder.HasIndex(l => l.HrTravelExpenseId)
               .HasFilter("hr_travel_expense_id IS NOT NULL");
    }
}

public class CostCenterConfiguration : IEntityTypeConfiguration<CostCenter>
{
    public void Configure(EntityTypeBuilder<CostCenter> builder)
    {
        builder.ToTable("cost_centers", "core");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.EntityId, c.Code }).IsUnique();
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        // Partial Index: Nur ein aktiver CostCenter pro HR-Employee
        builder.HasIndex(c => c.HrEmployeeId)
               .HasFilter("hr_employee_id IS NOT NULL AND is_active = true")
               .IsUnique();
    }
}
```

### 2.4 IAppDbContext — neue DbSets

```csharp
// In ClarityBoardContext.cs:
// Accounting
public DbSet<LegalEntityExtension> LegalEntityExtensions { get; set; }
public DbSet<CostCenter> CostCenters { get; set; }
public DbSet<BusinessPartner> BusinessPartners { get; set; }
public DbSet<Account> Accounts { get; set; }
public DbSet<AccountLabel> AccountLabels { get; set; }
public DbSet<StandardAccount> StandardAccounts { get; set; }
public DbSet<StandardAccountLabel> StandardAccountLabels { get; set; }
public DbSet<VatCode> VatCodes { get; set; }
public DbSet<FiscalPeriod> FiscalPeriods { get; set; }
public DbSet<JournalEntry> JournalEntries { get; set; }
public DbSet<JournalEntryLine> JournalEntryLines { get; set; }
public DbSet<RecurringEntry> RecurringEntries { get; set; }
// Documents
public DbSet<AccountingDocument> AccountingDocuments { get; set; }
public DbSet<DocumentLine> DocumentLines { get; set; }
public DbSet<PaymentAllocation> PaymentAllocations { get; set; }
// Planning
public DbSet<Scenario> Scenarios { get; set; }
public DbSet<PlanEntry> PlanEntries { get; set; }
// DATEV
public DbSet<DatevExport> DatevExports { get; set; }
// i18n
public DbSet<AccountingTranslation> AccountingTranslations { get; set; }
```

### 2.5 SQL-Migration-Snippets (kritische Teile)

```sql
-- Migration: AddHrAccountingLinks
-- HR-Integration: cost_centers bekommt hr_employee_id

ALTER TABLE core.cost_centers
    ADD COLUMN hr_employee_id UUID REFERENCES hr.employees(id) ON DELETE SET NULL;

-- journal_entry_lines bekommt HR-Referenzen
ALTER TABLE accounting.journal_entry_lines
    ADD COLUMN hr_employee_id UUID REFERENCES hr.employees(id) ON DELETE SET NULL,
    ADD COLUMN hr_travel_expense_id UUID REFERENCES hr.travel_expense_reports(id) ON DELETE SET NULL;

-- plan_entries bekommt HR-Employee-Referenz
ALTER TABLE planning.plan_entries
    ADD COLUMN hr_employee_id UUID REFERENCES hr.employees(id) ON DELETE SET NULL;

-- Unique: Ein aktiver Kostenstellen-Employee pro HR-Mitarbeiter
CREATE UNIQUE INDEX idx_cc_hr_employee_active
    ON core.cost_centers (hr_employee_id)
    WHERE hr_employee_id IS NOT NULL AND is_active = true;

-- Grants nach Migration
GRANT USAGE ON SCHEMA core, accounting, document, planning, cashflow, i18n, datev, audit TO app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA
    core, accounting, document, planning, cashflow, i18n, datev, audit TO app;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA core, accounting TO app;
```

---

## 3. Backend-Architektur

### 3.1 Neue Layer-Struktur

```
ClarityBoard.Domain/Entities/Accounting/
    Account.cs, AccountLabel.cs, StandardAccount.cs
    JournalEntry.cs, JournalEntryLine.cs
    FiscalPeriod.cs, CostCenter.cs, BusinessPartner.cs
    AccountingDocument.cs, DocumentLine.cs
    Scenario.cs, PlanEntry.cs
    RecurringEntry.cs, VatCode.cs
    DatevExport.cs, LegalEntityExtension.cs
    Events/
        JournalEntryPostedEvent.cs
        HrSalaryChangedEvent.cs          ← triggert Plan-Update
        HrTravelExpenseApprovedEvent.cs  ← triggert Journal Entry
        HrEmployeeTerminatedEvent.cs     ← triggert Abgrenzungsbuchung

ClarityBoard.Application/Features/Accounting/
    Commands/
        CreateJournalEntryCommand.cs
        PostJournalEntryCommand.cs
        ReverseJournalEntryCommand.cs
        CreateJournalEntryFromTravelExpenseCommand.cs  ← HR-Integration
        SyncHrSalaryToPlanCommand.cs                  ← HR-Integration
        UploadDocumentCommand.cs
        ExtractDocumentAiCommand.cs
        CreateScenarioCommand.cs
        UpdatePlanEntryCommand.cs
        GenerateDatevExportCommand.cs
        CloseFiscalPeriodCommand.cs
        CreateCostCenterCommand.cs
        CreateCostCenterForEmployeeCommand.cs          ← HR-Integration
    Queries/
        GetTrialBalanceQuery.cs
        GetBwaQuery.cs
        GetPlanActualComparisonQuery.cs
        GetJournalEntriesQuery.cs
        GetDocumentsQuery.cs
        GetScenariosQuery.cs
        GetCashFlowQuery.cs
        GetKpiDashboardQuery.cs                       ← HR-Daten inkludiert
        GetHrCostAnalysisQuery.cs                     ← HR-Integration
    EventHandlers/
        HrSalaryChangedEventHandler.cs
        HrTravelExpenseApprovedEventHandler.cs
        HrEmployeeTerminatedEventHandler.cs
        HrEmployeeCreatedEventHandler.cs

ClarityBoard.Infrastructure/
    Persistence/Configurations/Accounting/
        (alle Config-Dateien)
    Services/
        DatevExportService.cs
        JournalEntryHashService.cs
        OcrAiExtractionService.cs
        AccountingSyncService.cs    ← HR-Sync
        RecurringEntryProcessor.cs

ClarityBoard.API/
    Controllers/AccountingController.cs
    Hubs/AccountingHub.cs
```

### 3.2 HR-Event-Handler (vollständige Implementierung)

```csharp
// ClarityBoard.Application/Features/Accounting/EventHandlers/HrTravelExpenseApprovedEventHandler.cs

public record HrTravelExpenseApprovedEvent : INotification
{
    public required Guid TravelExpenseReportId { get; init; }
    public required Guid EntityId { get; init; }
    public required Guid EmployeeId { get; init; }
    public required Guid ApprovedBy { get; init; }
}

public class HrTravelExpenseApprovedEventHandler(
    IAppDbContext db,
    IAccountingHubNotifier notifier,
    ILogger<HrTravelExpenseApprovedEventHandler> logger)
    : INotificationHandler<HrTravelExpenseApprovedEvent>
{
    public async Task Handle(HrTravelExpenseApprovedEvent notification, CancellationToken ct)
    {
        var report = await db.TravelExpenseReports
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == notification.TravelExpenseReportId, ct)
            ?? throw new NotFoundException($"TravelExpenseReport {notification.TravelExpenseReportId} not found");

        // Kostenstelle des Mitarbeiters ermitteln
        var costCenter = await db.CostCenters
            .FirstOrDefaultAsync(cc =>
                cc.HrEmployeeId == notification.EmployeeId &&
                cc.EntityId == notification.EntityId &&
                cc.IsActive, ct);

        // Aktive Periode ermitteln
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var period = await db.FiscalPeriods
            .FirstOrDefaultAsync(fp =>
                fp.EntityId == notification.EntityId &&
                fp.Year == today.Year &&
                fp.Month == today.Month &&
                fp.Status == FiscalPeriodStatus.Open, ct)
            ?? throw new InvalidOperationException("No open fiscal period found.");

        // Buchungskonto für Reisekosten: 6650 (DATEV Reisekosten AN)
        var travelAccount = await db.Accounts
            .FirstOrDefaultAsync(a =>
                a.EntityId == notification.EntityId &&
                a.AccountNumber == "6650" &&
                a.IsActive, ct)
            ?? throw new InvalidOperationException("Account 6650 (Reisekosten) not found.");

        // Verbindlichkeitenkonto 3300 oder spezifisches Reisekostenkonto
        var liabilityAccount = await db.Accounts
            .FirstOrDefaultAsync(a =>
                a.EntityId == notification.EntityId &&
                a.AccountNumber == "3300" &&
                a.IsActive, ct)
            ?? throw new InvalidOperationException("Account 3300 not found.");

        // Buchungssatz erstellen
        var entry = JournalEntry.CreateDraft(
            entityId: notification.EntityId,
            fiscalPeriodId: period.Id,
            entryDate: today,
            description: $"Reisekosten {report.Title} (HR)",
            sourceType: JournalEntrySource.HrTravel,
            createdBy: notification.ApprovedBy,
            documentRef: report.Id.ToString()[..8],
            sourceRef: $"hr:travel:{notification.TravelExpenseReportId}");

        // Zeilen: Soll Reisekosten / Haben Verbindlichkeiten
        entry.Lines.Add(JournalEntryLine.Create(
            journalEntryId: entry.Id,
            lineNumber: 1,
            accountId: travelAccount.Id,
            debitAmountCents: report.TotalAmountCents,
            creditAmountCents: 0,
            costCenterId: costCenter?.Id,
            hrEmployeeId: notification.EmployeeId,
            hrTravelExpenseId: notification.TravelExpenseReportId));

        entry.Lines.Add(JournalEntryLine.Create(
            journalEntryId: entry.Id,
            lineNumber: 2,
            accountId: liabilityAccount.Id,
            debitAmountCents: 0,
            creditAmountCents: report.TotalAmountCents,
            hrTravelExpenseId: notification.TravelExpenseReportId));

        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        await notifier.NotifyJournalEntryCreatedAsync(
            notification.EntityId, entry.Id, ct);

        logger.LogInformation(
            "Auto-created journal entry {EntryId} from travel expense {ReportId}",
            entry.Id, notification.TravelExpenseReportId);
    }
}
```

```csharp
// ClarityBoard.Application/Features/Accounting/EventHandlers/HrSalaryChangedEventHandler.cs

public record HrSalaryChangedEvent : INotification
{
    public required Guid EmployeeId { get; init; }
    public required Guid EntityId { get; init; }
    public required long OldGrossAmountCents { get; init; }
    public required long NewGrossAmountCents { get; init; }
    public required DateOnly ValidFrom { get; init; }
    public required Guid ChangedBy { get; init; }
}

public class HrSalaryChangedEventHandler(IAppDbContext db, IAccountingHubNotifier notifier)
    : INotificationHandler<HrSalaryChangedEvent>
{
    public async Task Handle(HrSalaryChangedEvent notification, CancellationToken ct)
    {
        // Alle aktiven Forecast-Szenarien für die Entity aktualisieren
        var forecastScenarios = await db.Scenarios
            .Where(s => s.EntityId == notification.EntityId &&
                        s.ScenarioType == ScenarioType.Forecast &&
                        !s.IsLocked &&
                        s.Year >= notification.ValidFrom.Year)
            .ToListAsync(ct);

        if (!forecastScenarios.Any()) return;

        // Gehaltsbudgetkonto 6020
        var salaryAccount = await db.Accounts
            .FirstOrDefaultAsync(a =>
                a.EntityId == notification.EntityId &&
                a.AccountNumber == "6020" && a.IsActive, ct);
        if (salaryAccount is null) return;

        var costCenter = await db.CostCenters
            .FirstOrDefaultAsync(cc =>
                cc.HrEmployeeId == notification.EmployeeId &&
                cc.EntityId == notification.EntityId && cc.IsActive, ct);

        foreach (var scenario in forecastScenarios)
        {
            // Monate ab ValidFrom bis Jahresende anpassen
            var startMonth = notification.ValidFrom.Year == scenario.Year
                ? notification.ValidFrom.Month
                : 1;

            for (var month = startMonth; month <= 12; month++)
            {
                var existing = await db.PlanEntries
                    .FirstOrDefaultAsync(pe =>
                        pe.ScenarioId == scenario.Id &&
                        pe.AccountId == salaryAccount.Id &&
                        pe.PeriodYear == scenario.Year &&
                        pe.PeriodMonth == month &&
                        pe.HrEmployeeId == notification.EmployeeId, ct);

                if (existing is not null)
                {
                    // Update via neue PlanEntry (da kein direktes Update auf private setter)
                    db.PlanEntries.Remove(existing);
                }

                db.PlanEntries.Add(PlanEntry.Create(
                    scenarioId: scenario.Id,
                    entityId: notification.EntityId,
                    accountId: salaryAccount.Id,
                    year: (short)scenario.Year,
                    month: (short)month,
                    amountCents: notification.NewGrossAmountCents,
                    costCenterId: costCenter?.Id,
                    hrEmployeeId: notification.EmployeeId,
                    source: PlanEntrySource.HrSync));
            }
        }

        await db.SaveChangesAsync(ct);
        await notifier.NotifyPlanUpdatedAsync(notification.EntityId, ct);
    }
}
```

```csharp
// ClarityBoard.Application/Features/Accounting/EventHandlers/HrEmployeeCreatedEventHandler.cs
// Auto-Erstellt Mitarbeiter-Kostenstelle bei HR-Onboarding

public class HrEmployeeCreatedEventHandler(IAppDbContext db, IMediator mediator)
    : INotificationHandler<EmployeeCreatedEvent>  // EmployeeCreatedEvent aus HR-Modul
{
    public async Task Handle(EmployeeCreatedEvent notification, CancellationToken ct)
    {
        var exists = await db.CostCenters
            .AnyAsync(cc =>
                cc.HrEmployeeId == notification.EmployeeId &&
                cc.EntityId == notification.EntityId, ct);

        if (exists) return;

        var code = $"E{notification.EmployeeNumber}";
        var cc = CostCenter.Create(
            entityId: notification.EntityId,
            code: code,
            shortName: notification.FullName[..Math.Min(notification.FullName.Length, 50)],
            type: CostCenterType.Employee,
            hrEmployeeId: notification.EmployeeId);

        db.CostCenters.Add(cc);
        await db.SaveChangesAsync(ct);
    }
}
```

### 3.3 PostJournalEntryCommand — vollständig

```csharp
// ClarityBoard.Application/Features/Accounting/Commands/PostJournalEntryCommand.cs

public record PostJournalEntryCommand : IRequest<PostJournalEntryResult>
{
    public required Guid JournalEntryId { get; init; }
    public required Guid EntityId { get; init; }
}

public record PostJournalEntryResult(long EntryNumber, string Hash);

[RequirePermission("accounting.post")]
public class PostJournalEntryCommandHandler(
    IAppDbContext db,
    IJournalEntryHashService hashService,
    IAccountingHubNotifier notifier,
    IMediator mediator)
    : IRequestHandler<PostJournalEntryCommand, PostJournalEntryResult>
{
    public async Task<PostJournalEntryResult> Handle(
        PostJournalEntryCommand request, CancellationToken ct)
    {
        // Pessimistisches Lock auf die Entry (für atomare EntryNumber-Vergabe)
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var entry = await db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e =>
                e.Id == request.JournalEntryId &&
                e.EntityId == request.EntityId, ct)
            ?? throw new NotFoundException($"Journal entry {request.JournalEntryId} not found.");

        if (entry.Status != JournalEntryStatus.Draft)
            throw new ConflictException($"Entry is already in status {entry.Status}.");

        // Lückenlose Buchungsnummer (DB-seitig via Sequence oder Max+1 mit Lock)
        var nextNumber = await db.Database.SqlQuery<long>(
            $"SELECT nextval('accounting.journal_entry_number_seq_{request.EntityId:N}')"
        ).FirstAsync(ct);

        // SHA-256-Verkettung: letzten Hash des Entities holen
        var previousHash = await db.JournalEntries
            .Where(e => e.EntityId == request.EntityId && e.Status == JournalEntryStatus.Posted)
            .OrderByDescending(e => e.EntryNumber)
            .Select(e => e.Hash)
            .FirstOrDefaultAsync(ct) ?? "0000000000000000000000000000000000000000000000000000000000000000";

        entry.Post(nextNumber, previousHash, request.EntityId /* = postedBy via CurrentUser */);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Materialized Views refreshen (async, nicht blockierend)
        _ = Task.Run(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW CONCURRENTLY accounting.mv_trial_balance");
        }, CancellationToken.None);

        await notifier.NotifyJournalEntryPostedAsync(request.EntityId, entry.Id, ct);
        await mediator.Publish(new JournalEntryPostedEvent { JournalEntryId = entry.Id }, ct);

        return new PostJournalEntryResult(nextNumber, entry.Hash);
    }
}
```

### 3.4 DATEV-Export-Service

```csharp
// ClarityBoard.Infrastructure/Services/DatevExportService.cs

public class DatevExportService(
    IAppDbContext db,
    IMinioService minioService,
    ILogger<DatevExportService> logger) : IDatevExportService
{
    // Windows-1252 Encoding (kritisch für DATEV)
    private static readonly Encoding Win1252 = Encoding.GetEncoding(1252);

    public async Task<DatevExport> GenerateExportAsync(
        Guid entityId, Guid fiscalPeriodId, DatevExportType exportType,
        Guid generatedBy, CancellationToken ct)
    {
        var export = DatevExport.Create(entityId, fiscalPeriodId, exportType, generatedBy);
        db.DatevExports.Add(export);
        export.SetGenerating();
        await db.SaveChangesAsync(ct);

        try
        {
            var entity = await db.LegalEntityExtensions
                .FirstOrDefaultAsync(e => e.EntityId == entityId, ct)
                ?? throw new InvalidOperationException($"No entity extension for {entityId}");

            var period = await db.FiscalPeriods.FindAsync([fiscalPeriodId], ct)
                ?? throw new NotFoundException($"Period {fiscalPeriodId} not found");

            var entries = await db.JournalEntries
                .Include(je => je.Lines)
                    .ThenInclude(l => l.Account)
                .Where(je => je.EntityId == entityId &&
                             je.FiscalPeriodId == fiscalPeriodId &&
                             je.Status == JournalEntryStatus.Posted)
                .OrderBy(je => je.EntryNumber)
                .ToListAsync(ct);

            // Validierungen
            var errors = ValidateEntries(entries, entity);
            if (errors.Any(e => e.Severity == "error"))
            {
                export.SetFailed(System.Text.Json.JsonSerializer.Serialize(errors));
                await db.SaveChangesAsync(ct);
                return export;
            }

            // CSV generieren
            var csvContent = GenerateBuchungsstapelCsv(entries, entity, period);
            var csvBytes = Win1252.GetBytes(csvContent);

            // SHA-256 Checksumme
            var checksum = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(csvBytes));

            // MinIO speichern
            var fileName = $"EXTF_Buchungsstapel_{period.Year:D4}{period.Month:D2}.csv";
            var storageKey = $"datev/{entityId}/{export.Id}/{fileName}";
            await minioService.PutObjectAsync(storageKey, csvBytes, "text/csv", ct);

            export.SetReady(
                fileCount: 1,
                recordCount: entries.Count,
                checksums: System.Text.Json.JsonSerializer.Serialize(new { [fileName] = checksum }),
                fileStorageKeys: System.Text.Json.JsonSerializer.Serialize(new { [fileName] = storageKey }));

            // Periode als exported markieren
            period.IncrementExportCount();
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DATEV export failed for entity {EntityId}", entityId);
            export.SetFailed($"[{{\"message\":\"{ex.Message}\"}}]");
            await db.SaveChangesAsync(ct);
        }

        return export;
    }

    private string GenerateBuchungsstapelCsv(
        List<JournalEntry> entries, LegalEntityExtension entity, FiscalPeriod period)
    {
        var sb = new StringBuilder();

        // Zeile 1: EXTF-Header
        var now = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        sb.AppendLine(
            $"\"EXTF\";700;21;\"Buchungsstapel\";12;{now};;\"CB\";\"\";;{entity.DatevConsultantNumber ?? string.Empty};" +
            $"{entity.DatevClientNumber ?? string.Empty};{period.Year:D4}0101;4;" +
            $"{period.StartDate:yyyyMMdd};{period.EndDate:yyyyMMdd};" +
            $"\"Clarity Board Export {period.Month:D2}/{period.Year}\";\"\";" +
            "1;0;;;\"EUR\";\"\";;\"\"\r\n");

        // Zeile 2: Spaltenheader (DATEV-Pflichtformat)
        sb.AppendLine("\"Umsatz (ohne Soll/Haben-Kz)\";\"Soll/Haben-Kennzeichen\";\"WKZ Umsatz\";" +
                      "\"Kurs\";\"Basis-Umsatz\";\"WKZ Basis-Umsatz\";\"Konto\";\"Gegenkonto (ohne BU-Schlüssel)\";" +
                      "\"BU-Schlüssel\";\"Belegdatum\";\"Belegfeld 1\";\"Belegfeld 2\";\"Skonto\";\"Buchungstext\";" +
                      ";;;;;;\"Leistungsdatum\";;;;;;;;;;;;;;;;;;;;;;\"Kostenstelle\"\r\n");

        // Zeile 3: Leer (DATEV-Pflicht)
        sb.AppendLine();

        // Buchungszeilen (eine Zeile pro journal_entry_line)
        foreach (var entry in entries)
        {
            foreach (var line in entry.Lines.OrderBy(l => l.LineNumber))
            {
                var isDebit = line.DebitAmountCents > 0;
                var amountCents = isDebit ? line.DebitAmountCents : line.CreditAmountCents;
                var amountEur = amountCents / 100m;

                // Gegenkonto: erstes Gegenkonto der Entry (vereinfacht)
                var contraLine = entry.Lines.FirstOrDefault(l =>
                    l.LineNumber != line.LineNumber &&
                    (isDebit ? l.CreditAmountCents > 0 : l.DebitAmountCents > 0));

                var buchungstext = (line.Description ?? entry.Description)
                    [..Math.Min((line.Description ?? entry.Description).Length, 60)];

                // Windows-1252-fähige Zeichen sicherstellen (Umlaute OK, aber kein €-Zeichen)
                buchungstext = buchungstext.Replace("€", "EUR");

                var fields = new string[]
                {
                    amountEur.ToString("F2", System.Globalization.CultureInfo.GetCultureInfo("de-DE")),
                    isDebit ? "S" : "H",
                    line.Currency,
                    line.ExchangeRate != 1.0m ? line.ExchangeRate.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "",
                    "",                                                     // Basis-Umsatz
                    "",                                                     // WKZ Basis
                    line.Account?.AccountNumber ?? "",
                    contraLine?.Account?.AccountNumber ?? "",
                    line.TaxCode ?? "",
                    (entry.DocumentDate ?? entry.EntryDate).ToString("ddMM"),
                    entry.DocumentRef ?? "",
                    entry.DocumentRef2 ?? "",
                    "",                                                     // Skonto
                    EscapeCsvField(buchungstext),
                    "", "", "", "", "",                                     // EU-Felder 14-18
                    "",                                                     // Feld 19
                    "",                                                     // Beleglink (20)
                    "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", // 21-35
                    // Kostenstelle (Feld 36)
                    ""  // TODO: cost_center.Code lookup
                };

                sb.AppendLine(string.Join(";", fields.Select(EscapeCsvField)));
            }
        }

        return sb.ToString();
    }

    private static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private List<ValidationIssue> ValidateEntries(
        List<JournalEntry> entries, LegalEntityExtension entity)
    {
        var issues = new List<ValidationIssue>();
        foreach (var entry in entries)
        {
            var balance = entry.Lines.Sum(l => l.DebitAmountCents) - entry.Lines.Sum(l => l.CreditAmountCents);
            if (balance != 0)
                issues.Add(new("error", $"Entry {entry.EntryNumber}: Not balanced by {balance} cents"));
        }
        return issues;
    }
}

public record ValidationIssue(string Severity, string Message);
```

### 3.5 SignalR Hub

```csharp
// ClarityBoard.API/Hubs/AccountingHub.cs
[Authorize]
public class AccountingHub : Hub
{
    public async Task JoinEntityGroup(string entityId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"accounting-{entityId}");
}

public interface IAccountingHubNotifier
{
    Task NotifyJournalEntryPostedAsync(Guid entityId, Guid entryId, CancellationToken ct);
    Task NotifyJournalEntryCreatedAsync(Guid entityId, Guid entryId, CancellationToken ct);
    Task NotifyDocumentStatusChangedAsync(Guid entityId, Guid documentId, string status, CancellationToken ct);
    Task NotifyDatevExportReadyAsync(Guid entityId, Guid exportId, CancellationToken ct);
    Task NotifyPlanUpdatedAsync(Guid entityId, CancellationToken ct);
    Task NotifyTrialBalanceRefreshedAsync(Guid entityId, CancellationToken ct);
}

public class AccountingHubNotifier(IHubContext<AccountingHub> hub) : IAccountingHubNotifier
{
    public Task NotifyJournalEntryPostedAsync(Guid entityId, Guid entryId, CancellationToken ct)
        => hub.Clients.Group($"accounting-{entityId}")
               .SendAsync("JournalEntryPosted", new { EntryId = entryId }, ct);

    public Task NotifyJournalEntryCreatedAsync(Guid entityId, Guid entryId, CancellationToken ct)
        => hub.Clients.Group($"accounting-{entityId}")
               .SendAsync("JournalEntryCreated", new { EntryId = entryId }, ct);

    public Task NotifyPlanUpdatedAsync(Guid entityId, CancellationToken ct)
        => hub.Clients.Group($"accounting-{entityId}")
               .SendAsync("PlanUpdated", new { EntityId = entityId }, ct);

    public Task NotifyDocumentStatusChangedAsync(Guid entityId, Guid documentId, string status, CancellationToken ct)
        => hub.Clients.Group($"accounting-{entityId}")
               .SendAsync("DocumentStatusChanged", new { DocumentId = documentId, Status = status }, ct);

    public Task NotifyDatevExportReadyAsync(Guid entityId, Guid exportId, CancellationToken ct)
        => hub.Clients.Group($"accounting-{entityId}")
               .SendAsync("DatevExportReady", new { ExportId = exportId }, ct);

    public Task NotifyTrialBalanceRefreshedAsync(Guid entityId, CancellationToken ct)
        => hub.Clients.Group($"accounting-{entityId}")
               .SendAsync("TrialBalanceRefreshed", new { EntityId = entityId }, ct);
}
```

---

## 4. API-Design

### 4.1 Endpunkte (Übersicht)

```
# Kontenrahmen
GET    /api/accounting/accounts                         Kontenplan (filter: class, type, active)
POST   /api/accounting/accounts                         Individualkonto anlegen [accounting.admin]
PUT    /api/accounting/accounts/{id}                    Konto updaten

# Buchungsjournal
GET    /api/accounting/journal-entries                  Paginiert, filter: period, account, status, source
POST   /api/accounting/journal-entries                  Buchungssatz anlegen (Draft)
GET    /api/accounting/journal-entries/{id}             Detail inkl. Zeilen
PUT    /api/accounting/journal-entries/{id}/post        Buchen (Draft → Posted) [accounting.post]
POST   /api/accounting/journal-entries/{id}/reverse     Storno [accounting.post]
GET    /api/accounting/journal-entries/hr-travel-sync   HR-Reisekosten unverbucht [accounting.view]

# Auswertungen
GET    /api/accounting/trial-balance                    Saldenliste (year, month, entity)
GET    /api/accounting/bwa                              BWA (year, from_month, to_month)
GET    /api/accounting/plan-actual                      Soll/Ist-Vergleich (year, scenario_id)
GET    /api/accounting/kpis                             KPI-Dashboard (year, month, filters)
GET    /api/accounting/kpis?include_hr=true             KPIs mit HR-Kostenstellen-Breakdown

# HR-Integration
GET    /api/accounting/cost-centers                     Alle Kostenstellen (inkl. HR-Link)
POST   /api/accounting/cost-centers                     Kostenstelle anlegen
GET    /api/accounting/cost-centers/hr-employees        Mitarbeiter-Kostenstellen mit HR-Daten
POST   /api/accounting/travel-costs/sync                Genehmigte HR-Reisekosten verbuchen [accounting.post]
GET    /api/accounting/hr-cost-analysis                 Personal-Kostenanalyse nach KSt/Dept/Contractor

# Belege
GET    /api/accounting/documents                        Belegarchiv
POST   /api/accounting/documents/upload                 Beleg hochladen (multipart)
GET    /api/accounting/documents/{id}                   Detail + AI-Extraktion
PUT    /api/accounting/documents/{id}/link-journal      Buchungssatz zuordnen

# Szenarien / Planung
GET    /api/accounting/scenarios                        Alle Szenarien
POST   /api/accounting/scenarios                        Neues Szenario
GET    /api/accounting/scenarios/{id}/plan-entries      Plan-Entries abrufen
PUT    /api/accounting/scenarios/{id}/plan-entries      Plan-Entry setzen (bulk-capable)
POST   /api/accounting/scenarios/{id}/sync-hr-salaries  HR-Gehälter in Szenario synchronisieren

# DATEV-Export
GET    /api/accounting/datev/exports                    Export-History
POST   /api/accounting/datev/exports                    Export auslösen
GET    /api/accounting/datev/exports/{id}/download      ZIP herunterladen

# Perioden
GET    /api/accounting/fiscal-periods                   Perioden-Übersicht
POST   /api/accounting/fiscal-periods/{id}/close-soft   Weich schließen
POST   /api/accounting/fiscal-periods/{id}/close-hard   Hard schließen [accounting.admin]
```

### 4.2 Haupt-DTOs

```csharp
// Request: Buchungssatz anlegen
public record CreateJournalEntryRequest
{
    public required DateOnly EntryDate { get; init; }
    public DateOnly? DocumentDate { get; init; }
    public DateOnly? ServiceDate { get; init; }
    public required string Description { get; init; }        // Max 500 (DATEV truncates to 60)
    public string? DocumentRef { get; init; }                // Max 36
    public string? DocumentRef2 { get; init; }               // Max 12
    public Guid? DocumentId { get; init; }
    public Guid? PartnerId { get; init; }
    public required CreateJournalEntryLineRequest[] Lines { get; init; }
}

public record CreateJournalEntryLineRequest
{
    public required Guid AccountId { get; init; }
    public Guid? ContraAccountId { get; init; }
    public required long DebitAmountCents { get; init; }     // genau eines von beiden > 0
    public required long CreditAmountCents { get; init; }
    public string? TaxCode { get; init; }
    public Guid? CostCenterId { get; init; }
    public Guid? ProjectId { get; init; }
    public string? Description { get; init; }
}

// Response: BWA
public record BwaDto
{
    public required string BwaLine { get; init; }            // "1020", "1060" etc.
    public required string BwaLineName { get; init; }        // "Umsatzerlöse"
    public required long AmountCents { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
}

// Response: KPI mit HR-Breakdown
public record KpiWithHrDto
{
    public required long RevenueCents { get; init; }
    public required long GrossProfitCents { get; init; }
    public required long PersonalCostsCents { get; init; }    // aus 6020+6110 (Ist)
    public required long PersonalCostsPlanCents { get; init; } // aus HR-Gehaltsdaten
    public required long FreelancerCostsCents { get; init; }  // aus 5900 (HR-Contractors)
    public required long TravelCostsCents { get; init; }      // aus HR-Reisekosten
    public required decimal GrossMarginPct { get; init; }
    public required decimal PersonalCostsPct { get; init; }
    public required decimal EbitdaCents { get; init; }
    public required IReadOnlyList<HrCostBreakdownDto> HrBreakdown { get; init; }
}

public record HrCostBreakdownDto
{
    public required Guid EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public required string CostCenterCode { get; init; }
    public required string Department { get; init; }
    public required bool IsContractor { get; init; }
    public required long GrossSalaryCents { get; init; }
    public required long TravelCostsCents { get; init; }
    public required long TotalCostsCents { get; init; }
}

// Response: Soll/Ist-Vergleich
public record PlanActualDto
{
    public required Guid AccountId { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountName { get; init; }
    public required string? CostCenterCode { get; init; }
    public required long ActualCents { get; init; }
    public required long PlanCents { get; init; }
    public required long DeviationCents { get; init; }
    public required decimal? DeviationPct { get; init; }
    public required string ScenarioName { get; init; }
}
```

### 4.3 FluentValidation-Beispiel

```csharp
public class CreateJournalEntryRequestValidator : AbstractValidator<CreateJournalEntryRequest>
{
    public CreateJournalEntryRequestValidator()
    {
        RuleFor(x => x.EntryDate).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DocumentRef).MaximumLength(36).When(x => x.DocumentRef is not null);
        RuleFor(x => x.DocumentRef2).MaximumLength(12).When(x => x.DocumentRef2 is not null);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line required.");
        RuleFor(x => x.Lines).Must(lines =>
        {
            var totalDebit = lines.Sum(l => l.DebitAmountCents);
            var totalCredit = lines.Sum(l => l.CreditAmountCents);
            return totalDebit == totalCredit;
        }).WithMessage("Journal entry must be balanced (debit == credit).");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.DebitAmountCents).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.CreditAmountCents).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l).Must(l => !(l.DebitAmountCents > 0 && l.CreditAmountCents > 0))
                .WithMessage("Line cannot have both debit and credit.");
            line.RuleFor(l => l).Must(l => l.DebitAmountCents > 0 || l.CreditAmountCents > 0)
                .WithMessage("Line must have either debit or credit.");
        });
    }
}
```

### 4.4 Beispiel-Request: HR-Reisekosten-Sync

```
POST /api/accounting/travel-costs/sync
Authorization: Bearer {token}
Content-Type: application/json

{
  "entityId": "uuid",
  "fromDate": "2026-02-01",
  "toDate": "2026-02-28"
}

Response 200:
{
  "syncedCount": 7,
  "skippedCount": 2,
  "journalEntryIds": ["uuid1", "uuid2", ...],
  "errors": []
}
```

---

## 5. Frontend-Struktur

### 5.1 Neue Routen + Pages

```
src/frontend/src/features/accounting/
  overview/
    AccountingOverview.tsx            ← Dashboard: BWA, KPIs, offene Belege
  journal/
    JournalEntryList.tsx              ← Journal mit Filter nach Periode/Konto/Status
    JournalEntryForm.tsx              ← Buchungssatz erstellen (doppelte Buchführung UI)
    JournalEntryDetail.tsx            ← Detail + Hash-Anzeige + Storno
  accounts/
    ChartOfAccounts.tsx               ← Kontenplan (SKR 04 + Individualkonten)
    AccountDetail.tsx
  documents/
    DocumentInbox.tsx                 ← Upload + AI-Extraktion-Status
    DocumentDetail.tsx                ← Beleg + vorgeschlagener Buchungssatz
  bwa/
    BwaReport.tsx                     ← BWA (Monat/Kumuliert/Vorjahresvergleich)
  trial-balance/
    TrialBalance.tsx                  ← Saldenliste
  plan-actual/
    PlanActualView.tsx                ← Soll/Ist-Vergleich (Recharts-Tabelle + Chart)
  cashflow/
    CashFlowView.tsx                  ← 13-Wochen-Liquidität
  datev/
    DatevExportPage.tsx               ← Export-Auslösung + Validierung + Download
  hr-integration/
    HrCostAnalysis.tsx                ← Personalkosten-Auswertung mit HR-Filtern
    TravelCostSync.tsx                ← Genehmigte Reisekosten verbuchen
  scenarios/
    ScenarioList.tsx
    ScenarioDetail.tsx                ← Plan-Entry-Tabelle + HR-Sync-Button
  admin/
    FiscalPeriods.tsx
    CostCenters.tsx
    BusinessPartners.tsx
```

### 5.2 Router-Konfiguration

```typescript
// In router.tsx — neue Accounting-Routen:
{ path: 'accounting',              lazy: () => import('@/features/accounting/overview/AccountingOverview') },
{ path: 'accounting/journal',      lazy: () => import('@/features/accounting/journal/JournalEntryList') },
{ path: 'accounting/journal/new',  lazy: () => import('@/features/accounting/journal/JournalEntryForm') },
{ path: 'accounting/journal/:id',  lazy: () => import('@/features/accounting/journal/JournalEntryDetail') },
{ path: 'accounting/accounts',     lazy: () => import('@/features/accounting/accounts/ChartOfAccounts') },
{ path: 'accounting/documents',    lazy: () => import('@/features/accounting/documents/DocumentInbox') },
{ path: 'accounting/bwa',          lazy: () => import('@/features/accounting/bwa/BwaReport') },
{ path: 'accounting/trial-balance',lazy: () => import('@/features/accounting/trial-balance/TrialBalance') },
{ path: 'accounting/plan-actual',  lazy: () => import('@/features/accounting/plan-actual/PlanActualView') },
{ path: 'accounting/datev',        lazy: () => import('@/features/accounting/datev/DatevExportPage') },
{ path: 'accounting/hr-costs',     lazy: () => import('@/features/accounting/hr-integration/HrCostAnalysis') },
{ path: 'accounting/scenarios',    lazy: () => import('@/features/accounting/scenarios/ScenarioList') },
{ path: 'accounting/scenarios/:id',lazy: () => import('@/features/accounting/scenarios/ScenarioDetail') },
```

### 5.3 Sidebar-Gruppe "Buchhaltung"

```typescript
// In Sidebar.tsx — neue NavGroup (nach den bestehenden Gruppen, vor HR):
const accountingNavGroup = useMemo<NavGroup>(() => ({
  label: t('navigation:groups.accounting'),
  items: [
    { label: t('navigation:items.accountingOverview'), path: '/accounting', icon: BookOpen },
    { label: t('navigation:items.journal'),            path: '/accounting/journal', icon: BookMarked },
    { label: t('navigation:items.bwa'),                path: '/accounting/bwa', icon: BarChart3 },
    { label: t('navigation:items.trialBalance'),       path: '/accounting/trial-balance', icon: Scale },
    { label: t('navigation:items.planActual'),         path: '/accounting/plan-actual', icon: TrendingUp },
    { label: t('navigation:items.documents'),          path: '/accounting/documents', icon: FileText },
    { label: t('navigation:items.datevExport'),        path: '/accounting/datev', icon: FileSpreadsheet },
    { label: t('navigation:items.hrCosts'),            path: '/accounting/hr-costs', icon: Users },
  ],
}), [t]);
```

### 5.4 TanStack Query Hooks

```typescript
// src/frontend/src/hooks/useAccounting.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import i18n from '@/i18n';
import { toast } from 'sonner';
import { queryKeys } from '@/lib/queryKeys';

// queryKeys ergänzen:
// accounting: {
//   all: ['accounting'] as const,
//   journalEntries: (params?) => ['accounting', 'journal-entries', params ?? {}] as const,
//   journalEntry: (id) => ['accounting', 'journal-entry', id] as const,
//   trialBalance: (entityId, year, month) => ['accounting', 'trial-balance', entityId, year, month] as const,
//   bwa: (entityId, year, fromMonth, toMonth) => ['accounting', 'bwa', entityId, year, fromMonth, toMonth] as const,
//   planActual: (entityId, scenarioId, year) => ['accounting', 'plan-actual', entityId, scenarioId, year] as const,
//   accounts: (entityId) => ['accounting', 'accounts', entityId] as const,
//   documents: (entityId, params?) => ['accounting', 'documents', entityId, params ?? {}] as const,
//   kpis: (entityId, year, month) => ['accounting', 'kpis', entityId, year, month] as const,
//   hrCostAnalysis: (entityId, year, month) => ['accounting', 'hr-costs', entityId, year, month] as const,
//   datevExports: (entityId) => ['accounting', 'datev-exports', entityId] as const,
//   scenarios: (entityId) => ['accounting', 'scenarios', entityId] as const,
//   fiscalPeriods: (entityId) => ['accounting', 'fiscal-periods', entityId] as const,
// }

export function useTrialBalance(entityId: string, year: number, month: number) {
  return useQuery({
    queryKey: queryKeys.accounting.trialBalance(entityId, year, month),
    queryFn: async () => {
      const { data } = await api.get<TrialBalanceDto[]>('/accounting/trial-balance', {
        params: { entityId, year, month },
      });
      return data;
    },
    enabled: !!entityId,
    staleTime: 30_000,  // 30s — Refresh after MV refresh
  });
}

export function useBwa(entityId: string, year: number, fromMonth: number, toMonth: number) {
  return useQuery({
    queryKey: queryKeys.accounting.bwa(entityId, year, fromMonth, toMonth),
    queryFn: async () => {
      const { data } = await api.get<BwaDto[]>('/accounting/bwa', {
        params: { entityId, year, fromMonth, toMonth },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

export function usePostJournalEntry() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await api.put(`/accounting/journal-entries/${id}/post`);
      return data as { entryNumber: number; hash: string };
    },
    onSuccess: () => {
      toast.success(i18n.t('accounting:toast.entryPosted'));
      queryClient.invalidateQueries({ queryKey: ['accounting', 'journal-entries'] });
      queryClient.invalidateQueries({ queryKey: ['accounting', 'trial-balance'] });
    },
    onError: () => toast.error(i18n.t('accounting:toast.postFailed')),
  });
}

export function useSyncTravelCosts() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: { entityId: string; fromDate: string; toDate: string }) => {
      const { data } = await api.post('/accounting/travel-costs/sync', params);
      return data as { syncedCount: number; skippedCount: number };
    },
    onSuccess: (data) => {
      toast.success(i18n.t('accounting:toast.travelSynced', { count: data.syncedCount }));
      queryClient.invalidateQueries({ queryKey: ['accounting'] });
    },
    onError: () => toast.error(i18n.t('accounting:toast.travelSyncFailed')),
  });
}

export function useHrCostAnalysis(entityId: string, year: number, month: number) {
  return useQuery({
    queryKey: queryKeys.accounting.hrCostAnalysis(entityId, year, month),
    queryFn: async () => {
      const { data } = await api.get<KpiWithHrDto>('/accounting/kpis', {
        params: { entityId, year, month, include_hr: true },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

export function useGenerateDatevExport() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: { entityId: string; fiscalPeriodId: string; exportType: string }) => {
      const { data } = await api.post('/accounting/datev/exports', params);
      return data as { exportId: string };
    },
    onSuccess: () => {
      toast.success(i18n.t('accounting:toast.datevExportStarted'));
      queryClient.invalidateQueries({ queryKey: ['accounting', 'datev-exports'] });
    },
    onError: () => toast.error(i18n.t('accounting:toast.datevExportFailed')),
  });
}

// SignalR Hook für Echtzeit-Updates
export function useAccountingHub(entityId: string) {
  const queryClient = useQueryClient();
  useEffect(() => {
    if (!entityId) return;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/accounting', {
        accessTokenFactory: () => localStorage.getItem('cb_access_token') ?? '',
      })
      .withAutomaticReconnect()
      .build();

    connection.on('JournalEntryPosted', () => {
      queryClient.invalidateQueries({ queryKey: ['accounting', 'journal-entries'] });
      queryClient.invalidateQueries({ queryKey: ['accounting', 'trial-balance'] });
    });
    connection.on('JournalEntryCreated', () => {
      queryClient.invalidateQueries({ queryKey: ['accounting', 'journal-entries'] });
    });
    connection.on('PlanUpdated', () => {
      queryClient.invalidateQueries({ queryKey: ['accounting', 'plan-actual'] });
    });
    connection.on('TrialBalanceRefreshed', () => {
      queryClient.invalidateQueries({ queryKey: ['accounting', 'trial-balance'] });
      queryClient.invalidateQueries({ queryKey: ['accounting', 'bwa'] });
    });
    connection.on('DatevExportReady', ({ exportId }: { exportId: string }) => {
      toast.success(i18n.t('accounting:toast.datevExportReady'));
      queryClient.invalidateQueries({ queryKey: ['accounting', 'datev-exports'] });
    });

    connection.start()
      .then(() => connection.invoke('JoinEntityGroup', entityId))
      .catch(console.error);

    return () => { connection.stop(); };
  }, [entityId, queryClient]);
}
```

### 5.5 HrCostAnalysis Component (Beispiel mit HR-Filtern)

```tsx
// src/frontend/src/features/accounting/hr-integration/HrCostAnalysis.tsx
export function Component() {
  const { selectedEntityId } = useEntity();
  const [year, setYear] = useState(new Date().getFullYear());
  const [month, setMonth] = useState(new Date().getMonth() + 1);
  const [filterType, setFilterType] = useState<'all' | 'employee' | 'contractor'>('all');
  const [filterDept, setFilterDept] = useState('');

  const { data, isLoading } = useHrCostAnalysis(selectedEntityId ?? '', year, month);

  const filteredBreakdown = useMemo(() => {
    if (!data?.hrBreakdown) return [];
    return data.hrBreakdown.filter(row => {
      if (filterType === 'employee' && row.isContractor) return false;
      if (filterType === 'contractor' && !row.isContractor) return false;
      if (filterDept && row.department !== filterDept) return false;
      return true;
    });
  }, [data, filterType, filterDept]);

  return (
    <div>
      <PageHeader title={t('accounting:hrCosts.title')} />
      {/* KPI-Karten: Personalkosten gesamt, Contractor-Quote, Reisekosten */}
      {/* Recharts BarChart: Mitarbeiter-Kosten aufsteigend */}
      {/* DataTable: filterable nach Typ/Abteilung */}
    </div>
  );
}
```

---

## 6. Security, GoBD-Compliance & Audit

### 6.1 Berechtigungen (neue Einträge ins Seeding)

```
accounting.view          -- Buchungsjournal, BWA, Saldenliste lesen
accounting.post          -- Buchungen buchen/stornieren
accounting.plan          -- Plandaten / Szenarien bearbeiten
accounting.close         -- Perioden soft-schließen
accounting.admin         -- Perioden hard-schließen, Kontenplan, Stammdaten
accounting.export        -- DATEV-Export erstellen und herunterladen
accounting.document      -- Belege hochladen und verwalten
```

### 6.2 GoBD-Unveränderbarkeit

```
1. journal_entries.status = 'posted' → kein UPDATE mehr (DB-Trigger verhindert es)
2. SHA-256-Verkettung: hash = SHA256(id|entity_id|entry_number|entry_date|description|lines|previous_hash)
3. Kein DELETE auf journal_entries/journal_entry_lines (DB-Trigger)
4. Storno nur über Gegenbuchung (is_reversal = true, reversal_of_id gesetzt)
5. audit.change_log: DB-Trigger auf accounting.journal_entries (INSERT, kein UPDATE/DELETE nach Post)
```

**DB-Trigger gegen nachträgliche Änderungen:**

```sql
CREATE OR REPLACE FUNCTION accounting.prevent_posted_entry_modification()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD.status = 'Posted' THEN
        RAISE EXCEPTION 'Cannot modify a posted journal entry. Use reversal instead.';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_no_update_posted
    BEFORE UPDATE ON accounting.journal_entries
    FOR EACH ROW EXECUTE FUNCTION accounting.prevent_posted_entry_modification();

CREATE TRIGGER trg_no_delete_posted
    BEFORE DELETE ON accounting.journal_entries
    FOR EACH ROW
    WHEN (OLD.status = 'Posted')
    EXECUTE FUNCTION accounting.prevent_posted_entry_modification();
```

### 6.3 Aufbewahrungsfristen (GoBD)

| Datentyp | Frist | Umsetzung |
|----------|-------|-----------|
| Buchungsbelege (§ 147 AO) | 10 Jahre | `document.retention_until = upload_date + 10y` |
| Buchungsjournal | 10 Jahre | `audit.change_log` partitioniert, Partition-Drop nach 10 Jahren |
| Handels-/Geschäftsbriefe | 6 Jahre | Separate `document_type` mit entsprechender Retention |
| Lohnunterlagen | 6 Jahre | HR-Modul (bereits implementiert) |
| Reisekostenbelege (§ 4 EStG) | 10 Jahre | HR-Dokumente + `hr_travel_expense_id` in Journal |

### 6.4 HR-Datenschutz in der Buchhaltung

- `journal_entry_lines.hr_employee_id` ist nur sichtbar für Nutzer mit `accounting.view` **UND** (`hr.salary.view` ODER `hr.manager` für eigene Reports)
- Gehaltsbuchungen (Konto 6020): In DATEV-Export nur als Summenbuchung (kein Einzelmitarbeiter), aber in der internen BWA mit Kostenstellen-Drill-down
- `GetHrCostAnalysisQuery` prüft `hr.view`-Permission für individuelle Mitarbeiterdaten

---

## 7. Implementierungs-Roadmap

### Phase 1: Stammdaten & Buchungsinfrastruktur (18 MT)

**Abhängigkeiten:** Keine (aber HR muss bereits laufen für CostCenter-Events)

| Task | Aufwand | Abhängigkeit |
|------|---------|--------------|
| DB-Migrations: core + accounting + i18n + audit | 3 MT | — |
| EF Core Entities + Configurations (alle) | 4 MT | Migrations |
| SKR-04-Seeding (alle 2.500 Standardkonten + 119 Aqua-Cloud-Konten) | 2 MT | Entities |
| FiscalPeriod-Management (Commands + Queries) | 1 MT | Entities |
| CostCenter CRUD + HR-Employee-Link | 1 MT | HR-Modul |
| HrEmployeeCreatedEventHandler → Auto-CostCenter | 0.5 MT | CostCenter |
| AccountingController Grundgerüst + Auth | 1 MT | — |
| AccountingHub + Notifier | 1 MT | — |
| Frontend: ChartOfAccounts, FiscalPeriods | 2 MT | API |
| Frontend: Sidebar-Gruppe "Buchhaltung" + Routen | 0.5 MT | — |
| Program.cs: DI-Registrierungen, Hub-Mapping | 0.5 MT | — |
| Tests (Integration: JournalEntry-Balance, GoBD-Trigger) | 2 MT | alles |

### Phase 2: Buchungserfassung & DATEV (20 MT)

**Abhängigkeiten:** Phase 1 abgeschlossen

| Task | Aufwand | Abhängigkeit |
|------|---------|--------------|
| CreateJournalEntryCommand + PostJournalEntryCommand | 2 MT | Phase 1 |
| SHA-256-Verkettung (JournalEntryHashService) | 1 MT | Post-Command |
| ReverseJournalEntryCommand | 0.5 MT | Post-Command |
| RecurringEntryProcessor (IHostedService) | 1.5 MT | Journal |
| DATEV-Export-Service (Windows-1252, EXTF v700) | 4 MT | Journal |
| DATEV-Validierungen (alle aus §8.5) | 2 MT | Export-Service |
| Belegerfassung (Upload, Document-Entity, MinIO) | 2 MT | Phase 1 |
| AI-Extraktion (OCR-Pipeline, Buchungsvorschlag) | 3 MT | Belege |
| HR-Reisekosten-Sync: HrTravelExpenseApprovedEventHandler | 1 MT | Phase 1 |
| Frontend: JournalEntryForm (doppelte Buchführung UI) | 3 MT | API |
| Frontend: DocumentInbox + AI-Extraktion-Flow | 3 MT | Belege-API |
| Frontend: DatevExportPage | 1.5 MT | Datev-API |
| Frontend: useAccounting.ts Hooks (komplett) | 1 MT | API |
| Tests (DATEV-Encoding, Hash-Verkettung, Reisekosten-Sync) | 3 MT | alles |

### Phase 3: Auswertungen & HR-Integration (15 MT)

**Abhängigkeiten:** Phase 2

| Task | Aufwand | Abhängigkeit |
|------|---------|--------------|
| Materialized Views (mv_trial_balance, mv_bwa) + Refresh-Trigger | 2 MT | Phase 2 |
| GetTrialBalanceQuery + GetBwaQuery | 2 MT | Views |
| GetKpiDashboardQuery (inkl. HR-Daten) | 2 MT | Views |
| GetHrCostAnalysisQuery (Personal + Reisekosten + Contractor) | 2 MT | HR-Modul |
| HR-Salary-Event-Handler → Plan-Sync | 1.5 MT | Phase 2 |
| HR-Employee-Terminated-Handler → Abgrenzungsbuchung | 1 MT | Phase 2 |
| Frontend: BwaReport (Monat/Kumuliert/Vorjahresvergleich) | 2 MT | Queries |
| Frontend: TrialBalance | 1.5 MT | Queries |
| Frontend: HrCostAnalysis (Recharts + Filter) | 2 MT | Queries |
| Frontend: AccountingOverview Dashboard | 2 MT | alles |
| Tests (KPI-Berechnung, HR-Sync, Concurrent Refresh) | 2 MT | alles |

### Phase 4: Szenarien-Engine & Plan/Ist (12 MT)

**Abhängigkeiten:** Phase 3

| Task | Aufwand | Abhängigkeit |
|------|---------|--------------|
| Scenario + PlanEntry CRUD | 2 MT | Phase 1 |
| SyncHrSalaryToPlanCommand | 1 MT | HR-Modul |
| GetPlanActualComparisonQuery | 2 MT | Szenarien |
| planning.v_plan_actual_comparison View | 1 MT | Szenarien |
| Frontend: ScenarioList + ScenarioDetail | 3 MT | API |
| Frontend: PlanActualView (Tabelle + Recharts) | 3 MT | API |
| Tests (Szenario-Vergleich, HR-Sync-Korrektheit) | 2 MT | alles |

### Phase 5: Cash-Flow & Liquidität (10 MT)

**Abhängigkeiten:** Phase 3

| Task | Aufwand | Abhängigkeit |
|------|---------|--------------|
| cashflow-Schema: Entities, Migrations | 1.5 MT | Phase 1 |
| CashFlowItem-Population aus JournalEntries | 2 MT | Phase 2 |
| 13-Wochen-Forecast-Query | 2 MT | cashflow-Schema |
| Indirekte CF-Methode (GuV + Bilanzveränderungen) | 2 MT | mv_trial_balance |
| Frontend: CashFlowView mit Szenario-Overlay | 3 MT | API |
| Tests | 1 MT | alles |

### Phase 6: Bankintegration & AI-Kontozuordnung (12 MT)

**Abhängigkeiten:** Phase 2 + Phase 3

| Task | Aufwand | Abhängigkeit |
|------|---------|--------------|
| HBCI/FinTS oder moss-API-Import | 4 MT | Phase 2 |
| Automatischer Zahlungsabgleich | 3 MT | Bankimport |
| AI-Kontozuordnung (Learning aus Buchungshistorie) | 3 MT | Phase 3 |
| Anomalie-Erkennung | 2 MT | AI-Zuordnung |

**Gesamtaufwand: ~87 Mann-Tage**

---

## 8. Offene Punkte & Empfehlungen

### 8.1 Direkt aus Fachkonzept §11 (konkretisiert)

**OP-1: DATEV-Nummern (Kritisch — blockiert Phase 2)**
Ohne `datev_consultant_nr` und `datev_client_nr` erzeugt der EXTF-Header ein ungültiges Exportpaket. Interimslösung: Export mit leeren Feldern generieren, manuell im DATEV-Header ergänzen. Ziel: Feld in `LegalEntityExtension` per Admin-UI pflegbar machen.

**OP-2: Erlösbuchungen**
Ausgangsrechnungen/Erlöse fehlen in den 2025-Buchungsdaten. Optionen:
1. Import aus CRM (z.B. HubSpot-Webhook → `document.Document` vom Typ `OutgoingInvoice`)
2. Manuelle Erfassung in JournalEntryForm
3. API-Import (CSV/JSON) mit Duplikat-Check über `document_number`
Empfehlung: API-Import als Phase-1-Feature, CRM-Webhook in Phase 6.

**OP-3: Lohnbuchhaltung**
Bei externer Lohnbuchhaltung (DATEV Lohn): Summenbuchungs-Import pro Monat (Konto 6020, 6110, 3720-3790). Import via CSV-Upload oder DATEV-Lodas-Export. HR-Modul liefert nur die Mitarbeiter-Kostenstellen-Zuordnung — keine doppelte Datenhaltung, da Brutto-Zahlen aus DATEV Lohn kommen, HR-Modul liefert nur die Zuordnung.

**OP-4: Intercompany**
Clearing-Konten 3420/3422/3424 erfordern IC-Abstimmungsmodul. Minimallösung Phase 1: Konten anlegen, manuell buchen. Phase 6: Automatische IC-Eliminierung über `entity_relationships`-Tabelle.

**OP-5: moss-API**
moss bietet REST-API für Kreditkartentransaktionen. Integration als `JournalEntrySource.BankImport` mit Kreditkartenkonto 3610/3611. Kann parallel zu Phase 4 implementiert werden.

**OP-6: Lohnbuchhaltung vs. HR-Modul**
HR-Modul speichert `hr.salary_history.gross_amount_cents` — diese Daten werden für Plan-Sync verwendet. Die **tatsächliche Buchung** (nach Steuer, SV-Abzügen) kommt aus DATEV Lohn als Summenbuchung. Das ist kein Widerspruch: HR = Planungsgrundlage, DATEV Lohn = Ist-Buchung.

### 8.2 Technische Empfehlungen (eigene)

**EntryNumber-Sequenz pro Entity:**
`BIGSERIAL` reicht nicht für Entity-isolierte Sequenzen. Lösung:
```sql
-- Pro Entity eine Sequenz anlegen:
CREATE SEQUENCE accounting.journal_entry_number_seq_[entityid_ohne_dashes];
-- In PostJournalEntryCommandHandler via raw SQL abrufen (kein EF Core Sequence-Support für dynamische Namen)
```
Alternativ: `SELECT COALESCE(MAX(entry_number), 0) + 1 FROM accounting.journal_entries WHERE entity_id = @id FOR UPDATE` — einfacher, aber Deadlock-Risiko bei Parallel-Posts.

**CONCURRENT Materialized View Refresh:**
`REFRESH MATERIALIZED VIEW CONCURRENTLY` braucht `UNIQUE INDEX`. Ist in §4.10 bereits mit `idx_mv_tb` angelegt. Refresh-Trigger: nach jedem `PostJournalEntryCommand` (async, via `IBackgroundTaskQueue` — nicht im HTTP-Request-Thread blockieren).

**DATEV-Buchungstext (Windows-1252):**
Der `€`-Character (U+20AC) existiert nicht in Windows-1252 — wird zu `\x80` (DATEV interpretiert es korrekt, aber sicherer ist Ersatz durch "EUR"). Implementierung: Pre-Processing im `DatevExportService.EscapeCsvField()`.

**HR-Reisekosten-Deduplication:**
Mehrfaches Ausführen von `TravelCostSync` darf keine Doppelbuchungen erzeugen. Check: `JournalEntries.Any(je => je.SourceRef == $"hr:travel:{reportId}" && je.Status != Reversed)`.

**Partitionierung audit.change_log:**
Monatliche Partitionen manuell anlegen oder via `pg_partman` automatisieren. Retention-Job: `DROP TABLE audit.change_log_YYYY_MM` nach 10 Jahren (läuft als `IHostedService` täglich).

**Kontenplan-Update (DATEV SKR 04 jährlich):**
Upsert-Strategie: Neue Standardkonten einfügen, bestehende aktualisieren (außer entity-spezifische). Implementierung: Admin-Endpoint `POST /api/accounting/admin/skr04-update` mit JSON-Datei-Upload.

**Aqua Cloud Migrations-Import (2.250 Buchungen aus 2025):**
Separater `ImportMigrationDataCommand` der:
1. Alle 2.250 Zeilen als `JournalEntry` mit `SourceType.Migration` anlegt
2. Status direkt auf `Posted` setzt (Sonderfall: GoBD-konform, da es historische Daten sind)
3. Hash-Kette initialisiert (erster Hash mit `PreviousHash = "0000...0000"`)
4. Kostenstellen-Codes (200-240, 2001-2018) aus CSV auf `core.cost_centers` mappt