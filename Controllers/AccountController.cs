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

namespace LightenUp.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly UserUploadService _uploadService;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, IConfiguration config, UserUploadService uploadService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _config = config;
            _uploadService = uploadService;
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
                    if (isHr) return RedirectToAction("Index", "Dashboard", new { area = "Hr" });
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
                        // ─── Cross-host guard ───
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
                        return RedirectToAction("Index", "Psychologist");
                    }
                }

                ModelState.AddModelError(string.Empty, "Email atau Kata Sandi salah.");
            }

            return View(model);
        }

        // ==========================================
        // 2. HALAMAN REGISTER
        // ==========================================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
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
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Register");

            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost]
        public IActionResult VerifyEmail(VerifyOtpViewModel model)
        {
            TempData.Keep("RegisterData");

            if (ModelState.IsValid)
            {
                if (model.OtpCode == "1234")
                {
                    return RedirectToAction("CreatePassword", new { email = model.Email });
                }

                ModelState.AddModelError("OtpCode", "Kode OTP salah. Ketik '1234'.");
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

                        // Default notification preferences for the new patient
                        _context.PatientNotificationPreferences.Add(new PatientNotificationPreference
                        {
                            PatientId = newPatient.PatientId
                            // Other fields use model defaults (all toggles on, 09:00)
                        });
                    }
                    else if (registerData.AccountType == "Psychologist")
                    {
                        _context.Psychologists.Add(new Psychologist { UserId = user.Id });
                    }
                    else if (registerData.AccountType == "HR")
                    {
                        _context.HrStaffs.Add(new HrStaff { UserId = user.Id });
                        // HrNotificationPreference is created during onboarding completion.
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
                    catch
                    {
                        // Non-fatal: if folder creation fails, continue registration flow.
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
    }
}