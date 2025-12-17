using BCrypt.Net;
using ClinicApp.Data;
using ClinicApp.Models.Core;
using ClinicApp.Models.PatientModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Services.Core
{
    public class AuthService : IAuthService
    {
        private readonly ClinicContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(ClinicContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<User?> Authenticate(string login, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == login && u.IsActive);

            if (user == null) return null;

            try
            {
                if (user.PasswordHash.StartsWith("$2a$") || user.PasswordHash.StartsWith("$2b$"))
                {
                    return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
                }
                else
                {
                    return user.PasswordHash == password ? user : null;
                }
            }
            catch
            {
                return user.PasswordHash == password ? user : null;
            }
        }

        public async Task<bool> Register(User user)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Login == user.Login))
                    return false;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Login(User user)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Session != null)
            {
                httpContext.Session.SetInt32("UserId", user.Id);
                httpContext.Session.SetString("UserRole", user.Role);
                httpContext.Session.SetString("UserName", user.FullName ?? string.Empty);
            }
        }

        public void Logout()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            httpContext?.Session.Clear();
        }

        public User? GetCurrentUser()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.Session.GetInt32("UserId");
            if (userId == null) return null;

            return _context.Users.FirstOrDefault(u => u.Id == userId);
        }

        public string? GetCurrentUserRole()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Session.GetString("UserRole");
        }

        public bool IsLoggedIn()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Session.GetInt32("UserId") != null;
        }

        public async Task<bool> RegisterPatient(User user, Patient patient)
        {
            if (await _context.Users.AnyAsync(u => u.Login == user.Login)) return false;

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                patient.Id = user.Id;
                _context.Patients.Add(patient);
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
    }
}