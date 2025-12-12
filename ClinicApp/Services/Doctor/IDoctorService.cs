using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
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
    }
}