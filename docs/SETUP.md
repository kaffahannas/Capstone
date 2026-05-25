# LightenUp — Setup guide (for teammates)

Follow these steps after cloning the repo. All commands assume **Windows** and **PowerShell** (or Terminal in VS Code). Adjust paths if your folder name differs.

---

## 1. Install prerequisites

### .NET 8 SDK

1. Download: https://dotnet.microsoft.com/download/dotnet/8.0  
2. Install the **SDK** (not only the runtime).  
3. Verify:

```powershell
dotnet --version
```

You should see `8.0.x` (or higher 8.x).

### SQL Server LocalDB (recommended on Windows)

Usually installed with **Visual Studio** or **SQL Server Express**.

Check if LocalDB is available:

```powershell
sqllocaldb info
```

If that fails, install one of:

- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (includes LocalDB), or  
- Visual Studio workload **ASP.NET and web development**

### EF Core CLI (for database migrations)

```powershell
dotnet tool install --global dotnet-ef
```

If already installed, update:

```powershell
dotnet tool update --global dotnet-ef
```

Verify:

```powershell
dotnet ef --version
```

### Git

```powershell
git --version
```

---

## 2. Clone the repository

```powershell
cd C:\Projects
git clone <PASTE-YOUR-GITHUB-REPO-URL-HERE>
cd WebsiteLightenUp
```

> Replace the URL with the real GitHub link from your team lead.

---

## 3. Restore and build

From the project folder (where `LightenUp.Web.csproj` lives):

```powershell
dotnet restore
dotnet build
```

**Expected:** `Build succeeded` with **0 Error(s)**.  
Warnings are usually OK for local dev.

If build fails because the app is still running, stop it (Ctrl+C in the terminal, or close Visual Studio debug session) and run `dotnet build` again.

---

## 4. Create / update the database

The app uses **Entity Framework Core** migrations in the `Migrations/` folder.

```powershell
dotnet ef database update
```

This creates/updates the database `LightenUpDB` on LocalDB.

**First run only:** if you see an error that the database cannot be created, ensure LocalDB is running:

```powershell
sqllocaldb start MSSQLLocalDB
dotnet ef database update
```

### Optional: reset database (destructive)

Only if you want a clean DB and don’t mind losing local data:

```powershell
dotnet ef database drop --force
dotnet ef database update
```

---

## 5. Run the application

Use the **https** launch profile (starts **two** URLs: customer site + admin console):

```powershell
dotnet run --launch-profile https
```

Wait until you see lines like:

```text
Now listening on: https://localhost:7040
Now listening on: https://localhost:7041
```

### Open in the browser

| What | URL |
|------|-----|
| **Customer site** (Patient / Psychologist / HR login) | https://localhost:7040 |
| **Admin console** | https://localhost:7041/AdminAuth/Login |

### HTTPS certificate warning

The first time, the browser may show **“Your connection is not private”** for `localhost`.

- Chrome/Edge: click **Advanced** → **Proceed to localhost** (dev only).  
- Or trust the dev certificate once:

```powershell
dotnet dev-certs https --trust
```

Then run the app again.

---

## 6. Log in with dummy / seed accounts

On first startup, the app seeds companies, HR, patients, psychologists, schedules, and more.

**Full list (all roles, referral codes, bulk dummy users):**  
👉 **[SEED_DATA.md](SEED_DATA.md)**

### Quick reference (most used)

| Role | Email | Password | Login URL |
|------|-------|----------|-----------|
| **Admin** | `admin@lightenup.com` | `Admin123!` | https://localhost:7041/AdminAuth/Login |
| **Psychologist** | `dr.dina@lightenup.com` | `Password123!` | https://localhost:7040/Account/Login |
| **HR** | `hr@perusahaana.com` | `Password123!` | https://localhost:7040/Account/Login |
| **Patient** | `kaffah@perusahaana.com` | `Password123!` | https://localhost:7040/Account/Login |

Also seeded: `siti@perusahaana.com`, `dummy1@`…`dummy8@perusahaana.com`, `psikolog1@`…`psikolog4@lightenup.com` — all `Password123!` unless noted in [SEED_DATA.md](SEED_DATA.md).

---

## 7. Run from Visual Studio (optional)

1. Open `LightenUp.Web.csproj` or the solution in Visual Studio.  
2. Set launch profile to **https** (dropdown next to the green Run button).  
3. Press **F5** or **Ctrl+F5**.

Same URLs as above (7040 + 7041).

---

## 8. Configuration (usually no changes needed)

| File | Purpose |
|------|---------|
| `appsettings.json` | Default connection string, Duitku mock mode |
| `appsettings.Development.json` | Dev hosts: `localhost:7040` / `localhost:7041` |
| `Properties/launchSettings.json` | Defines the `https` profile URLs |

**Payments:** local dev uses mock Duitku (`"Duitku": { "UseMock": true }`). No API keys required to test subscription flows.

**Uploads:** files go to `wwwroot/uploads/` (folder is created at runtime; not in Git).

---

## 9. Common problems

### `dotnet ef` not found

```powershell
dotnet tool install --global dotnet-ef
```

Close and reopen the terminal, then retry `dotnet ef database update`.

### Cannot connect to SQL Server / LocalDB

1. Check connection string in `appsettings.json` (default uses `(localdb)\MSSQLLocalDB`).  
2. Start LocalDB: `sqllocaldb start MSSQLLocalDB`  
3. Or install SQL Server Express with LocalDB.

### Port already in use (7040 or 7041)

Another instance is still running. Stop it:

- Close the terminal where `dotnet run` is active, or  
- Task Manager → end `LightenUp.Web.exe`, or  
- Change ports in `Properties/launchSettings.json` (and update `Site:PatientHost` / `Site:AdminHost` in `appsettings.Development.json` to match).

### Build error: file locked by LightenUp.Web

Stop the running app, then:

```powershell
dotnet build
```

### Admin URL on wrong port

Admin must be opened on **7041**, e.g. `https://localhost:7041/AdminAuth/Login`.  
Opening `/Admin/...` on **7040** returns **404** by design (security).

### Migrations folder missing after clone

Ask your teammate to ensure `Migrations/` was pushed to GitHub. Then:

```powershell
git pull
dotnet ef database update
```

If you are the first developer on a machine without migrations, contact the repo owner—do not invent migrations unless you know the schema.

---

## 10. Daily workflow (cheat sheet)

```powershell
cd path\to\WebsiteLightenUp
git pull
dotnet build
dotnet ef database update
dotnet run --launch-profile https
```

Stop the server: **Ctrl+C** in the terminal.

---

## 11. More documentation

- [SEED_DATA.md](SEED_DATA.md) — **all dummy logins**, companies, referral codes, test flows  
- [DEMO.md](DEMO.md) — step-by-step demo for presentation  
- [../README.md](../README.md) — project overview  
- [DEFERRED_SCOPE.md](DEFERRED_SCOPE.md) — features not implemented

If something still fails, send your teammate: full error text, `dotnet --version`, and whether `dotnet build` succeeded.
