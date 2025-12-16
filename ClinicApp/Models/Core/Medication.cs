using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicApp.Models.Core
{
    [Table("Medications")]
    public class Medication
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Form { get; set; }

        [StringLength(100)]
        public string? Manufacturer { get; set; }

        public string? DosageForms { get; set; }

        public string? Contraindications { get; set; }

        public bool PrescriptionRequired { get; set; } = false;
    }
}