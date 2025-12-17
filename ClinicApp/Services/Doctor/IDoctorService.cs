using ClinicApp.Controllers;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
using System.Numerics;

namespace ClinicApp.Services.DoctorService
{
    public interface IDoctorService
    {
        Task<Doctor?> GetCurrentDoctor();
        Task<List<Schedule>> GetDoctorSchedule();
        Task<List<Appointment>> GetTodayAppointments();
        Task<List<Appointment>> GetUpcomingAppointments(int days = 7);
        Task<bool> UpdateAppointmentStatus(int appointmentId, AppointmentStatus status);
        Task<List<Prescription>> GetDoctorPrescriptions();
        Task<bool> CreatePrescription(Prescription prescription);
        Task<Appointment?> GetAppointmentForConsultation(int appointmentId);
        Task<bool> CompleteConsultationAsync(ConsultationViewModel model);
        Task<List<Patient>> SearchPatients(string search);
        Task<Patient?> GetPatientDetails(int id);
        Task<(List<Medication> All, List<Medication> Strict, List<Diagnosis> Diagnoses)> GetConsultationData();
    }
}