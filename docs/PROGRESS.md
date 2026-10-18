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

---

## Phase 2 — Identity, Roles, and Permissions
**Date:** 2026-10-18  
**Status:** ✅ Complete (Tagged `phase-02-complete`)

### What was done:
- Extended ASP.NET Core Identity with [ApplicationUser](backend/src/Hms.Infrastructure/Identity/IdentityModels.cs) (FullName, StaffId, BMDC registration number, DepartmentId, Designation, HospitalId).
- Configured roles (`ApplicationRole`), fine-grained permissions (`Permission`), and role-permission mappings (`RolePermission`).
- Implemented rotating refresh tokens ([RefreshToken](backend/src/Hms.Infrastructure/Identity/IdentityModels.cs)) and [IJwtTokenService](backend/src/Hms.Infrastructure/Services/JwtTokenService.cs).
- Implemented [IAuthService](backend/src/Hms.Infrastructure/Services/AuthService.cs) supporting login, rotating refresh, token revocation, and user profile retrieval.
- Implemented [IAuditService](backend/src/Hms.Infrastructure/Services/AuditService.cs) and [AuditLog](backend/src/Hms.Domain/Entities/Audit/AuditLog.cs) writing structured audit records to database.
- Implemented dynamic policy-based authorization with custom `PermissionRequirement`, `PermissionAuthorizationHandler`, `PermissionPolicyProvider`, and `[HasPermission]` attribute.
- Account Lockout configured: 5 consecutive wrong password attempts locks the account for 15 minutes.
- Implemented [AuthController](backend/src/Hms.Api/Controllers/AuthController.cs) (`/api/v1/auth/login`, `/refresh`, `/logout`, `/me`).
- Implemented [BillingController](backend/src/Hms.Api/Controllers/BillingController.cs) protected by `[HasPermission("billing.refund")]`.
- Created EF Core migration `AddIdentityAndPermissions`.
- Created [DbSeeder](backend/src/Hms.Infrastructure/Persistence/DbSeeder.cs) seeding demo hospital, 20 granular permissions, 12 hospital roles, role-permission mappings, and 4 demo accounts.
- Added comprehensive integration test suite ([AuthAndPermissionsTests](backend/tests/Hms.IntegrationTests/AuthAndPermissionsTests.cs)) proving all 4 Checkpoint 2 requirements.

### Tests:
- Checkpoint 2 Requirement 1 (Valid login returns tokens and user permissions): ✅ Passed
- Checkpoint 2 Requirement 2 (5 wrong passwords locks account): ✅ Passed
- Checkpoint 2 Requirement 3 (Receptionist 403 on billing.refund, BillingOfficer 200 OK): ✅ Passed
- Checkpoint 2 Requirement 4 (Rotating refresh token invalidates old token): ✅ Passed
- Total test suite: 30 passed, 0 failed.

### Known gaps: None
