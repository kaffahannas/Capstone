# Out of Scope (Document in Report Only)

Items appearing in **Form 3 Design** or **Form 2 draft database** that are **not implemented** in the capstone web application. Describe these as **future work** in your final report.

---

## Infrastructure (Form 3 architecture diagram)

| Component | Report treatment |
|-----------|------------------|
| Redis cache layer | Future scalability — not required for MVP |
| Message queue (RabbitMQ/Kafka) | Future async jobs |
| Worker service | Future background processing |
| Load balancer + web server cluster | Production deployment note; dev uses single Kestrel instance |
| Multiple app servers | Same as above |

---

## Product features

| Item | Reason deferred |
|------|-----------------|
| Native mobile app | Responsive web meets smartphone requirement |
| WhatsApp consultation integration | Replaced by in-app schedules/worksheets |
| Stripe / other gateways | Capstone specifies DuitKu only |
| Admin dynamic questionnaire weights | Fixed mood/check-in questions in v1 |
| Company subscription / seat billing (B2B) | B2B via company referral; payment MVP is B2C patient |
| Audit log table | Not required for MVP; use Identity + application logs |
| Generic notifications table | Replaced by `PatientNotificationPreference`, `HrNotificationPreference`, `PsyNotificationPreference` |

---

## Database (Form 2 draft — do not implement as-is)

| Draft table | Implementation choice |
|-------------|----------------------|
| `Users` (INT PK) | `AspNetUsers` / `ApplicationUser` (GUID) |
| `Invoice` / `InvoiceItems` | `PaymentTransaction` + `Subscription` |
| `Tasks` | `Worksheets` entity |
| `merchat_code` typo column | Use `Duitku` config in appsettings / secrets |

---

## How to reference in Form 3

In **System Design**, keep the layered diagram from your proposal but add a footnote:

> *Development prototype implements Client → ASP.NET Core MVC → EF Core → SQL Server. Cache, queue, and cluster components are planned for production scaling.*

This shows architectural awareness without building unused infrastructure for the defense demo.
