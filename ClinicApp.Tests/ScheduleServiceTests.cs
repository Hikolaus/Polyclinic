using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ClinicApp.Data;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class ScheduleServiceTests
    {
        private ClinicContext _context;
        private ScheduleService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ClinicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new ClinicContext(options);
            _service = new ScheduleService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task GetAvailableTimeSlots_GeneratesSlots()
        {
            var date = DateTime.Today.AddDays(1);
            int dayOfWeek = (int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek;

            _context.Schedules.Add(new Schedule { DoctorId = 1, DayOfWeek = dayOfWeek, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0), SlotDurationMinutes = 30, IsActive = true });
            await _context.SaveChangesAsync();

            var slots = await _service.GetAvailableTimeSlots(1, date);
            Assert.AreEqual(2, slots.Count);
        }

        [Test]
        public async Task GetAvailableTimeSlots_RespectsBreaks()
        {
            var date = DateTime.Today.AddDays(1);
            int dayOfWeek = (int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek;

            _context.Schedules.Add(new Schedule
            {
                DoctorId = 1,
                DayOfWeek = dayOfWeek,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                BreakStart = new TimeSpan(9, 30, 0),
                BreakEnd = new TimeSpan(10, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            });
            await _context.SaveChangesAsync();

            var slots = await _service.GetAvailableTimeSlots(1, date);
            Assert.AreEqual(3, slots.Count);
        }

        [Test]
        public async Task GetAvailableTimeSlots_MarksBooked()
        {
            var date = DateTime.Today.AddDays(1);
            int dayOfWeek = (int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek;

            _context.Schedules.Add(new Schedule { DoctorId = 1, DayOfWeek = dayOfWeek, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0), SlotDurationMinutes = 30, IsActive = true });
            _context.Appointments.Add(new Appointment { DoctorId = 1, AppointmentDateTime = date.Date.AddHours(9), Status = AppointmentStatus.Scheduled });
            await _context.SaveChangesAsync();

            var slots = await _service.GetAvailableTimeSlots(1, date);
            Assert.IsFalse(slots.First(s => s.StartTime.Hour == 9).IsAvailable);
            Assert.IsTrue(slots.Last().IsAvailable);
        }

        [Test]
        public async Task IsTimeSlotAvailable_ChecksLogic()
        {
            var date = DateTime.Today.AddDays(1);
            int dayOfWeek = (int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek;

            _context.Schedules.Add(new Schedule { DoctorId = 1, DayOfWeek = dayOfWeek, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0), SlotDurationMinutes = 30, IsActive = true });
            await _context.SaveChangesAsync();

            Assert.IsTrue(await _service.IsTimeSlotAvailable(1, date.Date.AddHours(9)));
            Assert.IsFalse(await _service.IsTimeSlotAvailable(1, date.Date.AddHours(9).AddMinutes(15)));

            _context.Appointments.Add(new Appointment { DoctorId = 1, AppointmentDateTime = date.Date.AddHours(9), Status = AppointmentStatus.Scheduled });
            await _context.SaveChangesAsync();
            Assert.IsFalse(await _service.IsTimeSlotAvailable(1, date.Date.AddHours(9)));
        }

        [Test]
        public async Task GetDoctorSchedules_ReturnsList()
        {
            _context.Schedules.Add(new Schedule { DoctorId = 1, DayOfWeek = 1, StartTime = TimeSpan.Zero, EndTime = TimeSpan.Zero, IsActive = true });
            await _context.SaveChangesAsync();
            Assert.AreEqual(1, (await _service.GetDoctorSchedules(1)).Count);
        }

        [Test]
        public async Task GetMonthAvailability_ReturnsData()
        {
            var today = DateTime.Today;
            int dayOfWeek = (int)today.DayOfWeek == 0 ? 7 : (int)today.DayOfWeek;

            _context.Schedules.Add(new Schedule { DoctorId = 1, DayOfWeek = dayOfWeek, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0), SlotDurationMinutes = 60, IsActive = true });
            await _context.SaveChangesAsync();

            var res = await _service.GetMonthAvailability(1, today.Year, today.Month);
            Assert.IsNotEmpty(res);
        }
    }
}