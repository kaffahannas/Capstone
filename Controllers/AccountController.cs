using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;

namespace LightenUp.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
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
                    // Psychologist belum di-approve
                    if (user.RoleType == "Psychologist" && !user.IsApprovedByHR)
                    {
                        ModelState.AddModelError(string.Empty, "Akun Anda sedang ditinjau. Silakan tunggu persetujuan dari HR sebelum dapat masuk.");
                        return View(model);
                    }

                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false
                    );

                    if (result.Succeeded)
                    {
                        // 🔥 ADMIN CHECK
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }

                        return RedirectToAction("Index", "Home");
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
                if (model.AccountType != "Patient" && model.AccountType != "Psychologist")
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
                        _context.Patients.Add(new Patient { UserId = user.Id });
                    }
                    else if (registerData.AccountType == "Psychologist")
                    {
                        _context.Psychologists.Add(new Psychologist { UserId = user.Id });
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
    }
}