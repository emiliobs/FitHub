using FitHub.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FitHub.Web.Controllers
{
    // Handles main public pages and system errors
    public class HomeController : Controller
    {
        // GET: Show home page
        public IActionResult Index()
        {
            return View();
        }

        // GET: Show about page
        public IActionResult About()
        {
            return View();
        }

        // GET: Show pricing page
        public IActionResult Pricing()
        {
            return View();
        }

        // GET: Redirect users based on login status
        public IActionResult GetStarted()
        {
            // Go to classes if logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "FitnessClasses");
            }

            // Go to pricing if guest
            return RedirectToAction(nameof(Pricing));
        }

        // GET: Handle specific HTTP error codes (like 404 or 500)
        [Route("/Home/HandleError/{code}")]
        public IActionResult HandleError(int code)
        {
            // Set custom messages based on the error code
            switch (code)
            {
                case 404: // Page not found
                    ViewData["ErrorMessage"] = "THE SECTOR YOU ARE LOOKING FOR DOES NOT EXIST.";
                    ViewData["ErrorIcon"] = "bi-geo-alt-fill";
                    break;

                case 500: // Server error
                    ViewData["ErrorMessage"] = "INTERNAL SYSTEM OVERLOAD. TRY AGAIN LATER.";
                    ViewData["ErrorIcon"] = "bi-exclamation-triangle-fill";
                    break;

                default: // Other errors
                    ViewData["ErrorMessage"] = "AN UNKNOWN ERROR HAS OCCURRED IN THE ARENA.";
                    ViewData["ErrorIcon"] = "bi-shield-exclamation";
                    break;
            }

            // Show the custom error view
            return View("ErrorPage");
        }

        // GET: Show default system error page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Display error details for debugging (RequestId)
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}