using System.ComponentModel.DataAnnotations.Schema;
using ClinicApp.Models.PatientModels;
using ClinicApp.Models.DoctorModels;

namespace ClinicApp.Models.Core
{
    public enum PrescriptionStatus
    {
        Active,
        Used,
        Expired,
        Cancelled
    }

    public class Prescription
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int MedicationId { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public DateTime ExpiryDate { get; set; }
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Active;
        public int RemainingRepeats { get; set; } = 0;
        public int? AppointmentId { get; set; }

        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }
        [ForeignKey("MedicationId")]
        public virtual Medication? Medication { get; set; }
        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }
    }
}