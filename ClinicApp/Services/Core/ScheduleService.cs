using Microsoft.EntityFrameworkCore;
using ClinicApp.Data;
using ClinicApp.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicApp.Services.Core
{
    public class ScheduleService : IScheduleService
    {
        private readonly ClinicContext _context;

        public ScheduleService(ClinicContext context)
        {
            _context = context;
        }

        public async Task<List<TimeSlot>> GetAvailableTimeSlots(int doctorId, DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            var schedules = await _context.Schedules
                .Where(s => s.DoctorId == doctorId &&
                          s.DayOfWeek == dayOfWeek &&
                          s.IsActive)
                .ToListAsync();

            var timeSlots = new List<TimeSlot>();

            foreach (var schedule in schedules)
            {
                var currentTime = date.Date.Add(schedule.StartTime);
                var endTime = date.Date.Add(schedule.EndTime);

                while (currentTime < endTime)
                {
                    var slotEndTime = currentTime.AddMinutes(schedule.SlotDurationMinutes);

                    var isBreak = schedule.BreakStart.HasValue && schedule.BreakEnd.HasValue &&
                                 currentTime.TimeOfDay >= schedule.BreakStart.Value &&
                                 slotEndTime.TimeOfDay <= schedule.BreakEnd.Value;

                    if (!isBreak && slotEndTime <= endTime)
                    {
                        var isBooked = await _context.Appointments
                            .AnyAsync(a => a.DoctorId == doctorId &&
                                         a.AppointmentDateTime == currentTime &&
                                         (a.Status == AppointmentStatus.Scheduled ||
                                          a.Status == AppointmentStatus.Confirmed ||
                                          a.Status == AppointmentStatus.InProgress));

                        timeSlots.Add(new TimeSlot
                        {
                            StartTime = currentTime,
                            EndTime = slotEndTime,
                            IsAvailable = !isBooked,
                            IsBreak = false
                        });
                    }

                    currentTime = currentTime.AddMinutes(schedule.SlotDurationMinutes);
                }
            }

            return timeSlots;
        }

        public async Task<bool> IsTimeSlotAvailable(int doctorId, DateTime dateTime)
        {
            var dayOfWeek = (int)dateTime.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId &&
                                        s.DayOfWeek == dayOfWeek &&
                                        s.IsActive);

            if (schedule == null) return false;

            var time = dateTime.TimeOfDay;

            if (time < schedule.StartTime || time >= schedule.EndTime)
                return false;

            if (schedule.BreakStart.HasValue && schedule.BreakEnd.HasValue &&
                time >= schedule.BreakStart.Value && time < schedule.BreakEnd.Value)
                return false;

            var minutes = time.Minutes;
            if (minutes % schedule.SlotDurationMinutes != 0)
                return false;

            var isBooked = await _context.Appointments
                .AnyAsync(a => a.DoctorId == doctorId &&
                             a.AppointmentDateTime == dateTime &&
                             (a.Status == AppointmentStatus.Scheduled ||
                              a.Status == AppointmentStatus.Confirmed ||
                              a.Status == AppointmentStatus.InProgress));

            return !isBooked;
        }

        public async Task<List<Schedule>> GetDoctorSchedules(int doctorId)
        {
            return await _context.Schedules
                .Where(s => s.DoctorId == doctorId && s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }
    }
}