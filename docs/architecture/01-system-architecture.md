# System Architecture Overview

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Architecture Style

**Clean Architecture (Onion Architecture)** with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION                              │
│  React SPA (Tailwind + Shadcn + Tremor)                         │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ Dashboard │ KPI Views │ Scenarios │ Documents │ Admin     │  │
│  └───────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                        API LAYER                                 │
│  .NET Core 10 REST API + SignalR Hubs                           │
│  ┌────────────┬────────────┬────────────┬────────────────────┐  │
│  │ Controllers│ SignalR    │ Middleware │ Background Services │  │
│  │ (REST)     │ Hubs       │ Pipeline   │ (IHostedService)   │  │
│  └────────────┴────────────┴────────────┴────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                     APPLICATION LAYER                            │
│  Use Cases / Application Services / CQRS Handlers               │
│  ┌────────────┬────────────┬────────────┬────────────────────┐  │
│  │ Commands   │ Queries    │ Validators │ Mappers            │  │
│  │ (Write)    │ (Read)     │ (FluentVal)│ (AutoMapper)       │  │
│  └────────────┴────────────┴────────────┴────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                       DOMAIN LAYER                               │
│  Entities, Value Objects, Domain Events, Business Rules          │
│  ┌────────────┬────────────┬────────────┬────────────────────┐  │
│  │ Entities   │ Value      │ Domain     │ Interfaces         │  │
│  │            │ Objects    │ Services   │ (Ports)            │  │
│  └────────────┴────────────┴────────────┴────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                    INFRASTRUCTURE LAYER                           │
│  Database, External APIs, File Storage, Messaging               │
│  ┌────────────┬────────────┬────────────┬────────────────────┐  │
│  │ EF Core    │ AI         │ Message    │ File Storage       │  │
│  │ (Postgres) │ Middleware │ Queue      │ (Documents)        │  │
│  └────────────┴────────────┴────────────┴────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

**Decision: Clean Architecture over N-Tier**
- Rationale: Domain logic (KPI calculation, accounting rules, tax logic) is complex. Clean Architecture keeps domain logic independent of infrastructure, making it testable and maintainable. CQRS separates read-optimized queries from write-heavy command processing.

---

## 2. C4 Model

### Level 1: System Context

```
                    ┌──────────────┐
                    │   Users      │
                    │  (Browser)   │
                    └──────┬───────┘
                           │ HTTPS
                           ▼
┌───────────┐    ┌─────────────────────┐    ┌───────────────┐
│ Billing   │───>│                     │───>│ DATEV         │
│ System    │    │   CLARITY BOARD     │    │ (Tax Advisor) │
├───────────┤    │                     │    └───────────────┘
│ CRM       │───>│  SPA + REST API +   │
├───────────┤    │  Background Workers  │    ┌───────────────┐
│ Banking   │───>│                     │───>│ AI Providers  │
│ API       │    │                     │    │ (Anthropic,   │
├───────────┤    │                     │    │  xAI, DeepL,  │
│ HR System │───>│                     │    │  ElevenLabs)  │
├───────────┤    └─────────────────────┘    └───────────────┘
│ ERP       │───>          │
├───────────┤              │
│ Marketing │───>          ▼
│ Platforms │    ┌─────────────────────┐
├───────────┤    │    PostgreSQL       │
│ GetMOSS   │───>│    Redis Cache      │
└───────────┘    └─────────────────────┘
```

### Level 2: Container Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLARITY BOARD SYSTEM                          │
│                                                                      │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │ React SPA        │  │ .NET Core 10     │  │ Background       │  │
│  │ (Frontend)       │──│ REST API         │  │ Worker Service   │  │
│  │                  │  │                  │  │                  │  │
│  │ - Tailwind CSS   │  │ - Controllers    │  │ - KPI Recalc     │  │
│  │ - Shadcn UI      │  │ - SignalR Hubs   │  │ - DATEV Export   │  │
│  │ - Tremor Charts  │  │ - Auth Middleware│  │ - Webhook Queue  │  │
│  │ - React Query    │  │ - Rate Limiting  │  │ - Scheduled Jobs │  │
│  │ - React Router   │  │ - Validation     │  │ - AI Processing  │  │
│  └──────────────────┘  └────────┬─────────┘  └────────┬─────────┘  │
│           │                      │                      │            │
│           │ WebSocket            │                      │            │
│           └──────────────────────┤                      │            │
│                                  │                      │            │
│  ┌──────────────────┐  ┌────────▼─────────┐  ┌────────▼─────────┐  │
│  │ Redis            │  │ PostgreSQL       │  │ Message Queue    │  │
│  │                  │  │                  │  │ (RabbitMQ)       │  │
│  │ - Session Cache  │  │ - Entities       │  │                  │  │
│  │ - KPI Cache      │  │ - Journal Entries│  │ - Webhook Events │  │
│  │ - Rate Limits    │  │ - KPI Snapshots  │  │ - AI Requests    │  │
│  │ - Pub/Sub        │  │ - Documents      │  │ - Notifications  │  │
│  │   (SignalR)      │  │ - Audit Logs     │  │ - Export Jobs    │  │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘  │
│                                                                      │
│  ┌──────────────────┐  ┌──────────────────┐                        │
│  │ Object Storage   │  │ AI Middleware     │                        │
│  │ (Documents)      │  │ Service          │                        │
│  │                  │  │                  │                        │
│  │ - Receipts       │  │ - Provider Router│                        │
│  │ - Invoices       │  │ - Rate Limiter   │                        │
│  │ - Reports        │  │ - Fallback Chain │                        │
│  │ - DATEV Exports  │  │ - PII Filter     │                        │
│  └──────────────────┘  └──────────────────┘                        │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 3. Technology Stack Decisions

### Backend

| Component | Technology | Version | Rationale |
|-----------|-----------|---------|-----------|
| **Runtime** | .NET 10 | 10.x | LTS, high performance, excellent ecosystem |
| **Web Framework** | ASP.NET Core 10 | 10.x | Minimal API + Controllers, built-in DI, middleware pipeline |
| **ORM** | Entity Framework Core | 10.x | PostgreSQL provider (Npgsql), migrations, LINQ |
| **Real-time** | SignalR | 10.x | WebSocket abstraction, automatic fallback, scale-out with Redis |
| **Validation** | FluentValidation | 11.x | Declarative validation rules, testable |
| **Mapping** | AutoMapper | 13.x | Object-to-object mapping (Entity → DTO) |
| **Authentication** | JWT Bearer + TOTP | Built-in + OTP lib | Stateless auth, 2FA support |
| **Background Jobs** | IHostedService + Hangfire | Built-in + 1.8.x | Simple tasks = IHostedService, scheduled = Hangfire |
| **Message Queue** | RabbitMQ (MassTransit) | 3.x (MassTransit 8.x) | Reliable messaging, dead-letter, competing consumers |
| **Logging** | Serilog | 4.x | Structured logging, multiple sinks |
| **HTTP Client** | IHttpClientFactory | Built-in | Connection pooling, Polly policies |
| **API Docs** | Swagger / OpenAPI | Swashbuckle 7.x | Auto-generated API documentation |

### Frontend

| Component | Technology | Version | Rationale |
|-----------|-----------|---------|-----------|
| **Framework** | React | 19.x | Component model, huge ecosystem, team expertise |
| **Language** | TypeScript | 5.x | Type safety, better DX, catch errors at compile time |
| **Build** | Vite | 6.x | Fast HMR, ESBuild, optimized production builds |
| **Styling** | Tailwind CSS | 4.x | Utility-first, consistent, purged in production |
| **Components** | Shadcn/ui | Latest | Composable, accessible, customizable, not a dependency |
| **Charts** | Tremor | 3.x | React + Tailwind native, 15+ chart types |
| **State (Server)** | TanStack Query (React Query) | 5.x | Server state cache, automatic refetch, optimistic updates |
| **State (Client)** | Zustand | 5.x | Lightweight, TypeScript-native, no boilerplate |
| **Routing** | React Router | 7.x | Nested routes, loaders, type-safe |
| **Forms** | React Hook Form + Zod | 7.x + 3.x | Performant forms, schema validation |
| **i18n** | react-i18next | 15.x | German + English, namespace separation |
| **Real-time** | @microsoft/signalr | 10.x | SignalR client, auto-reconnect |
| **HTTP** | Axios / Fetch + React Query | 1.x | Interceptors for auth, combined with React Query |

### Infrastructure

| Component | Technology | Rationale |
|-----------|-----------|-----------|
| **Database** | PostgreSQL 18 (local) | Mature, JSONB support, partitioning, full-text search |
| **Cache** | Redis 7 | KPI cache, session store, SignalR backplane, rate limiting |
| **Message Queue** | RabbitMQ 4 | Reliable delivery, dead-letter exchanges, management UI |
| **Object Storage** | MinIO / S3-compatible | Document storage, DATEV exports, encrypted at rest |
| **Reverse Proxy** | Nginx / Traefik | TLS termination, load balancing, rate limiting |
| **Containers** | Docker + Docker Compose | Consistent environments, easy deployment |
| **Orchestration** | Docker Compose (initial) → Kubernetes (scale) | Start simple, scale when needed |
| **CI/CD** | GitHub Actions | Integrated with repo, free tier sufficient initially |
| **Monitoring** | Prometheus + Grafana | Metrics, dashboards, alerting |
| **Log Aggregation** | Seq / ELK | Structured log search, correlation |
| **Secrets** | Docker Secrets / Azure Key Vault | No secrets in code or env files |

---

## 4. Project Structure (Monorepo)

```
clarityboard.net/
├── docs/                          # Documentation (functional + architecture)
│   ├── README.md
│   ├── 00-executive-summary.md
│   ├── ...
│   └── architecture/
│       ├── README.md
│       └── ...
│
├── src/
│   ├── backend/                   # .NET Solution
│   │   ├── ClarityBoard.sln
│   │   ├── ClarityBoard.Domain/          # Domain Layer (no dependencies)
│   │   │   ├── Entities/
│   │   │   ├── ValueObjects/
│   │   │   ├── Events/
│   │   │   ├── Interfaces/
│   │   │   ├── Services/
│   │   │   └── Exceptions/
│   │   ├── ClarityBoard.Application/     # Application Layer
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   ├── DTOs/
│   │   │   ├── Validators/
│   │   │   ├── Mappings/
│   │   │   └── Interfaces/
│   │   ├── ClarityBoard.Infrastructure/  # Infrastructure Layer
│   │   │   ├── Persistence/              # EF Core, DbContext, Migrations
│   │   │   ├── ExternalServices/         # AI Middleware, DATEV, GetMOSS
│   │   │   ├── Messaging/               # RabbitMQ, MassTransit
│   │   │   ├── Storage/                 # Document storage
│   │   │   ├── Caching/                 # Redis
│   │   │   └── Identity/               # JWT, 2FA
│   │   ├── ClarityBoard.API/            # API Layer (entry point)
│   │   │   ├── Controllers/
│   │   │   ├── Hubs/                    # SignalR
│   │   │   ├── Middleware/
│   │   │   ├── Filters/
│   │   │   ├── BackgroundServices/
│   │   │   └── Program.cs
│   │   └── ClarityBoard.Tests/          # Test Projects
│   │       ├── Unit/
│   │       ├── Integration/
│   │       └── Architecture/
│   │
│   └── frontend/                  # React Application
│       ├── package.json
│       ├── tsconfig.json
│       ├── vite.config.ts
│       ├── tailwind.config.ts
│       ├── src/
│       │   ├── app/               # App shell, routing, providers
│       │   ├── components/        # Shared components
│       │   │   ├── ui/            # Shadcn components
│       │   │   ├── charts/        # Tremor chart wrappers
│       │   │   ├── kpi/           # KPI cards and displays
│       │   │   └── layout/        # Header, sidebar, footer
│       │   ├── features/          # Feature modules
│       │   │   ├── dashboard/
│       │   │   ├── financial/
│       │   │   ├── sales/
│       │   │   ├── marketing/
│       │   │   ├── hr/
│       │   │   ├── cashflow/
│       │   │   ├── scenarios/
│       │   │   ├── documents/
│       │   │   ├── budget/
│       │   │   ├── datev/
│       │   │   ├── assets/        # Fixed asset management
│       │   │   ├── regulatory/
│       │   │   └── admin/
│       │   ├── hooks/             # Custom React hooks
│       │   ├── lib/               # Utilities, API client, auth
│       │   ├── stores/            # Zustand stores
│       │   ├── types/             # TypeScript type definitions
│       │   └── i18n/              # Translations (de, en)
│       └── tests/
│
├── infrastructure/                # Deployment configs
│   ├── docker/
│   │   ├── Dockerfile.api
│   │   ├── Dockerfile.frontend
│   │   ├── Dockerfile.worker
│   │   └── nginx.conf
│   ├── docker-compose.yml         # Local development
│   ├── docker-compose.prod.yml    # Production
│   └── scripts/
│       ├── init-db.sh
│       ├── seed-data.sh
│       └── backup.sh
│
├── .github/
│   └── workflows/
│       ├── ci.yml                 # Build + Test
│       ├── cd-staging.yml         # Deploy to staging
│       └── cd-production.yml      # Deploy to production
│
├── .gitignore
├── CLAUDE.md
└── README.md
```

---

## 5. Communication Patterns

### Synchronous (Request-Response)

| Pattern | Use Case |
|---------|----------|
| **REST API** | CRUD operations, KPI queries, configuration, DATEV export triggers |
| **SignalR** | Real-time KPI updates, alert notifications, document processing status |

### Asynchronous (Event-Driven)

| Pattern | Use Case |
|---------|----------|
| **Message Queue (RabbitMQ)** | Webhook event processing, AI request processing, export generation |
| **Domain Events** | KPI recalculation trigger, alert evaluation, audit logging |
| **Background Jobs** | Daily KPI snapshots, scheduled pulls, depreciation posting, cleanup |

### Data Flow for Webhook Event

```
1. External System → POST /api/v1/webhooks/{type}/{id}
2. WebhookController validates auth + signature
3. Event published to RabbitMQ "webhook.events" exchange
4. HTTP 202 Accepted returned to caller
5. WebhookProcessor consumer picks up event
6. Transforms → Books (journal entries) → Calculates (KPIs)
7. Domain event "KpiUpdated" raised
8. KpiUpdated handler:
   a. Updates Redis cache
   b. Publishes to SignalR "kpi-updates" hub
   c. Evaluates alert thresholds
   d. Logs to audit trail
9. Connected dashboards receive update via WebSocket
```

---

## 6. Cross-Cutting Concerns

| Concern | Implementation |
|---------|---------------|
| **Logging** | Serilog with structured JSON, correlation IDs, enriched with user/entity context |
| **Error Handling** | Global exception middleware, ProblemDetails RFC 7807, no stack traces in production |
| **Validation** | FluentValidation at Application layer, model validation at API layer |
| **Mapping** | AutoMapper profiles per domain module |
| **Caching** | Redis with entity-scoped keys, invalidation via domain events |
| **Rate Limiting** | ASP.NET Core built-in rate limiter, per-user and per-endpoint |
| **Health Checks** | ASP.NET Core health checks: DB, Redis, RabbitMQ, AI providers |
| **Correlation** | Correlation ID header propagated through all layers and external calls |
| **Multitenancy** | Entity-scoped data access via EF Core global query filters |
| **Audit** | Domain event-driven audit logging, append-only, hash-chained |

---

## Document Navigation

- Next: [Backend Architecture](./02-backend-architecture.md)
- [Back to Index](./README.md)
