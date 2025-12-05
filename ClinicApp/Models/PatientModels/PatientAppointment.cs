using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Models.PatientModels
{
    public class PatientAppointment
    {
        public int Id { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string PatientReason { get; set; } = string.Empty;
    }
}