# LightenUp

**LightenUp** is an ASP.NET Core 8 MVC platform for workplace mental-health support. It connects **patients**, **psychologists**, **HR managers**, and **platform administrators** in one system—with mood tracking, counseling schedules, worksheets, company analytics, subscriptions (Duitku), and a separate admin console.

## Features by role

| Role | Highlights |
|------|------------|
| **Patient** | Dashboard, mood wizard, journal check-in, statistics, tasks/worksheets, counseling schedule (`Jadwal`), profile, premium subscription |
| **Psychologist** | Patient roster, scheduling, worksheets, company stats, session history |
| **HR** | Employee management, schedule requests, worksheets, reports, division statistics, company subscription |
| **Admin** | Dashboard KPIs, account approvals (Psychologist/HR), user & company management, invite admins, settings |

Admin runs on a **separate host/port** in development so `/Admin/*` is not exposed on the public customer site.

## Tech stack

- ASP.NET Core 8 MVC (Areas: Patient, Hr, Admin, AdminAuth)
- ASP.NET Core Identity + SQL Server (LocalDB by default)
- Entity Framework Core migrations
- Duitku payment gateway (mock mode for local dev)
- Static UI: custom CSS (`site.css`, role-specific styles)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB (Windows) or update `ConnectionStrings:DefaultConnection` in `appsettings.json`
- [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

## Setup guide (for teammates)

**New to the project?** Use the full walkthrough:

**[docs/SETUP.md](docs/SETUP.md)** — clone, `dotnet restore`, `dotnet build`, `dotnet ef database update`, `dotnet run --launch-profile https`, login accounts, and troubleshooting.

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

## Dummy / seed data (logins for testing)

Created automatically on first run. **Full reference:** [docs/SEED_DATA.md](docs/SEED_DATA.md) (all roles, bulk users, referral codes, which account to use per feature).

| Role | Email | Password | Where to log in |
|------|-------|----------|-----------------|
| Admin | `admin@lightenup.com` | `Admin123!` | :7041 `/AdminAuth/Login` |
| Psychologist | `dr.dina@lightenup.com` | `Password123!` | :7040 `/Account/Login` |
| HR | `hr@perusahaana.com` | `Password123!` | :7040 |
| Patient | `kaffah@perusahaana.com` | `Password123!` | :7040 |

> Change these passwords before any public deployment.

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
  Admin/          # Admin console (Dashboard, Users, Companies, Approvals, …)
  AdminAuth/      # Admin login/logout
  Patient/        # Patient portal
  Hr/             # HR portal
Controllers/      # Shared controllers (Account, Psychologist, …)
Data/             # DbContext, seeding
Models/           # Domain + view models
Services/         # Duitku, subscriptions, uploads, email
Views/            # Psychologist + shared views
wwwroot/          # CSS, JS, images (uploads/ is gitignored)
docs/             # Demo script, capstone appendix, deferred scope
```

## Documentation

- [docs/SETUP.md](docs/SETUP.md) — **first-time setup** for friends / teammates  
- [docs/SEED_DATA.md](docs/SEED_DATA.md) — **dummy accounts**, referral codes, test flows
- [docs/DEMO.md](docs/DEMO.md) — final demo / defense walkthrough (~20 min)
- [docs/CAPSTONE_REPORT_APPENDIX.md](docs/CAPSTONE_REPORT_APPENDIX.md) — use cases, ERD notes, test matrix
- [docs/DEFERRED_SCOPE.md](docs/DEFERRED_SCOPE.md) — intentionally out-of-scope items

## What is not in Git

See `.gitignore`. In particular:

- `bin/`, `obj/`, `.vs/` — build and IDE output
- `wwwroot/uploads/` — user-uploaded files
- Local secrets (`.env`, `appsettings.*.local.json`, User Secrets)
- Design/capstone asset folders (`Form_Capstone/`, `_figma/`)

## License

Academic / capstone project — add your license or institution notice here if required.
