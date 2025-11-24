using System.ComponentModel.DataAnnotations;
using ClinicApp.Models.DoctorModels;

namespace ClinicApp.Models.Core
{
    public class Specialization
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int AverageConsultationTime { get; set; } = 30;

        public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    }
}