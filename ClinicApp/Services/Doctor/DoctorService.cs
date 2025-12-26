using ClinicApp.Data;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
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
            return await GetAppointments(date: DateTime.Today);
        }

        public async Task<List<Appointment>> GetAppointments(DateTime? date = null, AppointmentStatus? status = null, int days = 7)
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return new List<Appointment>();

            var query = _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.DoctorId == doctor.Id)
                .AsQueryable();

            if (date.HasValue)
            {
                query = query.Where(a => a.AppointmentDateTime.Date == date.Value.Date);
            }
            else
            {
                var startDate = DateTime.Today;
                var endDate = startDate.AddDays(days);
                query = query.Where(a => a.AppointmentDateTime >= startDate && a.AppointmentDateTime <= endDate);
            }

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            return await query.OrderBy(a => a.AppointmentDateTime).ToListAsync();
        }

        public async Task<List<Appointment>> GetUpcomingAppointments(int days = 7)
        {
            return await GetAppointments(days: days);
        }

        public async Task<bool> UpdateAppointmentStatus(int appointmentId, AppointmentStatus status)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null) return false;

            var oldStatus = appointment.Status;
            appointment.Status = status;
            appointment.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _notificationService.NotifyAppointmentStatusChanged(appointment, oldStatus.ToString());

            return true;
        }

        public async Task<Appointment?> GetAppointmentForConsultation(int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Patient).ThenInclude(p => p.MedicalRecords)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
        }

        public async Task<List<Prescription>> GetDoctorPrescriptions()
        {
            var doctor = await GetCurrentDoctor();
            if (doctor == null) return new List<Prescription>();

            return await _context.Prescriptions
                .Include(p => p.Patient).ThenInclude(pat => pat.User)
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

                await _notificationService.NotifyPrescriptionCreated(prescription);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CompleteConsultationAsync(ConsultationViewModel model)
        {
            var appointment = await _context.Appointments.FindAsync(model.AppointmentId);
            if (appointment == null || appointment.Status == AppointmentStatus.Completed) return false;

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                string diagText = model.DiagnosisNote;
                if (model.DiagnosisId > 0)
                {
                    var d = await _context.Diagnoses.FindAsync(model.DiagnosisId);
                    if (d != null) diagText = $"{d.Code} — {d.Name} " + diagText;
                }

                var record = new MedicalRecord
                {
                    AppointmentId = model.AppointmentId,
                    PatientId = appointment.PatientId,
                    RecordDate = DateTime.Now,
                    Complaints = appointment.Reason,
                    Symptoms = model.Symptoms,
                    DiagnosisId = model.DiagnosisId > 0 ? model.DiagnosisId : null,
                    Diagnosis = diagText,
                    Treatment = model.Treatment,
                    Recommendations = model.Recommendations
                };
                _context.MedicalRecords.Add(record);

                var allMeds = (model.Meds ?? new List<PrescriptionItem>())
                              .Concat(model.Recipes ?? new List<PrescriptionItem>())
                              .Where(m => m.MedicationId > 0);

                foreach (var item in allMeds)
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

                appointment.Status = AppointmentStatus.Completed;
                appointment.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<List<Patient>> SearchPatients(string search, int take = 50)
        {
            var query = _context.Patients.Include(p => p.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p => p.User.FullName.Contains(search) || p.PolicyNumber.Contains(search));
            }
            return await query.OrderBy(p => p.User.FullName).Take(take).ToListAsync();
        }

        public async Task<Patient?> GetPatientDetails(int id)
        {
            return await _context.Patients
                .Include(p => p.User)

                .Include(p => p.MedicalRecords)
                    .ThenInclude(m => m.Appointment)
                        .ThenInclude(a => a.Doctor)
                            .ThenInclude(d => d.Specialization)
                .Include(p => p.Prescriptions)
                    .ThenInclude(pr => pr.Medication)

                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.Specialization)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<(List<Medication> All, List<Medication> Strict, List<Diagnosis> Diagnoses)> GetConsultationData()
        {
            var all = await _context.Medications.Where(m => !m.PrescriptionRequired).Take(100).OrderBy(m => m.Name).ToListAsync();
            var strict = await _context.Medications.Where(m => m.PrescriptionRequired).Take(100).OrderBy(m => m.Name).ToListAsync();
            var diags = await _context.Diagnoses.Take(100).OrderBy(d => d.Code).ToListAsync();
            return (all, strict, diags);
        }

        public async Task<List<Diagnosis>> SearchDiagnoses(string term)
        {
            var query = _context.Diagnoses.AsQueryable();
            if (!string.IsNullOrEmpty(term))
                query = query.Where(d => d.Code.Contains(term) || d.Name.Contains(term));
            return await query.Take(20).ToListAsync();
        }

        public async Task<List<Medication>> SearchMedications(string term, bool strictOnly = false)
        {
            var query = _context.Medications.AsQueryable();

            if (strictOnly)
            {
                query = query.Where(m => m.PrescriptionRequired);
            }

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(m => m.Name.Contains(term));
            }

            return await query.Take(20).ToListAsync();
        }
    }
}