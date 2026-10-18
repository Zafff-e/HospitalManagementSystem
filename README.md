# 🏥 Bangladesh Hospital Management System (HMS)

<div align="center">

![HMS Banner](https://img.shields.io/badge/HMS-Hospital%20Management%20System-0EA5E9?style=for-the-badge&logo=hospital&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-22-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![React](https://img.shields.io/badge/React-19-61DAFB?style=for-the-badge&logo=react&logoColor=black)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-29-2496ED?style=for-the-badge&logo=docker&logoColor=white)

**A comprehensive, cloud-based & on-premise hospital management platform built for Bangladesh healthcare.**

[Live Demo](#) · [Documentation](docs/) · [API Reference](docs/API.md) · [Contributing](#contributing)

</div>

---

## ✨ Key Features

| Feature | Description |
|---------|-------------|
| 🏥 **Patient Management** | OPD/IPD/Emergency with MRN-based tracking |
| 📋 **EHR/EMR** | Complete electronic health records with prescriptions |
| 🔬 **Laboratory Integration** | Sample tracking, result entry, verification workflow |
| 💊 **Pharmacy & Inventory** | FEFO-based dispensing, batch tracking, expiry alerts |
| 💰 **Smart Billing** | VAT-compliant invoicing with bKash/Nagad/cash/card |
| 🌐 **Bilingual Interface** | Full Bangla (বাংলা) & English support |
| ☁️ **Hybrid Deployment** | Cloud-based SaaS + Local on-premise edge servers |
| 📱 **SMS & Mobile** | Appointment reminders, OTP verification |
| 👥 **HR & Payroll** | Staff management, duty rosters, BMDC registration |
| 🔒 **Security & Compliance** | Role-based access, audit logging, NID encryption |

## 🏗️ Architecture

```
                    ┌──────────────────────┐
  Staff browsers ──▶│ Angular Staff Portal │──┐
                    └──────────────────────┘  │   HTTPS + JWT
                    ┌──────────────────────┐  ├──────────────▶ ┌──────────────────────────┐
  Clinicians ─────▶ │ React Clinical App   │──┘                │ ASP.NET Core Web API     │
  + Patients        └──────────────────────┘                   │  Controllers (MVC-style) │
                                                               │  Application services    │
                                                               │  Domain + EF Core        │
                                                               │  SignalR hubs            │
                                                               └──────────┬───────────────┘
                                                                          │ EF Core
                                                               ┌──────────▼───────────────┐
                                                               │ SQL Server               │
                                                               └──────────────────────────┘
```

## 🚀 Quick Start

```bash
# Clone the repository
git clone https://github.com/Zafff-e/HospitalManagementSystem.git
cd HospitalManagementSystem

# Start everything with Docker
docker compose up -d

# Access the applications:
# Staff Portal:    http://localhost:4200
# Clinical App:    http://localhost:5173
# API Docs:        http://localhost:5000/docs
```

### Default Demo Credentials

| Role | Email | Password |
|------|-------|----------|
| Super Admin | admin@hms-demo.bd | Set in .env |
| Doctor | doctor@hms-demo.bd | Set in .env |
| Receptionist | reception@hms-demo.bd | Set in .env |

> ⚠️ **All data is fictional.** Demo credentials must be set via environment variables.

## 📁 Project Structure

```
hms/
├─ backend/                        # .NET Solution
│  ├─ src/
│  │  ├─ Hms.Api/                  # Controllers, Program.cs, SignalR hubs
│  │  ├─ Hms.Application/          # Services, DTOs, validators, interfaces
│  │  ├─ Hms.Domain/               # Entities, value objects, enums, rules
│  │  └─ Hms.Infrastructure/       # EF Core, repos, Identity, adapters
│  └─ tests/
│     ├─ Hms.UnitTests/
│     └─ Hms.IntegrationTests/
├─ apps/
│  ├─ staff-portal/                # Angular 22 — Staff Admin
│  └─ clinical-app/                # React 19 — Clinical Workspace
├─ packages/
│  ├─ api-client/                  # Generated TypeScript API client
│  ├─ design-tokens/               # Shared design system tokens
│  └─ shared-types/                # Shared enums and constants
├─ e2e/                            # Playwright E2E tests
├─ infra/                          # Docker, Nginx, SQL init
├─ docs/                           # Documentation
└─ .github/workflows/              # CI/CD
```

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | .NET 9 LTS, ASP.NET Core Web API, EF Core, SignalR |
| **Database** | SQL Server 2022 |
| **Staff Portal** | Angular 22, Angular Material, Signals |
| **Clinical App** | React 19, Vite, TanStack Query, shadcn/ui |
| **Auth** | ASP.NET Core Identity + JWT |
| **Real-time** | SignalR (OPD queue, bed board, ER board) |
| **PDF** | QuestPDF |
| **Testing** | xUnit, Vitest, Playwright |
| **Infrastructure** | Docker, Nginx, GitHub Actions |

## 📄 Documentation

- [Architecture Decisions](docs/DECISIONS.md)
- [Version Registry](docs/VERSIONS.md)
- [Compliance Checklist](docs/COMPLIANCE_TODO.md)
- [Progress Log](docs/PROGRESS.md)
- [Operations Runbook](docs/OPERATIONS.md)

## 🇧🇩 Bangladesh-Specific Features

- **Phone**: E.164 format (+8801XXXXXXXXX) with operator validation
- **NID**: 10/13/17-digit format with encrypted storage
- **Address**: Division → District → Upazila hierarchy
- **Currency**: BDT (৳) with lakh/crore formatting
- **Payments**: bKash, Nagad, Rocket, Cash, Card
- **Tax**: Configurable VAT (default 15%)
- **Calendar**: Configurable weekends (Friday default)
- **Timezone**: Asia/Dhaka (UTC+06:00)
- **BMDC**: Doctor registration number validation

## 📝 License

This project is proprietary. See [LICENSE](LICENSE) for details.

## 👤 Author

**Zafir Hasan** — [GitHub](https://github.com/Zafff-e)

---

<div align="center">
Made with ❤️ for Bangladesh Healthcare
</div>
