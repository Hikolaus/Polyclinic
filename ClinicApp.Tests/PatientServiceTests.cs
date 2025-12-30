using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Microsoft.AspNetCore.Http;
using ClinicApp.Data;
using ClinicApp.Services.PatientService;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;
using ClinicApp.Models.PatientModels;
using ClinicApp.Models.DoctorModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class PatientServiceTests
    {
        private ClinicContext _context;
        private PatientService _service;
        private ScheduleService _scheduleService;
        private NotificationService _notifService;
        private AuthService _authService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<ISession> _sessionMock;
        private Mock<HttpContext> _httpContextMock;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ClinicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new ClinicContext(options);

            _sessionMock = new Mock<ISession>();
            _httpContextMock = new Mock<HttpContext>();
            _httpContextMock.Setup(s => s.Session).Returns(_sessionMock.Object);
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(_httpContextMock.Object);

            _authService = new AuthService(_context, _httpContextAccessorMock.Object);
            _scheduleService = new ScheduleService(_context);
            _notifService = new NotificationService(_context);
            _service = new PatientService(_context, _authService, _scheduleService, _notifService);

            _context.Users.Add(new User { Id = 1, Login = "pat", Role = "Patient", FullName = "Pat", IsActive = true });
            _context.Patients.Add(new Patient { Id = 1, PolicyNumber = "123", DateOfBirth = DateTime.Now, Gender = "M" });

            _context.Specializations.Add(new Specialization { Id = 1, Name = "General" });

            _context.Users.Add(new User { Id = 2, Login = "doc", Role = "Doctor", FullName = "Doc", IsActive = true });
            _context.Doctors.Add(new Doctor { Id = 2, LicenseNumber = "L1", SpecializationId = 1 });
            _context.Schedules.Add(new Schedule { DoctorId = 2, DayOfWeek = (int)DateTime.Today.DayOfWeek == 0 ? 7 : (int)DateTime.Today.DayOfWeek, StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromHours(23), SlotDurationMinutes = 60, IsActive = true });
            _context.SaveChanges();

            byte[] idBytes = BitConverter.GetBytes(1);
            if (BitConverter.IsLittleEndian) Array.Reverse(idBytes);
            _sessionMock.Setup(s => s.TryGetValue("UserId", out idBytes)).Returns(true);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task GetCurrentPatient_ReturnsPatient()
        {
            var p = await _service.GetCurrentPatient();
            Assert.IsNotNull(p);
            Assert.AreEqual(1, p.Id);
        }

        [Test]
        public async Task CreateAppointment_Success()
        {
            var app = new Appointment { DoctorId = 2, AppointmentDateTime = DateTime.Today.AddHours(12) };
            var res = await _service.CreateAppointment(app);
            Assert.IsTrue(res);
            Assert.AreEqual(1, await _context.Appointments.CountAsync());
            Assert.AreEqual(1, await _context.Notifications.CountAsync());
        }

        [Test]
        public async Task GetPatientAppointments_ReturnsList()
        {
            _context.Appointments.Add(new Appointment { PatientId = 1, DoctorId = 2, AppointmentDateTime = DateTime.Now });
            await _context.SaveChangesAsync();

            var list = await _service.GetPatientAppointments(1);
            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public async Task CancelAppointment_Success_And_NotifiesWaitlist()
        {
            _context.Appointments.Add(new Appointment { Id = 10, PatientId = 1, DoctorId = 2, Status = AppointmentStatus.Scheduled, AppointmentDateTime = DateTime.Now });
            _context.WaitlistRequests.Add(new WaitlistRequest { PatientId = 1, DoctorId = 2, IsNotified = false });
            await _context.SaveChangesAsync();

            var res = await _service.CancelAppointment(10);
            Assert.IsTrue(res);

            var app = await _context.Appointments.FindAsync(10);
            Assert.AreEqual(AppointmentStatus.Cancelled, app.Status);

            var req = await _context.WaitlistRequests.FirstAsync();
            Assert.IsTrue(req.IsNotified);
            Assert.AreEqual(1, await _context.Notifications.CountAsync());
        }

        [Test]
        public async Task JoinWaitlist_AddsRequest()
        {
            var res = await _service.JoinWaitlist(2);
            Assert.IsTrue(res);
            Assert.AreEqual(1, await _context.WaitlistRequests.CountAsync());
        }

        [Test]
        public async Task GetAvailableDoctors_ReturnsList()
        {
            var docs = await _service.GetAvailableDoctors();
            Assert.AreEqual(1, docs.Count);
        }

        [Test]
        public async Task GetPatientMedicalRecords_ReturnsList()
        {
            _context.Appointments.Add(new Appointment { Id = 5, PatientId = 1, DoctorId = 2, AppointmentDateTime = DateTime.Now });
            _context.MedicalRecords.Add(new MedicalRecord { PatientId = 1, AppointmentId = 5 });
            await _context.SaveChangesAsync();

            var recs = await _service.GetPatientMedicalRecords();
            Assert.AreEqual(1, recs.Count);
        }

        [Test]
        public async Task GetAvailableTimeSlots_ProxyTest()
        {
            var slots = await _service.GetAvailableTimeSlots(2, DateTime.Today);
            Assert.IsNotNull(slots);
        }
    }
}