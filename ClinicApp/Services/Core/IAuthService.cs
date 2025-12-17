using ClinicApp.Models.Core;
using ClinicApp.Models.PatientModels;

namespace ClinicApp.Services.Core
{
    public interface IAuthService
    {
        Task<User?> Authenticate(string login, string password);
        Task<bool> Register(User user);
        Task<bool> RegisterPatient(User user, Patient patient);
        void Login(User user);
        void Logout();
        User? GetCurrentUser();
        string? GetCurrentUserRole();
        bool IsLoggedIn();
    }
}