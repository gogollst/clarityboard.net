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
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Common.Interfaces;

public interface IAppDbContext
{
    // Accounting
    DbSet<Account> Accounts { get; }
    DbSet<JournalEntry> JournalEntries { get; }
    DbSet<JournalEntryLine> JournalEntryLines { get; }
    DbSet<FiscalPeriod> FiscalPeriods { get; }
    DbSet<RecurringEntry> RecurringEntries { get; }
    DbSet<VatRecord> VatRecords { get; }

    // Entity
    DbSet<LegalEntity> LegalEntities { get; }
    DbSet<EntityRelationship> EntityRelationships { get; }
    DbSet<TaxUnit> TaxUnits { get; }
    DbSet<IntercompanyRule> IntercompanyRules { get; }

    // KPI
    DbSet<KpiDefinition> KpiDefinitions { get; }
    DbSet<KpiSnapshot> KpiSnapshots { get; }
    DbSet<KpiAlert> KpiAlerts { get; }
    DbSet<KpiAlertEvent> KpiAlertEvents { get; }

    // CashFlow
    DbSet<CashFlowEntry> CashFlowEntries { get; }
    DbSet<CashFlowForecast> CashFlowForecasts { get; }
    DbSet<LiquidityAlert> LiquidityAlerts { get; }

    // Scenario
    DbSet<Scenario> Scenarios { get; }
    DbSet<ScenarioParameter> ScenarioParameters { get; }
    DbSet<ScenarioResult> ScenarioResults { get; }
    DbSet<SimulationRun> SimulationRuns { get; }

    // Document
    DbSet<Document> Documents { get; }
    DbSet<DocumentField> DocumentFields { get; }
    DbSet<BookingSuggestion> BookingSuggestions { get; }
    DbSet<RecurringPattern> RecurringPatterns { get; }

    // Budget
    DbSet<Budget> Budgets { get; }
    DbSet<BudgetLine> BudgetLines { get; }
    DbSet<BudgetRevision> BudgetRevisions { get; }

    // Asset
    DbSet<FixedAsset> FixedAssets { get; }
    DbSet<DepreciationSchedule> DepreciationSchedules { get; }
    DbSet<AssetDisposal> AssetDisposals { get; }

    // Integration
    DbSet<WebhookConfig> WebhookConfigs { get; }
    DbSet<WebhookEvent> WebhookEvents { get; }
    DbSet<MappingRule> MappingRules { get; }
    DbSet<PullAdapterConfig> PullAdapterConfigs { get; }

    // AI Management
    DbSet<AiProviderConfig> AiProviderConfigs { get; }
    DbSet<AiPrompt> AiPrompts { get; }
    DbSet<AiPromptVersion> AiPromptVersions { get; }
    DbSet<AiCallLog> AiCallLogs { get; }

    // Mail
    DbSet<MailConfig> MailConfigs { get; }
    DbSet<EmailLog> EmailLogs { get; }

    // Admin
    DbSet<AuthConfig> AuthConfigs { get; }

    // Hr Module
    DbSet<Employee> Employees { get; }
    DbSet<Department> Departments { get; }
    DbSet<SalaryHistory> SalaryHistories { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<EmployeeAddressHistory> EmployeeAddressHistories { get; }
    DbSet<EmployeeContactHistory> EmployeeContactHistories { get; }
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<LeaveBalance> LeaveBalances { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<WorkTimeEntry> WorkTimeEntries { get; }
    DbSet<PerformanceReview> PerformanceReviews { get; }
    DbSet<FeedbackEntry> FeedbackEntries { get; }
    DbSet<TravelExpenseReport> TravelExpenseReports { get; }
    DbSet<TravelExpenseItem> TravelExpenseItems { get; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; }
    DbSet<DataAccessLog> DataAccessLogs { get; }
    DbSet<DeletionRequest> DeletionRequests { get; }
    DbSet<PublicHoliday> PublicHolidays { get; }

    // Identity
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
