using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicApp.Data;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;

namespace ClinicApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly ClinicContext _context;

        public AdminController(ClinicContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");

            ViewBag.TotalPatients = await _context.Patients.CountAsync();
            ViewBag.TotalDoctors = await _context.Doctors.CountAsync();
            ViewBag.TotalAppointments = await _context.Appointments.CountAsync();

            var twoWeeksAgo = DateTime.Today.AddDays(-13);
            var appointmentsByDate = await _context.Appointments
                .Where(a => a.AppointmentDateTime >= twoWeeksAgo)
                .GroupBy(a => a.AppointmentDateTime.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            ViewBag.Dates = appointmentsByDate.Select(x => x.Date.ToString("dd.MM")).ToArray();
            ViewBag.DateCounts = appointmentsByDate.Select(x => x.Count).ToArray();

            var topDiagnoses = await _context.MedicalRecords
                .Include(m => m.DiagnosisRef)
                .Where(m => m.DiagnosisId != null)
                .GroupBy(m => m.DiagnosisRef.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            if (!topDiagnoses.Any())
            {
                ViewBag.DiagLabels = new[] { "Нет данных" };
                ViewBag.DiagData = new[] { 1 };
            }
            else
            {
                ViewBag.DiagLabels = topDiagnoses.Select(x => x.Name).ToArray();
                ViewBag.DiagData = topDiagnoses.Select(x => x.Count).ToArray();
            }

            var birthDates = await _context.Patients.Select(p => p.DateOfBirth).ToListAsync();
            var today = DateTime.Today;

            int groupChild = 0;
            int groupYoung = 0;
            int groupMiddle = 0;
            int groupSenior = 0;

            foreach (var dob in birthDates)
            {
                var age = today.Year - dob.Year;
                if (dob.Date > today.AddYears(-age)) age--;

                if (age < 18) groupChild++;
                else if (age <= 35) groupYoung++;
                else if (age <= 60) groupMiddle++;
                else groupSenior++;
            }

            ViewBag.AgeData = new[] { groupChild, groupYoung, groupMiddle, groupSenior };

            return View();
        }

        public async Task<IActionResult> Users(string search, string role)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(u => u.FullName.Contains(search) || u.Login.Contains(search) || u.Email.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            var users = await query.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRole = role;
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Medications(string search)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");

            var query = _context.Medications.AsQueryable();
            if (!string.IsNullOrEmpty(search)) query = query.Where(m => m.Name.Contains(search));

            return View(await query.OrderBy(m => m.Name).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddMedication(Medication medication)
        {
            if (ModelState.IsValid) { _context.Medications.Add(medication); await _context.SaveChangesAsync(); }
            return RedirectToAction("Medications");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMedication(int id)
        {
            var med = await _context.Medications.FindAsync(id);
            if (med != null) { _context.Medications.Remove(med); await _context.SaveChangesAsync(); }
            return RedirectToAction("Medications");
        }

        public async Task<IActionResult> Specializations()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");
            return View(await _context.Specializations.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddSpecialization(Specialization spec)
        {
            if (ModelState.IsValid)
            {
                if (spec.AverageConsultationTime <= 0) spec.AverageConsultationTime = 15;

                _context.Specializations.Add(spec);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Специализация добавлена";
            }
            return RedirectToAction("Specializations");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSpecializationTime(int id, int minutes)
        {
            var spec = await _context.Specializations.FindAsync(id);
            if (spec != null)
            {
                if (minutes < 5) minutes = 5;
                spec.AverageConsultationTime = minutes;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Время приема обновлено";
            }
            return RedirectToAction("Specializations");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSpecialization(int id)
        {
            var spec = await _context.Specializations.FindAsync(id);
            if (spec != null)
            {
                _context.Specializations.Remove(spec);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Specializations");
        }

        [HttpGet]
        public async Task<IActionResult> RegisterDoctor()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");
            ViewBag.Specializations = await _context.Specializations.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterDoctor(
    string login,
    string password,
    string fullName,
    string email,
    string phone,
    int specializationId,
    string license,
    int experience)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");

            if (await _context.Users.AnyAsync(u => u.Login == login))
            {
                TempData["Error"] = "Пользователь с таким логином уже существует";
                return RedirectToAction("RegisterDoctor");
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = new User
                {
                    Login = login,
                    PasswordHash = password,
                    Role = "Doctor",
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    IsActive = true,
                    RegistrationDate = DateTime.Now
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var doctor = new Doctor
                {
                    Id = user.Id,
                    SpecializationId = specializationId,
                    LicenseNumber = license,
                    Experience = experience,
                    IsActive = true
                };
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                TempData["Success"] = $"Врач {fullName} успешно зарегистрирован";
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Ошибка при регистрации: " + ex.Message;
                return RedirectToAction("RegisterDoctor");
            }
        }

        public async Task<IActionResult> Schedules()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");
            var doctors = await _context.Doctors.Include(d => d.User).Include(d => d.Specialization).Include(d => d.Schedules).Where(d => d.User.IsActive).OrderBy(d => d.User.FullName).ToListAsync();
            return View(doctors);
        }

        [HttpGet]
        public async Task<IActionResult> EditSchedule(int doctorId)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");
            var doctor = await _context.Doctors.Include(d => d.User).Include(d => d.Specialization).FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null) return NotFound();
            var schedules = await _context.Schedules.Where(s => s.DoctorId == doctorId).OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ToListAsync();
            ViewBag.Doctor = doctor;
            return View(schedules);
        }

        [HttpPost]
        public async Task<IActionResult> AddSchedule(Schedule schedule)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");
            if (schedule.StartTime >= schedule.EndTime) { TempData["Error"] = "Ошибка во времени"; return RedirectToAction("EditSchedule", new { doctorId = schedule.DoctorId }); }
            schedule.IsActive = true;
            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();
            return RedirectToAction("EditSchedule", new { doctorId = schedule.DoctorId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSchedule(int scheduleId)
        {
            var s = await _context.Schedules.FindAsync(scheduleId);
            if (s != null) { s.IsActive = !s.IsActive; await _context.SaveChangesAsync(); return RedirectToAction("EditSchedule", new { doctorId = s.DoctorId }); }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSchedule(int scheduleId)
        {
            var s = await _context.Schedules.FindAsync(scheduleId);
            if (s != null) { var docId = s.DoctorId; _context.Schedules.Remove(s); await _context.SaveChangesAsync(); return RedirectToAction("EditSchedule", new { doctorId = docId }); }
            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> BulkSchedule()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");

            ViewBag.Doctors = await _context.Doctors.Include(d => d.User).Include(d => d.Specialization).OrderBy(d => d.User.FullName).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BulkSchedule(int doctorId, List<int> daysOfWeek, TimeSpan startTime, TimeSpan endTime, int duration)
        {
            if (daysOfWeek == null || !daysOfWeek.Any()) return RedirectToAction("BulkSchedule");
            foreach (var day in daysOfWeek)
            {
                if (!await _context.Schedules.AnyAsync(s => s.DoctorId == doctorId && s.DayOfWeek == day && s.IsActive))
                {
                    _context.Schedules.Add(new Schedule { DoctorId = doctorId, DayOfWeek = day, StartTime = startTime, EndTime = endTime, SlotDurationMinutes = duration, IsActive = true, MaxPatients = 0 });
                }
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "График сгенерирован";
            return RedirectToAction("Schedules");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSchedule(Schedule schedule)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");

            var existing = await _context.Schedules.FindAsync(schedule.Id);
            if (existing == null) return NotFound();

            if (schedule.StartTime >= schedule.EndTime)
            {
                TempData["Error"] = "Время начала должно быть меньше времени окончания";
                return RedirectToAction("EditSchedule", new { doctorId = existing.DoctorId });
            }

            existing.DayOfWeek = schedule.DayOfWeek;
            existing.StartTime = schedule.StartTime;
            existing.EndTime = schedule.EndTime;
            existing.SlotDurationMinutes = schedule.SlotDurationMinutes;
            existing.BreakStart = schedule.BreakStart;
            existing.BreakEnd = schedule.BreakEnd;
            existing.MaxPatients = schedule.MaxPatients;

            await _context.SaveChangesAsync();

            TempData["Success"] = "График успешно обновлен";
            return RedirectToAction("EditSchedule", new { doctorId = existing.DoctorId });
        }


        public async Task<IActionResult> Diagnoses(string search)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");

            var query = _context.Diagnoses.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(d => d.Code.Contains(search) || d.Name.Contains(search));
            }

            return View(await query.OrderBy(d => d.Code).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddDiagnosis(Diagnosis diagnosis)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Diagnoses.AnyAsync(d => d.Code == diagnosis.Code))
                {
                    TempData["Error"] = $"Код {diagnosis.Code} уже существует!";
                }
                else
                {
                    _context.Diagnoses.Add(diagnosis);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Диагноз добавлен";
                }
            }
            return RedirectToAction("Diagnoses");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDiagnosis(int id)
        {
            var diag = await _context.Diagnoses.FindAsync(id);
            if (diag != null)
            {
                _context.Diagnoses.Remove(diag);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Diagnoses");
        }
        [HttpPost]
        public async Task<IActionResult> UpdateDiagnosis(Diagnosis diagnosis)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");

            var existing = await _context.Diagnoses.FindAsync(diagnosis.Id);
            if (existing == null) return NotFound();

            if (existing.Code != diagnosis.Code && await _context.Diagnoses.AnyAsync(d => d.Code == diagnosis.Code))
            {
                TempData["Error"] = $"Код {diagnosis.Code} уже существует у другой болезни.";
                return RedirectToAction("Diagnoses");
            }

            existing.Code = diagnosis.Code;
            existing.Name = diagnosis.Name;
            existing.DefaultTreatment = diagnosis.DefaultTreatment;
            existing.DefaultRecommendations = diagnosis.DefaultRecommendations;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Данные диагноза обновлены";
            return RedirectToAction("Diagnoses");
        }
        public async Task<IActionResult> Appointments() { return RedirectToAction("Dashboard"); }
    }
}