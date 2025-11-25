using ClinicApp.Models.Core;
using ClinicApp.Models.PatientModels;
using ClinicApp.Services.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicApp.Data;

namespace ClinicApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ClinicContext _context;

        public AuthController(IAuthService authService, ClinicContext context)
        {
            _authService = authService;
            _context = context;
        }

        public IActionResult Login()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (!string.IsNullOrEmpty(userRole))
            {
                return RedirectToPanel(userRole);
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string login, string password)
        {
            var user = await _authService.Authenticate(login, password);
            if (user != null)
            {
                _authService.Login(user);
                return RedirectToPanel(user.Role);
            }

            ViewBag.Error = "Неверный логин или пароль";
            return View();
        }

        public IActionResult Logout()
        {
            _authService.Logout();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            string login,
            string password,
            string confirmPassword,
            string fullName,
            string? email,
            string? phone,
            string policyNumber,
            DateTime dateOfBirth,
            string gender,
            string? address)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Пароли не совпадают";
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Login == login))
            {
                ViewBag.Error = "Пользователь с таким логином уже существует";
                return View();
            }

            var user = new User
            {
                Login = login,
                PasswordHash = password,
                Role = "Patient",
                FullName = fullName,
                Email = email,
                Phone = phone,
                RegistrationDate = DateTime.Now,
                IsActive = true
            };

            var result = await _authService.Register(user);
            if (!result)
            {
                ViewBag.Error = "Ошибка при регистрации";
                return View();
            }

            var patient = new Patient
            {
                Id = user.Id,
                PolicyNumber = policyNumber,
                DateOfBirth = dateOfBirth,
                Gender = gender,
                Address = address,
                IsActive = true
            };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            _authService.Login(user);
            return RedirectToAction("Dashboard", "Patient");
        }

        private IActionResult RedirectToPanel(string role)
        {
            return role switch
            {
                "Patient" => RedirectToAction("Dashboard", "Patient"),
                "Doctor" => RedirectToAction("Dashboard", "Doctor"),
                "Administrator" => RedirectToAction("Dashboard", "Admin"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        public IActionResult NotAuthorized()
        {
            return View();
        }
    }
}