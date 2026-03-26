using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniTestSystem.Domain;
using UniTestSystem.ViewModels;

namespace UniTestSystem.Controllers
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
            if (User?.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirstValue(ClaimTypes.Role);
                return role switch
                {
                    nameof(Role.Admin) => RedirectToAction("Dashboard", "Admin"),
                    nameof(Role.Staff) => RedirectToAction("Dashboard", "Staff"),
                    nameof(Role.Lecturer) => RedirectToAction("Dashboard", "Lecturer"),
                    nameof(Role.Student) => RedirectToAction("Index", "MyTests"),
                    _ => View()
                };
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("Home/Error/{statusCode?}")]
        public IActionResult Error(int? statusCode = null)
        {
            var resolvedStatusCode = statusCode ?? HttpContext.Response.StatusCode;

            if (resolvedStatusCode < 400)
            {
                resolvedStatusCode = StatusCodes.Status500InternalServerError;
            }

            var message = resolvedStatusCode switch
            {
                StatusCodes.Status404NotFound => "The resource you requested was not found.",
                StatusCodes.Status401Unauthorized => "You are not authorized to access this resource.",
                StatusCodes.Status403Forbidden => "Access to this resource is forbidden.",
                _ => "An internal server error occurred while processing your request."
            };

            Response.StatusCode = resolvedStatusCode;

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = resolvedStatusCode,
                Message = message
            });
        }
    }
}
