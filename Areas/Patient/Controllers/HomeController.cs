using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LightenUp.Web.Areas.Patient.Controllers
{
    // Placeholder root controller for the Patient area.
    // Real controllers (Dashboard, Mood, Journal, Tasks, Profile, Statistik, Onboarding)
    // land in this same folder in later turns.
    [Area("Patient")]
    // #Class HomeController#
    [Authorize(Roles = "Patient")]
    public class HomeController : Controller
    {
        // #Function Index#
        public IActionResult Index()
        {
            // For now redirect to the eventual dashboard. The dashboard view comes next.
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
