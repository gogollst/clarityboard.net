# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Dev Commands

### Frontend (`src/frontend/`)
```bash
npm run dev          # Vite dev server on http://localhost:3000
npm run build        # tsc -b && vite build (production)
npm run lint         # ESLint
```

### Backend (`src/backend/`)
```bash
dotnet restore       # Restore NuGet packages
dotnet build         # Build all projects
dotnet test          # Run all tests
```
Note: `dotnet` CLI is NOT installed locally on the production server. Migrations are created manually as SQL + Designer files.

### Docker (development)
```bash
docker compose up -d                    # Start Redis, RabbitMQ, MinIO (PostgreSQL runs on host)
docker compose -f docker-compose.prod.yml --env-file .env.production up -d  # Production
```

### Deploy
```bash
./deploy.sh   # Requires sudo. Bumps patch version, builds frontend, deploys to /var/www/clarityboard/frontend, rebuilds API Docker image, restarts containers, reloads Nginx
```

## Architecture

### Clean Architecture (4 layers)
```
API → Application → Domain ← Infrastructure
```
- **Domain** (`ClarityBoard.Domain`): Entities, value objects, domain events. Zero external dependencies.
- **Application** (`ClarityBoard.Application`): CQRS commands/queries via MediatR, FluentValidation, DTOs.
- **Infrastructure** (`ClarityBoard.Infrastructure`): EF Core, PostgreSQL, Redis, RabbitMQ, MinIO, SMTP, JWT.
- **API** (`ClarityBoard.API`): ASP.NET Core controllers, SignalR hubs, middleware.

### Backend Patterns (CQRS + MediatR)
- Features organized by domain: `Features/{Domain}/Commands/`, `Features/{Domain}/Queries/`
- Each command/query is a single file containing the request class, handler, and validator
- Naming: `{Action}Command`, `{Action}CommandHandler`, `{Action}CommandValidator`
- EF Core migrations auto-applied via `MigrateAsync()` on API startup
- Entity configurations in `Persistence/Configurations/{Domain}/`

### Frontend Stack
- **React 19** + TypeScript 5, **Vite**, **Tailwind CSS 4**, **shadcn/ui**
- **TanStack Query** for server state, **Zustand** for client state
- **React Router 7** with lazy-loaded routes
- **i18next** with 3 languages (de, en, ru) in `src/locales/{lang}/{namespace}.json`
- **React Hook Form + Zod** for form validation

### Frontend Conventions
- Route components use `export function Component()` (React Router lazy convention)
- Feature pages in `src/features/{domain}/` with pattern: `*List.tsx`, `*Create.tsx`, `*Detail.tsx`
- Hooks in `src/hooks/use{Domain}.ts` — wrap TanStack Query with toast notifications
- Query key factories in `src/lib/queryKeys.ts`
- TypeScript types in `src/types/{domain}.ts` — must match backend DTOs exactly
- API client in `src/lib/api.ts` — Axios with JWT auto-refresh interceptor
- Path alias: `@/` maps to `src/`

### Auth & Multi-Tenancy
- JWT with refresh token rotation, "Remember Me" (localStorage vs sessionStorage)
- Entity switching via `POST /api/auth/switch-entity` — issues new JWT with `entity_id` claim
- Token-based user onboarding (invitation emails, 72h expiry)
- Hooks use `useEntity()` for the currently selected `entityId`; most API queries are entity-scoped

### Infrastructure
- **PostgreSQL 18** on host (not Docker), multiple schemas (public, accounting, hr, kpi, etc.)
- **Redis**: caching/sessions. **RabbitMQ**: async messaging (MassTransit). **MinIO**: S3 file storage
- **SignalR**: real-time notifications (accounting updates, KPI alerts)
- Production: Nginx reverse proxy → `app.clarityboard.net` (frontend) + `api.clarityboard.net` (API)

## Key Directories
```
src/backend/src/ClarityBoard.API/Controllers/     # REST endpoints
src/backend/src/ClarityBoard.Application/Features/ # CQRS commands & queries
src/backend/src/ClarityBoard.Domain/Entities/      # Domain models
src/backend/src/ClarityBoard.Infrastructure/Persistence/ # EF Core, migrations
src/frontend/src/features/                         # Feature pages
src/frontend/src/hooks/                            # TanStack Query hooks
src/frontend/src/types/                            # TypeScript interfaces (mirror backend DTOs)
src/frontend/src/locales/                          # i18n translations (de, en, ru)
src/frontend/src/components/ui/                    # shadcn/ui components
src/frontend/src/stores/                           # Zustand stores (auth, entity, ui)
docs/architecture/                                 # Technical architecture docs
```

## CI Pipeline
GitHub Actions (`.github/workflows/ci.yml`):
- Backend: `dotnet restore` → `dotnet build` → `dotnet test` (with PostgreSQL 18 service)
- Frontend: `npm ci` → `npm run lint` → `npm run build`
- Docker build on main branch only
