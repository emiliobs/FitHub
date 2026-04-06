using FitHub.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FitHub.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Pricing()
        {
            return View();
        }

        public IActionResult GetStarted()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "FitnessClasses");
            }

            return RedirectToAction(nameof(Pricing));
        }

        [Route("/Home/HandleError/{code}")]
        public IActionResult HandleError(int code)
        {
            // Logic: We categorize the error to show a specific message
            switch (code)
            {
                case 404:
                    ViewData["ErrorMessage"] = "THE SECTOR YOU ARE LOOKING FOR DOES NOT EXIST.";
                    ViewData["ErrorIcon"] = "bi-geo-alt-fill";
                    break;

                case 500:
                    ViewData["ErrorMessage"] = "INTERNAL SYSTEM OVERLOAD. TRY AGAIN LATER.";
                    ViewData["ErrorIcon"] = "bi-exclamation-triangle-fill";
                    break;

                default:
                    ViewData["ErrorMessage"] = "AN UNKNOWN ERROR HAS OCCURRED IN THE ARENA.";
                    ViewData["ErrorIcon"] = "bi-shield-exclamation";
                    break;
            }

            return View("ErrorPage"); // We will create this view
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}