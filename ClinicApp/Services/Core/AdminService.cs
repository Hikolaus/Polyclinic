using Microsoft.EntityFrameworkCore;
using ClinicApp.Data;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;

namespace ClinicApp.Services.Core
{
    public class AdminService : IAdminService
    {
        private readonly ClinicContext _context;

        public AdminService(ClinicContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<string, object>> GetDashboardStats()
        {
            var stats = new Dictionary<string, object>();
            stats["TotalPatients"] = await _context.Patients.CountAsync();
            stats["TotalDoctors"] = await _context.Doctors.CountAsync();
            stats["TotalAppointments"] = await _context.Appointments.CountAsync();

            // График посещений
            var twoWeeksAgo = DateTime.Today.AddDays(-13);
            var appointmentsByDate = await _context.Appointments
                .Where(a => a.AppointmentDateTime >= twoWeeksAgo)
                .GroupBy(a => a.AppointmentDateTime.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();
            stats["ChartDates"] = appointmentsByDate.Select(x => x.Date.ToString("dd.MM")).ToArray();
            stats["ChartCounts"] = appointmentsByDate.Select(x => x.Count).ToArray();

            // Топ диагнозов
            var topDiagnoses = await _context.MedicalRecords
                .Include(m => m.DiagnosisRef)
                .Where(m => m.DiagnosisId != null)
                .GroupBy(m => m.DiagnosisRef.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();
            stats["DiagLabels"] = topDiagnoses.Any() ? topDiagnoses.Select(x => x.Name).ToArray() : new[] { "Нет данных" };
            stats["DiagData"] = topDiagnoses.Any() ? topDiagnoses.Select(x => x.Count).ToArray() : new[] { 1 };

            // Возраст
            var birthDates = await _context.Patients.Select(p => p.DateOfBirth).ToListAsync();
            var today = DateTime.Today;
            int[] ageGroups = new int[4]; // 0-17, 18-35, 36-60, 60+

            foreach (var dob in birthDates)
            {
                var age = today.Year - dob.Year;
                if (dob.Date > today.AddYears(-age)) age--;
                if (age < 18) ageGroups[0]++;
                else if (age <= 35) ageGroups[1]++;
                else if (age <= 60) ageGroups[2]++;
                else ageGroups[3]++;
            }
            stats["AgeData"] = ageGroups;

            return stats;
        }

        public async Task<List<User>> GetUsers(string search, string role)
        {
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(u => u.FullName.Contains(search) || u.Login.Contains(search) || u.Email.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role == role);
            return await query.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();
        }

        public async Task ToggleUserStatus(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(bool Success, string Error)> RegisterDoctor(string login, string password, string fullName, string email, string phone, int specializationId, string license, int experience)
        {
            if (await _context.Users.AnyAsync(u => u.Login == login)) return (false, "Логин занят");

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = new User { Login = login, PasswordHash = password, Role = "Doctor", FullName = fullName, Email = email, Phone = phone, IsActive = true };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var doctor = new Doctor { Id = user.Id, SpecializationId = specializationId, LicenseNumber = license, Experience = experience, IsActive = true };
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, ex.Message);
            }
        }

        // --- Медикаменты ---
        public async Task<List<Medication>> GetMedications(string search)
        {
            var query = _context.Medications.AsQueryable();
            if (!string.IsNullOrEmpty(search)) query = query.Where(m => m.Name.Contains(search));
            return await query.OrderBy(m => m.Name).ToListAsync();
        }

        public async Task AddMedication(Medication medication)
        {
            _context.Medications.Add(medication);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteMedication(int id)
        {
            try
            {
                var item = await _context.Medications.FindAsync(id);
                if (item == null) return false;
                _context.Medications.Remove(item);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        // --- Специализации ---
        public async Task<List<Specialization>> GetSpecializations() => await _context.Specializations.ToListAsync();

        public async Task AddSpecialization(Specialization spec)
        {
            if (spec.AverageConsultationTime <= 0) spec.AverageConsultationTime = 15;
            _context.Specializations.Add(spec);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSpecializationTime(int id, int minutes)
        {
            var spec = await _context.Specializations.FindAsync(id);
            if (spec != null)
            {
                spec.AverageConsultationTime = Math.Max(5, minutes);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> DeleteSpecialization(int id)
        {
            try
            {
                var item = await _context.Specializations.FindAsync(id);
                if (item == null) return false;
                _context.Specializations.Remove(item);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        // --- Диагнозы ---
        public async Task<List<Diagnosis>> GetDiagnoses(string search)
        {
            var query = _context.Diagnoses.AsQueryable();
            if (!string.IsNullOrEmpty(search)) query = query.Where(d => d.Code.Contains(search) || d.Name.Contains(search));
            return await query.OrderBy(d => d.Code).ToListAsync();
        }

        public async Task<(bool Success, string Error)> AddDiagnosis(Diagnosis diagnosis)
        {
            if (await _context.Diagnoses.AnyAsync(d => d.Code == diagnosis.Code)) return (false, "Код уже существует");
            _context.Diagnoses.Add(diagnosis);
            await _context.SaveChangesAsync();
            return (true, "");
        }

        public async Task UpdateDiagnosis(Diagnosis diagnosis)
        {
            var existing = await _context.Diagnoses.FindAsync(diagnosis.Id);
            if (existing != null)
            {
                existing.Code = diagnosis.Code;
                existing.Name = diagnosis.Name;
                existing.DefaultTreatment = diagnosis.DefaultTreatment;
                existing.DefaultRecommendations = diagnosis.DefaultRecommendations;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> DeleteDiagnosis(int id)
        {
            try
            {
                var item = await _context.Diagnoses.FindAsync(id);
                if (item == null) return false;
                _context.Diagnoses.Remove(item);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        // --- Расписание ---
        public async Task<List<Doctor>> GetDoctorsWithSchedules()
        {
            return await _context.Doctors.Include(d => d.User).Include(d => d.Specialization).Include(d => d.Schedules).Where(d => d.User.IsActive).OrderBy(d => d.User.FullName).ToListAsync();
        }

        public async Task<Doctor?> GetDoctorWithSchedule(int doctorId)
        {
            return await _context.Doctors.Include(d => d.User).Include(d => d.Specialization).FirstOrDefaultAsync(d => d.Id == doctorId);
        }

        public async Task AddSchedule(Schedule schedule)
        {
            schedule.IsActive = true;
            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();
        }

        public async Task ToggleSchedule(int scheduleId)
        {
            var s = await _context.Schedules.FindAsync(scheduleId);
            if (s != null) { s.IsActive = !s.IsActive; await _context.SaveChangesAsync(); }
        }

        public async Task<bool> DeleteSchedule(int scheduleId)
        {
            try
            {
                var s = await _context.Schedules.FindAsync(scheduleId);
                if (s == null) return false;
                _context.Schedules.Remove(s);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task UpdateSchedule(Schedule schedule)
        {
            var existing = await _context.Schedules.FindAsync(schedule.Id);
            if (existing != null)
            {
                existing.DayOfWeek = schedule.DayOfWeek;
                existing.StartTime = schedule.StartTime;
                existing.EndTime = schedule.EndTime;
                existing.SlotDurationMinutes = schedule.SlotDurationMinutes;
                existing.BreakStart = schedule.BreakStart;
                existing.BreakEnd = schedule.BreakEnd;
                existing.MaxPatients = schedule.MaxPatients;
                await _context.SaveChangesAsync();
            }
        }

        public async Task GenerateBulkSchedule(int doctorId, List<int> daysOfWeek, TimeSpan startTime, TimeSpan endTime, int duration)
        {
            foreach (var day in daysOfWeek)
            {
                if (!await _context.Schedules.AnyAsync(s => s.DoctorId == doctorId && s.DayOfWeek == day && s.IsActive))
                {
                    _context.Schedules.Add(new Schedule { DoctorId = doctorId, DayOfWeek = day, StartTime = startTime, EndTime = endTime, SlotDurationMinutes = duration, IsActive = true });
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}