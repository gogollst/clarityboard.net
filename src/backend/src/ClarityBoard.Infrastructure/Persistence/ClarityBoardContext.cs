using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Entities.Asset;
using ClarityBoard.Domain.Entities.Budget;
using ClarityBoard.Domain.Entities.CashFlow;
using ClarityBoard.Domain.Entities.Document;
using ClarityBoard.Domain.Entities.Entity;
using ClarityBoard.Domain.Entities.Identity;
using ClarityBoard.Domain.Entities.Integration;
using ClarityBoard.Domain.Entities.KPI;
using ClarityBoard.Domain.Entities.Scenario;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Interfaces;
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
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Locale).HasMaxLength(10);
            entity.Property(e => e.Timezone).HasMaxLength(50);
            entity.Property(e => e.AvatarPath).HasMaxLength(500);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.TwoFactorSecret).HasMaxLength(500);
            entity.Property(e => e.RecoveryCodesHash).HasMaxLength(2000);
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
    }
}
