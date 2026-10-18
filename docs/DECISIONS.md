# Architecture Decision Records (ADR)

## ADR-001: Technology Stack Selection
**Date:** 2026-10-18  
**Status:** Accepted

### Context
Building a comprehensive Hospital Management System for Bangladesh healthcare facilities requiring both cloud and on-premise deployment capabilities.

### Decision
- **Backend:** ASP.NET Core 9 Web API with EF Core (will upgrade to .NET 10 when GA)
- **Database:** SQL Server 2022 Developer Edition (will upgrade to 2025 when available)
- **Staff Portal:** Angular 22 (form-heavy, table-heavy back-office modules)
- **Clinical App:** React 19 + Vite (fast, interactive, real-time clinical screens)
- **Auth:** ASP.NET Core Identity + JWT (access + refresh tokens)
- **Real-time:** SignalR for OPD queue, bed board, ER board
- **Monorepo:** pnpm workspaces (JS) + .NET solution (backend)

### Rationale
Angular's structure, DI, and forms suit the staff portal's back-office needs. React with TanStack Query handles live clinical updates well. Both share a common API contract.

---

## ADR-002: Frontend Architecture Split
**Date:** 2026-10-18  
**Status:** Accepted

### Decision
- **Angular 22 — Staff Admin Portal:** Reception, billing, pharmacy stock, HR, settings, reports
- **React 19 — Clinical Workspace:** Doctor consultation, ER triage, bed board, lab worklist, OT board, patient portal

### Rationale
Neither frontend calls the database directly. Both call the same versioned API (`/api/v1/...`). Authentication is the same JWT across both.

---

## ADR-003: Fictional Data Only Policy
**Date:** 2026-10-18  
**Status:** Accepted

### Decision
All seed data, test data, and demo data MUST be fictional. Real hospitals may be referenced as workflow models but never as real records or branding.

---

## ADR-004: Payment & SMS Interfaces
**Date:** 2026-10-18  
**Status:** Accepted

### Decision
- Payment: `IPaymentProvider` interface with `CashProvider` and `MockMobilePaymentProvider` implementations
- SMS: `ISmsGateway` interface with logging stub implementation
- Real gateway integrations only after human provides sandbox credentials

---

## ADR-005: Primary Keys — UUIDv4 / ULID
**Date:** 2026-10-18  
**Status:** Accepted

### Decision
Strictly use UUIDv4 or ULID for primary keys. Never use incremental integers (SERIAL, AUTO_INCREMENT). This supports the hybrid cloud/on-premise sync architecture.

---

## ADR-006: Sync Strategy — Transactional Outbox Pattern
**Date:** 2026-10-18  
**Status:** Accepted

### Decision
Implement the Transactional Outbox Pattern. Wrap every local record insert with a matching insert into a `sync_outbox` table within a single atomic transaction.

### Conflict Resolution
- **Clinical Data:** Append-Only / Immutable logs (vitals, diagnoses, e-prescriptions)
- **Administrative Data:** Last-Write-Wins (LWW) using deterministic UTC timestamps (`updated_at`)

---

## ADR-007: Network Strategy
**Date:** 2026-10-18  
**Status:** Accepted

### Decision
- Decouple structured text data from binary objects
- Sync text data instantly
- Batch schedule DICOM/PACS image uploads to cloud storage (S3) during off-peak hours (12 AM – 6 AM)

---

## ADR-008: Single Hospital, Multi-Tenant Ready
**Date:** 2026-10-18  
**Status:** Accepted

### Decision
Default to single hospital deployment. Include `HospitalId` on every core table for future multi-tenant support. The system can be commercialized as a SaaS for diagnostic centers while offering on-premise edge deployments to large hospitals.

---

## ADR-009: Rounding Strategy
**Date:** 2026-10-18  
**Status:** Accepted

### Decision
Use "round away from zero" (standard mathematical rounding) for all billing calculations to 2 decimal places. This is the expected behavior in Bangladesh commercial contexts.
