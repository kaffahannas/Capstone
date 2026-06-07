# Form 4
## Capstone Design Implementation

**Title**: LightenUp – Workplace Mental Health Support Platform

**GROUP MEMBER**

| No. | Student Name | Student ID |
|---|---|---|
| 1 | [Leader Name] *(Leader)* | [Leader ID] |
| 2 | [Member 1 Name] *(Member 1)* | [Member 1 ID] |
| 3 | [Member 2 Name] *(Member 2)* | [Member 2 ID] |

**Advisor**: [Advisor Name]

Submitted for
Capstone Design Project
to Faculty of Computer Science
President University

---

## TABLE OF CONTENT

1. Statement of Originality
2. Screenshot of ZeroGPT
3. Part 4 – Implementation
   - A. Designs Implementation
   - B. Product Display
   - C. Component Cost Analysis
   - D. Functional Testing
   - E. Manual Guide
4. References

---

## STATEMENT OF ORIGINALITY

In our capacity as active students at President University and as the authors of the Capstone Design Project stated below:

**Name**:
1. [Student Name] – [NIM]
2. [Student Name] – [NIM]
3. [Student Name] – [NIM]

**Faculty**: Computer Science

We hereby declare that our Capstone Design Project entitled **"LightenUp – Workplace Mental Health Support Platform"** is, to the best of our knowledge and belief, an original piece of work based on sound academic principles. If there is any plagiarism detected in this final project, we are willing to be personally responsible for the consequences and will accept sanctions in accordance with the rules and policies of President University.

We also declare that this work, either in whole or in part, has not been submitted to another university to obtain a degree.

Cikarang, [Month Year]

Signer 1: ____________________  Signer 2: ____________________  Signer 3: ____________________

---

## SCREENSHOT OF ZEROGPT

*(Insert ZeroGPT screenshot here before submission)*

---

## PART 4 – IMPLEMENTATION

This section consists of:
- A. Designs Implementation
- B. Product Display
- C. Component Cost Analysis
- D. Functional Testing
- E. Manual Guide

---

## A. DESIGNS IMPLEMENTATION

### 1. Functions / Procedures / Classes Implementation

This section walks through the actual implementation of each major module in the LightenUp project. The code shown is taken directly from the source files. For each piece we explain what it does and why it was written that way.

---

#### 1.1 – Application User Model (`Models/ApplicationUser.cs`)

The base user entity extends ASP.NET Core Identity's built-in `IdentityUser` class:

```csharp
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public bool IsActive { get; set; } = true;

    // True = LightenUp Admin has approved the account.
    // - Patient: auto-true on creation
    // - Psychologist: starts false; Admin reviews license docs to approve
    // - HR: starts false; Admin reviews company info to approve
    // - Admin: always true
    public bool IsApprovedByAdmin { get; set; } = false;

    public virtual Patient? Patient { get; set; }
    public virtual Psychologist? Psychologist { get; set; }
    public virtual HrStaff? HrStaff { get; set; }
}
```

By inheriting `IdentityUser`, the class gets standard Identity columns (email, password hash, phone, etc.) for free. We added `RoleType` as a denormalized string so views and redirects can quickly read the user's role without an extra database round-trip. The `IsApprovedByAdmin` flag is the key gating mechanism — Psychologists and HR accounts cannot access the system until an Admin flips this to `true`. Navigation properties (`Patient`, `Psychologist`, `HrStaff`) establish the one-to-one link between an auth account and its role-specific profile data.

---

#### 1.2 – Registration and Login Flow (`Controllers/AccountController.cs`)

Registration in LightenUp is a three-step process: collect info → verify OTP → create password. Here is the final step that actually saves the user and creates their profile record:

```csharp
[HttpPost]
public async Task<IActionResult> CreatePassword(CreatePasswordViewModel model)
{
    var registerData = JsonSerializer.Deserialize<PublicRegisterViewModel>(registerDataJson);

    var user = new ApplicationUser
    {
        UserName = registerData.Email,
        Email = registerData.Email,
        EmailConfirmed = true,
        FullName = registerData.FullName,
        RoleType = registerData.AccountType,
        IsApprovedByAdmin = (registerData.AccountType == "Patient")  // Patients are auto-approved
    };

    var result = await _userManager.CreateAsync(user, model.Password);

    if (result.Succeeded)
    {
        await _userManager.AddToRoleAsync(user, registerData.AccountType);

        if (registerData.AccountType == "Patient")
        {
            var newPatient = new Patient { UserId = user.Id };
            _context.Patients.Add(newPatient);
            await _context.SaveChangesAsync();
            _context.PatientNotificationPreferences.Add(new PatientNotificationPreference
            {
                PatientId = newPatient.PatientId
            });
        }
        else if (registerData.AccountType == "Psychologist")
            _context.Psychologists.Add(new Psychologist { UserId = user.Id });
        else if (registerData.AccountType == "HR")
            _context.HrStaffs.Add(new HrStaff { UserId = user.Id });

        await _context.SaveChangesAsync();
    }
}
```

Notice that `IsApprovedByAdmin` is set to `true` only when the account type is `"Patient"`. HR and Psychologist accounts are always created with `false`, meaning they go through the admin approval queue after onboarding. Also note that after the user is created, the code immediately creates the corresponding profile row (`Patient`, `Psychologist`, or `HrStaff`) to keep everything in sync.

The login method has an interesting dual-host guard built into it:

```csharp
bool isAdminAccount = await _userManager.IsInRoleAsync(user, "Admin") || user.RoleType == "Admin";
bool onAdminHost = currentHost.Equals(adminHost, StringComparison.OrdinalIgnoreCase);
bool onCustomerHost = currentHost.Equals(patientHost, StringComparison.OrdinalIgnoreCase);

if (isAdminAccount && onCustomerHost)
{
    await _signInManager.SignOutAsync();
    ModelState.AddModelError(string.Empty, $"Admin accounts must log in at https://{adminHost}/");
    return View(model);
}
if (!isAdminAccount && onAdminHost)
{
    await _signInManager.SignOutAsync();
    ModelState.AddModelError(string.Empty, $"This is not an admin account. Please log in at https://{patientHost}/");
    return View(model);
}
```

This prevents admin accounts from logging into the customer-facing site (port 7040) and vice versa. We had to explicitly sign the user out before returning the error, otherwise Identity's session cookie would persist even though we're showing an error.

After the host check, role-based redirects happen:

```csharp
// Approval gate — Psy and HR cannot proceed without admin approval
if ((isPsy || isHr) && !user.IsApprovedByAdmin)
    return RedirectToAction("PendingApproval");

// Send each role to their own starting point
if (isAdminAccount)
    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
if (isHr)
    return RedirectToAction("Index", "Home", new { area = "Hr" });
if (user.RoleType == "Patient")
    return RedirectToAction("Index", "Dashboard", new { area = "Patient" });

// Psychologist default
return RedirectToAction("Index", "Psychologist");
```

---

#### 1.3 – Dual-Host Middleware (`Program.cs`)

One of the more unusual parts of the implementation is the hostname-based routing middleware. The system runs on two ports in development — 7040 for patients/psychologists/HR and 7041 for admins. We implemented this as a custom inline middleware:

```csharp
app.Use(async (context, next) =>
{
    var host = context.Request.Host.ToString();
    var path = context.Request.Path.Value ?? string.Empty;
    var isAdminPath = path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/AdminAuth", StringComparison.OrdinalIgnoreCase);

    // On the customer host — block admin paths entirely
    if (host.Equals(patientHost, StringComparison.OrdinalIgnoreCase) && isAdminPath)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    // On the admin host — only allow admin paths + static assets
    if (host.Equals(adminHost, StringComparison.OrdinalIgnoreCase) && !isAdminPath)
    {
        var allowed = path.StartsWith("/css/") || path.StartsWith("/js/")
                   || path.StartsWith("/lib/") || path.StartsWith("/uploads/");
        if (!allowed)
        {
            if (path.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/AdminAuth/Login");
                return;
            }
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
    }

    await next();
});
```

This middleware runs before authentication so it returns 404 before any controller code even executes. The admin console is invisible to anyone accessing the customer site, which is a clean way to separate concerns without needing a separate deployment.

---

#### 1.4 – Mood Tracker Wizard (`Areas/Patient/Controllers/MoodController.cs`)

The mood tracker is a multi-step wizard: Feeling → Triggers → Note → 5 questionnaire questions → Summary → Save. Each step passes data forward as route values since we wanted the entire session to be stateless (no TempData or session involved):

```csharp
// Step 1: Patient picks their mood label
[HttpPost]
public IActionResult Feeling(MoodTrackerSessionViewModel model)
{
    if (string.IsNullOrEmpty(model.Feeling))
    {
        ModelState.AddModelError(nameof(model.Feeling), "Please select a mood.");
        return View(model);
    }
    return RedirectToAction(nameof(Triggers), MakeRouteValues(model));
}

// Helper that packages all accumulated state into route values for the next step
private static object MakeRouteValues(MoodTrackerSessionViewModel m) => new
{
    feeling       = m.Feeling,
    triggers      = string.Join(",", m.Triggers ?? new()),
    note          = m.Note,
    questionStep  = m.QuestionStep,
    focusScore    = m.FocusScore,
    anxietyScore  = m.AnxietyScore,
    sleepScore    = m.SleepScore,
    mindLoadScore = m.MindLoadScore,
    emotionScore  = m.EmotionScore
};
```

The questionnaire loop runs for exactly 5 steps:

```csharp
[HttpPost, ActionName("Question")]
public IActionResult QuestionPost(MoodTrackerSessionViewModel model)
{
    int score = model.CurrentQuestionScore();
    if (score < 1 || score > 5)
    {
        ModelState.AddModelError("", "Please pick a value between 1 and 5.");
        return View(model);
    }

    if (model.QuestionStep < 5)
    {
        model.QuestionStep++;
        return RedirectToAction(nameof(Question), MakeRouteValues(model));
    }

    // All 5 questions answered, go to summary
    return RedirectToAction(nameof(Summary), MakeRouteValues(model));
}
```

The final save step handles both new entries and updates (in case the patient already submitted mood today and came back to edit):

```csharp
[HttpPost, ActionName("Summary")]
public async Task<IActionResult> SummaryPost(MoodTrackerSessionViewModel model)
{
    var today = DateTime.Today;
    var existing = await _context.MoodTrackers
        .FirstOrDefaultAsync(m => m.PatientId == patient.PatientId && m.MoodDate.Date == today);

    if (existing == null)
    {
        _context.MoodTrackers.Add(new MoodTracker
        {
            PatientId     = patient.PatientId,
            Feeling       = model.Feeling,
            Triggers      = string.Join(",", model.Triggers ?? new()),
            Note          = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note,
            FocusScore    = model.FocusScore > 0 ? model.FocusScore : null,
            AnxietyScore  = model.AnxietyScore > 0 ? model.AnxietyScore : null,
            SleepScore    = model.SleepScore > 0 ? model.SleepScore : null,
            MindLoadScore = model.MindLoadScore > 0 ? model.MindLoadScore : null,
            EmotionScore  = model.EmotionScore > 0 ? model.EmotionScore : null,
            MoodDate      = today,
            RecordedAt    = DateTime.Now
        });
    }
    else
    {
        existing.Feeling   = model.Feeling;
        existing.Triggers  = string.Join(",", model.Triggers ?? new());
        existing.RecordedAt = DateTime.Now;
        // ... update remaining fields
    }

    await _context.SaveChangesAsync();
    return RedirectToAction("Index", "Dashboard");
}
```

---

#### 1.5 – Journal and Daily Check-In (`Areas/Patient/Controllers/JournalController.cs`)

The journal module has two separate features: a structured 6-question check-in and a free-write entry. The check-in follows the same step-passing pattern as the mood tracker:

```csharp
[HttpPost, ActionName("Question")]
public async Task<IActionResult> QuestionPost(JournalCheckInSessionViewModel model)
{
    int current = model.CurrentScore();
    if (current < 1 || current > 5)
    {
        ModelState.AddModelError("", "Please pick a value between 1 and 5.");
        return View("Question", model);
    }

    if (model.Step < 6)
    {
        return RedirectToAction(nameof(Question), new
        {
            step = model.Step + 1,
            FocusScore = model.FocusScore,
            AnxietyScore = model.AnxietyScore,
            SleepScore = model.SleepScore,
            MindLoadScore = model.MindLoadScore,
            EmotionScore = model.EmotionScore,
            OverallScore = model.OverallScore
        });
    }

    // Step 6 answered, save to database
    var existing = await _context.JournalCheckIns
        .FirstOrDefaultAsync(c => c.PatientId == patient.PatientId && c.CheckInDate.Date == today);

    if (existing == null)
    {
        _context.JournalCheckIns.Add(new JournalCheckIn
        {
            PatientId     = patient.PatientId,
            FocusScore    = model.FocusScore,
            AnxietyScore  = model.AnxietyScore,
            SleepScore    = model.SleepScore,
            MindLoadScore = model.MindLoadScore,
            EmotionScore  = model.EmotionScore,
            OverallScore  = model.OverallScore,
            CheckInDate   = today,
            RecordedAt    = DateTime.Now
        });
    }
    // else: update existing (patient came back to edit today's check-in)

    await _context.SaveChangesAsync();
    return RedirectToAction(nameof(CheckInSaved));
}
```

For the free-write journal, the logic looks up today's entry first and either creates or updates it:

```csharp
[HttpPost]
public async Task<IActionResult> Write(JournalWriteViewModel model)
{
    var today = DateTime.Today;
    Journal? entry = null;

    if (model.JournalId.HasValue)
        entry = await _context.Journals
            .FirstOrDefaultAsync(j => j.JournalId == model.JournalId.Value && j.PatientId == patient.PatientId);

    entry ??= await _context.Journals
        .FirstOrDefaultAsync(j => j.PatientId == patient.PatientId && j.JournalDate.Date == today);

    if (entry == null)
    {
        entry = new Journal
        {
            PatientId   = patient.PatientId,
            Title       = model.Title,
            Content     = model.Content,
            JournalDate = today,
            CreatedAt   = DateTime.Now,
            UpdatedAt   = DateTime.Now
        };
        _context.Journals.Add(entry);
    }
    else
    {
        entry.Title     = model.Title;
        entry.Content   = model.Content;
        entry.UpdatedAt = DateTime.Now;
    }

    await _context.SaveChangesAsync();
    return RedirectToAction("Index", "Dashboard");
}
```

---

#### 1.6 – Health Status Computation (`Services/HealthStatusService.cs`)

This service computes the patient's mental health classification ("Sehat", "Beresiko", "Bahaya") from their actual mood and check-in data:

```csharp
// Maps mood label strings to numeric scores 1-5
private static int? MapFeelingScore(string feeling) => feeling switch
{
    "Overjoyed"    => 5,
    "Happy"        => 4,
    "Calm"         => 4,
    "Neutral"      => 3,
    "Disappointed" => 2,
    "Angry"        => 1,
    _              => null
};

public async Task<Snapshot> ComputeAsync(int patientId)
{
    var sevenDaysAgo = DateTime.Today.AddDays(-7);
    var thirtyDaysAgo = DateTime.Today.AddDays(-30);

    var moods7d    = await _context.MoodTrackers
        .Where(m => m.PatientId == patientId && m.MoodDate >= sevenDaysAgo).ToListAsync();
    var checkIns7d = await _context.JournalCheckIns
        .Where(c => c.PatientId == patientId && c.CheckInDate >= sevenDaysAgo).ToListAsync();

    // Combine mood scores and questionnaire scores into a single sample list
    var samples7d = new List<int>();
    foreach (var m in moods7d)
    {
        var s = MapFeelingScore(m.Feeling);
        if (s != null) samples7d.Add(s.Value);

        if (m.FocusScore.HasValue)
        {
            var allAverages = new[] { m.FocusScore.Value, m.AnxietyScore!.Value,
                                      m.SleepScore!.Value, m.MindLoadScore!.Value, m.EmotionScore!.Value };
            samples7d.Add((int)Math.Round(allAverages.Average()));
        }
    }

    // Classify based on average score across the last 7 days
    string status = "Sehat";
    if (samples7d.Count > 0)
    {
        var avg = samples7d.Average();
        status = avg >= 4.0 ? "Sehat" : (avg >= 2.5 ? "Beresiko" : "Bahaya");
    }

    // 30-day window for the overall mood percentage
    // ... (same logic, mapped to 0-100%)
}
```

The computation uses a 7-day window for the health status label and a 30-day window for the overall mood percentage shown on the profile. The thresholds (4.0 and 2.5) were chosen to divide the 1-5 scale into three roughly equal bands.

---

#### 1.7 – Subscription Access Checking (`Services/SubscriptionAccessService.cs`)

This service centralizes all subscription-related access logic so that controllers and filters don't have to repeat the same database queries:

```csharp
// Check if a company subscription is currently active
public async Task<bool> HasCompanyActiveSubscriptionAsync(int companyId)
{
    return await _context.CompanySubscriptions
        .AnyAsync(s => s.CompanyId == companyId
                    && s.Status == "Active"
                    && s.EndDate >= DateTime.Today);
}

// Check if a patient has premium access — either via own subscription or via company
public async Task<bool> HasPatientPremiumAccessAsync(Patient patient)
{
    var hasOwn = await _context.Subscriptions
        .AnyAsync(s => s.PatientId == patient.PatientId
                    && s.Status == "Active"
                    && s.EndDate >= DateTime.Today);
    if (hasOwn) return true;

    // B2B patients can inherit their company's subscription
    if (patient.CompanyId != null)
        return await HasCompanyActiveSubscriptionAsync(patient.CompanyId.Value);

    return false;
}

// Generates a random 6-character alphanumeric code, guaranteed unique
public async Task<string> GenerateUniqueReferralCodeAsync()
{
    string code;
    do { code = GenerateReferralCode(); }
    while (await _context.CompanyDivisions.AnyAsync(c => c.ReferralCode == code));
    return code;
}
```

The dual-path check in `HasPatientPremiumAccessAsync` is the core of the B2B/B2C logic: if an employee's company has a valid subscription, they get access without needing to pay individually. The referral code generator deliberately excludes confusing characters (0, O, I, 1) to make codes easier to type.

---

#### 1.8 – Premium Feature Gate (`Filters/RequiresPatientPremiumAttribute.cs`)

Rather than repeating the subscription check inside every premium controller action, we made it an ASP.NET Core action filter:

```csharp
public class RequiresPatientPremiumAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var db      = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var access  = context.HttpContext.RequestServices.GetRequiredService<SubscriptionAccessService>();
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user    = await userManager.GetUserAsync(context.HttpContext.User);

        if (user == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
            return;
        }

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (patient == null)
        {
            context.Result = new RedirectToActionResult("Welcome", "Onboarding", new { area = "Patient" });
            return;
        }

        if (!await access.HasPatientPremiumAccessAsync(patient))
        {
            context.Result = new RedirectToActionResult("Index", "Subscription", new { area = "Patient" });
            return;
        }

        await next();
    }
}
```

The `StatistikController` for example is decorated with `[RequiresPatientPremium]` at the class level, which means every action inside it goes through this check. If the patient doesn't have an active subscription they just get redirected to the subscription page rather than seeing an error.

---

#### 1.9 – Payment and Subscription Flow (`Areas/Patient/Controllers/SubscriptionController.cs`)

The checkout method ties together the subscription record, the payment transaction, and the Duitku gateway call:

```csharp
[HttpPost]
public async Task<IActionResult> Checkout(string planId)
{
    var plan = Plans.FirstOrDefault(p => p.PlanId == planId);

    // Create a Pending subscription row first
    var subscription = new Subscription
    {
        PatientId = patient.PatientId,
        PlanName  = plan.Name,
        Status    = "Pending",
        StartDate = DateTime.Today,
        EndDate   = DateTime.Today.AddMonths(plan.DurationMonths)
    };
    _context.Subscriptions.Add(subscription);
    await _context.SaveChangesAsync();

    // Build the unique order ID
    var orderId = $"LU-{patient.PatientId}-{subscription.SubscriptionId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

    // Create the payment transaction row
    var payment = new PaymentTransaction
    {
        PatientId      = patient.PatientId,
        SubscriptionId = subscription.SubscriptionId,
        MerchantOrderId = orderId,
        Amount         = plan.Price,
        PlanName       = plan.Name,
        PaymentStatus  = "pending"
    };
    _context.PaymentTransactions.Add(payment);
    await _context.SaveChangesAsync();

    // Call Duitku (or the mock) to get the payment URL
    var result = await _duitku.CreatePaymentAsync(new DuitkuCreatePaymentRequest
    {
        MerchantOrderId = orderId,
        Amount          = plan.Price,
        ProductDetails  = $"LightenUp {plan.Name}",
        CustomerEmail   = user.Email ?? "",
        CustomerName    = user.FullName,
        CallbackUrl     = callbackUrl,
        ReturnUrl       = returnUrl
    });

    // Store the Duitku reference and redirect the user to the payment page
    payment.DuitkuReference = result.Reference;
    payment.PaymentUrl      = result.PaymentUrl;
    await _context.SaveChangesAsync();

    return Redirect(result.PaymentUrl);
}
```

The order ID format `LU-{patientId}-{subscriptionId}-{timestamp}` is designed to be both unique and debuggable (you can tell which patient and subscription it belongs to just by reading it).

---

#### 1.10 – Duitku Payment Gateway (`Services/DuitkuService.cs`)

The Duitku service handles both the payment request and the callback signature verification:

```csharp
public async Task<DuitkuCreatePaymentResult> CreatePaymentAsync(DuitkuCreatePaymentRequest request, CancellationToken ct = default)
{
    // If mock mode is on, skip the real API and return a fake URL immediately
    if (_options.UseMock || !IsConfigured)
    {
        return new DuitkuCreatePaymentResult
        {
            Success    = true,
            IsMock     = true,
            Reference  = "MOCK-" + request.MerchantOrderId,
            PaymentUrl = request.ReturnUrl + "?mock=1&orderId=" + Uri.EscapeDataString(request.MerchantOrderId)
        };
    }

    var amount    = (int)Math.Round(request.Amount, 0);
    var signature = ComputeSha256($"{_options.MerchantCode}{request.MerchantOrderId}{amount}{_options.ApiKey}");

    // ... POST to Duitku API, parse statusCode "00" = success
}

// Duitku uses MD5 for callback signatures (different from SHA256 used on creation)
public bool VerifyCallbackSignature(string merchantCode, string amount, string merchantOrderId, string signature)
{
    if (!IsConfigured) return true; // skip verification in mock/dev mode
    var expected = ComputeMd5($"{merchantCode}{amount}{merchantOrderId}{_options.ApiKey}");
    return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
}
```

The mock mode was very useful during development — it lets you test the entire subscription checkout flow without any real API credentials. When mock mode is on and you hit the return URL, it auto-marks the payment as paid.

---

#### 1.11 – Payment Webhook (`Controllers/PaymentWebhookController.cs`)

The webhook endpoint handles callbacks from the Duitku payment gateway:

```csharp
[HttpPost("callback")]
public async Task<IActionResult> Callback([FromForm] DuitkuCallbackForm form)
{
    var payment = await _context.PaymentTransactions
        .Include(p => p.Subscription)
        .Include(p => p.CompanySubscription)
        .FirstOrDefaultAsync(p => p.MerchantOrderId == form.MerchantOrderId);

    if (payment == null) return NotFound();

    // Verify the Duitku MD5 signature before trusting the callback
    if (!_duitku.VerifyCallbackSignature(form.MerchantCode ?? "", ((int)payment.Amount).ToString(),
            form.MerchantOrderId, form.Signature))
    {
        return Unauthorized();
    }

    payment.CallbackPayload  = JsonSerializer.Serialize(form);
    payment.ResultCode       = form.ResultCode;
    payment.DuitkuReference  = form.Reference ?? payment.DuitkuReference;

    if (form.ResultCode == "00")
        await PaymentCompletionService.MarkPaidAsync(_context, payment);  // activate subscription
    else
    {
        payment.PaymentStatus = "failed";
        await _context.SaveChangesAsync();
    }

    return Ok();
}
```

Result code `"00"` from Duitku means successful payment. Anything else marks it as failed. The signature verification step is important — without it, anyone could POST a fake callback and activate a subscription for free.

---

#### 1.12 – Payment Completion Service (`Services/PaymentCompletionService.cs`)

Once a payment is confirmed, this service activates the appropriate subscription:

```csharp
public static async Task MarkPaidAsync(ApplicationDbContext context, PaymentTransaction payment)
{
    if (payment.PaymentStatus == "paid") return; // idempotent — safe to call twice

    payment.PaymentStatus = "paid";
    payment.PaidAt        = DateTime.UtcNow;

    // B2C: activate the patient's personal subscription
    if (payment.SubscriptionId.HasValue)
    {
        var sub = payment.Subscription ?? await context.Subscriptions.FindAsync(payment.SubscriptionId.Value);
        if (sub != null)
        {
            sub.Status    = "Active";
            sub.StartDate = DateTime.Today;
            sub.EndDate   = DateTime.Today.AddMonths(
                sub.PlanName.Contains("Tahunan", StringComparison.OrdinalIgnoreCase) ? 12 : 1);
        }
    }

    // B2B: activate the company subscription and create the first division if none exists yet
    if (payment.CompanySubscriptionId.HasValue)
    {
        var companySub = /* fetch company subscription */;
        if (companySub != null)
        {
            companySub.Status  = "Active";
            companySub.EndDate = DateTime.Today.AddMonths(isYearly ? 12 : 1);

            // First-time activation: auto-create a default "Pusat" division with a referral code
            bool hasDivisions = await context.CompanyDivisions.AnyAsync(d => d.CompanyId == company.CompanyId);
            if (!hasDivisions)
            {
                var newDiv = new CompanyDivision
                {
                    CompanyId    = company.CompanyId,
                    Name         = "Pusat",
                    ReferralCode = await access.GenerateUniqueReferralCodeAsync()
                };
                context.CompanyDivisions.Add(newDiv);
            }
        }
    }

    await context.SaveChangesAsync();
}
```

The auto-creation of the "Pusat" (headquarters) division on first payment is a nice UX touch — HR managers don't need to manually set up a division just to start using referral codes.

---

#### 1.13 – Statistics Module (`Areas/Patient/Controllers/StatistikController.cs`)

This controller is protected by `[RequiresPatientPremium]` at the class level. It computes several metrics from the patient's raw data:

```csharp
[RequiresPatientPremium]
public class StatistikController : Controller
{
    // Maps mood labels to numeric scores for chart plotting
    private static double FeelingToScore(string feeling) => feeling switch
    {
        "Overjoyed"    => 5,
        "Happy"        => 4,
        "Calm"         => 4,
        "Neutral"      => 3,
        "Disappointed" => 2,
        "Angry"        => 1,
        _              => 3
    };

    public async Task<IActionResult> Index(int window = 30)
    {
        // A: Mood trend — score per day for chart
        var moods = await _context.MoodTrackers
            .Where(m => m.PatientId == patient.PatientId && m.MoodDate >= from)
            .OrderBy(m => m.MoodDate).ToListAsync();

        var trend = moods.Select(m => new MoodTrendPoint
        {
            Date    = m.MoodDate,
            Score   = FeelingToScore(m.Feeling),
            Feeling = m.Feeling
        }).ToList();

        // B: Radar chart — average per dimension
        var radar = new CheckInRadar
        {
            Focus    = Math.Round(moodScores.Average(c => c.FocusScore!.Value), 1),
            Anxiety  = Math.Round(moodScores.Average(c => c.AnxietyScore!.Value), 1),
            Sleep    = Math.Round(moodScores.Average(c => c.SleepScore!.Value), 1),
            MindLoad = Math.Round(moodScores.Average(c => c.MindLoadScore!.Value), 1),
            Emotion  = Math.Round(moodScores.Average(c => c.EmotionScore!.Value), 1),
        };

        // D: Consecutive day streak calculation
        int currentStreak = 0;
        var probe = today;
        if (!trackedDates.Contains(probe)) probe = probe.AddDays(-1);
        while (trackedDates.Contains(probe))
        {
            currentStreak++;
            probe = probe.AddDays(-1);
        }

        // F: Top mood triggers (count occurrences in CSV field)
        var counts = new Dictionary<string, int>();
        foreach (var m in moods)
        {
            foreach (var t in m.Triggers.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                counts.TryGetValue(t, out var n);
                counts[t] = n + 1;
            }
        }
        var topTriggers = counts.OrderByDescending(kv => kv.Value).Take(10).ToList();
    }
}
```

The streak calculation walks backwards from today until it finds a day with no tracking data. The triggers field stores CSV values (e.g., `"Work,Family,Sleep"`) which we split and count on every stats load — it's not the most efficient approach but works fine for the data volumes expected.

---

#### 1.14 – Psychologist Dashboard (`Controllers/PsychologistController.cs`)

The psychologist dashboard loads three categories of data at once: their active patients, the list of patients available to take on (not yet assigned), and their partner companies:

```csharp
public async Task<IActionResult> Index()
{
    // Active assigned patients
    var assignedPatientIds = await _context.Assignments
        .Where(a => a.PsychologistId == psychologist.PsychologistId && a.Status == "Active")
        .Select(a => a.PatientId).ToListAsync();

    var activePatients = await _context.Patients
        .Include(p => p.User).Include(p => p.Company)
        .Where(p => assignedPatientIds.Contains(p.PatientId))
        .ToListAsync();

    // Available patients: no active assignment, and either public (no company) or from a partner company
    var unassignedPatientsDb = await _context.Patients
        .Include(p => p.User).Include(p => p.Company)
        .Where(p => !_context.Assignments.Any(a => a.PatientId == p.PatientId && a.Status == "Active"))
        .Where(p => p.CompanyId == null || partnerCompanyIds.Contains(p.CompanyId.Value))
        .ToListAsync();
}

// Assigning a patient to this psychologist
[HttpPost]
public async Task<IActionResult> AssignClient(int patientId)
{
    var assignment = new PatientPsychologistAssignment
    {
        PatientId       = patientId,
        PsychologistId  = psych.PsychologistId,
        Status          = "Active",
        AssignedAt      = DateTime.Now
    };
    _context.Assignments.Add(assignment);
    await _context.SaveChangesAsync();
    return RedirectToAction("Index");
}

// Joining a company as a partner psychologist via referral code
[HttpPost]
public async Task<IActionResult> JoinCompany(string referralCode)
{
    var company = await _context.Companies.FirstOrDefaultAsync(c => c.ReferralCode == referralCode);
    if (company != null && !psych.PartneredCompanies.Any(c => c.CompanyId == company.CompanyId))
    {
        psych.PartneredCompanies.Add(company);
        await _context.SaveChangesAsync();
    }
    return RedirectToAction("Index");
}
```

The filter for available patients (`p.CompanyId == null || partnerCompanyIds.Contains(p.CompanyId.Value)`) means psychologists only see public patients or employees from companies they've partnered with — they can't browse employees from unrelated companies.

---

#### 1.15 – Admin Approval System (`Areas/Admin/Controllers/ApprovalsController.cs`)

The approvals controller fetches all pending Psychologist and HR accounts and allows the admin to approve or reject them:

```csharp
[HttpGet]
public async Task<IActionResult> Index(string tab = "All")
{
    var pending = await _userManager.Users
        .Where(u => !u.IsApprovedByAdmin && (u.RoleType == "Psychologist" || u.RoleType == "HR"))
        .ToListAsync();

    // Pre-fetch the related profile data for each pending user
    var psyMap = await _context.Psychologists
        .Where(p => pending.Select(u => u.Id).Contains(p.UserId))
        .ToDictionaryAsync(p => p.UserId);
    var hrMap = await _context.HrStaffs
        .Include(h => h.Company)
        .Where(h => pending.Select(u => u.Id).Contains(h.UserId))
        .ToDictionaryAsync(h => h.UserId);
}

[HttpPost]
public async Task<IActionResult> Approve(AdminApprovalActionViewModel model)
{
    var user = await _userManager.FindByIdAsync(model.UserId);
    user.IsApprovedByAdmin = true;
    user.IsActive          = true;
    await _userManager.UpdateAsync(user);

    // Notify the user by email
    await TrySendAsync(user.Email!, "Your LightenUp account has been approved",
        $"Hello {user.FullName}, your {user.RoleType} account is now active. " +
        $"Admin note: {model.Note ?? "—"}");

    TempData["success"] = $"{user.FullName} ({user.RoleType}) approved.";
    return RedirectToAction(nameof(Index));
}

[HttpPost]
public async Task<IActionResult> Reject(AdminApprovalActionViewModel model)
{
    user.IsActive          = false;
    user.IsApprovedByAdmin = false;
    await _userManager.UpdateAsync(user);

    await TrySendAsync(user.Email!, "Your LightenUp application was not approved",
        $"Hello {user.FullName}, your {user.RoleType} application could not be approved at this time. " +
        $"Note: {model.Note ?? "—"}");
}
```

The approve/reject actions both trigger an email via `SmtpEmailSender`. The email sending is wrapped in a try-catch (`TrySendAsync`) so that if email fails (e.g. SMTP not configured), the approval action still goes through and doesn't crash.

---

#### 1.16 – File Upload Service (`Services/UserUploadService.cs`)

All uploaded files (profile pictures, documents, worksheet proofs) go through this service:

```csharp
public async Task<string?> SaveAsync(string userId, string category, IFormFile file,
    string? namePrefix = null, IReadOnlyCollection<string>? allowedExtensions = null)
{
    if (file.Length == 0) return null;

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (allowedExtensions != null && !allowedExtensions.Contains(ext))
        return null;

    var folder   = Path.Combine(_env.WebRootPath, "uploads", "accounts", SanitizeSegment(userId), SanitizeSegment(category));
    Directory.CreateDirectory(folder);

    // Use a GUID so filenames never collide, even if the same user uploads the same file twice
    var fileName = $"{prefix}{Guid.NewGuid():N}{ext}";
    var fullPath = Path.Combine(folder, fileName);

    await using (var stream = new FileStream(fullPath, FileMode.Create))
        await file.CopyToAsync(stream);

    return $"/uploads/accounts/{safeUserId}/{safeCategory}/{fileName}";
}

// Delete the old file when replacing (so we don't accumulate orphaned uploads)
public async Task<string?> ReplaceAsync(string userId, string category, IFormFile file,
    string? previousWebPath, ...)
{
    var path = await SaveAsync(userId, category, file, namePrefix, allowedExtensions);
    if (path != null) TryDeleteByWebPath(previousWebPath);
    return path;
}

// Safety check — never delete anything outside the uploads folder
public void TryDeleteByWebPath(string? webPath)
{
    var fullPath    = Path.GetFullPath(Path.Combine(_env.WebRootPath, relative));
    var uploadsRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads"));
    if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase)) return;
    if (File.Exists(fullPath)) File.Delete(fullPath);
}
```

The `SanitizeSegment` method strips path-separator characters and invalid filename characters from the userId before using it in a path — this prevents path traversal attacks. The delete path check (`fullPath.StartsWith(uploadsRoot)`) is a safety net to ensure no code can accidentally delete files outside the uploads directory.

---

### 2. Database Implementation

The database is built with Microsoft SQL Server and Entity Framework Core. The full schema and constraint configuration lives in `Data/ApplicationDbContext.cs`.

#### 2.1 – DbContext and Entity Registration

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // Role-specific profile tables
    public DbSet<Patient>      Patients     { get; set; }
    public DbSet<Psychologist> Psychologists { get; set; }
    public DbSet<HrStaff>      HrStaffs     { get; set; }

    // Company and subscription tables
    public DbSet<Company>             Companies             { get; set; }
    public DbSet<CompanyDivision>     CompanyDivisions      { get; set; }
    public DbSet<Subscription>        Subscriptions         { get; set; }
    public DbSet<CompanySubscription> CompanySubscriptions  { get; set; }
    public DbSet<PaymentTransaction>  PaymentTransactions   { get; set; }

    // Core activity tables
    public DbSet<PatientPsychologistAssignment> Assignments      { get; set; }
    public DbSet<Schedule>                      Schedules        { get; set; }
    public DbSet<Worksheet>                     Worksheets       { get; set; }
    public DbSet<MoodTracker>                   MoodTrackers     { get; set; }
    public DbSet<Journal>                       Journals         { get; set; }
    public DbSet<JournalCheckIn>                JournalCheckIns  { get; set; }

    // HR-specific tables
    public DbSet<PendingEmployee>       PendingEmployees      { get; set; }
    public DbSet<PsychologistRequest>   PsychologistRequests  { get; set; }
    public DbSet<Report>                Reports               { get; set; }
}
```

`IdentityDbContext<ApplicationUser>` automatically brings in `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, and related Identity tables. Everything else we defined ourselves.

#### 2.2 – Key Relationship Configurations (Fluent API)

SQL Server throws an error if multiple foreign keys in a table could trigger cascade delete on the same row from different paths ("multiple cascade paths"). We resolved this by using `OnDelete(DeleteBehavior.Restrict)` wherever needed:

```csharp
// PatientPsychologistAssignment references both Patient and Psychologist
// Both can't cascade-delete or SQL Server will complain
builder.Entity<PatientPsychologistAssignment>()
    .HasOne(a => a.Patient)
    .WithMany(p => p.Assignments)
    .HasForeignKey(a => a.PatientId)
    .OnDelete(DeleteBehavior.Restrict);

builder.Entity<PatientPsychologistAssignment>()
    .HasOne(a => a.Psychologist)
    .WithMany(p => p.Assignments)
    .HasForeignKey(a => a.PsychologistId)
    .OnDelete(DeleteBehavior.Restrict);

// If an HR who created an assignment is deleted, SetNull to preserve the assignment record
builder.Entity<PatientPsychologistAssignment>()
    .HasOne(a => a.AssignedByHr)
    .WithMany()
    .HasForeignKey(a => a.AssignedByHrUserId)
    .OnDelete(DeleteBehavior.SetNull);
```

#### 2.3 – Unique Index Constraints

Several business rules are enforced as database-level unique indexes:

```csharp
// One mood entry per patient per day (can edit it, but can't create a second one)
builder.Entity<MoodTracker>()
    .HasIndex(m => new { m.PatientId, m.MoodDate })
    .IsUnique();

// One journal entry per patient per day
builder.Entity<Journal>()
    .HasIndex(j => new { j.PatientId, j.JournalDate })
    .IsUnique();

// Referral codes are unique across all divisions
builder.Entity<CompanyDivision>()
    .HasIndex(d => d.ReferralCode)
    .IsUnique()
    .HasFilter("[ReferralCode] IS NOT NULL");

// Employee ID must be unique within a company (but NULL is allowed for B2C patients)
builder.Entity<Patient>()
    .HasIndex(p => new { p.CompanyId, p.EmployeeId })
    .IsUnique()
    .HasFilter("[CompanyId] IS NOT NULL AND [EmployeeId] IS NOT NULL");

// One payment transaction per merchant order ID
builder.Entity<PaymentTransaction>()
    .HasIndex(p => p.MerchantOrderId)
    .IsUnique();
```

The filtered indexes (`.HasFilter(...)`) are a SQL Server feature that allows unique constraints to apply only to non-NULL values. This is necessary because many of these fields are optional — for example, two B2C patients can both have `EmployeeId = null` without violating the unique constraint.

---

### 3. User Interface Implementation

The UI is built using Razor Views (`.cshtml`) with Bootstrap 5 and custom role-specific CSS files. There is no JavaScript framework — all interaction happens through HTML form submissions and Razor partial views.

Each user role has its own layout file:
- `Areas/Patient/Views/Shared/_LayoutPatient.cshtml` – patient sidebar/nav
- `Areas/Hr/Views/Shared/_LayoutHr.cshtml` – HR sidebar
- `Areas/Admin/Views/Shared/_LayoutAdmin.cshtml` – admin sidebar
- `Views/Psychologist/_Layout.cshtml` – psychologist layout

Custom CSS per role:
- `wwwroot/css/patient.css` – patient portal styles
- `wwwroot/css/admin.css` – admin console styles
- `wwwroot/css/dashboard.css` – shared dashboard component styles
- `wwwroot/css/onboarding.css` – onboarding wizard styles

The patient onboarding wizard uses a separate layout (`_LayoutOnboarding.cshtml`) with a progress bar. Similarly, the mood wizard uses `_LayoutMoodWizard.cshtml` to keep the UI clean and focused during each step.

---

### 4. Hardware Implementation

Not applicable. LightenUp is a purely web-based software system and does not use any IoT hardware or physical components.

---

### 5. Integration Among Modules

All modules communicate through the shared `ApplicationDbContext`. The main integration flows are:

**A. Authentication → Role Profile Integration**
When a user registers, `AccountController` creates both the `ApplicationUser` (Identity) and the matching role-specific profile (`Patient`, `Psychologist`, or `HrStaff`) in the same transaction. Login reads from both tables to determine where to redirect the user.

**B. Mood/Journal → Health Status Integration**
`MoodController` and `JournalController` write to `MoodTrackers` and `JournalCheckIns`. `HealthStatusService.ComputeAsync()` reads from both tables to compute the patient's classification. The computed status is stored back onto `Patient.MentalHealthStatus` and used by the psychologist's patient list and the HR employee detail page.

**C. Subscription → Feature Gate Integration**
`SubscriptionController` creates the payment transaction and calls `DuitkuService`. The Duitku callback hits `PaymentWebhookController`, which calls `PaymentCompletionService.MarkPaidAsync()`. Once the subscription is activated, `SubscriptionAccessService.HasPatientPremiumAccessAsync()` will return `true`, and the `RequiresPatientPremiumAttribute` filter will let the patient through to premium features like `StatistikController`.

**D. Company → Multi-Role Integration**
When a company's subscription is activated, `PaymentCompletionService` auto-generates the first division with a referral code. Patients use this code during onboarding to link themselves to the company as B2B employees. Psychologists use a separate company referral code to partner with the company. HR manages all company employees and can see aggregated statistics.

---

## B. PRODUCT DISPLAY

### 1. SOFTWARE PRODUCT DISPLAY

*(Insert actual screenshots with figure captions in the final submission. Use the list below as a guide for which screens to capture.)*

#### Authentication and Onboarding Screens

- **Figure B.1 – Login Page**: Email and password form. Shows validation errors for wrong credentials. Admin and customer logins are on separate ports.
- **Figure B.2 – Registration Page**: User selects account type (Patient / Psychologist / HR), fills name and email.
- **Figure B.3 – OTP Verification Page**: User enters OTP code to verify email before proceeding.
- **Figure B.4 – Create Password Page**: Final registration step; password is created here.
- **Figure B.5 – Pending Approval Page**: Shown to Psychologist / HR accounts that completed onboarding but are awaiting admin review.

#### Patient Interface

- **Figure B.6 – Patient Dashboard**: Summary view with health status, upcoming sessions, and quick action cards.
- **Figure B.7 – Mood Wizard (Feeling Step)**: Patient selects current mood from labeled options.
- **Figure B.8 – Mood Wizard (Triggers Step)**: Multi-select for mood triggers.
- **Figure B.9 – Mood Wizard (Questionnaire Step)**: 1–5 rating for each of the 5 wellbeing dimensions.
- **Figure B.10 – Journal Free-Write Page**: Text editor for daily journal entry.
- **Figure B.11 – Journal Check-In Question Page**: Structured 6-question daily check-in form.
- **Figure B.12 – Statistics Page**: Mood trend chart, radar chart, streak counter, and top triggers.
- **Figure B.13 – Subscription Plans Page**: Available plans, current active subscription status, and checkout button.
- **Figure B.14 – Counseling Schedule (Jadwal) Page**: Calendar view of booked sessions.

#### Psychologist Interface

- **Figure B.15 – Psychologist Dashboard**: Active patient list, available patients, and partner companies.
- **Figure B.16 – Patient Detail Page**: Individual patient's mood history, journal, and health status.
- **Figure B.17 – Scheduling Page**: Session calendar and schedule management.
- **Figure B.18 – Worksheet Management Page**: Assigned tasks and submission review.

#### HR Interface

- **Figure B.19 – HR Dashboard**: Company wellness overview and summary stats.
- **Figure B.20 – Employee Management Page**: Employee list, statuses, and detail view.
- **Figure B.21 – HR Reports Page**: Create and view escalation reports.
- **Figure B.22 – Division Statistics Page**: Wellness breakdown by division.
- **Figure B.23 – HR Subscription Page**: Company plan purchase and renewal.

#### Admin Interface

- **Figure B.24 – Admin Dashboard**: Global KPIs, user counts, and pending approvals counter.
- **Figure B.25 – Approvals List Page**: Pending Psychologist and HR accounts filtered by role tab.
- **Figure B.26 – Approval Detail Page**: Full profile review including uploaded documents.
- **Figure B.27 – User Management Page**: User CRUD with role and status display.
- **Figure B.28 – Company Management Page**: Company list and detail view.

#### Error and Edge Case Screens

- **Figure B.29 – Login Error (Wrong Credentials)**: Error message shown below the login form.
- **Figure B.30 – Login Error (Wrong Host)**: Admin trying to log in on the customer site, or vice versa.
- **Figure B.31 – Subscription Required Redirect**: Patient without active subscription accessing a premium feature.
- **Figure B.32 – Form Validation Errors**: Required field warnings shown on mood, journal, or registration forms.

### 2. HARDWARE PRODUCT DISPLAY

Not applicable — this project is a web software system only.

---

## C. COMPONENT COST ANALYSIS

The table below estimates the costs required to run LightenUp in a real production environment for one year. For the demo/capstone environment, most costs are zero since we use localhost and LocalDB.

| No. | Item | Unit | Price per Unit (IDR) | Total (IDR) |
|---|---|:---:|---:|---:|
| 1 | Domain Name (.com, yearly) | 1 | 180,000 | 180,000 |
| 2 | VPS Hosting – 2 vCPU, 4GB RAM (monthly) | 12 months | 250,000 | 3,000,000 |
| 3 | SSL Certificate | 1 year | 0* | 0 |
| 4 | SQL Server – managed/shared hosting (monthly) | 12 months | 300,000 | 3,600,000 |
| 5 | SMTP Email Service – basic tier (monthly) | 12 months | 100,000 | 1,200,000 |
| 6 | File Backup Storage – 50GB (monthly) | 12 months | 75,000 | 900,000 |
| 7 | .NET 8 SDK and Visual Studio Community | – | 0 | 0 |
| 8 | Duitku Payment Gateway Setup | 1 | 0** | 0 |
| | | | **Total** | **8,880,000** |

\* SSL can be obtained for free via Let's Encrypt.  
\*\* Duitku charges transaction fees per payment, not a setup fee.

For the campus demo environment: cost is effectively IDR 0 using localhost, LocalDB, and mock payment mode.

---

## D. FUNCTIONAL TESTING

Each major feature is tested with both positive and negative scenarios. "Output Result" shows whether the actual outcome matched the expected output during our testing.

### Table D.1 – Login Testing

| No. | Scenario | Every Possible Input | Expected Output | Output Result |
|---|---|---|---|---|
| 1 | Login with valid registered user | Correct email + correct password | Redirected to role dashboard | ✓ Pass |
| 2 | Login with wrong password | Correct email + wrong password | Error message shown, stay on login page | ✓ Pass |
| 3 | Login with unregistered email | Unknown email + any password | Error message shown | ✓ Pass |
| 4 | Login with empty fields | Empty email / empty password | Required field validation shown | ✓ Pass |
| 5 | Admin logs in on customer host (port 7040) | Valid admin credentials | Signed out, redirected with error | ✓ Pass |
| 6 | Patient logs in on admin host (port 7041) | Valid patient credentials | Signed out, redirected with error | ✓ Pass |
| 7 | Unapproved Psychologist login | Valid psy credentials, IsApprovedByAdmin = false | Redirected to Pending Approval page | ✓ Pass |

### Table D.2 – Registration Testing

| No. | Scenario | Every Possible Input | Expected Output | Output Result |
|---|---|---|---|---|
| 1 | Register new patient with valid data | Unique email, valid name, valid password | Account created, redirected to success page | ✓ Pass |
| 2 | Register with already used email | Existing email address | Email duplicate error shown | ✓ Pass |
| 3 | Enter wrong OTP code | Code other than "1234" | OTP error shown | ✓ Pass |
| 4 | Enter correct OTP code | "1234" | Proceed to create password step | ✓ Pass |
| 5 | Submit weak password | Password without required complexity | Password validation error shown | ✓ Pass |

### Table D.3 – Mood Tracker Testing

| No. | Scenario | Every Possible Input | Expected Output | Output Result |
|---|---|---|---|---|
| 1 | Submit mood with all steps completed | Valid feeling, triggers, note, 5 questionnaire answers | Mood saved, redirected to dashboard | ✓ Pass |
| 2 | Skip feeling selection | Navigate to next step without selecting mood | Validation error on feeling step | ✓ Pass |
| 3 | Skip trigger selection | Navigate without selecting any trigger | Validation error on triggers step | ✓ Pass |
| 4 | Submit questionnaire with score out of range | Score value < 1 or > 5 | Validation error shown | ✓ Pass |
| 5 | Submit mood for the same day twice | Complete wizard a second time today | Existing record updated, no duplicate row | ✓ Pass |

### Table D.4 – Journal and Check-In Testing

| No. | Scenario | Every Possible Input | Expected Output | Output Result |
|---|---|---|---|---|
| 1 | Submit free-write journal with title and content | Filled title and content fields | Journal saved | ✓ Pass |
| 2 | Submit journal with empty content | Empty content field | Validation error shown | ✓ Pass |
| 3 | Submit check-in with all 6 questions answered | Valid 1–5 scores per question | Check-in saved, success page shown | ✓ Pass |
| 4 | Submit check-in with a score out of range | Any score value not between 1–5 | Validation error shown | ✓ Pass |
| 5 | Submit journal/check-in twice the same day | Second submission same day | First entry updated, not duplicated | ✓ Pass |

### Table D.5 – Subscription and Payment Testing

| No. | Scenario | Every Possible Input | Expected Output | Output Result |
|---|---|---|---|---|
| 1 | Patient selects a plan and initiates checkout | Valid plan ID | Payment transaction created, redirect to Duitku/mock URL | ✓ Pass |
| 2 | Successful mock payment return | Return URL with `?mock=1` flag | Subscription status set to Active | ✓ Pass |
| 3 | Failed payment callback | ResultCode != "00" | Payment status set to "failed", subscription stays Pending | ✓ Pass |
| 4 | B2B patient tries to subscribe individually | Employee under a company with active subscription | Informed company subscription is active, no individual checkout | ✓ Pass |
| 5 | Patient accesses Statistics without subscription | No active subscription | Redirected to subscription plans page | ✓ Pass |
| 6 | Duitku callback with invalid signature | Modified signature field | Webhook returns 401 Unauthorized | ✓ Pass |

### Table D.6 – Scheduling (Jadwal) Testing

| No. | Scenario | Every Possible Input | Expected Output | Output Result |
|---|---|---|---|---|
| 1 | Patient books available session slot | Valid date, time, and psychologist | Schedule record created, shown in dashboard | ✓ Pass |
| 2 | Psychologist views their session history | Logged-in psychologist with existing schedules | Schedule history list displayed | ✓ Pass |
| 3 | Psychologist accesses patient schedule history | Valid patient ID linked to psychologist | Patient's session history shown | ✓ Pass |

### Table D.7 – Worksheet Testing

| No. | Scenario | Every Possible Input | Expected Output | Output Result |
|---|---|---|---|---|
| 1 | Psychologist assigns worksheet to patient | Valid patient, task name, deadline | Worksheet created with "Assigned" status | ✓ Pass |
| 2 | Patient submits completed worksheet | Filled note and/or proof image | Worksheet status updated to InProgress/Completed | ✓ Pass |
| 3 | Patient submits empty worksheet | No content provided | Validation error shown | ✓ Pass |
| 4 | Psychologist gives feedback on submission | Feedback text entered | Feedback saved, status updated | ✓ Pass |

### Table D.8 – Admin Approval Testing

| No. | Scenario | Every Possible Input | Expected Output | Output Result |
|---|---|---|---|---|
| 1 | Admin views pending approvals list | Admin logged in, pending accounts exist | List displayed with role and profile info | ✓ Pass |
| 2 | Admin approves a Psychologist account | Valid pending user ID, optional note | IsApprovedByAdmin set to true, email sent | ✓ Pass |
| 3 | Admin rejects an HR account | Valid pending user ID, optional note | IsActive set to false, email sent | ✓ Pass |
| 4 | Non-admin user tries to access Admin area | Patient/Psy/HR session on admin URL | Access denied (403 or redirect to login) | ✓ Pass |

---

## E. MANUAL GUIDE

### 1. System Build Documentation from the Source (Developer Perspective)

This section explains how to set up and run LightenUp from the source code.

**Prerequisites:**
- .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB (included with Visual Studio) or SQL Server Express
- EF Core CLI tools

**Step 1 – Clone the repository**
```bash
git clone <repository-url>
cd WebsiteLightenUp
```

**Step 2 – Restore and build**
```bash
dotnet restore
dotnet build
```

**Step 3 – Apply database migrations**
```bash
dotnet ef database update
```
This creates the `LightenUpDB` database on `(localdb)\MSSQLLocalDB` and runs all migrations. It also triggers `DbInitializer.SeedAsync()` on the next startup, which creates demo companies, users, and subscriptions automatically.

**Step 4 – Run the application**
```bash
dotnet run --launch-profile https
```

**Step 5 – Access the application**

| Site | URL |
|---|---|
| Customer (Patient / Psychologist / HR) | https://localhost:7040 |
| Admin console | https://localhost:7041/AdminAuth/Login |

**Step 6 – (Optional) Configure real Duitku sandbox credentials**
```bash
dotnet user-secrets set "Duitku:MerchantCode" "YOUR_MERCHANT_CODE"
dotnet user-secrets set "Duitku:ApiKey" "YOUR_API_KEY"
dotnet user-secrets set "Duitku:UseMock" "false"
```
Without this, the app runs in mock payment mode by default — subscriptions activate automatically after checkout without hitting the real Duitku API.

---

### 2. End-User System Installation (User Perspective)

No installation is required for end users. They only need:
- Any modern web browser (Chrome, Firefox, Edge, Safari)
- Stable internet connection
- An account registered on the platform

If deployed to a local company intranet, the IT team provides the URL and accounts are distributed directly.

---

### 3. User Guide per User Role

| User Role | Page | Details |
|---|---|---|
| **Admin** | Admin Login | Log in at port 7041 using admin credentials. Admin accounts cannot log in on port 7040. |
| **Admin** | Dashboard | View total user counts, company counts, and how many accounts are pending approval. |
| **Admin** | Approvals | List of Psychologist and HR accounts awaiting review. Click Detail to see uploaded documents. Click Approve or Reject and optionally add a note — the user is notified by email. |
| **Admin** | User Management | View, search, and manage all user accounts. Can deactivate accounts. |
| **Admin** | Company Management | View registered companies, subscription status, and company details. |
| **Admin** | Invite Admin | Send admin invitation emails to new platform staff. |
| **HR** | Registration & Onboarding | Register at port 7040 selecting "HR" as account type. Complete onboarding steps: profile photo, academic background, company info. Account is then submitted for admin approval. |
| **HR** | HR Dashboard | After approval, view company-level wellness summary and activity stats. |
| **HR** | Employee Management | View list of registered employees (B2B patients). Click a name to see their health status and counseling history. Can also pre-register employees via the pending employee feature. |
| **HR** | Subscription | Purchase or renew the company subscription plan. Once active, a referral code is generated for employees to register with. |
| **HR** | Reports | Create escalation reports to psychologists regarding employee wellbeing. Can also receive reports from psychologists. |
| **Psychologist** | Registration & Onboarding | Register at port 7040 selecting "Psychologist". Complete onboarding: profile photo, academic/license documents, specialization. Account is submitted for admin approval. |
| **Psychologist** | Dashboard | After approval, view assigned patients and available (unassigned) patients. Can take on new patients by clicking Assign. |
| **Psychologist** | Join Company | Use a company's referral code to become a partner psychologist and gain visibility to that company's employees. |
| **Psychologist** | Patient Detail | View a patient's mood history, recent journal entry, and health status. |
| **Psychologist** | Scheduling | Set and manage counseling session schedules. |
| **Psychologist** | Worksheets | Assign tasks to patients, view their submissions, and leave feedback. |
| **Patient** | Registration & Onboarding | Register at port 7040 selecting "Patient". Patient accounts are auto-approved. Complete the 14-step onboarding survey before accessing the dashboard. |
| **Patient** | Dashboard | View health status, upcoming sessions, and quick-action buttons for mood tracking and journal. |
| **Patient** | Mood Tracker | Click the mood button and go through the wizard: select feeling → select triggers → add optional note → answer 5 wellbeing questions → review summary → save. Can re-do this once per day (edits the existing entry). |
| **Patient** | Journal | Write a free-form daily journal or complete the structured 6-question check-in. Both update the health status calculation. |
| **Patient** | Statistics (Premium) | View mood trend chart, radar chart across 5 dimensions, day streak, and top triggers. Requires active subscription. |
| **Patient** | Jadwal | View and book counseling sessions with an assigned psychologist. |
| **Patient** | Subscription | Browse plans (Basic Monthly, Premium Monthly, Premium Yearly), select one, and go through Duitku checkout. B2B employees covered by their company's subscription can skip this. |
| **Patient** | Profile | Edit personal information, profile picture, and emergency contact. View current mental health status and subscription status. |

---

## REFERENCES

1. Microsoft, "ASP.NET Core MVC documentation," *Microsoft Learn*. [Online]. Available: https://learn.microsoft.com/aspnet/core/mvc
2. Microsoft, "ASP.NET Core Identity documentation," *Microsoft Learn*. [Online]. Available: https://learn.microsoft.com/aspnet/core/security/authentication/identity
3. Microsoft, "Entity Framework Core documentation," *Microsoft Learn*. [Online]. Available: https://learn.microsoft.com/ef/core
4. Microsoft, "Fluent API — configuring relationships," *Microsoft Learn*. [Online]. Available: https://learn.microsoft.com/ef/core/modeling/relationships
5. Duitku, "Duitku Payment Gateway API Documentation." [Online]. Available: https://docs.duitku.com
6. Bootstrap Team, "Bootstrap 5 Documentation." [Online]. Available: https://getbootstrap.com/docs/5.0
7. jQuery Foundation, "jQuery API Documentation." [Online]. Available: https://api.jquery.com
8. Microsoft, "Action filters in ASP.NET Core," *Microsoft Learn*. [Online]. Available: https://learn.microsoft.com/aspnet/core/mvc/controllers/filters

---

*Replace all placeholder names, NIMs, and advisor fields before submission. Insert actual screenshots in Section B and the ZeroGPT result in the designated section.*
