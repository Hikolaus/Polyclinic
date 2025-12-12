using ClinicApp.Data;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Services.Core;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Services.DoctorService
{
    public class DoctorService : IDoctorService
    {
        private readonly ClinicContext _context;
        private readonly IAuthService _authService;
        private readonly INotificationService _notificationService;

        public DoctorService(ClinicContext context, IAuthService authService, INotificationService notificationService)
        {
            _context = context;
            _authService = authService;
            _notificationService = notificationService;
        }

        public async Task<Doctor?> GetCurrentDoctor()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null) return null;

            return await _context.Doctors
                .Include(d => d.Specialization)
                .Include(d => d.User)
                .Include(d => d.Schedules)
                .FirstOrDefaultAsync(d => d.Id == currentUser.Id);
        }

        public async Task<List<Schedule>> GetDoctorSchedule()
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return new List<Schedule>();

            return await _context.Schedules
                .Where(s => s.DoctorId == doctor.Id && s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetTodayAppointments()
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return new List<Appointment>();

            var today = DateTime.Today;
            return await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Where(a => a.DoctorId == doctor.Id && a.AppointmentDateTime.Date == today)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetUpcomingAppointments(int days = 7)
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return new List<Appointment>();

            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(days);

            return await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Where(a => a.DoctorId == doctor.Id &&
                           a.AppointmentDateTime >= startDate &&
                           a.AppointmentDateTime <= endDate)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();
        }

        public async Task<bool> UpdateAppointmentStatus(int appointmentId, AppointmentStatus status)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return false;

            var oldStatus = appointment.Status;
            appointment.Status = status;
            appointment.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (_notificationService != null)
                await _notificationService.NotifyAppointmentStatusChanged(appointment, oldStatus.ToString());

            return true;
        }

        public async Task<Appointment?> GetAppointmentForConsultation(int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.MedicalRecords)
                        .ThenInclude(mr => mr.Appointment)
                            .ThenInclude(mra => mra.Doctor)
                                .ThenInclude(d => d.Specialization)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
        }

        public async Task<List<Prescription>> GetDoctorPrescriptions()
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return new List<Prescription>();

            return await _context.Prescriptions
                .Include(p => p.Patient)
                    .ThenInclude(pat => pat.User)
                .Include(p => p.Medication)

                .Where(p => p.DoctorId == doctor.Id && p.Medication.PrescriptionRequired)
                .OrderByDescending(p => p.IssueDate)
                .ToListAsync();
        }

        public async Task<bool> CreatePrescription(Prescription prescription)
        {
            try
            {
                var doctor = await GetCurrentDoctor();
                if (doctor == null) return false;

                prescription.DoctorId = doctor.Id;
                prescription.IssueDate = DateTime.Now;

                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();

                if (_notificationService != null)
                    await _notificationService.NotifyPrescriptionCreated(prescription);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}