using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicApp.Models.Core;
namespace ClinicApp.Models.PatientModels
{
    public class Patient
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string PolicyNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Required]
        [StringLength(10)]
        public string Gender { get; set; } = string.Empty;

        [ForeignKey("Id")]
        public virtual User? User { get; set; }

        [NotMapped]
        public string FullName => User?.FullName ?? string.Empty;
        [NotMapped]
        public string? Email => User?.Email;
        [NotMapped]
        public string? Phone => User?.Phone;
        [NotMapped]
        public bool IsActive
        {
            get => User?.IsActive ?? false;
            set
            {
                if (User != null)
                    User.IsActive = value;
            }
        }

        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}