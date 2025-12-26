using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ClinicApp.Data;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class AdminTests
    {
        private ClinicContext _context;
        private AdminService _adminService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ClinicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ClinicContext(options);
            _adminService = new AdminService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task GenerateBulkSchedule_CreatesSlotsCorrectly()
        {
            var doctor = new Doctor { Id = 1, SpecializationId = 1, LicenseNumber = "123" };
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            var days = new List<int> { 1, 2 };
            var start = new TimeSpan(9, 0, 0);
            var end = new TimeSpan(12, 0, 0);
            int duration = 30;

            await _adminService.GenerateBulkSchedule(1, days, start, end, duration);

            var schedules = await _context.Schedules.ToListAsync();

            Assert.AreEqual(2, schedules.Count, "Должно создаться 2 записи расписания (Пн и Вт)");
            Assert.AreEqual(1, schedules[0].DoctorId);
            Assert.AreEqual(30, schedules[0].SlotDurationMinutes);
        }

        [Test]
        public async Task GenerateBulkSchedule_SkipExistingDays()
        {
            var doctor = new Doctor { Id = 1, SpecializationId = 1, LicenseNumber = "123" };
            _context.Doctors.Add(doctor);
            _context.Schedules.Add(new Schedule { DoctorId = 1, DayOfWeek = 1, StartTime = TimeSpan.Zero, EndTime = TimeSpan.Zero, IsActive = true });
            await _context.SaveChangesAsync();

            var days = new List<int> { 1, 2 };

            await _adminService.GenerateBulkSchedule(1, days, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), 15);

            var count = await _context.Schedules.CountAsync();
            Assert.AreEqual(2, count, "Должно быть 2 записи: 1 старая (Пн) + 1 новая (Вт). Пн не должен дублироваться.");
        }
    }
}