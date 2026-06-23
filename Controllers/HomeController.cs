using System.Diagnostics;
using LightenUp.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace LightenUp.Web.Controllers
{
    // #Class HomeController#
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // #Function Index#
        public IActionResult Index()
        {
            return RedirectToAction("Login", "Account");
        }

        // #Function Privacy#
        public IActionResult Privacy()
        {
            return View();
        }

        // #Function Error#
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
