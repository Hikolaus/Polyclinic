using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicApp.Models.Core;

namespace ClinicApp.Models.DoctorModels
{
    [Table("Doctors")]
    public class Doctor
    {
        [Key]
        [ForeignKey("User")]
        public int Id { get; set; }

        public int SpecializationId { get; set; }

        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; } = string.Empty;

        public int Experience { get; set; }

        [StringLength(200)]
        public string? Qualification { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual User? User { get; set; }

        [ForeignKey("SpecializationId")]
        public virtual Specialization? Specialization { get; set; }

        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

        [NotMapped]
        public string FullName => User?.FullName ?? "Неизвестный врач";

        [NotMapped]
        public string? Email => User?.Email;

        [NotMapped]
        public string? Phone => User?.Phone;
    }
}