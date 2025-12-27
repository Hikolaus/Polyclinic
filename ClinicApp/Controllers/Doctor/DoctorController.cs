using Microsoft.AspNetCore.Mvc;
using ClinicApp.Services.DoctorService;
using ClinicApp.Services.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.Core;

namespace ClinicApp.Controllers
{
    public class DoctorController : Controller
    {
        private readonly IDoctorService _doctorService;

        public DoctorController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        private bool IsDoctor() => HttpContext.Session.GetString("UserRole") == "Doctor";

        public async Task<IActionResult> Dashboard()
        {
            if (!IsDoctor()) return View("NotAuthorized");
            var doctor = await _doctorService.GetCurrentDoctor();
            if (doctor == null) return View("NotAuthorized");
            ViewBag.TodayAppointments = await _doctorService.GetTodayAppointments();
            return View(doctor);
        }

        public async Task<IActionResult> Schedule()
        {
            if (!IsDoctor()) return View("NotAuthorized");
            return View(await _doctorService.GetDoctorSchedule());
        }

        public async Task<IActionResult> Appointments(DateTime? date, string status)
        {
            if (!IsDoctor()) return View("NotAuthorized");

            AppointmentStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, out var s))
            {
                statusEnum = s;
            }

            var apps = await _doctorService.GetAppointments(date, statusEnum, days: 30);
            return View(apps);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNoShow(int appointmentId)
        {
            await _doctorService.UpdateAppointmentStatus(appointmentId, AppointmentStatus.NoShow);
            return RedirectToAction("Appointments");
        }

        [HttpGet]
        public async Task<IActionResult> Consultation(int id)
        {
            if (!IsDoctor()) return View("NotAuthorized");
            var app = await _doctorService.GetAppointmentForConsultation(id);
            if (app == null) return NotFound();

            if (app.Status == AppointmentStatus.Scheduled || app.Status == AppointmentStatus.Confirmed)
                await _doctorService.UpdateAppointmentStatus(id, AppointmentStatus.InProgress);

            var data = await _doctorService.GetConsultationData();
            ViewBag.AllMedications = data.All;
            ViewBag.StrictMedications = data.Strict;
            ViewBag.Diagnoses = data.Diagnoses;

            return View(app);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteConsultation(ConsultationViewModel model)
        {
            if (!IsDoctor()) return View("NotAuthorized");
            var success = await _doctorService.CompleteConsultationAsync(model);
            if (success) TempData["Success"] = "Прием завершен";
            else TempData["Error"] = "Ошибка при сохранении";
            return RedirectToAction("Appointments");
        }

        public async Task<IActionResult> Prescriptions()
        {
            if (!IsDoctor()) return View("NotAuthorized");
            return View(await _doctorService.GetDoctorPrescriptions());
        }

        public async Task<IActionResult> Patients(string search)
        {
            if (!IsDoctor()) return View("NotAuthorized");
            ViewBag.SearchTerm = search;
            return View(await _doctorService.SearchPatients(search, take: 50));
        }

        public async Task<IActionResult> PatientDetails(int id)
        {
            if (!IsDoctor()) return View("NotAuthorized");
            var p = await _doctorService.GetPatientDetails(id);
            if (p == null) return NotFound();
            return View(p);
        }

        [HttpGet]
        public async Task<IActionResult> SearchDiagnoses(string term)
        {
            if (!IsDoctor()) return Unauthorized();
            return Json(await _doctorService.SearchDiagnoses(term));
        }

        [HttpGet]
        public async Task<IActionResult> SearchMedications(string term, bool strict = false)
        {
            if (!IsDoctor()) return Unauthorized();
            return Json(await _doctorService.SearchMedications(term, strict));
        }
    }
}