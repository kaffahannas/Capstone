# Capstone Report Appendix — Implementation vs Form2

Use this when writing Form 2/3. **Capstone form database tables are draft**; this document reflects the **actual codebase**.

---

## Use case matrix (Form2 → implementation)

| # | Use case | Actor | Status | Route / notes |
|---|----------|-------|--------|----------------|
| 1 | Register Account | All | Done | `/Account/Register` |
| 2 | Login | All | Done | `/Account/Login`, `/AdminAuth/Login` (admin) |
| 3 | Fill Mood Questionnaire | Patient | Done | `/Patient/Mood/*` |
| 4 | View Mood History | Patient | Done | `/Patient/Statistik` |
| 5 | View Assigned Task | Patient | Done | `/Patient/Tasks` |
| 6 | Submit Task | Patient | Done | `/Patient/Tasks/Detail` + proof upload |
| 7 | Make Payment | Patient | Done (MVP) | `/Patient/Subscription` — mock or DuitKu |
| 8 | View Assigned Patients | Psychologist | Done | `/Psychologist` |
| 9 | Review Questionnaire Result | Psychologist | Done | Patient detail + mood/journal |
| 10 | Create Task | Psychologist | Done | `/Psychologist/Worksheet` |
| 11 | Manage Task | Psychologist | Done | Worksheet review / feedback |
| 12 | Add/Manage Client | HR | Done | `/Hr/Employees`, `PendingEmployee` |
| 13 | Monitor Client Schedule | HR | Done | `/Hr/Schedules`, `/Hr/Worksheets` |
| 14 | Generate Report | HR | Done | `/Hr/Reports`, `/Hr/Statistik` |
| 15 | Process Payment | DuitKu | Done (MVP) | `POST /api/payment/duitku/callback` |

**Screenshot column:** Add your own screenshots per row for the final report.

---

## Entity model (implementation ERD summary)

### Identity (ASP.NET Core Identity)

- `AspNetUsers` → `ApplicationUser` (GUID `Id`, `FullName`, `RoleType`, `IsApprovedByAdmin`, `IsActive`, …)
- `AspNetRoles`, `AspNetUserRoles`, …

### Domain (`Models/DomainModels.cs`)

| Table | Purpose |
|-------|---------|
| `Patients` | B2B/B2C patient profile + onboarding answers |
| `Psychologists` | License, documents, B2B opt-in |
| `HrStaffs` | Company link, onboarding docs |
| `Companies` | B2B org + `ReferralCode` |
| `Subscriptions` | Plan name, status, dates |
| `PaymentTransactions` | DuitKu orders (merchantOrderId, status, callback) |
| `PatientPsychologistAssignments` | M:N assignment |
| `Schedules` | Sessions |
| `Worksheets` | Tasks (proof image, feedback) |
| `MoodTrackers` | Daily mood |
| `Journals` | Free-write |
| `JournalCheckIns` | 6-question scale |
| `PendingEmployees` | HR pre-register |
| `PsychologistRequests` | HR → Psy requests |
| `Reports` | HR ↔ Psy escalation email |
| `*NotificationPreferences` | Per-role settings |

**Not implemented (Form2 draft only):** `Invoice`, `InvoiceItems`, separate `Users` INT table, `AuditLog`, generic `Notifications`.

---

## Functional test matrix (Form3 style)

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| T1 | Patient mood | Login → Mood wizard → Save | Mood on dashboard/statistik |
| T2 | Host isolation | `/Admin/*` on 7040 | HTTP 404 |
| T3 | Admin approval | Approve pending HR/Psy | User can access dashboard |
| T4 | HR report | Create report → send email | Email logged/sent |
| T5 | Payment mock | Subscription checkout | Status Active |
| T6 | Cross-host login | Admin on 7040 login | Error message, signed out |
| T7 | Worksheet flow | Psy assign → Patient submit → Psy complete | Status Completed |

---

## Form vs code narrative (paste into report)

> The capstone Form 2 data dictionary was an early draft. The implementation uses ASP.NET Identity for authentication (GUID user keys) and domain tables aligned with B2B corporate mental-health workflows. Payment is modeled as `PaymentTransaction` linked to `Subscription` rather than separate Invoice/InvoiceItem tables, while preserving the DuitKu callback flow described in the system analysis.
