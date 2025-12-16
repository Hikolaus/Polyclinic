using System.ComponentModel.DataAnnotations.Schema;
using ClinicApp.Models.PatientModels;

namespace ClinicApp.Models.Core
{
    public class MedicalRecord
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int AppointmentId { get; set; }
        public string? Complaints { get; set; }
        public string? Diagnosis { get; set; }
        public int? DiagnosisId { get; set; }
        [ForeignKey("DiagnosisId")]
        public virtual Diagnosis? DiagnosisRef { get; set; }

        public string? Treatment { get; set; }
        public string? Recommendations { get; set; }
        public DateTime RecordDate { get; set; } = DateTime.Now;
        public string? Symptoms { get; set; }
        public string? TestResults { get; set; }

        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }
    }
}