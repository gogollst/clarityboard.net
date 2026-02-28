# Deployment & Infrastructure

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Infrastructure Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Load Balancer                             │
│                   (Nginx / Traefik + TLS)                       │
└────────────────────────┬────────────────────────────────────────┘
                         │
          ┌──────────────┼──────────────┐
          │              │              │
     ┌────▼────┐   ┌────▼────┐   ┌────▼────┐
     │ API     │   │ API     │   │ API     │
     │ Node 1  │   │ Node 2  │   │ Node N  │
     └────┬────┘   └────┬────┘   └────┬────┘
          │              │              │
          └──────────────┼──────────────┘
                         │
          ┌──────────────┼──────────────┐
          │              │              │
     ┌────▼────┐   ┌────▼────┐   ┌────▼────┐
     │PostgreSQL│   │  Redis  │   │RabbitMQ │
     │ Primary  │   │ Primary │   │ Cluster │
     │ + Replica│   │ + Replica│  │         │
     └─────────┘   └─────────┘   └─────────┘
          │
     ┌────▼────┐
     │ MinIO   │   Object storage (documents, exports, backups)
     │ (S3)    │
     └─────────┘
```

---

## 2. Docker Compose (Development + Staging)

### Service Definitions

```yaml
# docker-compose.yml
version: '3.9'

services:
  # ===================
  # Application Services
  # ===================
  api:
    build:
      context: ./src/backend
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Default=Host=localhost;Port=5432;Database=clarityboard;Username=app;Password=${DB_PASSWORD}
      - Redis__ConnectionString=redis:6379
      - RabbitMQ__Host=rabbitmq
      - MinIO__Endpoint=minio:9000
    depends_on:
      redis:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  frontend:
    build:
      context: ./src/frontend
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    depends_on:
      - api
    restart: unless-stopped

  worker:
    build:
      context: ./src/backend
      dockerfile: Dockerfile.worker
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Default=Host=localhost;Port=5432;Database=clarityboard;Username=app;Password=${DB_PASSWORD}
      - Redis__ConnectionString=redis:6379
      - RabbitMQ__Host=rabbitmq
    depends_on:
      - redis
      - rabbitmq
    restart: unless-stopped

  # ===================
  # Infrastructure
  # ===================
  # NOTE: PostgreSQL 18 runs locally (not in Docker).
  # See infrastructure/postgres/init.sql for initial schema setup.
  # PgBouncer is optional and can be installed locally if needed.

  redis:
    image: redis:7-alpine
    command: redis-server --requirepass ${REDIS_PASSWORD} --maxmemory 512mb --maxmemory-policy allkeys-lru
    volumes:
      - redis_data:/data
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "${REDIS_PASSWORD}", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  rabbitmq:
    image: rabbitmq:4-management-alpine
    environment:
      RABBITMQ_DEFAULT_USER: clarityboard
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD}
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "check_running"]
      interval: 30s
      timeout: 10s
      retries: 3

  minio:
    image: minio/minio:latest
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: ${MINIO_ACCESS_KEY}
      MINIO_ROOT_PASSWORD: ${MINIO_SECRET_KEY}
    volumes:
      - minio_data:/data
    ports:
      - "9000:9000"
      - "9001:9001"

volumes:
  redis_data:
  rabbitmq_data:
  minio_data:
```

---

## 3. Dockerfile Patterns

### API Dockerfile (Multi-Stage)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.sln .
COPY src/ClarityBoard.Domain/*.csproj src/ClarityBoard.Domain/
COPY src/ClarityBoard.Application/*.csproj src/ClarityBoard.Application/
COPY src/ClarityBoard.Infrastructure/*.csproj src/ClarityBoard.Infrastructure/
COPY src/ClarityBoard.API/*.csproj src/ClarityBoard.API/
RUN dotnet restore

COPY . .
RUN dotnet publish src/ClarityBoard.API -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ClarityBoard.API.dll"]
```

### Frontend Dockerfile

```dockerfile
# Build stage
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# Runtime stage
FROM nginx:1.27-alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
```

---

## 4. CI/CD Pipeline

### GitHub Actions Workflow

```yaml
# .github/workflows/ci.yml
name: CI/CD Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  # ============
  # Backend
  # ============
  backend-test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:18
        env:
          POSTGRES_DB: clarityboard_test
          POSTGRES_USER: test
          POSTGRES_PASSWORD: test
        ports: ['5432:5432']
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal
        env:
          ConnectionStrings__Test: Host=localhost;Database=clarityboard_test;Username=test;Password=test

  backend-build:
    needs: backend-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/build-push-action@v5
        with:
          context: .
          file: src/ClarityBoard.API/Dockerfile
          push: ${{ github.ref == 'refs/heads/main' }}
          tags: ghcr.io/${{ github.repository }}/api:${{ github.sha }}

  # ============
  # Frontend
  # ============
  frontend-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: src/frontend/package-lock.json
      - run: npm ci
        working-directory: src/frontend
      - run: npm run lint
        working-directory: src/frontend
      - run: npm run test
        working-directory: src/frontend
      - run: npm run build
        working-directory: src/frontend

  frontend-build:
    needs: frontend-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/build-push-action@v5
        with:
          context: src/frontend
          push: ${{ github.ref == 'refs/heads/main' }}
          tags: ghcr.io/${{ github.repository }}/frontend:${{ github.sha }}

  # ============
  # E2E Tests
  # ============
  e2e-test:
    needs: [backend-build, frontend-build]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v4
      - run: docker compose -f docker-compose.test.yml up -d
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
      - run: npx playwright install --with-deps
      - run: npx playwright test
      - run: docker compose -f docker-compose.test.yml down

  # ============
  # Deploy
  # ============
  deploy-staging:
    needs: e2e-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    environment: staging
    steps:
      - uses: actions/checkout@v4
      - run: |
          # Deploy to staging (Docker Compose on VPS or Kubernetes)
          echo "Deploying to staging..."

  deploy-production:
    needs: deploy-staging
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    environment: production  # Requires manual approval
    steps:
      - uses: actions/checkout@v4
      - run: |
          echo "Deploying to production..."
```

---

## 5. Environment Strategy

| Environment | Purpose | Infrastructure | Data |
|------------|---------|---------------|------|
| **Local** | Developer workstation | Docker Compose | Seeded test data |
| **CI** | Automated testing | GitHub Actions + Docker | In-memory / Testcontainers |
| **Staging** | Pre-production validation | Docker Compose on VPS | Anonymized production copy |
| **Production** | Live system | Docker Compose → Kubernetes | Real data |

### Environment Variables

```
# Common across environments
ASPNETCORE_ENVIRONMENT=Production|Staging|Development
ConnectionStrings__Default=Host=...;Database=...;Username=...;Password=...
Redis__ConnectionString=...
RabbitMQ__Host=...
RabbitMQ__Password=...

# Secrets (from vault / GitHub secrets)
JWT__SigningKey=...
AI__Anthropic__ApiKey=...
AI__XAI__ApiKey=...
AI__DeepL__ApiKey=...
AI__ElevenLabs__ApiKey=...
MinIO__AccessKey=...
MinIO__SecretKey=...
SMTP__Password=...
```

---

## 6. Deployment Strategy

### Blue-Green Deployment

```
Load Balancer
      │
      ├──── Blue (current production) ──── v1.2.0
      │
      └──── Green (new version) ──── v1.3.0 (staged, tested)

Steps:
  1. Deploy new version to Green environment
  2. Run smoke tests against Green
  3. Run database migrations (backward-compatible)
  4. Switch load balancer to Green
  5. Monitor for 15 minutes
  6. If issues: rollback by switching back to Blue
  7. If healthy: decommission Blue, it becomes next Green
```

### Database Migration Strategy

```
1. Additive-only migrations in deployment:
   - New columns with defaults
   - New tables
   - New indexes (CONCURRENTLY)

2. Breaking changes split across 2 deployments:
   - Deploy 1: Add new column, dual-write
   - Deploy 2: Remove old column, stop dual-write

3. Migration execution:
   - Migrations run as part of startup (EF Core)
   - Idempotent migrations (IF NOT EXISTS)
   - Timeout: 5 minutes per migration
```

---

## 7. Health Checks

```csharp
// Registered health checks
services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql")
    .AddRedis(redisConnectionString, name: "redis")
    .AddRabbitMQ(rabbitConnectionString, name: "rabbitmq")
    .AddCheck<SignalRHealthCheck>("signalr")
    .AddCheck<AiProviderHealthCheck>("ai-providers")
    .AddCheck<MinIOHealthCheck>("minio");

// Endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false  // Always returns healthy if app is running
});
```

### Health Check Responses

```json
{
  "status": "Healthy",
  "results": {
    "postgresql": { "status": "Healthy", "duration": "00:00:00.012" },
    "redis": { "status": "Healthy", "duration": "00:00:00.003" },
    "rabbitmq": { "status": "Healthy", "duration": "00:00:00.008" },
    "signalr": { "status": "Healthy", "data": { "connections": 42 } },
    "ai-providers": { "status": "Degraded", "data": { "anthropic": "Healthy", "deepl": "Unhealthy" } },
    "minio": { "status": "Healthy", "duration": "00:00:00.015" }
  }
}
```

---

## 8. Monitoring & Observability

### Stack

| Concern | Tool | Purpose |
|---------|------|---------|
| **Metrics** | Prometheus + Grafana | System metrics, business metrics, dashboards |
| **Logging** | Serilog → Seq / Loki | Structured logging, search, correlation |
| **Tracing** | OpenTelemetry → Jaeger | Distributed request tracing |
| **Alerting** | Grafana Alerting | Threshold-based alerts, PagerDuty integration |
| **Uptime** | Uptime Kuma | External availability monitoring |

### Key Dashboards

| Dashboard | Metrics |
|-----------|---------|
| **API Performance** | Request rate, latency P50/P95/P99, error rate, by endpoint |
| **Database** | Query time, connection pool, cache hit rate, table sizes |
| **Message Queue** | Queue depth, consumer lag, processing rate, DLQ size |
| **AI Middleware** | Provider latency, cost, error rate, fallback rate |
| **Business** | Active users, webhook events/min, KPI calculations/day |
| **Infrastructure** | CPU, memory, disk, network per container |

### Structured Logging

```csharp
// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ClarityBoard.API")
    .Enrich.WithProperty("Environment", environment)
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.Seq("http://seq:5341")
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .CreateLogger();

// Correlation ID middleware
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

    context.Response.Headers["X-Correlation-ID"] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});
```

---

## Document Navigation

- Previous: [Caching & Performance](./09-caching-performance.md)
- Next: [Security Architecture](./11-security-architecture.md)
- [Back to Index](./README.md)
