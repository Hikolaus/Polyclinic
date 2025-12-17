using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;

namespace ClinicApp.Services.Core
{
    public interface IAdminService
    {
        // Дашборд
        Task<Dictionary<string, object>> GetDashboardStats();

        // Пользователи
        Task<List<User>> GetUsers(string search, string role);
        Task ToggleUserStatus(int userId);
        Task<(bool Success, string Error)> RegisterDoctor(string login, string password, string fullName, string email, string phone, int specializationId, string license, int experience);

        // Справочники
        Task<List<Medication>> GetMedications(string search);
        Task AddMedication(Medication medication);
        Task<bool> DeleteMedication(int id);

        Task<List<Specialization>> GetSpecializations();
        Task AddSpecialization(Specialization spec);
        Task UpdateSpecializationTime(int id, int minutes);
        Task<bool> DeleteSpecialization(int id);

        Task<List<Diagnosis>> GetDiagnoses(string search);
        Task<(bool Success, string Error)> AddDiagnosis(Diagnosis diagnosis);
        Task UpdateDiagnosis(Diagnosis diagnosis);
        Task<bool> DeleteDiagnosis(int id);

        // Расписание
        Task<List<Doctor>> GetDoctorsWithSchedules();
        Task<Doctor?> GetDoctorWithSchedule(int doctorId);
        Task AddSchedule(Schedule schedule);
        Task ToggleSchedule(int scheduleId);
        Task<bool> DeleteSchedule(int scheduleId);
        Task UpdateSchedule(Schedule schedule);
        Task GenerateBulkSchedule(int doctorId, List<int> daysOfWeek, TimeSpan startTime, TimeSpan endTime, int duration);
    }
}