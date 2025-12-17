using ClinicApp.Models.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClinicApp.Services.Core
{
    public interface IScheduleService
    {
        Task<List<TimeSlot>> GetAvailableTimeSlots(int doctorId, DateTime date);
        Task<bool> IsTimeSlotAvailable(int doctorId, DateTime dateTime);
        Task<List<Schedule>> GetDoctorSchedules(int doctorId);
        Task<List<object>> GetMonthAvailability(int doctorId, int year, int month);
    }

    public class TimeSlot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsBreak { get; set; }
    }
}