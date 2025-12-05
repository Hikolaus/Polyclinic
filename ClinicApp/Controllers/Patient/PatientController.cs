using Microsoft.AspNetCore.Mvc;
using ClinicApp.Services.PatientService;
using ClinicApp.Models.Core;
using ClinicApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Controllers
{
    public class PatientController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly ClinicContext _context;

        public PatientController(IPatientService patientService, ClinicContext context)
        {
            _patientService = patientService;
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Patient") return View("NotAuthorized");

            var patient = await _patientService.GetCurrentPatient();
            if (patient == null) return View("NotAuthorized");

            var allAppointments = await _patientService.GetPatientAppointments();

            ViewBag.TotalAppointments = allAppointments.Count;
            ViewBag.UpcomingAppointments = allAppointments.Count(a =>
                a.AppointmentDateTime > DateTime.Now && a.Status != AppointmentStatus.Cancelled);

            ViewBag.RecentAppointments = allAppointments
                .Where(a => a.AppointmentDateTime > DateTime.Now && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.AppointmentDateTime)
                .Take(5)
                .ToList();

            return View(patient);
        }

        public async Task<IActionResult> MyAppointments() => View(await _patientService.GetPatientAppointments());

        [HttpGet]
        public async Task<IActionResult> CreateAppointment()
        {
            ViewBag.Specializations = await _context.Specializations.ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorsBySpec(int specId)
        {
            var doctors = await _context.Doctors
                .Include(d => d.User)
                .Where(d => d.SpecializationId == specId)
                .Select(d => new { d.Id, Name = d.User.FullName })
                .ToListAsync();
            return Json(doctors);
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthAvailability(int doctorId, int year, int month)
        {
            var schedules = await _context.Schedules
                .Where(s => s.DoctorId == doctorId && s.IsActive)
                .ToListAsync();

            if (!schedules.Any()) return Json(new List<object>());

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                            a.AppointmentDateTime >= startDate &&
                            a.AppointmentDateTime < endDate &&
                            (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed))
                .ToListAsync();

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var availabilityList = new List<object>();

            var maxDate = DateTime.Today.AddDays(14);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);

                if (date.Date < DateTime.Today || date.Date > maxDate) continue;

                int dayOfWeek = (int)date.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7;

                var schedule = schedules.FirstOrDefault(s => s.DayOfWeek == dayOfWeek);

                if (schedule != null)
                {
                    double totalMinutes = (schedule.EndTime - schedule.StartTime).TotalMinutes;

                    if (schedule.BreakStart.HasValue && schedule.BreakEnd.HasValue)
                    {
                        totalMinutes -= (schedule.BreakEnd.Value - schedule.BreakStart.Value).TotalMinutes;
                    }

                    int capacity = (int)(totalMinutes / schedule.SlotDurationMinutes);

                    if (schedule.MaxPatients > 0)
                    {
                        capacity = Math.Min(capacity, schedule.MaxPatients);
                    }

                    int bookedCount = appointments.Count(a => a.AppointmentDateTime.Date == date.Date);

                    bool isFull = bookedCount >= capacity;

                    availabilityList.Add(new
                    {
                        day = day,
                        fullDate = date.ToString("yyyy-MM-dd"),
                        available = true,
                        isFull = isFull
                    });
                }
            }

            return Json(availabilityList);
        }

        [HttpPost]
        public async Task<IActionResult> JoinWaitlist(int doctorId)
        {
            var result = await _patientService.JoinWaitlist(doctorId);
            return Json(new { success = result });
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
            if (doctorId == 0 || string.IsNullOrEmpty(dateStr) || string.IsNullOrEmpty(reason))
            {
                TempData["Error"] = "Не все данные заполнены.";
                ViewBag.Specializations = await _context.Specializations.ToListAsync();
                return View("CreateAppointment");
            }

            try
            {
                if (!DateTime.TryParse(dateStr, out DateTime parsedDate))
                {
                    TempData["Error"] = "Неверный формат даты";
                    return View("CreateAppointment");
                }

                var appointment = new Appointment
                {
                    DoctorId = doctorId,
                    AppointmentDateTime = parsedDate,
                    Reason = reason,
                    Status = AppointmentStatus.Scheduled,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                var result = await _patientService.CreateAppointment(appointment);

                if (result)
                {
                    TempData["Success"] = "Вы успешно записаны!";
                    return RedirectToAction("MyAppointments");
                }
                else
                {
                    TempData["Error"] = "Не удалось записаться. Возможно, время уже занято.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка сервера: {ex.Message}";
            }

            ViewBag.Specializations = await _context.Specializations.ToListAsync();
            return View("CreateAppointment");
        }

        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            var result = await _patientService.CancelAppointment(appointmentId);
            if (result) TempData["Success"] = "Запись успешно отменена.";
            else TempData["Error"] = "Не удалось отменить запись.";

            return RedirectToAction("MyAppointments");
        }

        public async Task<IActionResult> MedicalRecords()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Patient") return RedirectToAction("NotAuthorized", "Auth");
            var records = await _patientService.GetPatientMedicalRecords();
            return View(records);
        }
    }
}