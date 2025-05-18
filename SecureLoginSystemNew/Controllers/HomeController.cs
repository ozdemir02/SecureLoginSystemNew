using Microsoft.AspNetCore.Mvc;
using SecureLoginSystemNew.Models;
using SecureLoginSystemNew.ViewModels;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace SecureLoginSystemNew.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Error(int? statusCode = null)
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            _logger.LogError(exception, "Error occurred (Request ID: {RequestId})",
                Activity.Current?.Id ?? HttpContext.TraceIdentifier);

            var errorVm = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ExceptionMessage = User.IsInRole("Admin") ? exception?.ToString() : null,
                StatusCode = statusCode
            };

            if (statusCode.HasValue)
            {
                Response.StatusCode = statusCode.Value;
                errorVm.ErrorMessage = statusCode switch
                {
                    404 => "The requested page was not found",
                    403 => "You are not authorized to view this page",
                    500 => "An internal server error occurred",
                    _ => "An unexpected error occurred"
                };
            }

            return View(errorVm);
        }

        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return View();
        }
    }
}