# LightenUp — Final Demo Script

**Duration:** ~20 minutes (5 min per role)  
**Run:** `dotnet run --launch-profile https`

| Site | URL |
|------|-----|
| Customer (Patient / Psy / HR) | https://localhost:7040 |
| Admin console | https://localhost:7041/AdminAuth/Login |

---

## 1. Admin (5 min) — port 7041

| Step | Action | URL |
|------|--------|-----|
| 1 | Login | `/AdminAuth/Login` |
| 2 | Dashboard stats | `/Admin/Dashboard` |
| 3 | Approvals queue | `/Admin/Approvals` |
| 4 | Open applicant → view documents → Approve | `/Admin/Approvals/Detail?id={userId}` |
| 5 | Users list | `/Admin/Users` |
| 6 | Companies | `/Admin/Companies` |

**Account:** `admin@lightenup.com` / `Admin123!`

**Security check:** Open https://localhost:7040/Admin/Dashboard → expect **404**.

---

## 2. Patient (5 min) — port 7040

| Step | Action | URL |
|------|--------|-----|
| 1 | Login | `/Account/Login` |
| 2 | Beranda / calendar | `/Patient/Dashboard` |
| 3 | Mood wizard | `/Patient/Mood/Feeling` |
| 4 | Journal check-in | `/Patient/Journal/CheckIn` |
| 5 | Statistik charts | `/Patient/Statistik` |
| 6 | Worksheet submit | `/Patient/Tasks` |
| 7 | Subscription / mock payment | `/Patient/Subscription` → Bayar → success |

**Account:** `kaffah@perusahaana.com` / `Password123!` (B2B seed — payment skipped)  
**B2C test:** register new patient without company code, then use Subscription.

---

## 3. Psychologist (5 min) — port 7040

| Step | Action | URL |
|------|--------|-----|
| 1 | Login | `/Account/Login` |
| 2 | Beranda / mitra | `/Psychologist` |
| 3 | Patient detail + mood | `/Psychologist/PatientDetail/{id}` |
| 4 | Assign worksheet | `/Psychologist/Worksheet` |
| 5 | HR requests | `/PsyRequests` |

**Account:** `dr.dina@lightenup.com` / `Password123!`

---

## 4. HR (5 min) — port 7040

| Step | Action | URL |
|------|--------|-----|
| 1 | Login (approved HR seed if available) or demo HR account | `/Account/Login` |
| 2 | Dashboard + Bahaya banner | `/Hr/Home` |
| 3 | Employees | `/Hr/Employees` |
| 4 | Statistik / export | `/Hr/Statistik` |
| 5 | Monitoring worksheets | `/Hr/Worksheets` |
| 6 | Report to psychologist | `/Hr/Reports` |

---

## Payment mock flow (B2C)

1. Patient → **Langganan** → choose plan → **Bayar sekarang**
2. Redirect to return URL with `mock=1`
3. Subscription status → **Active**
4. Confirm on Langganan page

Configure real DuitKu: see [README.md](../README.md).

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Build: file locked | Stop running `LightenUp.Web` process |
| DB error on startup | `dotnet ef database update` |
| Admin 404 on 7040 | Expected — use 7041 |
