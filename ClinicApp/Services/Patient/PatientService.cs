using ClinicApp.Data;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
using ClinicApp.Services.Core;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Services.PatientService
{
    public class PatientService : IPatientService
    {
        private readonly ClinicContext _context;
        private readonly IAuthService _authService;
        private readonly IScheduleService _scheduleService;

        public PatientService(ClinicContext context, IAuthService authService, IScheduleService scheduleService)
        {
            _context = context;
            _authService = authService;
            _scheduleService = scheduleService;
        }

        public async Task<Patient?> GetCurrentPatient()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null) return null;

            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == currentUser.Id);
        }

        public async Task<List<Appointment>> GetPatientAppointments()
        {
            var patient = await GetCurrentPatient();
            if (patient == null) return new List<Appointment>();

            return await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Doctor.Specialization)
                .Where(a => a.PatientId == patient.Id)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();
        }

        public async Task<bool> CreateAppointment(Appointment appointment)
        {
            try
            {
                var patient = await GetCurrentPatient();
                if (patient == null) return false;

                appointment.AppointmentDateTime = appointment.AppointmentDateTime
                    .AddSeconds(-appointment.AppointmentDateTime.Second)
                    .AddMilliseconds(-appointment.AppointmentDateTime.Millisecond);

                bool isAvailable = await _scheduleService.IsTimeSlotAvailable(appointment.DoctorId, appointment.AppointmentDateTime);
                if (!isAvailable) return false;

                appointment.PatientId = patient.Id;
                appointment.Status = AppointmentStatus.Scheduled;
                appointment.CreatedAt = DateTime.Now;
                appointment.UpdatedAt = DateTime.Now;

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CancelAppointment(int appointmentId)
        {
            try
            {
                var patient = await GetCurrentPatient();
                if (patient == null) return false;

                var appointment = await _context.Appointments
                    .Include(a => a.Doctor).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == patient.Id);

                if (appointment == null) return false;

                if (appointment.Status == AppointmentStatus.Completed ||
                    appointment.Status == AppointmentStatus.Cancelled) return false;

                appointment.Status = AppointmentStatus.Cancelled;
                appointment.UpdatedAt = DateTime.Now;

                var waiters = await _context.WaitlistRequests
                    .Where(w => w.DoctorId == appointment.DoctorId && !w.IsNotified)
                    .ToListAsync();

                if (waiters.Any())
                {
                    foreach (var waiter in waiters)
                    {
                        var notification = new Notification
                        {
                            UserId = waiter.PatientId,
                            Title = "Появилось свободное время",
                            Message = $"У врача {appointment.Doctor?.User?.FullName} освободилось окно: {appointment.AppointmentDateTime:dd.MM HH:mm}. Успейте записаться!",
                            Type = NotificationType.System,
                            CreatedAt = DateTime.Now,
                            IsRead = false
                        };
                        _context.Notifications.Add(notification);
                        waiter.IsNotified = true;
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> JoinWaitlist(int doctorId)
        {
            var patient = await GetCurrentPatient();
            if (patient == null) return false;

            bool exists = await _context.WaitlistRequests
                .AnyAsync(w => w.PatientId == patient.Id && w.DoctorId == doctorId && !w.IsNotified);

            if (exists) return true;

            var request = new WaitlistRequest
            {
                PatientId = patient.Id,
                DoctorId = doctorId,
                CreatedAt = DateTime.Now,
                IsNotified = false
            };

            _context.WaitlistRequests.Add(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Doctor>> GetAvailableDoctors()
        {
            return await _context.Doctors
               .Include(d => d.Specialization).Include(d => d.User)
               .Where(d => d.User.IsActive).ToListAsync();
        }

        public async Task<List<MedicalRecord>> GetPatientMedicalRecords()
        {
            var patient = await GetCurrentPatient();
            if (patient == null) return new List<MedicalRecord>();

            return await _context.MedicalRecords
                .Include(m => m.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.Specialization)
                .Include(m => m.Appointment.Doctor.User)
                .Include(m => m.Appointment.Prescriptions).ThenInclude(p => p.Medication)
                .Where(m => m.PatientId == patient.Id)
                .OrderByDescending(m => m.RecordDate)
                .ToListAsync();
        }

        public Task<List<TimeSlot>> GetAvailableTimeSlots(int doctorId, DateTime date)
        {
            return _scheduleService.GetAvailableTimeSlots(doctorId, date);
        }
    }
}