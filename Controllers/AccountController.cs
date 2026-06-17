using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using LightenUp.Web.Services;
using System.Threading.Tasks;
using System.Linq; // Tambahan untuk memanipulasi data database (FirstOrDefault)
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace LightenUp.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly UserUploadService _uploadService;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailSender _emailSender;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, IConfiguration config, UserUploadService uploadService, ILogger<AccountController> logger, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _config = config;
            _uploadService = uploadService;
            _logger = logger;
            _emailSender = emailSender;
        }

        // ==========================================
        // 1. HALAMAN LOGIN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin") || user.RoleType == "Admin";
                    if (isAdmin) return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    bool isHr = await _userManager.IsInRoleAsync(user, "HR") || user.RoleType == "HR";
                    if (isHr) return RedirectToAction("Index", "Home", new { area = "Hr" });
                    bool isPsy = await _userManager.IsInRoleAsync(user, "Psychologist") || user.RoleType == "Psychologist";
                    if (isPsy) return RedirectToAction("Index", "Dashboard", new { area = "Psychologist" });
                    return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
                }
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AccessDenied()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.UserRole = user.RoleType;
                }
            }
            return View();
        }

        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User {Email} logged in successfully.", model.Email);
                        // Admin accounts may only log in on the Admin host.
                        // Customer accounts (Patient/Psychologist/HR) may only log in on the customer host.
                        var adminHost = _config["Site:AdminHost"];
                        var patientHost = _config["Site:PatientHost"];
                        var currentHost = HttpContext.Request.Host.ToString();
                        bool isAdminAccount = await _userManager.IsInRoleAsync(user, "Admin") || user.RoleType == "Admin";
                        bool onAdminHost = !string.IsNullOrEmpty(adminHost) && currentHost.Equals(adminHost, StringComparison.OrdinalIgnoreCase);
                        bool onCustomerHost = !string.IsNullOrEmpty(patientHost) && currentHost.Equals(patientHost, StringComparison.OrdinalIgnoreCase);

                        if (isAdminAccount && onCustomerHost && !string.IsNullOrEmpty(adminHost))
                        {
                            await _signInManager.SignOutAsync();
                            ModelState.AddModelError(string.Empty,
                                $"Akun Admin harus login di https://{adminHost}/");
                            return View(model);
                        }
                        if (!isAdminAccount && onAdminHost && !string.IsNullOrEmpty(patientHost))
                        {
                            await _signInManager.SignOutAsync();
                            ModelState.AddModelError(string.Empty,
                                $"Akun ini bukan akun Admin. Silakan login di https://{patientHost}/");
                            return View(model);
                        }

                        // ─── Approval gate (Psychologist only; HR is now auto-approved) ───
                        bool isPsy = await _userManager.IsInRoleAsync(user, "Psychologist") || user.RoleType == "Psychologist";
                        bool isHr = await _userManager.IsInRoleAsync(user, "HR") || user.RoleType == "HR";

                        if (isPsy && !user.IsApprovedByAdmin)
                        {
                            var psy = _context.Psychologists.FirstOrDefault(p => p.UserId == user.Id);
                            bool onboardingDone = psy != null && !string.IsNullOrEmpty(psy.LicenseNumber);
                            if (!onboardingDone)
                                return RedirectToAction("Welcome", "Onboarding");
                            return RedirectToAction("PendingApproval");
                        }

                        // ─── Role-based dashboard routing ───
                        if (isAdminAccount)
                            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

                        if (isHr)
                        {
                            var hr = _context.HrStaffs.FirstOrDefault(h => h.UserId == user.Id);
                            if (hr == null || hr.OnboardingCompletedAt == null)
                                return RedirectToAction("Welcome", "Onboarding", new { area = "Hr" });
                            // HR must subscribe before accessing dashboard.
                            if (hr.CompanyId == null)
                                return RedirectToAction("Index", "Subscription", new { area = "Hr" });
                            return RedirectToAction("Index", "Home", new { area = "Hr" });
                        }

                        if (user.RoleType == "Patient")
                        {
                            var patient = _context.Patients.FirstOrDefault(p => p.UserId == user.Id);
                            if (patient == null || patient.OnboardingCompletedAt == null)
                                return RedirectToAction("Welcome", "Onboarding", new { area = "Patient" });
                            return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
                        }

                        // Psychologist
                        return RedirectToAction("Index", "Dashboard", new { area = "Psychologist" });
                    }
                }

                ModelState.AddModelError(string.Empty, "Email atau Kata Sandi salah.");
                _logger.LogWarning("Failed login attempt for {Email}.", model.Email);
            }

            return View(model);
        }

        // ==========================================
        // 2. HALAMAN REGISTER
        // ==========================================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register(PublicRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.AccountType != "Patient" && model.AccountType != "Psychologist" && model.AccountType != "HR")
                {
                    ModelState.AddModelError("", "Jenis akun tidak valid.");
                    return View(model);
                }

                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email ini sudah digunakan.");
                    return View(model);
                }

                string otp = new Random().Next(1000, 9999).ToString();
                TempData["ExpectedOtp"] = otp;

                try
                {
                    await _emailSender.SendAsync(model.Email, "Kode Verifikasi LightenUp", $"Kode OTP Anda adalah: {otp}. Jangan berikan kode ini kepada siapa pun.");
                    _logger.LogInformation("Sent OTP {Otp} to {Email}", otp, model.Email); // Logs to console/file, good for dev debugging
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email OTP to {Email}", model.Email);
                    // Continue anyway during dev to allow backdoor "1234" to work, or we could return error.
                }

                TempData["RegisterData"] = JsonSerializer.Serialize(model);
                return RedirectToAction("VerifyEmail", new { email = model.Email });
            }

            return View(model);
        }

        // ==========================================
        // 3. VERIFIKASI EMAIL (OTP)
        // ==========================================
        [HttpGet]
        public IActionResult VerifyEmail(string email)
        {
            TempData.Keep("RegisterData");
            TempData.Keep("ExpectedOtp");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Register");

            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost]
        public IActionResult VerifyEmail(VerifyOtpViewModel model)
        {
            TempData.Keep("RegisterData");
            TempData.Keep("ExpectedOtp");

            if (ModelState.IsValid)
            {
                var expected = TempData["ExpectedOtp"]?.ToString();
                
                // Allow "1234" as dev backdoor, or the real OTP
                if (model.OtpCode == "1234" || (!string.IsNullOrEmpty(expected) && model.OtpCode == expected))
                {
                    return RedirectToAction("CreatePassword", new { email = model.Email });
                }

                ModelState.AddModelError("OtpCode", "Kode OTP salah.");
            }

            return View(model);
        }

        // ==========================================
        // 4. BUAT PASSWORD & SIMPAN KE DB
        // ==========================================
        [HttpGet]
        public IActionResult CreatePassword(string email)
        {
            TempData.Keep("RegisterData");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Register");

            return View(new CreatePasswordViewModel { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePassword(CreatePasswordViewModel model)
        {
            var registerDataJson = TempData["RegisterData"] as string;

            if (string.IsNullOrEmpty(registerDataJson))
                return RedirectToAction("Register");

            if (ModelState.IsValid)
            {
                var registerData = JsonSerializer.Deserialize<PublicRegisterViewModel>(registerDataJson);

                if (registerData == null || string.IsNullOrEmpty(registerData.Email))
                {
                    return RedirectToAction("Register");
                }

                var user = new ApplicationUser
                {
                    UserName = registerData.Email,
                    Email = registerData.Email,
                    EmailConfirmed = true,
                    FullName = registerData.FullName,
                    RoleType = registerData.AccountType,
                    // HR no longer requires admin approval; they are gated by company subscription instead.
                    IsApprovedByAdmin = (registerData.AccountType == "Patient" || registerData.AccountType == "HR")
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // 🔥 ASSIGN ROLE
                    await _userManager.AddToRoleAsync(user, registerData.AccountType);

                    if (registerData.AccountType == "Patient")
                    {
                        var newPatient = new Patient { UserId = user.Id };
                        _context.Patients.Add(newPatient);
                        await _context.SaveChangesAsync();


                    }
                    else if (registerData.AccountType == "Psychologist")
                    {
                        _context.Psychologists.Add(new Psychologist { UserId = user.Id });
                    }
                    else if (registerData.AccountType == "HR")
                    {
                        _context.HrStaffs.Add(new HrStaff { UserId = user.Id });
                    }

                    await _context.SaveChangesAsync();

                    try
                    {
                        var accountFolder = _uploadService.GetAccountFolderPath(user.Id);
                        Directory.CreateDirectory(accountFolder);

                        var meta = new
                        {
                            user.Id,
                            user.FullName,
                            user.Email,
                            user.RoleType,
                            CreatedAt = DateTime.UtcNow
                        };
                        var metaPath = Path.Combine(accountFolder, "meta.json");
                        await System.IO.File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    catch (Exception ex)
                    {
                        // Non-fatal: if folder creation fails, continue registration flow.
                        _logger.LogWarning(ex, "Failed to create account folder for user {UserId}.", user.Id);
                    }

                    return RedirectToAction("RegistrationSuccess");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            TempData.Keep("RegisterData");
            return View(model);
        }

        // ==========================================
        // 5. SUCCESS PAGE
        // ==========================================
        [HttpGet]
        public IActionResult RegistrationSuccess()
        {
            return View();
        }

        // ==========================================
        // 6. LOGOUT
        // ==========================================
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // ==========================================
        // 7. PENDING APPROVAL (Psy + HR waiting for Admin review)
        // ==========================================
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> PendingApproval()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            // If they're now approved (or never needed it), bounce them to the right place
            if (user.IsApprovedByAdmin)
            {
                await _signInManager.SignOutAsync();
                TempData["info"] = "Akun Anda sudah disetujui. Silakan login kembali.";
                return RedirectToAction("Login");
            }

            ViewBag.FullName = user.FullName;
            ViewBag.RoleType = user.RoleType;
            return View();
        }

        // ==========================================
        // 8. EXTERNAL LOGIN (Google / Facebook)
        // ==========================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult ExternalLogin(string provider, string? flow, string? fullName, string? accountType, string? returnUrl = null)
        {
            // Validate register flow data before redirecting to provider
            if (flow == "register")
            {
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(accountType))
                {
                    TempData["ExternalError"] = "Silakan isi Nama Lengkap dan pilih Jenis Akun terlebih dahulu.";
                    return RedirectToAction("Register");
                }
                if (accountType != "Patient" && accountType != "Psychologist" && accountType != "HR")
                {
                    TempData["ExternalError"] = "Jenis akun tidak valid.";
                    return RedirectToAction("Register");
                }
                TempData["ExternalFlow"] = "register";
                TempData["ExternalFullName"] = fullName;
                TempData["ExternalAccountType"] = accountType;
            }
            else
            {
                TempData.Remove("ExternalFlow");
                TempData.Remove("ExternalFullName");
                TempData.Remove("ExternalAccountType");
            }
            
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            var flow = TempData["ExternalFlow"]?.ToString();
            var extFullName = TempData["ExternalFullName"]?.ToString();
            var extAccountType = TempData["ExternalAccountType"]?.ToString();

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ExternalError"] = "Gagal mendapatkan informasi dari provider. Silakan coba lagi.";
                return RedirectToAction("Login");
            }

            // 1. Get email and check if user exists (to prevent register flow from logging in)
            var email = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                TempData["ExternalError"] = "Tidak bisa mendapatkan email dari akun " + info.LoginProvider + ". Pastikan email Anda dapat diakses.";
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByEmailAsync(email);

            // Reject register flow if email is already in the system
            if (user != null && flow == "register")
            {
                await _signInManager.SignOutAsync();
                TempData["ExternalError"] = "Akun dengan email ini sudah terdaftar. Silakan langsung login atau gunakan email lain.";
                return RedirectToAction("Register");
            }

            // 2. Try sign in with this external login (already linked)
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                _logger.LogInformation("User logged in via {Provider}.", info.LoginProvider);
                // Route using same logic as manual login
                var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (existingUser != null)
                    return await RouteAfterLogin(existingUser);
                return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
            }

            // 3. Not linked yet — User exists (login flow)
            if (user != null)
            {
                // User exists — link external login and sign in (LOGIN FLOW ONLY)
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("Linked {Provider} to existing user {Email}.", info.LoginProvider, email);
                    return await RouteAfterLogin(user);
                }

                _logger.LogWarning("Failed to link {Provider} to {Email}: {Errors}",
                    info.LoginProvider, email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                await _signInManager.SignOutAsync();
                TempData["ExternalError"] = "Gagal menghubungkan akun " + info.LoginProvider + ". Silakan coba lagi.";
                return RedirectToAction("Login");
            }

            // 3. User does not exist
            if (flow != "register" || string.IsNullOrWhiteSpace(extFullName) || string.IsNullOrWhiteSpace(extAccountType))
            {
                // From Login page — don't create account
                await _signInManager.SignOutAsync();
                TempData["ExternalError"] = "Akun dengan email tersebut belum terdaftar. Silakan daftar terlebih dahulu.";
                return RedirectToAction("Login");
            }

            // 4. From Register page — create new user
            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = extFullName,
                RoleType = extAccountType,
                IsApprovedByAdmin = (extAccountType == "Patient" || extAccountType == "HR")
            };

            var createResult = await _userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning("Failed to create user {Email} via {Provider}: {Errors}",
                    email, info.LoginProvider, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                TempData["ExternalError"] = "Gagal membuat akun: " + string.Join(", ", createResult.Errors.Select(e => e.Description));
                return RedirectToAction("Register");
            }

            await _userManager.AddToRoleAsync(newUser, extAccountType);
            await _userManager.AddLoginAsync(newUser, info);

            // Create role-specific record
            if (extAccountType == "Patient")
            {
                _context.Patients.Add(new Patient { UserId = newUser.Id });
            }
            else if (extAccountType == "Psychologist")
            {
                _context.Psychologists.Add(new Psychologist { UserId = newUser.Id });
            }
            else if (extAccountType == "HR")
            {
                _context.HrStaffs.Add(new HrStaff { UserId = newUser.Id });
            }
            await _context.SaveChangesAsync();

            // Create account folder (non-fatal)
            try
            {
                var accountFolder = _uploadService.GetAccountFolderPath(newUser.Id);
                Directory.CreateDirectory(accountFolder);

                var meta = new
                {
                    newUser.Id,
                    newUser.FullName,
                    newUser.Email,
                    newUser.RoleType,
                    CreatedAt = DateTime.UtcNow
                };
                var metaPath = Path.Combine(accountFolder, "meta.json");
                await System.IO.File.WriteAllTextAsync(metaPath,
                    JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create account folder for user {UserId}.", newUser.Id);
            }

            _logger.LogInformation("Created new user {Email} via {Provider} as {Role}.", email, info.LoginProvider, extAccountType);
            
            TempData["SuccessMessage"] = "Pendaftaran berhasil! Silakan masuk menggunakan tombol " + info.LoginProvider + " di bawah.";
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Shared post-login routing logic — same rules as the manual Login POST.
        /// </summary>
        private async Task<IActionResult> RouteAfterLogin(ApplicationUser user)
        {
            var adminHost = _config["Site:AdminHost"];
            var patientHost = _config["Site:PatientHost"];
            var currentHost = HttpContext.Request.Host.ToString();
            bool isAdminAccount = await _userManager.IsInRoleAsync(user, "Admin") || user.RoleType == "Admin";
            bool onAdminHost = !string.IsNullOrEmpty(adminHost) && currentHost.Equals(adminHost, StringComparison.OrdinalIgnoreCase);
            bool onCustomerHost = !string.IsNullOrEmpty(patientHost) && currentHost.Equals(patientHost, StringComparison.OrdinalIgnoreCase);

            if (isAdminAccount && onCustomerHost && !string.IsNullOrEmpty(adminHost))
            {
                await _signInManager.SignOutAsync();
                TempData["ExternalError"] = $"Akun Admin harus login di https://{adminHost}/";
                return RedirectToAction("Login");
            }
            if (!isAdminAccount && onAdminHost && !string.IsNullOrEmpty(patientHost))
            {
                await _signInManager.SignOutAsync();
                TempData["ExternalError"] = $"Akun ini bukan akun Admin. Silakan login di https://{patientHost}/";
                return RedirectToAction("Login");
            }

            if (isAdminAccount)
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            bool isPsy = await _userManager.IsInRoleAsync(user, "Psychologist") || user.RoleType == "Psychologist";
            bool isHr = await _userManager.IsInRoleAsync(user, "HR") || user.RoleType == "HR";

            if (isPsy && !user.IsApprovedByAdmin)
            {
                var psy = _context.Psychologists.FirstOrDefault(p => p.UserId == user.Id);
                bool onboardingDone = psy != null && !string.IsNullOrEmpty(psy.LicenseNumber);
                if (!onboardingDone)
                    return RedirectToAction("Welcome", "Onboarding");
                return RedirectToAction("PendingApproval");
            }

            if (isHr)
            {
                var hr = _context.HrStaffs.FirstOrDefault(h => h.UserId == user.Id);
                if (hr == null || hr.OnboardingCompletedAt == null)
                    return RedirectToAction("Welcome", "Onboarding", new { area = "Hr" });
                if (hr.CompanyId == null)
                    return RedirectToAction("Index", "Subscription", new { area = "Hr" });
                return RedirectToAction("Index", "Home", new { area = "Hr" });
            }

            if (user.RoleType == "Patient")
            {
                var patient = _context.Patients.FirstOrDefault(p => p.UserId == user.Id);
                if (patient == null || patient.OnboardingCompletedAt == null)
                    return RedirectToAction("Welcome", "Onboarding", new { area = "Patient" });
                return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
            }

            // Psychologist (approved)
            return RedirectToAction("Index", "Dashboard", new { area = "Psychologist" });
        }

        // ==========================================
        // 9. CHANGE/SET PASSWORD
        // ==========================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            
            bool hasPassword = await _userManager.HasPasswordAsync(user);
            return View(new ChangePasswordViewModel { HasPassword = hasPassword });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            model.HasPassword = await _userManager.HasPasswordAsync(user);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            IdentityResult result;
            if (model.HasPassword)
            {
                if (string.IsNullOrEmpty(model.OldPassword))
                {
                    ModelState.AddModelError("OldPassword", "Password lama wajib diisi.");
                    return View(model);
                }
                result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            }
            else
            {
                result = await _userManager.AddPasswordAsync(user, model.NewPassword);
            }

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Password berhasil diperbarui.";

                if (await _userManager.IsInRoleAsync(user, "Admin") || user.RoleType == "Admin")
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                if (await _userManager.IsInRoleAsync(user, "Patient") || user.RoleType == "Patient")
                    return RedirectToAction("Index", "Profile", new { area = "Patient" });
                if (await _userManager.IsInRoleAsync(user, "Psychologist") || user.RoleType == "Psychologist")
                    return RedirectToAction("Profile", "Profile", new { area = "Psychologist" });
                if (await _userManager.IsInRoleAsync(user, "HR") || user.RoleType == "HR")
                    return RedirectToAction("Index", "Profile", new { area = "Hr" });

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}