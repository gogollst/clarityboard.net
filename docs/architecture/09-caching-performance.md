# Caching & Performance

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Caching Architecture

```
Client (Browser)
    │
    ├── React Query Cache (in-memory)       ← Stale-while-revalidate
    ├── Service Worker Cache (static assets) ← Cache-first strategy
    │
    ▼
CDN / Reverse Proxy
    │
    ├── Static assets (JS, CSS, images)      ← Immutable, long cache
    ├── API response cache (selected endpoints) ← Short TTL
    │
    ▼
API Server
    │
    ├── Response Cache Middleware             ← HTTP cache headers
    ├── In-Memory Cache (IMemoryCache)       ← Hot path, single instance
    │
    ▼
Redis 7
    │
    ├── KPI current values                   ← 30s TTL, invalidate on update
    ├── Dashboard aggregations               ← 60s TTL
    ├── User sessions / permissions          ← 15 min TTL (matches JWT)
    ├── Rate limiting counters               ← Sliding window
    ├── AI response cache                    ← 1h TTL (NLQ, translations)
    ├── Webhook idempotency bloom filter     ← 24h TTL
    │
    ▼
PostgreSQL
    │
    ├── Materialized Views                   ← Refreshed on mutation
    ├── Covering Indexes                     ← Index-only scans
    └── Table Partitions                     ← Partition pruning
```

---

## 2. Redis Cache Strategy

### Cache Key Naming Convention

```
clarityboard:{domain}:{entity_id}:{resource}:{identifier}

Examples:
  clarityboard:kpi:uuid:latest:financial.gross_margin
  clarityboard:dashboard:uuid:finance
  clarityboard:session:user-uuid
  clarityboard:ratelimit:webhook:source-id
  clarityboard:ai:nlq:hash-of-question
  clarityboard:fx:EUR:USD:2026-02-27
```

### Cache Policies by Domain

| Cache Key Pattern | TTL | Invalidation | Size Estimate |
|-------------------|-----|-------------|---------------|
| `kpi:*:latest:*` | 30 seconds | On KpiUpdated event | ~200 bytes/KPI |
| `dashboard:*:*` | 60 seconds | On any KPI update for entity | ~5 KB/dashboard |
| `session:*` | 15 minutes | On logout / token refresh | ~1 KB/session |
| `fx:*` | 24 hours | Daily ECB update | ~100 bytes/pair |
| `ai:nlq:*` | 1 hour | None (natural expiry) | ~2 KB/response |
| `ai:translation:*` | 24 hours | None | ~500 bytes/text |
| `account-chart:*` | 1 hour | On account mutation | ~10 KB/entity |
| `entity-config:*` | 5 minutes | On entity config change | ~2 KB/entity |

### Cache Implementation

```csharp
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(key);

        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<T>(cached!);
        }

        var value = await factory();
        var serialized = JsonSerializer.Serialize(value);

        await db.StringSetAsync(key, serialized, expiry ?? TimeSpan.FromMinutes(5));

        return value;
    }

    public async Task InvalidatePatternAsync(string pattern)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern).ToArray();

        if (keys.Length > 0)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(keys);
            _logger.LogDebug("Invalidated {Count} cache keys matching {Pattern}", keys.Length, pattern);
        }
    }
}
```

### Cache Invalidation on Domain Events

```csharp
public class CacheInvalidationHandler :
    INotificationHandler<KpiUpdatedEvent>,
    INotificationHandler<JournalEntryCreatedEvent>,
    INotificationHandler<EntityConfigChangedEvent>
{
    public async Task Handle(KpiUpdatedEvent evt, CancellationToken ct)
    {
        // Invalidate specific KPI cache
        await _cache.InvalidatePatternAsync(
            $"clarityboard:kpi:{evt.EntityId}:latest:{evt.KpiId}");

        // Invalidate dashboard caches for this entity
        await _cache.InvalidatePatternAsync(
            $"clarityboard:dashboard:{evt.EntityId}:*");
    }

    public async Task Handle(JournalEntryCreatedEvent evt, CancellationToken ct)
    {
        // Invalidate trial balance and KPI caches
        await _cache.InvalidatePatternAsync(
            $"clarityboard:kpi:{evt.EntityId}:*");
        await _cache.InvalidatePatternAsync(
            $"clarityboard:dashboard:{evt.EntityId}:*");
    }

    public async Task Handle(EntityConfigChangedEvent evt, CancellationToken ct)
    {
        await _cache.InvalidatePatternAsync(
            $"clarityboard:entity-config:{evt.EntityId}");
        await _cache.InvalidatePatternAsync(
            $"clarityboard:account-chart:{evt.EntityId}");
    }
}
```

---

## 3. HTTP Cache Headers

### API Response Caching

```csharp
// Static reference data (chart of accounts, KPI definitions)
[HttpGet("kpi-definitions")]
[ResponseCache(Duration = 300, VaryByHeader = "Authorization")]
public async Task<ActionResult<List<KpiDefinitionDto>>> GetKpiDefinitions()
{
    // ...
}

// Dynamic data (no HTTP cache, rely on React Query)
[HttpGet("{entityId}/dashboard")]
[ResponseCache(NoStore = true)]
public async Task<ActionResult<DashboardDto>> GetDashboard(Guid entityId)
{
    // ...
}
```

### Static Asset Caching (Vite)

```typescript
// vite.config.ts - Content-hash filenames for immutable caching
export default defineConfig({
    build: {
        rollupOptions: {
            output: {
                // Chunk files with content hash
                chunkFileNames: 'assets/[name]-[hash].js',
                entryFileNames: 'assets/[name]-[hash].js',
                assetFileNames: 'assets/[name]-[hash].[ext]',
            }
        }
    }
});
```

### CDN / Reverse Proxy Headers

```nginx
# Nginx configuration
location /assets/ {
    expires 1y;
    add_header Cache-Control "public, immutable";
    add_header Vary "Accept-Encoding";
}

location /api/ {
    add_header Cache-Control "no-store";
    proxy_pass http://api-backend;
}

location /locales/ {
    expires 1h;
    add_header Cache-Control "public";
}
```

---

## 4. React Query Cache Strategy

```typescript
// queryClient configuration
const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 30_000,           // Data fresh for 30s
            gcTime: 5 * 60_000,          // Garbage collect after 5 min
            refetchOnWindowFocus: true,   // Refetch when tab becomes active
            refetchOnReconnect: true,     // Refetch on network recovery
            retry: 3,                     // Retry failed requests 3x
            retryDelay: (attempt) => Math.min(1000 * 2 ** attempt, 30000),
        },
    },
});

// Per-query overrides
const queryConfigs = {
    // Dashboard: relatively fresh, auto-refetch
    dashboard: {
        staleTime: 30_000,
        refetchInterval: 60_000,
    },
    // KPI history: changes slowly
    kpiHistory: {
        staleTime: 5 * 60_000,
        refetchInterval: false,
    },
    // Reference data: rarely changes
    kpiDefinitions: {
        staleTime: 30 * 60_000,
        gcTime: 60 * 60_000,
    },
    // Exchange rates: daily
    exchangeRates: {
        staleTime: 60 * 60_000,
        refetchInterval: false,
    },
};
```

---

## 5. Database Performance

### Query Optimization Patterns

```sql
-- 1. Covering index: avoid table lookups for dashboard queries
CREATE INDEX idx_kpi_snapshots_covering
    ON kpi.kpi_snapshots (entity_id, kpi_id, snapshot_date DESC)
    INCLUDE (value, previous_value, change_pct, target_value);

-- 2. Partial index: only index what's queried
CREATE INDEX idx_journal_entries_unposted
    ON accounting.journal_entries (entity_id, created_at DESC)
    WHERE status = 'draft';

-- 3. Expression index: for computed filters
CREATE INDEX idx_journal_lines_net_amount
    ON accounting.journal_entry_lines ((debit_amount - credit_amount));

-- 4. BRIN index: for time-series data (compact, good for sequential inserts)
CREATE INDEX idx_webhook_events_received_brin
    ON integration.webhook_events USING BRIN (received_at);
```

### Connection Pool Monitoring

```sql
-- Monitor active connections
SELECT
    state,
    COUNT(*) as count,
    AVG(EXTRACT(EPOCH FROM (now() - state_change))) as avg_duration_sec
FROM pg_stat_activity
WHERE datname = 'clarityboard'
GROUP BY state;

-- Identify slow queries
SELECT
    pid,
    now() - pg_stat_activity.query_start AS duration,
    query,
    state
FROM pg_stat_activity
WHERE (now() - pg_stat_activity.query_start) > interval '5 seconds'
  AND state = 'active';
```

### Materialized View Refresh Strategy

| View | Refresh Trigger | Concurrency |
|------|----------------|-------------|
| `mv_trial_balance` | After journal entry posted | `CONCURRENTLY` |
| `mv_latest_kpis` | After KPI recalculation | `CONCURRENTLY` |
| `mv_monthly_cost` (AI) | Every 15 minutes (cron) | `CONCURRENTLY` |
| `mv_cash_position` | After cash flow entry | `CONCURRENTLY` |

---

## 6. API Performance Targets

| Endpoint Category | P50 | P95 | P99 | Strategy |
|------------------|-----|-----|-----|----------|
| Dashboard data | 50ms | 100ms | 200ms | Redis cache + materialized view |
| KPI history | 80ms | 150ms | 300ms | Partitioned table + covering index |
| Journal entry list | 60ms | 120ms | 250ms | Pagination + composite index |
| DATEV export | 2s | 5s | 10s | Background job, stream response |
| NLQ query | 1s | 3s | 5s | AI cache + pre-computed context |
| Document upload | 100ms | 200ms | 500ms | Async processing (202 Accepted) |
| Scenario simulation | 5s | 15s | 30s | Background job with progress |

### Performance Testing

```
Tool: k6 (load testing)
Scenarios:
  - Smoke: 5 VUs, 1 minute
  - Load: 50 VUs, 10 minutes
  - Stress: 200 VUs, 5 minutes
  - Soak: 50 VUs, 2 hours

Metrics collected:
  - http_req_duration (P50, P95, P99)
  - http_req_failed (error rate)
  - iterations (throughput)
  - vus_max (concurrent users)
```

---

## 7. Compression

| Content Type | Compression | Savings |
|-------------|-------------|---------|
| JSON API responses | Brotli (preferred), Gzip fallback | 70-85% |
| Static JS/CSS | Brotli pre-compressed at build | 80-90% |
| Images | WebP format, responsive srcset | 30-50% vs JPEG |
| WebSocket messages | No compression (low latency priority) | N/A |

```csharp
// ASP.NET Core compression
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/csv" });
});

services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;  // Brotli level 1 for dynamic
});
```

---

## Document Navigation

- Previous: [Real-Time Communication](./08-realtime.md)
- Next: [Deployment & Infrastructure](./10-deployment.md)
- [Back to Index](./README.md)
