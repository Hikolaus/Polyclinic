using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicApp.Models.Core
{
    [Table("Diagnoses")]
    public class Diagnosis
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;
        public string? DefaultTreatment { get; set; }
        public string? DefaultRecommendations { get; set; }

        [NotMapped]
        public string FullName => $"{Code} — {Name}";
    }
}