using System.ComponentModel.DataAnnotations.Schema;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;

namespace ClinicApp.Models.Core
{
    public class WaitlistRequest
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsNotified { get; set; } = false;

        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }
    }
}