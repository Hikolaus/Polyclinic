using ClinicApp.Data;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Services.Core;
using ClinicApp.Services.DoctorService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Controllers
{
    public class ConsultationViewModel
    {
        public int AppointmentId { get; set; }
        public string Symptoms { get; set; } = "";

        public int DiagnosisId { get; set; }
        public string DiagnosisNote { get; set; } = "";

        public string Treatment { get; set; } = "";
        public string Recommendations { get; set; } = "";
        public List<PrescriptionItem> Meds { get; set; } = new List<PrescriptionItem>();
        public List<PrescriptionItem> Recipes { get; set; } = new List<PrescriptionItem>();
    }

    public class PrescriptionItem
    {
        public int MedicationId { get; set; }
        public string Dosage { get; set; } = "";
        public string Instructions { get; set; } = "";
    }

    public class DoctorController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly ClinicContext _context;
        private readonly IAuthService _authService;

        public DoctorController(IDoctorService doctorService, ClinicContext context, IAuthService authService)
        {
            _doctorService = doctorService;
            _context = context;
            _authService = authService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Doctor") return View("NotAuthorized");

            var doctor = await _doctorService.GetCurrentDoctor();
            if (doctor == null) return View("NotAuthorized");

            ViewBag.TodayAppointments = await _doctorService.GetTodayAppointments();
            return View(doctor);
        }

        public async Task<IActionResult> Schedule()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Doctor") return View("NotAuthorized");
            var schedule = await _doctorService.GetDoctorSchedule();
            return View(schedule);
        }

        public async Task<IActionResult> Appointments(DateTime? date, string status)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Doctor") return View("NotAuthorized");

            var appointments = await _doctorService.GetUpcomingAppointments(30);

            if (date.HasValue)
                appointments = appointments.Where(a => a.AppointmentDateTime.Date == date.Value.Date).ToList();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, out var statusEnum))
                appointments = appointments.Where(a => a.Status == statusEnum).ToList();

            return View(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNoShow(int appointmentId)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Doctor") return View("NotAuthorized");
            await _doctorService.UpdateAppointmentStatus(appointmentId, AppointmentStatus.NoShow);
            TempData["Success"] = "Запись отмечена как 'Неявка'";
            return RedirectToAction("Appointments");
        }

        // --- СТРАНИЦА ПРИЕМА ---
        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Consultation(int id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Doctor") return View("NotAuthorized");

            var appointment = await _doctorService.GetAppointmentForConsultation(id);
            if (appointment == null) return NotFound();

            if (appointment.Status == AppointmentStatus.Completed ||
                appointment.Status == AppointmentStatus.Cancelled ||
                appointment.Status == AppointmentStatus.NoShow)
            {
                TempData["Error"] = "Этот прием уже завершен или отменен.";
                return RedirectToAction("PatientDetails", new { id = appointment.PatientId });
            }

            if (appointment.Status == AppointmentStatus.Scheduled || appointment.Status == AppointmentStatus.Confirmed)
            {
                await _doctorService.UpdateAppointmentStatus(id, AppointmentStatus.InProgress);
            }

            ViewBag.AllMedications = await _context.Medications
                .Where(m => !m.PrescriptionRequired)
                .OrderBy(m => m.Name).ToListAsync();

            ViewBag.StrictMedications = await _context.Medications
                .Where(m => m.PrescriptionRequired)
                .OrderBy(m => m.Name).ToListAsync();

            ViewBag.Diagnoses = await _context.Diagnoses
                .OrderBy(d => d.Code)
                .ToListAsync();

            return View(appointment);
        }

        [HttpPost]
        public async Task<IActionResult> CompleteConsultation(ConsultationViewModel model)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Doctor") return View("NotAuthorized");

            var appointment = await _context.Appointments.FindAsync(model.AppointmentId);
            if (appointment == null) return NotFound();

            if (appointment.Status == AppointmentStatus.Completed)
            {
                return RedirectToAction("Appointments");
            }

            try
            {
                var diagnosis = await _context.Diagnoses.FindAsync(model.DiagnosisId);

                string fullDiagnosisString = diagnosis != null
                    ? $"{diagnosis.Code} — {diagnosis.Name}" + (!string.IsNullOrWhiteSpace(model.DiagnosisNote) ? $" ({model.DiagnosisNote})" : "")
                    : model.DiagnosisNote;

                var medicalRecord = new MedicalRecord
                {
                    AppointmentId = model.AppointmentId,
                    PatientId = appointment.PatientId,
                    RecordDate = DateTime.Now,
                    Complaints = appointment.Reason,
                    Symptoms = model.Symptoms,

                    DiagnosisId = model.DiagnosisId,
                    Diagnosis = fullDiagnosisString,

                    Treatment = model.Treatment,
                    Recommendations = model.Recommendations
                };
                _context.MedicalRecords.Add(medicalRecord);

                if (model.Meds != null)
                {
                    foreach (var item in model.Meds)
                    {
                        if (item.MedicationId > 0)
                        {
                            _context.Prescriptions.Add(new Prescription
                            {
                                PatientId = appointment.PatientId,
                                DoctorId = appointment.DoctorId,
                                AppointmentId = model.AppointmentId,
                                MedicationId = item.MedicationId,
                                Dosage = item.Dosage,
                                Instructions = item.Instructions,
                                IssueDate = DateTime.Now,
                                ExpiryDate = DateTime.Now.AddMonths(1),
                                Status = PrescriptionStatus.Active
                            });
                        }
                    }
                }

                if (model.Recipes != null)
                {
                    foreach (var item in model.Recipes)
                    {
                        if (item.MedicationId > 0)
                        {
                            _context.Prescriptions.Add(new Prescription
                            {
                                PatientId = appointment.PatientId,
                                DoctorId = appointment.DoctorId,
                                AppointmentId = model.AppointmentId,
                                MedicationId = item.MedicationId,
                                Dosage = item.Dosage,
                                Instructions = item.Instructions,
                                IssueDate = DateTime.Now,
                                ExpiryDate = DateTime.Now.AddMonths(1),
                                Status = PrescriptionStatus.Active
                            });
                        }
                    }
                }

                appointment.Status = AppointmentStatus.Completed;
                appointment.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Прием завершен успешно.";
                return RedirectToAction("Appointments");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
                return RedirectToAction("Consultation", new { id = model.AppointmentId });
            }
        }

        public async Task<IActionResult> Prescriptions()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Doctor") return View("NotAuthorized");
            var prescriptions = await _doctorService.GetDoctorPrescriptions();
            return View(prescriptions);
        }

        [HttpGet]
        public async Task<IActionResult> Patients(string search)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Doctor") return View("NotAuthorized");

            var query = _context.Patients.Include(p => p.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p => p.User.FullName.Contains(search) || p.PolicyNumber.Contains(search));
            }
            var patients = await query.OrderBy(p => p.User.FullName).Take(50).ToListAsync();
            ViewBag.SearchTerm = search;
            return View(patients);
        }

        [HttpGet]
        public async Task<IActionResult> PatientDetails(int id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Doctor") return View("NotAuthorized");

            var patient = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.MedicalRecords).ThenInclude(m => m.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.Specialization)
                .Include(p => p.Prescriptions).ThenInclude(pr => pr.Medication)
                .Include(p => p.Appointments).ThenInclude(a => a.Doctor).ThenInclude(d => d.Specialization)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null) return NotFound();
            return View(patient);
        }
    }
}