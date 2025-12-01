using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
using ClinicApp.Services.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClinicApp.Services.PatientService
{
    public interface IPatientService
    {
        Task<Patient?> GetCurrentPatient();
        Task<List<Appointment>> GetPatientAppointments();
        Task<bool> CreateAppointment(Appointment appointment);
        Task<List<Doctor>> GetAvailableDoctors();
        Task<List<MedicalRecord>> GetPatientMedicalRecords();
        Task<bool> CancelAppointment(int appointmentId);
        Task<List<TimeSlot>> GetAvailableTimeSlots(int doctorId, DateTime date);
        Task<bool> JoinWaitlist(int doctorId);
    }
}