using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json; // Tambahan untuk mengubah data menjadi format JSON
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
                // CEK STATUS APPROVAL SEBELUM LOGIN
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // Jika dia adalah Psychologist dan belum di-approve oleh HR
                    if (user.RoleType == "Psychologist" && !user.IsApprovedByHR)
                    {
                        ModelState.AddModelError(string.Empty, "Akun Anda sedang ditinjau. Silakan tunggu persetujuan dari HR sebelum dapat masuk.");
                        return View(model);
                    }
                }

                // Jika aman (Patient, atau Psychologist yang sudah di-approve), lanjutkan proses login
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded) return RedirectToAction("Index", "Home");

                ModelState.AddModelError(string.Empty, "Email atau Kata Sandi salah.");
            }
            return View(model);
        }

        // ==========================================
        // 2. HALAMAN REGISTER (Simpan Sementara)
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

                // PERUBAHAN PENTING:
                // SIMPAN SEMENTARA KE TEMPDATA (TIDAK LANGSUNG MASUK DATABASE)
                TempData["RegisterData"] = JsonSerializer.Serialize(model);
                return RedirectToAction("VerifyEmail", new { email = model.Email });
            }
            return View(model);
        }

        // ==========================================
        // 3. HALAMAN VERIFIKASI EMAIL (OTP)
        // ==========================================
        [HttpGet]
        public IActionResult VerifyEmail(string email)
        {
            TempData.Keep("RegisterData"); // Pertahankan data register di memori
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Register");
            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost]
        public IActionResult VerifyEmail(VerifyOtpViewModel model)
        {
            TempData.Keep("RegisterData"); // Pertahankan data register untuk langkah selanjutnya
            if (ModelState.IsValid)
            {
                // Simulasi OTP Benar
                if (model.OtpCode == "1234")
                {
                    return RedirectToAction("CreatePassword", new { email = model.Email });
                }
                ModelState.AddModelError("OtpCode", "Kode OTP salah. Ketik '1234'.");
            }
            return View(model);
        }

        // ==========================================
        // 4. HALAMAN BUAT KATA SANDI & SIMPAN KE DB
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
            // Ambil kembali data pendaftaran dari memori sementara
            var registerDataJson = TempData["RegisterData"] as string;

            // Jika pengguna mengakses halaman ini tanpa lewat register / refresh terlalu lama
            if (string.IsNullOrEmpty(registerDataJson)) return RedirectToAction("Register");

            if (ModelState.IsValid)
            {
                // Baca kembali data pendaftaran
                var registerData = JsonSerializer.Deserialize<PublicRegisterViewModel>(registerDataJson);

                var user = new ApplicationUser
                {
                    UserName = registerData.Email,
                    Email = registerData.Email,
                    EmailConfirmed = true, // <--- TAMBAHAN PENTING AGAR BISA LOGIN
                    FullName = registerData.FullName,
                    RoleType = registerData.AccountType,
                    IsApprovedByHR = (registerData.AccountType == "Patient")
                };

                // SEKARANG BARU KITA BUAT USER BESERTA PASSWORDNYA DI DATABASE!
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Masukkan juga ke tabel spesifik (Patients / Psychologists)
                    if (registerData.AccountType == "Patient")
                    {
                        _context.Patients.Add(new Patient { UserId = user.Id });
                    }
                    else if (registerData.AccountType == "Psychologist")
                    {
                        _context.Psychologists.Add(new Psychologist { UserId = user.Id });
                    }

                    await _context.SaveChangesAsync();

                    // Setelah sukses, pindah ke halaman notifikasi berhasil
                    return RedirectToAction("RegistrationSuccess");
                }

                // Jika ada error (misal password kurang rumit), tampilkan errornya
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            TempData.Keep("RegisterData"); // Pertahankan jika ada error validasi
            return View(model);
        }

        // ==========================================
        // 5. HALAMAN SUKSES PENDAFTARAN
        // ==========================================
        [HttpGet]
        public IActionResult RegistrationSuccess()
        {
            return View();
        }
    }
}