using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;

namespace ClinicApp.Services.Core
{
    public interface ISearchService
    {
        Task<List<Patient>> SearchPatientsAsync(string searchTerm);
        Task<List<Doctor>> SearchDoctorsAsync(string searchTerm);
    }
}