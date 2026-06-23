# LightenUp

**LightenUp** is an ASP.NET Core 10 MVC platform for workplace mental-health support. It connects **patients**, **psychologists**, **HR managers**, and **platform administrators** in one system—with mood tracking, counseling schedules, worksheets, company analytics, subscriptions (Duitku), and a separate admin console.

## Features by role

| Role | Highlights |
|------|------------|
| **Patient** | Dashboard, mood wizard, journal check-in, statistics, tasks/worksheets, counseling schedule (`Jadwal`), profile, premium subscription |
| **Psychologist** | Patient roster, scheduling, worksheets, session history, payroll summary |
| **HR** | Employee management, schedule requests, worksheets, reports, division statistics, company subscription |
| **Admin** | Dashboard KPIs, account approvals (Psychologist/HR), user & company management, assignments, payroll, settings |

Admin runs on a **separate host/port** in development so `/Admin/*` is not exposed on the public customer site.

## Tech stack

- ASP.NET Core 10 MVC (Areas: Patient, Hr, Psychologist, Admin, AdminAuth)
- ASP.NET Core Identity + SQL Server (LocalDB by default)
- Entity Framework Core migrations
- Duitku payment gateway (mock mode for local dev)
- Static UI: custom CSS (`site.css`, role-specific styles)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server LocalDB (Windows) or update `ConnectionStrings:DefaultConnection` in `appsettings.json`
- [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

## Quick start

```powershell
git clone <your-repo-url>
cd WebsiteLightenUp

dotnet restore
dotnet build
dotnet ef database update
dotnet run --launch-profile https
```

| Site | URL |
|------|-----|
| Customer (Patient / Psychologist / HR) | https://localhost:7040 |
| Admin console | https://localhost:7041/AdminAuth/Login |

Host routing is configured in `appsettings.Development.json` (`Site:PatientHost`, `Site:AdminHost`). On the customer host, `/Admin/*` returns **404**.

### Admin password (required for first run)

Admin is **not** hardcoded in source. Set it via User Secrets before running:

```powershell
dotnet user-secrets set "Seed:AdminPassword" "YourSecurePassword123!"
```

Optional override: `Seed:AdminEmail` (default `admin@lightenup.com` in `appsettings.json`).

## Dummy / seed data

On first startup, `Data/DummyDataSeed.cs` seeds a compact demo dataset (one company, a few users per role, sample data for every feature table). **Default password for all demo users:** `Password123!`

Seed is **idempotent** — it only runs when company **PT Sasindo** does not exist yet.

### Demo accounts

| Role | Email | Password | Login URL |
|------|-------|----------|-----------|
| Admin | `admin@lightenup.com` | *(User Secrets)* | https://localhost:7041/AdminAuth/Login |
| HR | `hr@sasindo.com` | `Password123!` | https://localhost:7040/Account/Login |
| Psychologist | `dr.dina@lightenup.com` | `Password123!` | https://localhost:7040/Account/Login |
| Psychologist | `dr.andi@lightenup.com` | `Password123!` | https://localhost:7040/Account/Login |
| Psychologist (pending approval) | `dr.baru@lightenup.com` | `Password123!` | https://localhost:7040/Account/Login |
| Patient B2B | `kaffah@sasindo.com` | `Password123!` | https://localhost:7040/Account/Login |
| Patient B2B | `siti@sasindo.com` | `Password123!` | https://localhost:7040/Account/Login |
| Patient B2B | `budi@sasindo.com` | `Password123!` | https://localhost:7040/Account/Login |
| Patient B2C | `riza@gmail.com` | `Password123!` | https://localhost:7040/Account/Login |
| Patient B2C | `maya@gmail.com` | `Password123!` | https://localhost:7040/Account/Login |

### Referral codes (PT Sasindo)

| Division | Code |
|----------|------|
| Pusat | `SAS-PUSAT` |
| IT & Engineering | `SAS-IT-01` |
| Human Resources | `SAS-HR-01` |

### Which account to use per feature

| Feature | Suggested account |
|---------|-------------------|
| Active assignment + completed session | `kaffah@sasindo.com` |
| Pending cancellation (HR → admin) | `siti@sasindo.com` |
| Pending psychologist approval (patient request) | `budi@sasindo.com` |
| B2C subscription + schedule | `riza@gmail.com` |
| Pending admin assignment | `maya@gmail.com` |
| HR approvals, employees, reports | `hr@sasindo.com` |
| Admin approval queue (new psychologist) | `dr.baru@lightenup.com` |
| Payroll / payouts | `dr.dina@lightenup.com` |

> Change all passwords before any public deployment.

## Database errors & reset

If you see migration errors, seed failures, stale schema, or login/data that does not match this README (e.g. old `Perusahaan A` accounts), **drop the database first**, then recreate it:

```powershell
# Stop the running app first (Ctrl+C), then:
dotnet ef database drop -f
dotnet ef database update
dotnet run --launch-profile https
```

This removes `LightenUpDB` on LocalDB and applies all migrations from scratch. On the next startup, admin + demo seed run again automatically.

**Common symptoms that need a DB reset:**

- `dotnet ef database update` fails with column/table already exists or missing
- Seed log errors or partial data after pulling new migrations
- Demo emails from an older seed still in the database

Alternative (SQL Server Management Studio / Azure Data Studio): delete database `LightenUpDB`, then run `dotnet ef database update`.

## Configuration

### Database

Default connection (LocalDB):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=LightenUpDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### Duitku (payments)

Development uses **mock payments** when `Duitku:UseMock` is `true` (default in `appsettings.json` / Development).

For sandbox:

```powershell
dotnet user-secrets set "Duitku:MerchantCode" "YOUR_CODE"
dotnet user-secrets set "Duitku:ApiKey" "YOUR_KEY"
dotnet user-secrets set "Duitku:UseMock" "false"
```

Never commit real API keys; use User Secrets or environment variables.

### Migrations

```powershell
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

Migration files live under `Migrations/` and should be committed so others can apply the same schema.

## Project layout

```
Areas/
  Admin/          # Admin console (Dashboard, Users, Companies, Approvals, Payroll, …)
  AdminAuth/      # Admin login/logout
  Patient/        # Patient portal
  Hr/             # HR portal
  Psychologist/   # Psychologist portal
Controllers/      # Shared controllers (Account, …)
Data/             # DbContext, DummyDataSeed, DbInitializer
Models/           # Domain + view models
Services/         # Duitku, subscriptions, uploads, email
Views/            # Shared views (Account, onboarding)
wwwroot/          # CSS, JS, images (uploads/ is gitignored)
```

## What is not in Git

See `.gitignore`. In particular:

- `bin/`, `obj/`, `.vs/` — build and IDE output
- `wwwroot/uploads/` — user-uploaded files
- Local secrets (`.env`, `appsettings.*.local.json`, User Secrets)
- IDE/AI folders (`.cursor/`, `.claude/`, `CLAUDE.md`)
- Capstone/design drafts (`Form_Capstone/`, `Form4-*/`, `_figma/`, `*.docx.pdf`)
- Local scratch logs (`debug-*.log`, `temp_*.css`)

## License

Academic / capstone project — add your license or institution notice here if required.
