using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
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

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _config = config;
        }

        // ==========================================
        // 1. HALAMAN LOGIN
        // ==========================================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    // Cek khusus untuk Role Psychologist
                    if (user.RoleType == "Psychologist")
                    {
                        // Ambil data detail psikolog dari database
                        var psychData = _context.Psychologists.FirstOrDefault(p => p.UserId == user.Id);

                        // LOGIKA 1: Cek apakah sudah mengisi onboarding (Patokannya LicenseNumber/SIPP)
                        bool hasCompletedOnboarding = psychData != null && !string.IsNullOrEmpty(psychData.LicenseNumber);

                        if (!hasCompletedOnboarding)
                        {
                            // Belum isi biodata onboarding -> Izinkan login sementara, lempar ke Welcome Onboarding
                            var loginOnboardingResult = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                            if (loginOnboardingResult.Succeeded)
                            {
                                return RedirectToAction("Welcome", "Onboarding");
                            }
                        }
                        // LOGIKA 2: Sudah isi biodata TAPI belum di-approve HR -> TOLAK LOGIN
                        else if (!user.IsApprovedByHR)
                        {
                            ModelState.AddModelError(string.Empty, "Biodata sudah diterima. Silakan tunggu persetujuan dari HR sebelum dapat mengakses Dashboard.");
                            return View(model);
                        }
                    }

                    // Jika Patient, Admin, atau Psikolog yang SUDAH isi data & SUDAH di-approve
                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false
                    );

                    if (result.Succeeded)
                    {
                        // ─── Cross-host guard ───
                        // HR users must log in on the HR host; everyone else on the patient host.
                        var hrHost = _config["Site:HrHost"];
                        var patientHost = _config["Site:PatientHost"];
                        var currentHost = HttpContext.Request.Host.ToString();
                        bool isHrAccount = await _userManager.IsInRoleAsync(user, "HR") || user.RoleType == "HR";
                        bool onHrHost = !string.IsNullOrEmpty(hrHost) && currentHost.Equals(hrHost, StringComparison.OrdinalIgnoreCase);
                        bool onPatientHost = !string.IsNullOrEmpty(patientHost) && currentHost.Equals(patientHost, StringComparison.OrdinalIgnoreCase);

                        if (isHrAccount && onPatientHost && !string.IsNullOrEmpty(hrHost))
                        {
                            await _signInManager.SignOutAsync();
                            ModelState.AddModelError(string.Empty,
                                $"Akun HR harus login melalui situs HR. Buka https://{hrHost}/ kemudian masuk lagi.");
                            return View(model);
                        }
                        if (!isHrAccount && onHrHost && !string.IsNullOrEmpty(patientHost))
                        {
                            await _signInManager.SignOutAsync();
                            ModelState.AddModelError(string.Empty,
                                $"Akun ini bukan akun HR. Silakan login di https://{patientHost}/");
                            return View(model);
                        }

                        // Admin → Admin dashboard
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }

                        // HR → resume onboarding if incomplete, else home
                        if (isHrAccount)
                        {
                            var hr = _context.HrStaffs.FirstOrDefault(h => h.UserId == user.Id);
                            if (hr == null || hr.OnboardingCompletedAt == null)
                            {
                                return RedirectToAction("Welcome", "Onboarding", new { area = "Hr" });
                            }
                            return RedirectToAction("Index", "Home", new { area = "Hr" });
                        }

                        // Patient → resume onboarding if incomplete, else dashboard
                        if (user.RoleType == "Patient")
                        {
                            var patient = _context.Patients.FirstOrDefault(p => p.UserId == user.Id);
                            if (patient == null || patient.OnboardingCompletedAt == null)
                            {
                                return RedirectToAction("Welcome", "Onboarding", new { area = "Patient" });
                            }
                            return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
                        }

                        // Psychologist → existing flow (root Psychologist controller)
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
                    IsApprovedByHR = (registerData.AccountType == "Patient")
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
    }
}