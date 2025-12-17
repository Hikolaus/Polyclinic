using Microsoft.AspNetCore.Mvc;
using ClinicApp.Models.Core;
using ClinicApp.Models.PatientModels;
using ClinicApp.Services.Core;

namespace ClinicApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string login, string password)
        {
            var user = await _authService.Authenticate(login, password);
            if (user != null)
            {
                _authService.Login(user);
                return user.Role switch
                {
                    "Patient" => RedirectToAction("Dashboard", "Patient"),
                    "Doctor" => RedirectToAction("Dashboard", "Doctor"),
                    "Administrator" => RedirectToAction("Dashboard", "Admin"),
                    _ => RedirectToAction("Index", "Home")
                };
            }
            ViewBag.Error = "Неверный логин или пароль";
            return View();
        }

        public IActionResult Logout()
        {
            _authService.Logout();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string login, string password, string confirmPassword, string fullName, string? email, string? phone, string policyNumber, DateTime dateOfBirth, string gender, string? address)
        {
            if (password != confirmPassword) { ViewBag.Error = "Пароли не совпадают"; return View(); }

            var user = new User { Login = login, PasswordHash = password, Role = "Patient", FullName = fullName, Email = email, Phone = phone, IsActive = true, RegistrationDate = DateTime.Now };
            var patient = new Patient { PolicyNumber = policyNumber, DateOfBirth = dateOfBirth, Gender = gender, Address = address, IsActive = true };

            if (await _authService.RegisterPatient(user, patient))
            {
                _authService.Login(user);
                return RedirectToAction("Dashboard", "Patient");
            }

            ViewBag.Error = "Ошибка регистрации (возможно, логин занят)";
            return View();
        }

        public IActionResult NotAuthorized() => View();
    }
}