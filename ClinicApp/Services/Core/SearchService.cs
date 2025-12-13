using ClinicApp.Data;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Services.Core
{
    public class SearchService : ISearchService
    {
        private readonly ClinicContext _context;

        public SearchService(ClinicContext context)
        {
            _context = context;
        }

        public async Task<List<Patient>> SearchPatientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Patient>();

            searchTerm = searchTerm.Trim();

            return await _context.Patients
                .Include(p => p.User)
                .Where(p => p.User.FullName.Contains(searchTerm) ||
                            p.PolicyNumber.Contains(searchTerm))
                .OrderBy(p => p.User.FullName)
                .ToListAsync();
        }

        public async Task<List<Doctor>> SearchDoctorsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Doctor>();

            searchTerm = searchTerm.Trim();

            return await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .Where(d => d.User.FullName.Contains(searchTerm) ||
                            d.Specialization.Name.Contains(searchTerm))
                .OrderBy(d => d.User.FullName)
                .ToListAsync();
        }
    }
}