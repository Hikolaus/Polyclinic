using Microsoft.AspNetCore.Mvc;
using ClinicApp.Services.PatientService;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;

namespace ClinicApp.Controllers
{
    public class PatientController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly IScheduleService _scheduleService;
        private readonly IAdminService _adminService;

        public PatientController(IPatientService patientService, IScheduleService scheduleService, IAdminService adminService)
        {
            _patientService = patientService;
            _scheduleService = scheduleService;
            _adminService = adminService;
        }

        private bool IsPatient() => HttpContext.Session.GetString("UserRole") == "Patient";

        public async Task<IActionResult> Dashboard()
        {
            if (!IsPatient()) return View("NotAuthorized");
            var p = await _patientService.GetCurrentPatient();
            if (p == null) return View("NotAuthorized");

            var apps = await _patientService.GetPatientAppointments();
            ViewBag.TotalAppointments = apps.Count;
            ViewBag.UpcomingAppointments = apps.Count(a => a.AppointmentDateTime > DateTime.Now && a.Status != AppointmentStatus.Cancelled);
            ViewBag.RecentAppointments = apps.Where(a => a.AppointmentDateTime > DateTime.Now && a.Status != AppointmentStatus.Cancelled).OrderBy(a => a.AppointmentDateTime).Take(5).ToList();
            return View(p);
        }

        public async Task<IActionResult> MyAppointments() => View(await _patientService.GetPatientAppointments());

        [HttpGet]
        public async Task<IActionResult> CreateAppointment()
        {
            ViewBag.Specializations = await _adminService.GetSpecializations();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorsBySpec(int specId)
        {
            var docs = (await _patientService.GetAvailableDoctors()).Where(d => d.SpecializationId == specId)
                .Select(d => new { d.Id, Name = d.User.FullName });
            return Json(docs);
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthAvailability(int doctorId, int year, int month)
        {
            return Json(await _scheduleService.GetMonthAvailability(doctorId, year, month));
        }

        [HttpPost]
        public async Task<IActionResult> JoinWaitlist(int doctorId)
        {
            return Json(new { success = await _patientService.JoinWaitlist(doctorId) });
        }

        [HttpGet]
        public async Task<IActionResult> GetSlots(int doctorId, DateTime date)
        {
            var slots = await _patientService.GetAvailableTimeSlots(doctorId, date);
            return Json(slots.Where(s => s.IsAvailable).Select(s => s.StartTime.ToString("HH:mm")));
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment(int doctorId, string dateStr, string reason)
        {
            if (DateTime.TryParse(dateStr, out DateTime date))
            {
                var app = new Appointment { DoctorId = doctorId, AppointmentDateTime = date, Reason = reason };
                if (await _patientService.CreateAppointment(app))
                {
                    TempData["Success"] = "Записано";
                    return RedirectToAction("MyAppointments");
                }
            }
            TempData["Error"] = "Ошибка записи";
            ViewBag.Specializations = await _adminService.GetSpecializations();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            if (await _patientService.CancelAppointment(appointmentId)) TempData["Success"] = "Отменено";
            else TempData["Error"] = "Ошибка отмены";
            return RedirectToAction("MyAppointments");
        }

        public async Task<IActionResult> MedicalRecords()
        {
            if (!IsPatient()) return View("NotAuthorized");
            return View(await _patientService.GetPatientMedicalRecords());
        }
    }
}