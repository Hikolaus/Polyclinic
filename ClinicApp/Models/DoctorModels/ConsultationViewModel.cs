namespace ClinicApp.Models.DoctorModels
{
    public class ConsultationViewModel
    {
        public int AppointmentId { get; set; }
        public string Symptoms { get; set; } = "";

        public int DiagnosisId { get; set; }
        public string DiagnosisNote { get; set; } = "";

        public string Treatment { get; set; } = "";
        public string Recommendations { get; set; } = "";

        public List<PrescriptionItem> Meds { get; set; } = new List<PrescriptionItem>();
        public List<PrescriptionItem> Recipes { get; set; } = new List<PrescriptionItem>();
    }

    public class PrescriptionItem
    {
        public int MedicationId { get; set; }
        public string Dosage { get; set; } = "";
        public string Instructions { get; set; } = "";
    }
}