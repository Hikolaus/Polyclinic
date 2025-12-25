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

                while (currentTime.AddMinutes(schedule.SlotDurationMinutes) <= endTime)
                {
                    var slotEndTime = currentTime.AddMinutes(schedule.SlotDurationMinutes);

                    var isBreak = schedule.BreakStart.HasValue && schedule.BreakEnd.HasValue &&
                                 currentTime.TimeOfDay >= schedule.BreakStart.Value &&
                                 slotEndTime.TimeOfDay <= schedule.BreakEnd.Value;

                    if (!isBreak)
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
            dateTime = dateTime.AddSeconds(-dateTime.Second).AddMilliseconds(-dateTime.Millisecond);

            var dayOfWeek = (int)dateTime.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            var schedules = await _context.Schedules
                .Where(s => s.DoctorId == doctorId &&
                            s.DayOfWeek == dayOfWeek &&
                            s.IsActive)
                .ToListAsync();

            if (!schedules.Any()) return false;

            var time = dateTime.TimeOfDay;
            bool isInAnySchedule = false;

            foreach (var schedule in schedules)
            {
                if (time >= schedule.StartTime && time < schedule.EndTime)
                {
                    bool isBreak = schedule.BreakStart.HasValue && schedule.BreakEnd.HasValue &&
                                   time >= schedule.BreakStart.Value && time < schedule.BreakEnd.Value;

                    if (!isBreak)
                    {
                        var minutesFromStart = (time - schedule.StartTime).TotalMinutes;
                        if (minutesFromStart % schedule.SlotDurationMinutes == 0)
                        {
                            isInAnySchedule = true;
                            break;
                        }
                    }
                }
            }

            if (!isInAnySchedule) return false;

            var isBooked = await _context.Appointments
                .AnyAsync(a => a.DoctorId == doctorId &&
                             a.AppointmentDateTime == dateTime &&
                             (a.Status != AppointmentStatus.Cancelled &&
                              a.Status != AppointmentStatus.NoShow));

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

        public async Task<List<object>> GetMonthAvailability(int doctorId, int year, int month)
        {
            var schedules = await _context.Schedules.Where(s => s.DoctorId == doctorId && s.IsActive).ToListAsync();
            if (!schedules.Any()) return new List<object>();

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                       a.AppointmentDateTime >= startDate &&
                       a.AppointmentDateTime < endDate &&
                       (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed))
                .ToListAsync();

            var result = new List<object>();
            var maxDate = DateTime.Today.AddDays(14);

            int daysInMonth = DateTime.DaysInMonth(year, month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);

                if (date.Date < DateTime.Today || date.Date > maxDate) continue;

                int dayOfWeek = (int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek;

                var dailySchedules = schedules.Where(s => s.DayOfWeek == dayOfWeek).ToList();

                if (dailySchedules.Any())
                {
                    int totalCapacity = 0;

                    foreach (var sch in dailySchedules)
                    {
                        double totalMinutes = (sch.EndTime - sch.StartTime).TotalMinutes;
                        if (sch.BreakStart.HasValue && sch.BreakEnd.HasValue)
                            totalMinutes -= (sch.BreakEnd.Value - sch.BreakStart.Value).TotalMinutes;

                        int cap = (int)(totalMinutes / sch.SlotDurationMinutes);
                        if (sch.MaxPatients > 0) cap = Math.Min(cap, sch.MaxPatients);

                        totalCapacity += cap;
                    }

                    int booked = appointments.Count(a => a.AppointmentDateTime.Date == date.Date);

                    result.Add(new { day, fullDate = date.ToString("yyyy-MM-dd"), available = true, isFull = booked >= totalCapacity });
                }
            }
            return result;
        }
    }
}