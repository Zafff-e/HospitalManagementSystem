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

---

## Phase 3 — Core Reference Data & Patient Registration (M1)
**Date:** 2026-10-18  
**Status:** ✅ Complete (Tagged `phase-03-complete`)

### What was done:
- Added ADR-010 to [DECISIONS.md](docs/DECISIONS.md) documenting Bangladesh National Portal geo-hierarchy dataset.
- Seeded Bangladesh geo-hierarchy (all 8 divisions, 12 major districts, and 12 upazilas) in [DbSeeder.cs](backend/src/Hms.Infrastructure/Persistence/DbSeeder.cs).
- Seeded 200 realistic fictional patients in [DbSeeder.cs](backend/src/Hms.Infrastructure/Persistence/DbSeeder.cs) with valid BD phone numbers, NID last 4 digits, bilingual names, and blood groups.
- Implemented sequence-backed [MrnGenerator.cs](backend/src/Hms.Infrastructure/Services/MrnGenerator.cs) (`YYYY-NNNNNN` format per hospital).
- Implemented [PatientService.cs](backend/src/Hms.Infrastructure/Services/PatientService.cs) with:
  - Required bilingual name validation (at least one of EN or BN).
  - Date of birth validation (cannot be in the future).
  - Primary phone E.164 normalization (+8801...).
  - National ID length validation (10, 13, or 17 digits) and AES encryption at rest.
  - Soft duplicate warning algorithm (matching primary phone OR NID last-4 + DOB).
  - Paged search filtering by query string, MRN, phone, and NID last 4.
- Implemented [PatientsController.cs](backend/src/Hms.Api/Controllers/PatientsController.cs) and [GeoController.cs](backend/src/Hms.Api/Controllers/GeoController.cs).
- Built Angular 22 standalone Staff Portal ([apps/staff-portal](apps/staff-portal)):
  - Design system with vibrant medical teal/ocean palette inspired by Avela Clinic Management Software.
  - Bilingual switcher supporting English and বাংলা.
  - Debounced live search with type-ahead.
  - 4-Step Patient Registration Wizard (Demographics, Cascaded Geo-Hierarchy selects, Identity/NID, Review & Duplicate Warning).
  - Full Patient Profile view with MRN barcode visualization and clinical tabs.
  - Containerized with multi-stage [Dockerfile](apps/staff-portal/Dockerfile) and [nginx.conf](apps/staff-portal/nginx.conf).
- Implemented automated Playwright E2E test in [e2e/tests/patient-registration.spec.ts](e2e/tests/patient-registration.spec.ts) covering the full 4-step wizard registration.

### Tests:
- Checkpoint 3 Requirement 1 (Unit tests for Phone and NationalId pass with all edge cases): ✅ Passed (23 unit tests)
- Checkpoint 3 Requirement 2 (API integration tests for create, search, and duplicate warning pass): ✅ Passed (12 integration tests)
- Checkpoint 3 Requirement 3 (Playwright test "register a patient" passes): ✅ Passed (1 e2e test)
- Total test suite: 36 passed, 0 failed.

### Known gaps: None

---

## Phase 4 — Appointments & OPD Queue (M2)
**Date:** 2026-10-18  
**Status:** ✅ Complete (Tagged `phase-04-complete`)

### What was done:
- Added `Doctor`, `DoctorSchedule`, `Appointment`, and `OpdToken` domain entities and configurations.
- Implemented `IAppointmentService` and [AppointmentService](backend/src/Hms.Infrastructure/Services/AppointmentService.cs):
  - Doctor slot generation based on weekly schedule, appointment duration, and holiday exclusions.
  - Concurrency slot locking using in-memory slot mutex ensuring race conditions return 409 Conflict.
  - Asia/Dhaka daily token reset and sequential OPD token generation per doctor per day.
  - Real-time SignalR queue broadcasts via [OpdHub](backend/src/Hms.Infrastructure/Hubs/OpdHub.cs) at `/hubs/opd`.
- Implemented [AppointmentsController](backend/src/Hms.Api/Controllers/AppointmentsController.cs):
  - `GET /api/v1/doctors?departmentId=`
  - `GET /api/v1/doctors/{id}/slots?date=`
  - `GET /api/v1/appointments`
  - `POST /api/v1/appointments`
  - `POST /api/v1/appointments/{id}/check-in`
  - `GET /api/v1/opd/queue?doctorId=&date=`
- Updated [DbSeeder](backend/src/Hms.Infrastructure/Persistence/DbSeeder.cs) with Departments, Doctors (Dr. Anisur Rahman, Dr. Farhana Chowdhury), and schedules across all working days.
- Built Angular 22 Appointment Booking screen (`AppointmentBookingComponent`) with doctor selection, interactive date picker, time slot cards, and booking submission.
- Built Angular 22 OPD Reception Check-in & Live Queue screen (`OpdQueueComponent`) with SignalR real-time auto-updating queue cards and check-in modal.
- Integrated routes into Angular Router in `app.routes.ts` and verified clean production build.
- Added comprehensive integration tests in [AppointmentsAndOpdTests.cs](backend/tests/Hms.IntegrationTests/AppointmentsAndOpdTests.cs).

### Tests:
- Checkpoint 4 Requirement 1 (Concurrent bookings for the same slot -> 1 succeeds [201 Created], 1 gets 409 Conflict): ✅ Passed
- Checkpoint 4 Requirement 2 (Check-in assigns sequential token and updates doctor's queue via SignalR): ✅ Passed
- Total test suite: 38 passed, 0 failed (23 Unit Tests + 15 Integration Tests).

### Known gaps: None


