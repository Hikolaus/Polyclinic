using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Moq;
using ClinicApp.Data;
using ClinicApp.Services.PatientService;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class PatientLogicTests
    {
        private ClinicContext _context;
        private PatientService _patientService;
        private ScheduleService _scheduleService;
        private Mock<IAuthService> _authServiceMock;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ClinicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ClinicContext(options);
            _authServiceMock = new Mock<IAuthService>();

            _scheduleService = new ScheduleService(_context);
            _patientService = new PatientService(_context, _authServiceMock.Object, _scheduleService);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task GetAvailableTimeSlots_ReturnsCorrectSlots_ExcludingBreaks()
        {
            int docId = 1;
            var schedule = new Schedule
            {
                DoctorId = docId,
                DayOfWeek = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            };
            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            var date = new DateTime(2025, 12, 29);

            var slots = await _scheduleService.GetAvailableTimeSlots(docId, date);

            Assert.AreEqual(2, slots.Count);
            Assert.AreEqual(new DateTime(2025, 12, 29, 9, 0, 0), slots[0].StartTime);
            Assert.AreEqual(new DateTime(2025, 12, 29, 9, 30, 0), slots[1].StartTime);
        }

        [Test]
        public async Task CreateAppointment_SlotTaken_ReturnsFalse()
        {

            int docId = 1;
            int patId = 10;
            var appDate = new DateTime(2025, 12, 29, 9, 0, 0);

            _context.Schedules.Add(new Schedule
            {
                DoctorId = docId,
                DayOfWeek = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            });

            var patientUser = new User { Id = patId, Role = "Patient", FullName = "Pat" };
            var patient = new Patient { Id = patId, PolicyNumber = "123", DateOfBirth = DateTime.Now, Gender = "M" };
            _context.Users.Add(patientUser);
            _context.Patients.Add(patient);

            _context.Appointments.Add(new Appointment
            {
                DoctorId = docId,
                PatientId = 99,
                AppointmentDateTime = appDate,
                Status = AppointmentStatus.Confirmed
            });
            await _context.SaveChangesAsync();

            _authServiceMock.Setup(x => x.GetCurrentUser()).Returns(patientUser);

            var newApp = new Appointment { DoctorId = docId, AppointmentDateTime = appDate };
            bool result = await _patientService.CreateAppointment(newApp);

            Assert.IsFalse(result, "Система должна запретить запись на занятое время");
        }
    }
}