using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicApp.Models.Core
{
    public class Administrator
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string? Department { get; set; }

        public string? Responsibilities { get; set; }

        [ForeignKey("Id")]
        public virtual User? User { get; set; }

        [NotMapped]
        public string FullName => User?.FullName ?? string.Empty;
        [NotMapped]
        public string? Email => User?.Email;
        [NotMapped]
        public string? Phone => User?.Phone;
        [NotMapped]
        public bool IsActive => User?.IsActive ?? false;
    }
}