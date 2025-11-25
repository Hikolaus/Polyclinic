using ClinicApp.Models.Core;

namespace ClinicApp.Services.Core
{
    public interface IAuthService
    {
        Task<User?> Authenticate(string login, string password);
        Task<bool> Register(User user);
        void Login(User user);
        void Logout();
        User? GetCurrentUser();
        string? GetCurrentUserRole();
        bool IsLoggedIn();
    }
}