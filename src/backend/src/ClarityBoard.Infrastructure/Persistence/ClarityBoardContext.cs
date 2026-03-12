using ClarityBoard.Domain.Entities.Admin;
using ClarityBoard.Domain.Entities.AI;
using ClarityBoard.Domain.Entities.Mail;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Entities.Asset;
using ClarityBoard.Domain.Entities.Budget;
using ClarityBoard.Domain.Entities.CashFlow;
using ClarityBoard.Domain.Entities.Document;
using ClarityBoard.Domain.Entities.Entity;
using ClarityBoard.Domain.Entities.Hr;
using ClarityBoard.Domain.Entities.Identity;
using ClarityBoard.Domain.Entities.Integration;
using ClarityBoard.Domain.Entities.KPI;
using ClarityBoard.Domain.Entities.Scenario;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Interfaces;
using ClarityBoard.Infrastructure.Persistence.Configurations.Hr;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Persistence;

public class ClarityBoardContext : DbContext, IUnitOfWork, IAppDbContext
{
    public ClarityBoardContext(DbContextOptions<ClarityBoardContext> options)
        : base(options) { }

    // Accounting
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<RecurringEntry> RecurringEntries => Set<RecurringEntry>();
    public DbSet<VatRecord> VatRecords => Set<VatRecord>();
    public DbSet<LegalEntityExtension> LegalEntityExtensions => Set<LegalEntityExtension>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<AccountingDocument> AccountingDocuments => Set<AccountingDocument>();
    public DbSet<AccountingScenario> AccountingScenarios => Set<AccountingScenario>();
    public DbSet<AccountingPlanEntry> AccountingPlanEntries => Set<AccountingPlanEntry>();
    public DbSet<DatevExport> DatevExports => Set<DatevExport>();
    public DbSet<BusinessPartner> BusinessPartners => Set<BusinessPartner>();

    // Entity
    public DbSet<LegalEntity> LegalEntities => Set<LegalEntity>();
    public DbSet<EntityRelationship> EntityRelationships => Set<EntityRelationship>();
    public DbSet<TaxUnit> TaxUnits => Set<TaxUnit>();
    public DbSet<IntercompanyRule> IntercompanyRules => Set<IntercompanyRule>();

    // KPI
    public DbSet<KpiDefinition> KpiDefinitions => Set<KpiDefinition>();
    public DbSet<KpiSnapshot> KpiSnapshots => Set<KpiSnapshot>();
    public DbSet<KpiAlert> KpiAlerts => Set<KpiAlert>();
    public DbSet<KpiAlertEvent> KpiAlertEvents => Set<KpiAlertEvent>();

    // CashFlow
    public DbSet<CashFlowEntry> CashFlowEntries => Set<CashFlowEntry>();
    public DbSet<CashFlowForecast> CashFlowForecasts => Set<CashFlowForecast>();
    public DbSet<LiquidityAlert> LiquidityAlerts => Set<LiquidityAlert>();

    // Scenario
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<ScenarioParameter> ScenarioParameters => Set<ScenarioParameter>();
    public DbSet<ScenarioResult> ScenarioResults => Set<ScenarioResult>();
    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();

    // Document
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentField> DocumentFields => Set<DocumentField>();
    public DbSet<BookingSuggestion> BookingSuggestions => Set<BookingSuggestion>();
    public DbSet<RecurringPattern> RecurringPatterns => Set<RecurringPattern>();

    // Budget
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<BudgetRevision> BudgetRevisions => Set<BudgetRevision>();

    // Asset
    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<DepreciationSchedule> DepreciationSchedules => Set<DepreciationSchedule>();
    public DbSet<AssetDisposal> AssetDisposals => Set<AssetDisposal>();

    // Admin
    public DbSet<AuthConfig> AuthConfigs => Set<AuthConfig>();

    // Mail
    public DbSet<MailConfig> MailConfigs => Set<MailConfig>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    // Hr Module
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<SalaryHistory> SalaryHistories => Set<SalaryHistory>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<EmployeeAddressHistory> EmployeeAddressHistories => Set<EmployeeAddressHistory>();
    public DbSet<EmployeeContactHistory> EmployeeContactHistories => Set<EmployeeContactHistory>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<WorkTimeEntry> WorkTimeEntries => Set<WorkTimeEntry>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<FeedbackEntry> FeedbackEntries => Set<FeedbackEntry>();
    public DbSet<TravelExpenseReport> TravelExpenseReports => Set<TravelExpenseReport>();
    public DbSet<TravelExpenseItem> TravelExpenseItems => Set<TravelExpenseItem>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<DataAccessLog> DataAccessLogs => Set<DataAccessLog>();
    public DbSet<DeletionRequest> DeletionRequests => Set<DeletionRequest>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();
    public DbSet<OnboardingChecklist> OnboardingChecklists => Set<OnboardingChecklist>();
    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();

    // AI Management
    public DbSet<AiProviderConfig> AiProviderConfigs => Set<AiProviderConfig>();
    public DbSet<AiPrompt> AiPrompts => Set<AiPrompt>();
    public DbSet<AiPromptVersion> AiPromptVersions => Set<AiPromptVersion>();
    public DbSet<AiCallLog> AiCallLogs => Set<AiCallLog>();

    // Integration
    public DbSet<WebhookConfig> WebhookConfigs => Set<WebhookConfig>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<MappingRule> MappingRules => Set<MappingRule>();
    public DbSet<PullAdapterConfig> PullAdapterConfigs => Set<PullAdapterConfig>();

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ───────────────────────────────────────────────
        // Schema assignments
        // ───────────────────────────────────────────────

        // Accounting schema
        modelBuilder.Entity<Account>().ToTable("accounts", "accounting");
        modelBuilder.Entity<JournalEntry>().ToTable("journal_entries", "accounting");
        modelBuilder.Entity<JournalEntryLine>().ToTable("journal_entry_lines", "accounting");
        modelBuilder.Entity<FiscalPeriod>().ToTable("fiscal_periods", "accounting");
        modelBuilder.Entity<RecurringEntry>().ToTable("recurring_entries", "accounting");
        modelBuilder.Entity<VatRecord>().ToTable("vat_records", "accounting");

        // new accounting entities
        modelBuilder.Entity<LegalEntityExtension>().ToTable("legal_entity_extensions", "accounting");
        modelBuilder.Entity<CostCenter>().ToTable("cost_centers", "accounting");
        modelBuilder.Entity<AccountingDocument>().ToTable("accounting_documents", "accounting");
        modelBuilder.Entity<AccountingScenario>().ToTable("accounting_scenarios", "accounting");
        modelBuilder.Entity<AccountingPlanEntry>().ToTable("accounting_plan_entries", "accounting");
        modelBuilder.Entity<DatevExport>().ToTable("datev_exports", "accounting");
        modelBuilder.Entity<BusinessPartner>().ToTable("business_partners", "accounting");

        // Entity schema
        modelBuilder.Entity<LegalEntity>().ToTable("legal_entities", "entity");
        modelBuilder.Entity<EntityRelationship>().ToTable("entity_relationships", "entity");
        modelBuilder.Entity<TaxUnit>().ToTable("tax_units", "entity");
        modelBuilder.Entity<IntercompanyRule>().ToTable("intercompany_rules", "entity");

        // KPI schema
        modelBuilder.Entity<KpiDefinition>().ToTable("kpi_definitions", "kpi");
        modelBuilder.Entity<KpiSnapshot>().ToTable("kpi_snapshots", "kpi");
        modelBuilder.Entity<KpiAlert>().ToTable("kpi_alerts", "kpi");
        modelBuilder.Entity<KpiAlertEvent>().ToTable("kpi_alert_events", "kpi");

        // CashFlow schema
        modelBuilder.Entity<CashFlowEntry>().ToTable("cash_flow_entries", "cashflow");
        modelBuilder.Entity<CashFlowForecast>().ToTable("cash_flow_forecasts", "cashflow");
        modelBuilder.Entity<LiquidityAlert>().ToTable("liquidity_alerts", "cashflow");

        // Scenario schema
        modelBuilder.Entity<Scenario>().ToTable("scenarios", "scenario");
        modelBuilder.Entity<ScenarioParameter>().ToTable("scenario_parameters", "scenario");
        modelBuilder.Entity<ScenarioResult>().ToTable("scenario_results", "scenario");
        modelBuilder.Entity<SimulationRun>().ToTable("simulation_runs", "scenario");

        // Document schema
        modelBuilder.Entity<Document>().ToTable("documents", "document");
        modelBuilder.Entity<DocumentField>().ToTable("document_fields", "document");
        modelBuilder.Entity<BookingSuggestion>().ToTable("booking_suggestions", "document");
        modelBuilder.Entity<RecurringPattern>().ToTable("recurring_patterns", "document");

        // Budget schema
        modelBuilder.Entity<Budget>().ToTable("budgets", "budget");
        modelBuilder.Entity<BudgetLine>().ToTable("budget_lines", "budget");
        modelBuilder.Entity<BudgetRevision>().ToTable("budget_revisions", "budget");

        // Asset schema
        modelBuilder.Entity<FixedAsset>().ToTable("fixed_assets", "asset");
        modelBuilder.Entity<DepreciationSchedule>().ToTable("depreciation_schedules", "asset");
        modelBuilder.Entity<AssetDisposal>().ToTable("asset_disposals", "asset");

        // Integration schema
        modelBuilder.Entity<WebhookConfig>().ToTable("webhook_configs", "integration");
        modelBuilder.Entity<WebhookEvent>().ToTable("webhook_events", "integration");
        modelBuilder.Entity<MappingRule>().ToTable("mapping_rules", "integration");
        modelBuilder.Entity<PullAdapterConfig>().ToTable("pull_adapter_configs", "integration");

        // Admin schema (public)
        modelBuilder.Entity<AuthConfig>().ToTable("auth_configs", "public");

        // Mail schema
        modelBuilder.Entity<MailConfig>().ToTable("mail_configs", "mail");
        modelBuilder.Entity<EmailLog>().ToTable("email_logs", "mail");

        // AI schema
        modelBuilder.Entity<AiProviderConfig>().ToTable("ai_provider_configs", "ai");
        modelBuilder.Entity<AiPrompt>().ToTable("ai_prompts", "ai");
        modelBuilder.Entity<AiPromptVersion>().ToTable("ai_prompt_versions", "ai");
        modelBuilder.Entity<AiCallLog>().ToTable("ai_call_logs", "ai");

        // Public schema (Identity)
        modelBuilder.Entity<User>().ToTable("users", "public");
        modelBuilder.Entity<Role>().ToTable("roles", "public");
        modelBuilder.Entity<Permission>().ToTable("permissions", "public");
        modelBuilder.Entity<UserRole>().ToTable("user_roles", "public");
        modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens", "public");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs", "public");

        // ───────────────────────────────────────────────
        // ACCOUNTING SCHEMA
        // ───────────────────────────────────────────────

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.AccountNumber }).IsUnique();
            entity.Property(e => e.AccountNumber).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.AccountType).HasMaxLength(20);
            entity.Property(e => e.VatDefault).HasMaxLength(10);
            entity.Property(e => e.DatevAuto).HasMaxLength(10);
            entity.Property(e => e.CostCenterDefault).HasMaxLength(50);
            entity.Property(e => e.BwaLine).HasMaxLength(10);
        });

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.EntryNumber }).IsUnique();
            entity.HasIndex(e => new { e.EntityId, e.EntryDate });
            entity.HasIndex(e => new { e.EntityId, e.FiscalPeriodId });
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.SourceType).HasMaxLength(50);
            entity.Property(e => e.SourceRef).HasMaxLength(200);
            entity.Property(e => e.Hash).HasMaxLength(64);
            entity.Property(e => e.PreviousHash).HasMaxLength(64);
            entity.Property(e => e.DocumentRef).HasMaxLength(36);
            entity.Property(e => e.DocumentRef2).HasMaxLength(12);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasMany(e => e.Lines).WithOne().HasForeignKey(l => l.JournalEntryId);
        });

        modelBuilder.Entity<JournalEntryLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.JournalEntryId, e.LineNumber }).IsUnique();
            entity.HasIndex(e => new { e.AccountId, e.JournalEntryId });
            entity.Property(e => e.DebitAmount).HasPrecision(18, 2);
            entity.Property(e => e.CreditAmount).HasPrecision(18, 2);
            entity.Property(e => e.BaseAmount).HasPrecision(18, 2);
            entity.Property(e => e.ExchangeRate).HasPrecision(12, 6);
            entity.Property(e => e.VatAmount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.VatCode).HasMaxLength(10);
            entity.Property(e => e.CostCenter).HasMaxLength(50);
            entity.Property(e => e.TaxCode).HasMaxLength(10);
            entity.Property(e => e.Description).HasMaxLength(300);
        });

        modelBuilder.Entity<FiscalPeriod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.Year, e.Month }).IsUnique();
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<RecurringEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.IsActive });
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Frequency).HasMaxLength(20);
        });

        modelBuilder.Entity<VatRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.Year, e.Month });
            entity.Property(e => e.VatCode).HasMaxLength(10);
            entity.Property(e => e.VatType).HasMaxLength(30);
            entity.Property(e => e.NetAmount).HasPrecision(18, 2);
            entity.Property(e => e.VatAmount).HasPrecision(18, 2);
            entity.Property(e => e.VatRate).HasPrecision(5, 2);
        });

        modelBuilder.Entity<LegalEntityExtension>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntityId).IsUnique();
            entity.Property(e => e.DatevConsultantNumber).HasMaxLength(20);
            entity.Property(e => e.DatevClientNumber).HasMaxLength(20);
            entity.Property(e => e.HrbNumber).HasMaxLength(50);
            entity.Property(e => e.VatId).HasMaxLength(20);
            entity.Property(e => e.TaxOffice).HasMaxLength(100);
            entity.Property(e => e.DefaultCurrencyCode).HasMaxLength(3);
            entity.Property(e => e.FiscalYearStartMonth).HasMaxLength(2);
        });

        modelBuilder.Entity<CostCenter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.Code }).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ShortName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<AccountingDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.Status });
            entity.Property(e => e.DocumentType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DocumentNumber).HasMaxLength(100);
            entity.Property(e => e.VendorName).HasMaxLength(200);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3);
            entity.Property(e => e.StoragePath).HasMaxLength(1000);
            entity.Property(e => e.MimeType).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<AccountingScenario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.Year });
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ScenarioType).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<AccountingPlanEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ScenarioId, e.AccountId, e.PeriodYear, e.PeriodMonth, e.HrEmployeeId });
            entity.Property(e => e.Source).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        modelBuilder.Entity<DatevExport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.CreatedAt });
            entity.Property(e => e.ExportType).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<BusinessPartner>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.PartnerNumber }).IsUnique();
            entity.HasIndex(e => new { e.EntityId, e.Name });
            entity.Property(e => e.PartnerNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TaxId).HasMaxLength(50);
            entity.Property(e => e.VatNumber).HasMaxLength(50);
            entity.Property(e => e.Street).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.Country).HasMaxLength(2);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.BankName).HasMaxLength(200);
            entity.Property(e => e.Iban).HasMaxLength(34);
            entity.Property(e => e.Bic).HasMaxLength(11);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasOne<ClarityBoard.Domain.Entities.Hr.Employee>()
                .WithMany()
                .HasForeignKey(e => e.ContactEmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ───────────────────────────────────────────────
        // ENTITY SCHEMA
        // ───────────────────────────────────────────────

        modelBuilder.Entity<LegalEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.LegalForm).HasMaxLength(50);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.ChartOfAccounts).HasMaxLength(10);
            entity.Property(e => e.TaxId).HasMaxLength(50);
            entity.Property(e => e.VatId).HasMaxLength(50);
            entity.Property(e => e.RegistrationNumber).HasMaxLength(100);
            entity.Property(e => e.DatevClientNumber).HasMaxLength(7);
            entity.Property(e => e.DatevConsultantNumber).HasMaxLength(7);
            entity.Property(e => e.ManagingDirector).HasMaxLength(200);
            entity.Property(e => e.ManagingDirectorId).IsRequired(false);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.ManagingDirectorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EntityRelationship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ParentEntityId, e.ChildEntityId }).IsUnique();
            entity.Property(e => e.ConsolidationType).HasMaxLength(20);
            entity.Property(e => e.OwnershipPct).HasPrecision(5, 2);
        });

        modelBuilder.Entity<TaxUnit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.TaxType).HasMaxLength(50);
        });

        modelBuilder.Entity<IntercompanyRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleType).HasMaxLength(50);
            entity.Property(e => e.SourceAccountPattern).HasMaxLength(100);
            entity.Property(e => e.TargetAccountPattern).HasMaxLength(100);
            entity.Property(e => e.EliminationMethod).HasMaxLength(20);
        });

        // ───────────────────────────────────────────────
        // KPI SCHEMA
        // ───────────────────────────────────────────────

        modelBuilder.Entity<KpiDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Domain).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.Direction).HasMaxLength(20);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.CalculationClass).HasMaxLength(300);
        });

        modelBuilder.Entity<KpiSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.KpiId, e.SnapshotDate }).IsUnique();
            entity.Property(e => e.Value).HasPrecision(18, 6);
            entity.Property(e => e.PreviousValue).HasPrecision(18, 6);
            entity.Property(e => e.ChangePct).HasPrecision(8, 4);
            entity.Property(e => e.TargetValue).HasPrecision(18, 6);
            entity.Property(e => e.KpiId).HasMaxLength(100);
        });

        modelBuilder.Entity<KpiAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.KpiId, e.IsActive });
            entity.Property(e => e.KpiId).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Condition).HasMaxLength(20);
            entity.Property(e => e.Severity).HasMaxLength(20);
            entity.Property(e => e.ThresholdValue).HasPrecision(18, 6);
        });

        modelBuilder.Entity<KpiAlertEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.Status, e.CreatedAt });
            entity.Property(e => e.KpiId).HasMaxLength(100);
            entity.Property(e => e.Severity).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.CurrentValue).HasPrecision(18, 6);
            entity.Property(e => e.ThresholdValue).HasPrecision(18, 6);
        });

        // ───────────────────────────────────────────────
        // CASHFLOW SCHEMA
        // ───────────────────────────────────────────────

        modelBuilder.Entity<CashFlowEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.Category, e.EntryDate });
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Subcategory).HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.BaseAmount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.SourceType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Certainty).HasMaxLength(20).HasDefaultValue("confirmed");
        });

        modelBuilder.Entity<CashFlowForecast>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.ForecastDate });
            entity.Property(e => e.ProjectedInflow).HasPrecision(18, 2);
            entity.Property(e => e.ProjectedOutflow).HasPrecision(18, 2);
            entity.Property(e => e.ProjectedBalance).HasPrecision(18, 2);
            entity.Property(e => e.ConfidenceLow).HasPrecision(18, 2);
            entity.Property(e => e.ConfidenceHigh).HasPrecision(18, 2);
        });

        modelBuilder.Entity<LiquidityAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.Status });
            entity.Property(e => e.AlertType).HasMaxLength(50);
            entity.Property(e => e.Severity).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.ThresholdValue).HasPrecision(18, 2);
            entity.Property(e => e.CurrentValue).HasPrecision(18, 2);
        });

        // ───────────────────────────────────────────────
        // SCENARIO SCHEMA
        // ───────────────────────────────────────────────

        modelBuilder.Entity<Scenario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.Status });
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.HasMany(e => e.Parameters).WithOne().HasForeignKey(p => p.ScenarioId);
        });

        modelBuilder.Entity<ScenarioParameter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ScenarioId, e.ParameterKey }).IsUnique();
            entity.Property(e => e.ParameterKey).HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.BaseValue).HasPrecision(18, 6);
            entity.Property(e => e.AdjustedValue).HasPrecision(18, 6);
        });

        modelBuilder.Entity<ScenarioResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ScenarioId, e.KpiId, e.Month }).IsUnique();
            entity.Property(e => e.KpiId).HasMaxLength(100);
            entity.Property(e => e.ProjectedValue).HasPrecision(18, 6);
            entity.Property(e => e.BaselineValue).HasPrecision(18, 6);
            entity.Property(e => e.DeltaValue).HasPrecision(18, 6);
            entity.Property(e => e.DeltaPct).HasPrecision(8, 4);
        });

        modelBuilder.Entity<SimulationRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ScenarioId, e.RunNumber }).IsUnique();
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        // ───────────────────────────────────────────────
        // DOCUMENT SCHEMA
        // ───────────────────────────────────────────────

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.Status, e.CreatedAt });
            entity.Property(e => e.FileName).HasMaxLength(500);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.StoragePath).HasMaxLength(1000);
            entity.Property(e => e.DocumentType).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.VendorName).HasMaxLength(200);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.Confidence).HasPrecision(5, 4);
            entity.HasMany(e => e.Fields).WithOne().HasForeignKey(f => f.DocumentId);
        });

        modelBuilder.Entity<DocumentField>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DocumentId, e.FieldName });
            entity.Property(e => e.FieldName).HasMaxLength(100);
            entity.Property(e => e.FieldValue).HasMaxLength(1000);
            entity.Property(e => e.CorrectedValue).HasMaxLength(1000);
            entity.Property(e => e.Confidence).HasPrecision(5, 4);
        });

        modelBuilder.Entity<BookingSuggestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DocumentId, e.Status });
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.VatCode).HasMaxLength(10);
            entity.Property(e => e.VatAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Confidence).HasPrecision(5, 4);
        });

        modelBuilder.Entity<RecurringPattern>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.VendorName });
            entity.Property(e => e.VendorName).HasMaxLength(200);
            entity.Property(e => e.VendorPattern).HasMaxLength(500);
            entity.Property(e => e.VatCode).HasMaxLength(10);
            entity.Property(e => e.CostCenter).HasMaxLength(50);
            entity.Property(e => e.Confidence).HasPrecision(5, 4);
        });

        // ───────────────────────────────────────────────
        // BUDGET SCHEMA
        // ───────────────────────────────────────────────

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.FiscalYear, e.BudgetType });
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.BudgetType).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasMany(e => e.Lines).WithOne().HasForeignKey(l => l.BudgetId);
        });

        modelBuilder.Entity<BudgetLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BudgetId, e.AccountId, e.Month }).IsUnique();
            entity.Property(e => e.CostCenter).HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.ActualAmount).HasPrecision(18, 2);
            entity.Property(e => e.Variance).HasPrecision(18, 2);
            entity.Property(e => e.VariancePct).HasPrecision(8, 4);
        });

        modelBuilder.Entity<BudgetRevision>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BudgetId, e.RevisionNumber }).IsUnique();
            entity.Property(e => e.Reason).HasMaxLength(500);
        });

        // ───────────────────────────────────────────────
        // ASSET SCHEMA
        // ───────────────────────────────────────────────

        modelBuilder.Entity<FixedAsset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.AssetNumber }).IsUnique();
            entity.Property(e => e.AssetNumber).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.AssetCategory).HasMaxLength(50);
            entity.Property(e => e.DepreciationMethod).HasMaxLength(30);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.AfaCode).HasMaxLength(20);
            entity.Property(e => e.AcquisitionCost).HasPrecision(18, 2);
            entity.Property(e => e.ResidualValue).HasPrecision(18, 2);
            entity.Property(e => e.AccumulatedDepreciation).HasPrecision(18, 2);
            entity.Property(e => e.BookValue).HasPrecision(18, 2);
            entity.HasMany(e => e.Schedules).WithOne().HasForeignKey(s => s.AssetId);
        });

        modelBuilder.Entity<DepreciationSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AssetId, e.PeriodDate }).IsUnique();
            entity.Property(e => e.DepreciationAmount).HasPrecision(18, 2);
            entity.Property(e => e.AccumulatedAmount).HasPrecision(18, 2);
            entity.Property(e => e.BookValueAfter).HasPrecision(18, 2);
        });

        modelBuilder.Entity<AssetDisposal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AssetId }).IsUnique();
            entity.Property(e => e.DisposalType).HasMaxLength(30);
            entity.Property(e => e.DisposalProceeds).HasPrecision(18, 2);
            entity.Property(e => e.BookValueAtDisposal).HasPrecision(18, 2);
            entity.Property(e => e.GainLoss).HasPrecision(18, 2);
        });

        // ───────────────────────────────────────────────
        // INTEGRATION SCHEMA
        // ───────────────────────────────────────────────

        modelBuilder.Entity<WebhookConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.SourceType });
            entity.Property(e => e.SourceType).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.EndpointPath).HasMaxLength(500);
            entity.Property(e => e.SecretKey).HasMaxLength(500);
            entity.Property(e => e.HeaderSignatureKey).HasMaxLength(100);
        });

        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SourceType, e.SourceId, e.IdempotencyKey }).IsUnique();
            entity.HasIndex(e => new { e.Status, e.NextRetryAt });
            entity.HasIndex(e => new { e.SourceType, e.SourceId, e.ReceivedAt });
            entity.Property(e => e.SourceType).HasMaxLength(50);
            entity.Property(e => e.SourceId).HasMaxLength(100);
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.HasIndex(e => new { e.EntityId, e.Status });
        });

        modelBuilder.Entity<MappingRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.SourceType, e.EventType, e.Priority });
            entity.Property(e => e.SourceType).HasMaxLength(50);
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.VatCode).HasMaxLength(10);
            entity.Property(e => e.CostCenter).HasMaxLength(50);
        });

        modelBuilder.Entity<PullAdapterConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.AdapterType });
            entity.Property(e => e.AdapterType).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Schedule).HasMaxLength(50);
            entity.Property(e => e.LastRunStatus).HasMaxLength(20);
        });

        // ───────────────────────────────────────────────
        // IDENTITY (PUBLIC SCHEMA)
        // ───────────────────────────────────────────────

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.InvitationToken).IsUnique().HasFilter("\"InvitationToken\" IS NOT NULL");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired(false);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Locale).HasMaxLength(10);
            entity.Property(e => e.Timezone).HasMaxLength(50);
            entity.Property(e => e.AvatarPath).HasMaxLength(500);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.TwoFactorSecret).HasMaxLength(500);
            entity.Property(e => e.RecoveryCodesHash).HasMaxLength(2000);
            entity.Property(e => e.Status).HasDefaultValue(UserStatus.Active);
            entity.Property(e => e.InvitationToken).HasMaxLength(256);
            entity.Property(e => e.PasswordResetToken).HasMaxLength(256);
            entity.Ignore(e => e.FullName);
            entity.Ignore(e => e.IsLocked);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.HasMany(e => e.Permissions).WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany().HasForeignKey("PermissionId"),
                    l => l.HasOne<Role>().WithMany().HasForeignKey("RoleId"),
                    je =>
                    {
                        je.ToTable("role_permissions", "public");
                        je.HasKey("RoleId", "PermissionId");
                    });
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Module).HasMaxLength(50);
            entity.Property(e => e.Action).HasMaxLength(20);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.RoleId, e.EntityId }).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.DeviceFingerprint });
            entity.Property(e => e.Token).HasMaxLength(128);
            entity.Property(e => e.DeviceFingerprint).HasMaxLength(256);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.RevokedReason).HasMaxLength(100);
            entity.Ignore(e => e.IsExpired);
            entity.Ignore(e => e.IsRevoked);
            entity.Ignore(e => e.IsActive);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityId, e.TableName, e.CreatedAt });
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.TableName).HasMaxLength(100);
            entity.Property(e => e.RecordId).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Hash).HasMaxLength(64);
            entity.Property(e => e.PreviousHash).HasMaxLength(64);
        });

        // ───────────────────────────────────────────────
        // AI SCHEMA
        // ───────────────────────────────────────────────

        modelBuilder.Entity<AiProviderConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Only one active config per provider
            entity.HasIndex(e => new { e.Provider, e.IsActive }).IsUnique()
                .HasFilter("\"IsActive\" = true");
            entity.Property(e => e.Provider).HasConversion<int>();
            entity.Property(e => e.EncryptedApiKey).HasMaxLength(2000);
            entity.Property(e => e.KeyHint).HasMaxLength(10);
            entity.Property(e => e.BaseUrl).HasMaxLength(500);
            entity.Property(e => e.ModelDefault).HasMaxLength(100);
        });

        modelBuilder.Entity<AiPrompt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PromptKey).IsUnique();
            entity.Property(e => e.PromptKey).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Module).HasMaxLength(100);
            entity.Property(e => e.PrimaryModel).HasMaxLength(100);
            entity.Property(e => e.FallbackModel).HasMaxLength(100);
            entity.Property(e => e.Temperature).HasPrecision(4, 2);
            entity.Property(e => e.PrimaryProvider).HasConversion<int>();
            entity.Property(e => e.FallbackProvider).HasConversion<int>();
        });

        modelBuilder.Entity<AiPromptVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PromptId, e.Version }).IsUnique();
            entity.Property(e => e.ChangeSummary).HasMaxLength(500);
            entity.Property(e => e.PrimaryModel).HasMaxLength(100);
            entity.Property(e => e.PrimaryProvider).HasConversion<int>();
            entity.Property(e => e.FallbackModel).HasMaxLength(100);
            entity.Property(e => e.FallbackProvider).HasConversion<int>();
            entity.Property(e => e.Temperature).HasPrecision(4, 2);
            entity.HasOne<AiPrompt>().WithMany()
                .HasForeignKey(e => e.PromptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiCallLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PromptId, e.CreatedAt });
            entity.HasIndex(e => new { e.IsSuccess, e.CreatedAt });
            entity.Property(e => e.UsedProvider).HasConversion<int>();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.HasOne<AiPrompt>().WithMany()
                .HasForeignKey(e => e.PromptId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Mail ──────────────────────────────────────────────────────────────────
        modelBuilder.Entity<MailConfig>(entity =>
        {
            entity.ToTable("mail_configs", "mail");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Host).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(500).IsRequired();
            entity.Property(e => e.EncryptedPassword).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.FromEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FromName).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.ToTable("email_logs", "mail");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ToEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => e.SentAt);
            entity.HasIndex(e => e.UserId);
        });

        // ───────────────────────────────────────────────
        // HR SCHEMA
        // ───────────────────────────────────────────────

        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new SalaryHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new ContractConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeAddressHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeContactHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new LeaveTypeConfiguration());
        modelBuilder.ApplyConfiguration(new LeaveBalanceConfiguration());
        modelBuilder.ApplyConfiguration(new LeaveRequestConfiguration());
        modelBuilder.ApplyConfiguration(new WorkTimeEntryConfiguration());
        modelBuilder.ApplyConfiguration(new PerformanceReviewConfiguration());
        modelBuilder.ApplyConfiguration(new FeedbackEntryConfiguration());
        modelBuilder.ApplyConfiguration(new TravelExpenseReportConfiguration());
        modelBuilder.ApplyConfiguration(new TravelExpenseItemConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new DataAccessLogConfiguration());
        modelBuilder.ApplyConfiguration(new DeletionRequestConfiguration());
        modelBuilder.ApplyConfiguration(new PublicHolidayConfiguration());
        modelBuilder.ApplyConfiguration(new OnboardingChecklistConfiguration());
        modelBuilder.ApplyConfiguration(new OnboardingTaskConfiguration());
    }
}
