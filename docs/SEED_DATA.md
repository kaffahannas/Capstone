# Dummy / seed data — login & test reference

After the first `dotnet run`, the app **automatically seeds** the database (`Program.cs` + `Data/DbInitializer.cs`).  
Your friend does **not** need to register accounts manually for testing.

**Default password for almost all seeded users:** `Password123!`  
**Admin password:** `Admin123!`

---

## Where to log in

| Role | Login URL | Port |
|------|-----------|------|
| **Admin** | https://localhost:7041/AdminAuth/Login | 7041 |
| **Patient, Psychologist, HR** | https://localhost:7040/Account/Login | 7040 |

---

## Accounts by role

### Admin (platform staff)

| Name | Email | Password | Notes |
|------|-------|----------|--------|
| System Admin | `admin@lightenup.com` | `Admin123!` | Created in `Program.cs`; full Admin Console access |

**Try after login:** Dashboard → Persetujuan → Pengguna → Perusahaan.

---

### Psychologist (psikolog)

| Name | Email | Password | Specialization | Notes |
|------|-------|----------|----------------|--------|
| **Dr. Dina** (main demo) | `dr.dina@lightenup.com` | `Password123!` | Psikolog Klinis | Partner **Perusahaan A**; assigned to Kaffah & Siti |
| Dr. Spesialis 1 | `psikolog1@lightenup.com` | `Password123!` | Psikolog Klinis | Extra dummy (batch seed) |
| Dr. Spesialis 2 | `psikolog2@lightenup.com` | `Password123!` | Psikolog Pendidikan | Extra dummy |
| Dr. Spesialis 3 | `psikolog3@lightenup.com` | `Password123!` | Psikolog Industri | Extra dummy |
| Dr. Spesialis 4 | `psikolog4@lightenup.com` | `Password123!` | Psikolog Anak | Extra dummy |

**Good demo account:** `dr.dina@lightenup.com` — has schedules, worksheets, and patients.

**Try after login:** Scheduling, Worksheet, patient list, company stats.

---

### HR (human resources)

| Name | Email | Password | Company | Department |
|------|-------|----------|---------|------------|
| HR Manager A | `hr@perusahaana.com` | `Password123!` | Perusahaan A | Human Resources |

**Try after login:** Employees, Schedules, Statistik, Reports, Worksheets.

---

### Patient (karyawan / employee)

#### Named demo patients (best for presentation)

| Name | Email | Password | Company | Division | Mental status |
|------|-------|----------|---------|----------|---------------|
| **Kaffah An Nas** | `kaffah@perusahaana.com` | `Password123!` | Perusahaan A | IT & Engineering | Sehat |
| Siti Aisyah | `siti@perusahaana.com` | `Password123!` | Perusahaan A | Pemasaran | Beresiko |

**Kaffah** has extra seed data: upcoming **schedule** with Dr. Dina, **worksheet** “Meditasi 30 Menit”, mood/statistik history (if already used locally).

**Try after login:** Dashboard, Mood, Journal, Jadwal, Tasks, Profile, Statistik.

#### Bulk dummy patients (for HR/statistics volume)

| Email pattern | Password | Count |
|---------------|----------|-------|
| `dummy1@perusahaana.com` … `dummy8@perusahaana.com` | `Password123!` | 8 users |

Names in app: **Dummy Karyawan 1** … **Dummy Karyawan 8**  
Mixed departments and mental-health statuses (Sehat / Beresiko / Bahaya).

---

## Companies & referral codes

Seeded companies:

| Company | Address (sample) | HR login |
|---------|------------------|----------|
| **Perusahaan A** | Jakarta Selatan | `hr@perusahaana.com` |
| Perusahaan B | Tangerang | *(no default HR user)* |

**Perusahaan A** divisions (for patient onboarding / referral):

| Division | Referral code |
|----------|----------------|
| Pusat | `A-PUSAT-01` |
| IT & Engineering | `A-IT-123` |
| Pemasaran | `A-MKT-456` |

**Perusahaan A** has an active **Enterprise Plan** subscription (B2B — company pays; patients under this company typically skip individual payment in demo).

---

## Relationships (who is linked to whom)

```
Perusahaan A
├── HR: hr@perusahaana.com
├── Patients: kaffah@, siti@, dummy1@ … dummy8@
└── Partner psychologists: Dr. Dina + psikolog1@ … psikolog4@

Dr. Dina ←assigned→ Kaffah, Siti (+ random assignments for dummy patients)
```

- **Schedules / worksheets / reports** are generated for many patients (past, today, future sessions).
- **Approvals queue:** only **new** Psychologist/HR sign-ups who finished onboarding but are **not** yet `IsApprovedByAdmin` appear there. Seeded accounts are **already approved**.

---

## Testing flows by role

| Goal | Account to use |
|------|----------------|
| Admin dashboard & user list | `admin@lightenup.com` |
| Approve a *new* psychologist/HR | Register new user → complete onboarding → login as Admin → Persetujuan |
| Patient mood & journal | `kaffah@perusahaana.com` |
| Patient with “Beresiko” status | `siti@perusahaana.com` |
| Psychologist schedule & tasks | `dr.dina@lightenup.com` |
| HR employee list & stats | `hr@perusahaana.com` |
| HR charts with many employees | Use after bulk seed (`dummy1@` … `dummy8@`) |
| B2B referral onboarding | Register patient with code `A-IT-123` (or other codes above) |
| B2C + subscription (mock payment) | Register **new** patient **without** company code on port 7040 |

---

## Payments (Duitku)

- Local dev: **`Duitku:UseMock": true`** → “Bayar” succeeds without real money.
- B2B patient under **Perusahaan A** (e.g. Kaffah): company subscription active — individual premium may be skipped in logic.
- To test payment UI: register a **public** patient (no referral) and open **Subscription**.

---

## Reset seed data

To get a fresh database with all dummy users again:

```powershell
dotnet ef database drop --force
dotnet ef database update
dotnet run --launch-profile https
```

On next startup, seeding runs again (only creates users that do not already exist).

---

## Quick copy-paste (most used)

```
Admin:        admin@lightenup.com          / Admin123!
Psychologist: dr.dina@lightenup.com        / Password123!
HR:           hr@perusahaana.com           / Password123!
Patient:      kaffah@perusahaana.com       / Password123!
```

**Customer site:** https://localhost:7040/Account/Login  
**Admin site:** https://localhost:7041/AdminAuth/Login
