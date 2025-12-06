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

            var specs = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.Specialization)
                .GroupBy(a => a.Doctor.Specialization.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.SpecLabels = specs.Select(x => x.Name).ToArray();
            ViewBag.SpecData = specs.Select(x => x.Count).ToArray();

            return View();
        }

        public async Task<IActionResult> Medications(string search)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");

            var query = _context.Medications.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(m => m.Name.Contains(search));

            return View(await query.OrderBy(m => m.Name).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddMedication(Medication medication)
        {
            if (ModelState.IsValid)
            {
                _context.Medications.Add(medication);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Лекарство добавлено";
            }
            return RedirectToAction("Medications");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMedication(int id)
        {
            var med = await _context.Medications.FindAsync(id);
            if (med != null)
            {
                _context.Medications.Remove(med);
                await _context.SaveChangesAsync();
            }
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
                _context.Specializations.Add(spec);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Специализация добавлена";
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
        public async Task<IActionResult> RegisterDoctor(string login, string password, string fullName, int specializationId, string license, int experience)
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

        [HttpGet]
        public async Task<IActionResult> BulkSchedule()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");

            ViewBag.Doctors = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .OrderBy(d => d.User.FullName)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BulkSchedule(int doctorId, List<int> daysOfWeek, TimeSpan startTime, TimeSpan endTime, int duration)
        {
            if (daysOfWeek == null || !daysOfWeek.Any())
            {
                TempData["Error"] = "Выберите хотя бы один день недели";
                return RedirectToAction("BulkSchedule");
            }

            if (startTime >= endTime)
            {
                TempData["Error"] = "Время начала должно быть меньше конца";
                return RedirectToAction("BulkSchedule");
            }

            int addedCount = 0;
            foreach (var day in daysOfWeek)
            {
                var exists = await _context.Schedules
                    .AnyAsync(s => s.DoctorId == doctorId && s.DayOfWeek == day && s.IsActive);

                if (!exists)
                {
                    var schedule = new Schedule
                    {
                        DoctorId = doctorId,
                        DayOfWeek = day,
                        StartTime = startTime,
                        EndTime = endTime,
                        SlotDurationMinutes = duration,
                        IsActive = true,
                        MaxPatients = 0
                    };
                    _context.Schedules.Add(schedule);
                    addedCount++;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Создано {addedCount} графиков работы.";
            return RedirectToAction("Schedules");
        }

        public async Task<IActionResult> Users(string search, string role)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Administrator") return View("NotAuthorized");

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Login.Contains(search) ||
                    u.Email.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            var users = await query
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .ToListAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRole = role;

            return View(users);
        }
        public async Task<IActionResult> Schedules() { return View(await _context.Doctors.Include(d => d.User).Include(d => d.Specialization).Include(d => d.Schedules).ToListAsync()); }

        [HttpGet]
        public async Task<IActionResult> EditSchedule(int doctorId)
        {
            var doctor = await _context.Doctors.Include(d => d.User).Include(d => d.Specialization).FirstOrDefaultAsync(d => d.Id == doctorId);
            var schedules = await _context.Schedules.Where(s => s.DoctorId == doctorId).OrderBy(s => s.DayOfWeek).ToListAsync();
            ViewBag.Doctor = doctor;
            return View(schedules);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSchedule(int scheduleId)
        {
            var s = await _context.Schedules.FindAsync(scheduleId);
            if (s != null) { _context.Schedules.Remove(s); await _context.SaveChangesAsync(); return RedirectToAction("EditSchedule", new { doctorId = s.DoctorId }); }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Appointments() { return RedirectToAction("Dashboard"); }
    }
}