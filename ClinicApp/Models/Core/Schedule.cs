using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicApp.Models.DoctorModels;

namespace ClinicApp.Models.Core
{
    public class Schedule
    {
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        [Range(1, 7)]
        public int DayOfWeek { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;

        [DataType(DataType.Time)]
        public TimeSpan? BreakStart { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? BreakEnd { get; set; }

        public int MaxPatients { get; set; } = 10;

        public int SlotDurationMinutes { get; set; } = 15;

        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }
        public int GetAvailableSlots()
        {
            var totalMinutes = (EndTime - StartTime).TotalMinutes;
            double breakMinutes = 0;

            if (BreakStart.HasValue && BreakEnd.HasValue)
            {
                breakMinutes = (BreakEnd.Value - BreakStart.Value).TotalMinutes;
            }

            var availableMinutes = totalMinutes - breakMinutes;
            return (int)(availableMinutes / SlotDurationMinutes);
        }
    }
}