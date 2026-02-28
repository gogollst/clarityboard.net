# Backend Architecture

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Solution Structure

```
ClarityBoard.sln
│
├── src/
│   ├── ClarityBoard.Domain/              # Inner layer - no dependencies
│   ├── ClarityBoard.Application/         # Use cases, CQRS handlers
│   ├── ClarityBoard.Infrastructure/      # External concerns (DB, AI, MQ)
│   └── ClarityBoard.API/                 # Entry point (Controllers, Hubs)
│
└── tests/
    ├── ClarityBoard.Domain.Tests/
    ├── ClarityBoard.Application.Tests/
    ├── ClarityBoard.Infrastructure.Tests/
    ├── ClarityBoard.API.Tests/
    └── ClarityBoard.Architecture.Tests/   # ArchUnit-style dependency tests
```

**Dependency Rule**: Dependencies point inward only.
```
API → Application → Domain
API → Infrastructure → Application → Domain
Infrastructure → Domain (implements interfaces)
```

---

## 2. Domain Layer

### Entities (Core Business Objects)

```
Domain/
├── Entities/
│   ├── Accounting/
│   │   ├── Account.cs                  # Chart of accounts entry
│   │   ├── JournalEntry.cs             # Double-entry booking
│   │   ├── JournalEntryLine.cs         # Individual debit/credit line
│   │   ├── FiscalPeriod.cs             # Open/closed periods
│   │   └── RecurringEntry.cs           # Recurring revenue/costs
│   ├── KPI/
│   │   ├── KpiDefinition.cs            # KPI metadata + formula
│   │   ├── KpiSnapshot.cs              # Daily calculated value
│   │   ├── KpiAlert.cs                 # Alert threshold definition
│   │   └── KpiAlertEvent.cs            # Triggered alert instance
│   ├── Entity/
│   │   ├── LegalEntity.cs              # Company/subsidiary
│   │   ├── EntityRelationship.cs       # Ownership, consolidation scope
│   │   ├── TaxUnit.cs                  # Organschaft configuration
│   │   └── IntercompanyRule.cs         # IC elimination rules
│   ├── CashFlow/
│   │   ├── CashFlowEntry.cs            # Individual cash movement
│   │   ├── CashFlowForecast.cs         # Projected cash position
│   │   └── LiquidityAlert.cs           # Cash threshold alerts
│   ├── Scenario/
│   │   ├── Scenario.cs                 # Scenario definition
│   │   ├── ScenarioParameter.cs        # Modified parameter
│   │   ├── ScenarioResult.cs           # Calculated outcome
│   │   └── SimulationRun.cs            # Monte Carlo run
│   ├── Document/
│   │   ├── Document.cs                 # Uploaded file metadata
│   │   ├── DocumentField.cs            # Extracted field value
│   │   ├── BookingSuggestion.cs        # AI-generated suggestion
│   │   └── RecurringPattern.cs         # Detected recurring document
│   ├── Budget/
│   │   ├── Budget.cs                   # Annual budget
│   │   ├── BudgetLine.cs              # Individual line item
│   │   └── BudgetRevision.cs          # Mid-year change request
│   ├── Asset/
│   │   ├── FixedAsset.cs              # Asset register entry
│   │   ├── DepreciationSchedule.cs    # AfA plan
│   │   └── AssetDisposal.cs          # Asset sale/scrapping
│   ├── Integration/
│   │   ├── WebhookConfig.cs           # Source configuration
│   │   ├── WebhookEvent.cs            # Received event (immutable)
│   │   ├── MappingRule.cs             # Source → internal mapping
│   │   └── PullAdapterConfig.cs       # Scheduled pull configuration
│   └── Identity/
│       ├── User.cs                     # User account
│       ├── Role.cs                     # Role definition
│       └── Permission.cs              # Granular permission
│
├── ValueObjects/
│   ├── Money.cs                        # Amount + Currency
│   ├── AccountNumber.cs               # Validated HGB account number
│   ├── TaxId.cs                        # Steuernummer / USt-IdNr
│   ├── DateRange.cs                   # Period with start/end
│   ├── Percentage.cs                  # Validated 0-100 or ratio
│   └── VatRate.cs                     # German VAT rate enum + logic
│
├── Events/
│   ├── JournalEntryCreated.cs
│   ├── KpiUpdated.cs
│   ├── AlertTriggered.cs
│   ├── DocumentProcessed.cs
│   ├── ScenarioCalculated.cs
│   ├── DATEVExportGenerated.cs
│   └── WebhookEventProcessed.cs
│
├── Interfaces/                          # Ports (implemented by Infrastructure)
│   ├── IUnitOfWork.cs
│   ├── IAccountingRepository.cs
│   ├── IKpiRepository.cs
│   ├── IEntityRepository.cs
│   ├── IScenarioRepository.cs
│   ├── IDocumentRepository.cs
│   ├── IBudgetRepository.cs
│   ├── IAssetRepository.cs
│   ├── IAiService.cs
│   ├── ITranslationService.cs
│   ├── ITextToSpeechService.cs
│   ├── IDocumentStorage.cs
│   ├── IMessageBus.cs
│   ├── ICacheService.cs
│   └── IExchangeRateService.cs
│
├── Services/                            # Domain services (stateless logic)
│   ├── KpiCalculationService.cs        # KPI formula evaluation engine
│   ├── ConsolidationService.cs         # IC elimination, minority interest
│   ├── VatDeterminationService.cs      # VAT rate logic per transaction
│   ├── DepreciationService.cs          # AfA calculation (linear, declining)
│   ├── WorkingCapitalService.cs        # DSO/DIO/DPO/CCC calculations
│   └── TaxCalculationService.cs       # KSt, GewSt, Soli calculation
│
└── Exceptions/
    ├── DomainException.cs              # Base domain exception
    ├── UnbalancedEntryException.cs     # Debit != Credit
    ├── ClosedPeriodException.cs        # Booking in closed period
    ├── InvalidAccountException.cs      # Account not in chart
    └── EntityAccessDeniedException.cs  # Cross-entity violation
```

---

## 3. Application Layer (CQRS)

### Command Examples

```csharp
// Commands (Write Operations)
public record CreateJournalEntryCommand(
    Guid EntityId,
    DateOnly EntryDate,
    string Description,
    List<JournalLineDto> Lines,
    Guid? DocumentId
) : IRequest<Guid>;

public record ProcessWebhookEventCommand(
    string SourceType,
    string SourceId,
    string EventType,
    JsonDocument Payload,
    string IdempotencyKey
) : IRequest<WebhookProcessingResult>;

public record GenerateDatevExportCommand(
    Guid EntityId,
    int Year,
    int Month
) : IRequest<DatevExportResult>;

public record ProcessDocumentCommand(
    Guid DocumentId,
    Stream FileStream,
    string FileName,
    string ContentType
) : IRequest<DocumentProcessingResult>;

public record CreateScenarioCommand(
    Guid EntityId,
    string Name,
    ScenarioType Type,
    List<ScenarioParameterDto> Parameters,
    int ProjectionMonths
) : IRequest<Guid>;
```

### Query Examples

```csharp
// Queries (Read Operations)
public record GetKpiDashboardQuery(
    Guid EntityId,
    string Role,
    DateOnly? Date
) : IRequest<DashboardDto>;

public record GetKpiHistoryQuery(
    Guid EntityId,
    string KpiId,
    DateOnly StartDate,
    DateOnly EndDate,
    AggregationLevel Aggregation
) : IRequest<KpiHistoryDto>;

public record GetCashFlowForecastQuery(
    Guid EntityId,
    int WeeksAhead
) : IRequest<CashFlowForecastDto>;

public record GetPlanVsActualQuery(
    Guid EntityId,
    Guid DepartmentId,
    int Year,
    int Month
) : IRequest<PlanVsActualDto>;

public record GetConsolidatedBalanceSheetQuery(
    Guid ParentEntityId,
    DateOnly AsOfDate
) : IRequest<ConsolidatedBalanceSheetDto>;
```

### Pipeline Behaviors (MediatR)

```
Request → LoggingBehavior
        → ValidationBehavior (FluentValidation)
        → AuthorizationBehavior (RBAC check)
        → EntityAccessBehavior (entity scope check)
        → TransactionBehavior (UoW for commands)
        → Handler
```

---

## 4. API Layer

### Controller Organization

```
Controllers/
├── v1/
│   ├── AuthController.cs              # Login, refresh, 2FA, logout
│   ├── KpiController.cs               # KPI queries, history, drill-down
│   ├── DashboardController.cs         # Role-based dashboard data
│   ├── AccountingController.cs        # Journal entries, trial balance
│   ├── CashFlowController.cs         # Cash flow, forecast, WC
│   ├── ScenarioController.cs         # CRUD, simulation, comparison
│   ├── DocumentController.cs         # Upload, processing, archive
│   ├── BudgetController.cs           # Budget CRUD, plan vs actual
│   ├── AssetController.cs            # Fixed assets, depreciation
│   ├── DatevController.cs            # Export triggers, download
│   ├── EntityController.cs           # Entity management
│   ├── WebhookController.cs          # Webhook endpoints + config
│   ├── ReportController.cs           # Report generation
│   ├── NlqController.cs              # Natural language queries
│   └── AdminController.cs            # User management, system config
```

### API Versioning Strategy

```
/api/v1/kpis/{entityId}/financial
/api/v1/kpis/{entityId}/sales
/api/v1/kpis/{entityId}/{kpiId}/history?from=2026-01-01&to=2026-02-28

Versioning: URL path prefix (/api/v1/, /api/v2/)
Rationale: Most explicit, easy to route, easy to deprecate
```

### Middleware Pipeline Order

```csharp
app.UseExceptionHandler();           // 1. Global error handling
app.UseHsts();                       // 2. HTTP Strict Transport Security
app.UseHttpsRedirection();           // 3. Force HTTPS
app.UseCors();                       // 4. CORS headers
app.UseRateLimiter();                // 5. Rate limiting
app.UseAuthentication();             // 6. JWT validation
app.UseAuthorization();              // 7. RBAC check
app.UseAuditLogging();               // 8. Audit trail (custom)
app.UseEntityScopeValidation();      // 9. Entity access (custom)
app.MapControllers();                // 10. Route to controllers
app.MapHub<KpiHub>("/hubs/kpi");     // 11. SignalR hub
app.MapHub<AlertHub>("/hubs/alerts");// 12. Alert hub
app.MapHealthChecks("/health");      // 13. Health checks
```

---

## 5. Background Services

| Service | Type | Schedule | Purpose |
|---------|------|----------|---------|
| **WebhookProcessorService** | MassTransit Consumer | Continuous | Process queued webhook events |
| **KpiRecalculationService** | IHostedService | Daily 02:00 UTC | Full daily KPI snapshot recalculation |
| **DepreciationService** | IHostedService | Monthly 1st, 01:00 | Post monthly depreciation entries |
| **RecurringEntryService** | IHostedService | Monthly 1st, 00:00 | Generate recurring revenue/cost entries |
| **PrepaidAllocationService** | IHostedService | Monthly 1st, 00:30 | Allocate prepaid expenses to current month |
| **ExchangeRateService** | IHostedService | Daily 16:00 CET | Fetch ECB reference rates |
| **AlertEvaluationService** | MassTransit Consumer | On KpiUpdated event | Check thresholds, send notifications |
| **DocumentProcessingService** | MassTransit Consumer | Continuous | OCR + AI field extraction |
| **DatevExportService** | MassTransit Consumer | On demand (queued) | Generate DATEV export files |
| **DataQualityService** | IHostedService | Daily 03:00 UTC | Run data quality checks |
| **CleanupService** | IHostedService | Daily 04:00 UTC | Purge expired tokens, temp files |
| **ScheduledPullService** | Hangfire | Per adapter config | Fetch data from pull-based sources |

---

## 6. Error Handling Strategy

### API Error Response Format (RFC 7807)

```json
{
  "type": "https://clarityboard.net/errors/unbalanced-entry",
  "title": "Journal entry is not balanced",
  "status": 422,
  "detail": "Total debits (10,115.00 EUR) do not equal total credits (10,015.00 EUR). Difference: 100.00 EUR",
  "instance": "/api/v1/accounting/journal-entries",
  "traceId": "00-abc123-def456-01",
  "errors": {
    "lines": ["Debit sum must equal credit sum"]
  }
}
```

### Exception Hierarchy

```
Exception
├── DomainException (400-422)
│   ├── UnbalancedEntryException
│   ├── ClosedPeriodException
│   ├── InvalidAccountException
│   ├── DuplicateEventException
│   └── InsufficientPermissionException
├── NotFoundException (404)
│   ├── EntityNotFoundException
│   ├── KpiNotFoundException
│   └── DocumentNotFoundException
├── ConflictException (409)
│   ├── ConcurrencyException
│   └── DuplicateResourceException
└── InfrastructureException (500-503)
    ├── AiProviderException
    ├── DatabaseConnectionException
    └── MessageQueueException
```

---

## 7. Testing Strategy

| Layer | Test Type | Framework | Coverage Target |
|-------|----------|-----------|----------------|
| **Domain** | Unit tests | xUnit + FluentAssertions | > 90% (critical business logic) |
| **Application** | Unit tests (handlers) | xUnit + Moq + FluentAssertions | > 80% |
| **Infrastructure** | Integration tests | xUnit + Testcontainers (Postgres) | > 70% |
| **API** | Integration tests | WebApplicationFactory | > 70% |
| **Architecture** | Dependency tests | ArchUnitNET | 100% (rules) |
| **E2E** | End-to-end | Playwright | Critical paths |

### Architecture Tests Example

```csharp
[Fact]
public void Domain_Should_Not_Depend_On_Infrastructure()
{
    var result = Types.InAssembly(typeof(LegalEntity).Assembly)
        .ShouldNot()
        .HaveDependencyOn("ClarityBoard.Infrastructure")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}

[Fact]
public void Domain_Should_Not_Depend_On_Application()
{
    var result = Types.InAssembly(typeof(LegalEntity).Assembly)
        .ShouldNot()
        .HaveDependencyOn("ClarityBoard.Application")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}
```

---

## Document Navigation

- Previous: [System Architecture Overview](./01-system-architecture.md)
- Next: [Frontend Architecture](./03-frontend-architecture.md)
- [Back to Index](./README.md)
