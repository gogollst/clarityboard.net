# Non-Functional Requirements

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Performance

| Metric | Target | Measurement |
|--------|--------|-------------|
| Dashboard initial load | < 2 seconds | Time from navigation to first meaningful paint |
| Dashboard subsequent load | < 500ms | Cached data, WebSocket-connected |
| Webhook processing latency | < 5 seconds | Event received to KPI update on dashboard |
| KPI recalculation (single) | < 1 second | Any single KPI with dependencies |
| Full daily recalculation | < 30 minutes | All KPIs for 100k transactions |
| DATEV export generation | < 5 minutes | 50k posting records |
| Monte Carlo simulation (10k runs) | < 30 seconds | Full 12-month projection |
| API response time (P50) | < 100ms | Standard read endpoints |
| API response time (P95) | < 200ms | Standard read endpoints |
| API response time (P99) | < 500ms | Including complex queries |
| Document OCR processing | < 10 seconds | Single page receipt/invoice |
| Natural language query | < 5 seconds | Including AI processing |
| Report generation (PDF) | < 15 seconds | Standard monthly report |
| WebSocket message latency | < 100ms | Server to connected client |

### Performance Optimization Strategies

| Strategy | Implementation |
|----------|---------------|
| **Database Indexing** | Composite indexes on (entity_id, date) for all time-series tables |
| **Query Optimization** | Materialized views for frequently accessed aggregations |
| **Caching (Redis)** | KPI current values, user sessions, frequently accessed data |
| **Connection Pooling** | PgBouncer for PostgreSQL connection management |
| **CDN** | Static assets (JS, CSS, images) served via CDN |
| **Code Splitting** | React lazy loading, route-based code splitting |
| **Database Partitioning** | Table partitioning by entity_id and date range |
| **Background Processing** | Heavy calculations offloaded to background worker queue |
| **WebSocket** | Real-time updates without polling |
| **Compression** | Gzip/Brotli for API responses, image optimization |

---

## 2. Scalability

### Capacity Targets

| Dimension | Target |
|-----------|--------|
| Entities per installation | 100+ |
| Users per installation | 500+ concurrent |
| Transactions per entity per year | 1,000,000+ |
| KPI definitions per entity | 200+ |
| Historical snapshots | 3+ years daily resolution |
| Webhook events per minute | 10,000+ |
| Concurrent WebSocket connections | 1,000+ |
| Document uploads per day | 1,000+ |
| Scenarios per entity | 50+ |

### Scaling Strategy

| Component | Scaling Approach |
|-----------|-----------------|
| **API Servers** | Horizontal scaling behind load balancer (stateless) |
| **WebSocket Servers** | Horizontal with sticky sessions or Redis pub/sub |
| **Background Workers** | Horizontal scaling with competing consumers |
| **PostgreSQL** | Vertical (larger instance) + read replicas + partitioning |
| **Redis** | Redis Cluster for high availability |
| **Message Queue** | RabbitMQ / Azure Service Bus with multiple consumers |
| **Document Storage** | Object storage (S3/Azure Blob) with CDN |
| **AI Middleware** | Independent scaling, circuit breaker per provider |

### Database Partitioning Strategy

```
Partitioning Scheme:

kpi_daily_snapshots:
  Partition by: RANGE (snapshot_date)
  Partition size: Monthly
  Retention: Auto-drop partitions beyond retention policy

journal_entries:
  Partition by: RANGE (entry_date)
  Partition size: Monthly
  Sub-partition by: LIST (entity_id) if > 20 entities

webhook_events:
  Partition by: RANGE (received_at)
  Partition size: Weekly (high volume)
  Retention: Archive after 90 days, delete after 1 year

audit_log:
  Partition by: RANGE (timestamp)
  Partition size: Monthly
  Retention: 10 years (GoBD compliance)
```

---

## 3. Availability

| Metric | Target |
|--------|--------|
| **Uptime SLA** | 99.9% (max 8.76 hours downtime per year) |
| **Recovery Point Objective (RPO)** | 1 hour (max data loss) |
| **Recovery Time Objective (RTO)** | 4 hours (max time to restore service) |
| **Backup Frequency** | Every 6 hours (database), continuous (transaction log) |
| **Backup Testing** | Monthly restore verification |
| **Disaster Recovery** | Cross-region failover capability |
| **Maintenance Window** | Scheduled: Sunday 02:00-06:00 UTC (zero-downtime deployments preferred) |

### High Availability Architecture

```
                    Load Balancer (Active-Active)
                    ┌─────────┬─────────┐
                    │         │         │
                API Server 1  API Server 2  API Server 3
                    │         │         │
                    └────┬────┴────┬────┘
                         │         │
                 ┌───────▼───┐ ┌───▼────────┐
                 │ PostgreSQL │ │   Redis     │
                 │ Primary    │ │   Primary   │
                 │     │      │ │     │       │
                 │ Replica 1  │ │ Replica 1   │
                 │ Replica 2  │ │ Replica 2   │
                 └────────────┘ └─────────────┘
                         │
                 ┌───────▼───────┐
                 │ Message Queue  │
                 │ (Clustered)    │
                 └───────┬───────┘
                         │
                 ┌───────▼───────┐
                 │ Background     │
                 │ Workers (3+)   │
                 └───────────────┘
```

### Failover Strategy

| Component | Failover | Detection |
|-----------|----------|-----------|
| API Server | Auto-remove unhealthy instance from LB pool | Health check every 10s |
| Database | Automatic failover to replica (< 30s) | Streaming replication monitoring |
| Redis | Sentinel-based automatic failover | Sentinel consensus (3 nodes) |
| Message Queue | Mirrored queues across nodes | Node health monitoring |
| AI Provider | Automatic fallback to secondary provider | Response timeout + error rate |

---

## 4. Compatibility

### Browser Support

| Browser | Minimum Version | Notes |
|---------|----------------|-------|
| Chrome | 90+ | Primary development target |
| Firefox | 90+ | Full support |
| Safari | 15+ | Full support |
| Edge | 90+ | Chromium-based, full support |
| Mobile Safari (iOS) | 15+ | Responsive layout |
| Chrome Android | 90+ | Responsive layout |

### Screen Sizes

| Category | Width | Support Level |
|----------|-------|--------------|
| Mobile | 375-767px | Core KPIs, alerts, document upload |
| Tablet | 768-1023px | Full features, adapted layout |
| Desktop | 1024-1439px | Full features, standard layout |
| Desktop XL | 1440px+ | Full features, optimal layout |

Minimum supported: 1024px width for full functionality.

### Accessibility

| Standard | Level | Coverage |
|----------|-------|---------|
| WCAG | 2.1 Level AA | All public-facing features |
| Section 508 | Compliant | Government customer requirement |
| Keyboard Navigation | Full | All interactive elements |
| Screen Reader | Full | ARIA labels, live regions |

---

## 5. Localization

| Feature | Support |
|---------|---------|
| **UI Language** | German (primary), English |
| **Number Format** | German (1.234,56) and English (1,234.56) |
| **Date Format** | DD.MM.YYYY (German), YYYY-MM-DD (ISO), MM/DD/YYYY (US) |
| **Currency** | EUR (primary), configurable display currency |
| **Timezone** | CET/CEST (default), configurable per user |
| **Document Translation** | Via DeepL integration for non-German/English documents |
| **Report Language** | Selectable per report (German or English) |

---

## 6. Monitoring & Observability

### Application Monitoring

| Metric | Tool | Alert Threshold |
|--------|------|----------------|
| API response time | APM (e.g., Application Insights) | P95 > 500ms |
| Error rate | APM | > 1% of requests |
| CPU utilization | Infrastructure monitoring | > 80% sustained |
| Memory utilization | Infrastructure monitoring | > 85% |
| Disk usage | Infrastructure monitoring | > 80% |
| Database connections | PgBouncer metrics | > 80% pool |
| Queue depth | Message queue metrics | > 1000 pending |
| WebSocket connections | Custom metric | > 80% capacity |
| AI provider latency | Middleware metrics | > 10 seconds |
| AI provider error rate | Middleware metrics | > 5% |

### Health Check Endpoints

```
GET /health           → Overall system health (200 OK / 503 Unavailable)
GET /health/db        → Database connectivity
GET /health/redis     → Redis connectivity
GET /health/queue     → Message queue connectivity
GET /health/ai        → AI provider availability
GET /health/detailed  → Full component status (Admin only)
```

---

## 7. Deployment

### Strategy

| Aspect | Approach |
|--------|---------|
| **Deployment Method** | Docker containers orchestrated via Kubernetes or Docker Compose |
| **Release Strategy** | Blue-green deployment (zero-downtime) |
| **Rollback** | One-click rollback to previous version |
| **Database Migrations** | Forward-only, backward-compatible (EF Core migrations) |
| **Configuration** | Environment variables, secrets via Vault |
| **CI/CD** | GitHub Actions or Azure DevOps |
| **Environments** | Development, Staging, Production |
| **Infrastructure** | Cloud-hosted (Azure / AWS / Hetzner) |

---

## Document Navigation

- Previous: [UI/UX Principles](./19-ui-ux-principles.md)
- Next: [Glossary & Appendices](./21-glossary-appendices.md)
- [Back to Index](./README.md)
