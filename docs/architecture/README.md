# Clarity Board - Architecture Documentation

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## Architecture Documents

| # | Document | Description |
|---|----------|-------------|
| 01 | [System Architecture Overview](./01-system-architecture.md) | High-level architecture, C4 diagrams, component map, technology decisions |
| 02 | [Backend Architecture](./02-backend-architecture.md) | .NET Core 10, Clean Architecture layers, API design, middleware pipeline |
| 03 | [Frontend Architecture](./03-frontend-architecture.md) | React, state management, component hierarchy, real-time, routing |
| 04 | [Database Architecture](./04-database-architecture.md) | PostgreSQL schema strategy, partitioning, indexing, connection pooling |
| 05 | [AI Middleware Architecture](./05-ai-middleware.md) | Provider abstraction, routing, fallback, cost control, PII filtering |
| 06 | [Data Ingestion & Event Processing](./06-data-ingestion.md) | Webhook pipeline, message queue, idempotency, dead-letter, pull adapters |
| 07 | [Authentication & Authorization](./07-auth-architecture.md) | JWT lifecycle, 2FA, RBAC enforcement, session management |
| 08 | [Real-Time Communication](./08-realtime.md) | SignalR hubs, WebSocket, event broadcasting, client subscription |
| 09 | [Caching & Performance](./09-caching-performance.md) | Redis strategy, cache invalidation, CDN, query optimization |
| 10 | [Deployment & Infrastructure](./10-deployment.md) | Docker, orchestration, CI/CD, environments, blue-green, monitoring |
| 11 | [Security Architecture](./11-security-architecture.md) | Defense in depth, threat model, encryption, secrets management |
| 12 | [Integration Architecture](./12-integration-architecture.md) | External system patterns, DATEV export, GetMOSS, banking APIs |

---

## Architecture Decision Records

Key decisions are documented inline within each document under **"Decision"** sections with rationale.

---

## Cross-Reference

- Functional Concept: [docs/README.md](../README.md)
- Database Schema (detailed): Created after architecture approval

---

*Last updated: 2026-02-27*
