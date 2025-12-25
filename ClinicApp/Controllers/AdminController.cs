using Microsoft.AspNetCore.Mvc;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Services.Core;

namespace ClinicApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Administrator";

        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin()) return View("NotAuthorized");
            var stats = await _adminService.GetDashboardStats();

            foreach (var kvp in stats)
            {
                ViewData[kvp.Key] = kvp.Value;
            }

            if (stats.ContainsKey("TotalPatients")) ViewBag.TotalPatients = stats["TotalPatients"];
            if (stats.ContainsKey("TotalDoctors")) ViewBag.TotalDoctors = stats["TotalDoctors"];
            if (stats.ContainsKey("TotalAppointments")) ViewBag.TotalAppointments = stats["TotalAppointments"];

            if (stats.ContainsKey("ChartDates")) ViewBag.Dates = stats["ChartDates"];
            if (stats.ContainsKey("ChartCounts")) ViewBag.DateCounts = stats["ChartCounts"];

            if (stats.ContainsKey("DiagLabels")) ViewBag.DiagLabels = stats["DiagLabels"];
            if (stats.ContainsKey("DiagData")) ViewBag.DiagData = stats["DiagData"];

            if (stats.ContainsKey("AgeData")) ViewBag.AgeData = stats["AgeData"];

            return View();
        }

        public async Task<IActionResult> Users(string search, string role)
        {
            if (!IsAdmin()) return View("NotAuthorized");
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRole = role;
            return View(await _adminService.GetUsers(search, role));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            await _adminService.ToggleUserStatus(userId);
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Medications(string search)
        {
            if (!IsAdmin()) return View("NotAuthorized");
            return View(await _adminService.GetMedications(search));
        }

        [HttpPost]
        public async Task<IActionResult> AddMedication(Medication medication)
        {
            if (ModelState.IsValid) await _adminService.AddMedication(medication);
            return RedirectToAction("Medications");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMedication(int id)
        {
            if (!await _adminService.DeleteMedication(id)) TempData["Error"] = "Ошибка удаления (возможно, используется)";
            return RedirectToAction("Medications");
        }

        public async Task<IActionResult> Specializations()
        {
            if (!IsAdmin()) return View("NotAuthorized");
            return View(await _adminService.GetSpecializations());
        }

        [HttpPost]
        public async Task<IActionResult> AddSpecialization(Specialization spec)
        {
            await _adminService.AddSpecialization(spec);
            return RedirectToAction("Specializations");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSpecializationTime(int id, int minutes)
        {
            await _adminService.UpdateSpecializationTime(id, minutes);
            return RedirectToAction("Specializations");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSpecialization(int id)
        {
            if (!await _adminService.DeleteSpecialization(id)) TempData["Error"] = "Ошибка удаления";
            return RedirectToAction("Specializations");
        }

        [HttpGet]
        public async Task<IActionResult> RegisterDoctor()
        {
            if (!IsAdmin()) return View("NotAuthorized");
            ViewBag.Specializations = await _adminService.GetSpecializations();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterDoctor(string login, string password, string fullName, string email, string phone, int specializationId, string license, int experience)
        {
            if (!IsAdmin()) return View("NotAuthorized");
            var result = await _adminService.RegisterDoctor(login, password, fullName, email, phone, specializationId, license, experience);
            if (result.Success)
            {
                TempData["Success"] = "Врач зарегистрирован";
                return RedirectToAction("Users");
            }
            TempData["Error"] = result.Error;
            return RedirectToAction("RegisterDoctor");
        }

        public async Task<IActionResult> Schedules()
        {
            if (!IsAdmin()) return View("NotAuthorized");
            return View(await _adminService.GetDoctorsWithSchedules());
        }

        [HttpGet]
        public async Task<IActionResult> EditSchedule(int doctorId)
        {
            if (!IsAdmin()) return View("NotAuthorized");
            var doc = await _adminService.GetDoctorWithSchedule(doctorId);
            if (doc == null) return NotFound();
            ViewBag.Doctor = doc;
            return View(doc.Schedules.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> AddSchedule(Schedule schedule)
        {
            if (schedule.StartTime >= schedule.EndTime) TempData["Error"] = "Ошибка времени";
            else await _adminService.AddSchedule(schedule);
            return RedirectToAction("EditSchedule", new { doctorId = schedule.DoctorId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSchedule(int scheduleId)
        {
            var schedule = (await _adminService.GetDoctorsWithSchedules()).SelectMany(d => d.Schedules).FirstOrDefault(s => s.Id == scheduleId);
            if (schedule != null)
            {
                await _adminService.ToggleSchedule(scheduleId);
                return RedirectToAction("EditSchedule", new { doctorId = schedule.DoctorId });
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSchedule(int scheduleId)
        {
            var schedule = (await _adminService.GetDoctorsWithSchedules()).SelectMany(d => d.Schedules).FirstOrDefault(s => s.Id == scheduleId);
            if (schedule != null)
            {
                if (!await _adminService.DeleteSchedule(scheduleId)) TempData["Error"] = "Ошибка удаления";
                return RedirectToAction("EditSchedule", new { doctorId = schedule.DoctorId });
            }
            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> BulkSchedule()
        {
            if (!IsAdmin()) return View("NotAuthorized");
            ViewBag.Doctors = await _adminService.GetDoctorsWithSchedules();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BulkSchedule(int doctorId, List<int> daysOfWeek, TimeSpan startTime, TimeSpan endTime, int duration)
        {
            await _adminService.GenerateBulkSchedule(doctorId, daysOfWeek, startTime, endTime, duration);
            return RedirectToAction("Schedules");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSchedule(Schedule schedule)
        {
            await _adminService.UpdateSchedule(schedule);
            return RedirectToAction("EditSchedule", new { doctorId = schedule.DoctorId });
        }

        public async Task<IActionResult> Diagnoses(string search)
        {
            if (!IsAdmin()) return View("NotAuthorized");
            return View(await _adminService.GetDiagnoses(search));
        }

        [HttpPost]
        public async Task<IActionResult> AddDiagnosis(Diagnosis diagnosis)
        {
            var res = await _adminService.AddDiagnosis(diagnosis);
            if (!res.Success) TempData["Error"] = res.Error;
            return RedirectToAction("Diagnoses");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDiagnosis(int id)
        {
            if (!await _adminService.DeleteDiagnosis(id)) TempData["Error"] = "Ошибка удаления";
            return RedirectToAction("Diagnoses");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDiagnosis(Diagnosis diagnosis)
        {
            await _adminService.UpdateDiagnosis(diagnosis);
            return RedirectToAction("Diagnoses");
        }
    }
}