# Progress Log

## Phase 0 — Environment & Decisions
**Date:** 2026-10-18  
**Status:** ✅ Complete (Tagged `phase-0-complete`)

### What was done:
- Created GitHub repository: https://github.com/Zafff-e/HospitalManagementSystem
- Verified tool versions and recorded in `docs/VERSIONS.md`
- Created monorepo folder structure per guide Part 3.3
- Created `docs/DECISIONS.md` with 9 ADRs
- Created `docs/COMPLIANCE_TODO.md`
- Created `docs/VERSIONS.md`
- Created `.gitignore` covering all project technologies
- Created `README.md` with architecture overview and quick start
- Set up Docker Compose infrastructure
- Set up Nginx reverse proxy configuration
- Created design tokens package with shared design system
- Created `pnpm-workspace.yaml`

### Tests: N/A (infrastructure phase)
### Known gaps: None

---

## Phase 1 — Infrastructure & Backend Skeleton
**Date:** 2026-10-18  
**Status:** ✅ Complete (Tagged `phase-01-complete`)

### What was done:
- Configured [HmsDbContext](backend/src/Hms.Infrastructure/Persistence/HmsDbContext.cs) with entity sets, global soft-delete query filter, row-version concurrency tokens, and automatic UTC auditing in `SaveChangesAsync`.
- Generated Initial EF Core migration: `InitialCoreSchema`.
- Configured [Program.cs](backend/src/Hms.Api/Program.cs):
  - Serilog structured logging with console and daily rolling file sinks.
  - Health checks: `/health/live` (HTTP server check) and `/health/ready` (EF Core database check).
  - Modern API documentation: Microsoft.AspNetCore.OpenApi (`/openapi/v1.json`) + Scalar API Reference at `/docs`.
  - CORS policy configured for Angular staff portal (`:4200`), React clinical app (`:5173`), and Nginx proxy (`:80`).
  - Global RFC 7807 `ProblemDetails` exception handler.
- Implemented `SystemController` providing `GET /api/v1/system/version` with system metadata and Asia/Dhaka time.
- Created multi-stage [Dockerfile](backend/Dockerfile) for backend API.
- Implemented unit tests for domain value objects (`Phone`, `NationalId`, `Money`) fixing an edge-case prefix normalization bug.
- Implemented integration tests with `WebApplicationFactory` for health checks, system version, and API documentation.

### Tests:
- Unit tests: 23 passed, 0 failed.
- Integration tests: 3 passed, 0 failed.
- Total test suite: 26 passed, 0 failed.

### Known gaps: None
