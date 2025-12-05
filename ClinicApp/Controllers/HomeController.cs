using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        public IActionResult NotAuthorized()
        {
            return View();
        }
    }
}