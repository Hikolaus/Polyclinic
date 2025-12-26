using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
using ClinicApp.Services.Core;

namespace ClinicApp.Services.PatientService
{
    public interface IPatientService
    {
        Task<Patient?> GetCurrentPatient();

        Task<List<Appointment>> GetPatientAppointments(int patientId, DateTime? start = null, DateTime? end = null, AppointmentStatus[]? statuses = null);

        Task<bool> CreateAppointment(Appointment appointment);

        Task<List<Doctor>> GetAvailableDoctors(int? specializationId = null);

        Task<List<MedicalRecord>> GetPatientMedicalRecords();
        Task<bool> CancelAppointment(int appointmentId);
        Task<List<TimeSlot>> GetAvailableTimeSlots(int doctorId, DateTime date);
        Task<bool> JoinWaitlist(int doctorId);
    }
}