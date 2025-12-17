using ClinicApp.Controllers;
using ClinicApp.Data;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
using ClinicApp.Services.Core;
using ClinicApp.Services.PatientService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class PatientLogicTests
    {
        private ClinicContext _context;
        private SqliteConnection _connection;
        private PatientService _service;
        private Mock<IScheduleService> _mockSchedule;
        private Mock<IAuthService> _mockAuth;
        private PatientController _controller;

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<ClinicContext>().UseSqlite(_connection).Options;
            _context = new ClinicContext(options);
            _context.Database.EnsureCreated();

            _mockAuth = new Mock<IAuthService>();
            _mockSchedule = new Mock<IScheduleService>();

            _service = new PatientService(_context, _mockAuth.Object, _mockSchedule.Object);

            var user = new User { Id = 1, Role = "Patient", Login = "pat1", PasswordHash = "123", FullName = "Patient One" };
            _context.Users.Add(user);
            _context.Patients.Add(new Patient { Id = 1, PolicyNumber = "123" });
            _context.SaveChanges();

            _mockAuth.Setup(a => a.GetCurrentUser()).Returns(user);

            _controller = new PatientController(_service, _context);
            var mockSession = new Mock<ISession>();
            byte[] roleBytes = System.Text.Encoding.UTF8.GetBytes("Patient");
            mockSession.Setup(s => s.TryGetValue("UserRole", out roleBytes)).Returns(true);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Session = mockSession.Object } };
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
            _connection?.Close();
        }

        [Test]
        public async Task CreateAppointment_WhenSlotIsBusy_ReturnsFalse()
        {
            _context.Specializations.Add(new Specialization { Id = 1, Name = "Therapist" });
            _context.Users.Add(new User { Id = 2, Role = "Doctor", Login = "doc", PasswordHash = "p", FullName = "Doc" });
            _context.Doctors.Add(new Doctor { Id = 2, LicenseNumber = "L", SpecializationId = 1 });
            await _context.SaveChangesAsync();

            _mockSchedule.Setup(s => s.IsTimeSlotAvailable(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync(false);

            var result = await _service.CreateAppointment(new Appointment { DoctorId = 2, AppointmentDateTime = DateTime.Now });

            Assert.IsFalse(result);
            Assert.AreEqual(0, _context.Appointments.Count());
        }

        [Test]
        public async Task CancelAppointment_TriggersWaitlistNotification()
        {
            _context.Specializations.Add(new Specialization { Id = 1, Name = "S" });

            var docUser = new User { Id = 10, Role = "Doctor", Login = "d", PasswordHash = "1", FullName = "Dr. House" };
            var p2User = new User { Id = 2, Role = "Patient", Login = "p2", PasswordHash = "1", FullName = "User 2" };
            _context.Users.AddRange(docUser, p2User);

            _context.Doctors.Add(new Doctor { Id = 10, LicenseNumber = "L", SpecializationId = 1 });
            _context.Patients.Add(new Patient { Id = 2, PolicyNumber = "456" });
            await _context.SaveChangesAsync();

            var appt = new Appointment { Id = 100, PatientId = 1, DoctorId = 10, Status = AppointmentStatus.Scheduled, AppointmentDateTime = DateTime.Now };
            _context.Appointments.Add(appt);

            _context.WaitlistRequests.Add(new WaitlistRequest { PatientId = 2, DoctorId = 10, IsNotified = false });
            await _context.SaveChangesAsync();

            await _service.CancelAppointment(100);

            var waitRequest = await _context.WaitlistRequests.FirstAsync(w => w.PatientId == 2);
            Assert.IsTrue(waitRequest.IsNotified);

            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.UserId == 2);
            Assert.IsNotNull(notification);
        }

        [Test]
        public async Task Dashboard_CalculatesStatisticsCorrectly()
        {
            _context.Specializations.Add(new Specialization { Id = 1, Name = "S" });
            _context.Users.Add(new User { Id = 10, Role = "Doctor", Login = "d", PasswordHash = "p", FullName = "D" });
            _context.Doctors.Add(new Doctor { Id = 10, LicenseNumber = "L", SpecializationId = 1 });
            await _context.SaveChangesAsync();

            _context.Appointments.AddRange(
                new Appointment { PatientId = 1, DoctorId = 10, AppointmentDateTime = DateTime.Now.AddDays(1), Status = AppointmentStatus.Scheduled },
                new Appointment { PatientId = 1, DoctorId = 10, AppointmentDateTime = DateTime.Now.AddDays(2), Status = AppointmentStatus.Scheduled },
                new Appointment { PatientId = 1, DoctorId = 10, AppointmentDateTime = DateTime.Now.AddDays(-1), Status = AppointmentStatus.Completed }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.Dashboard() as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(3, _controller.ViewBag.TotalAppointments);
            Assert.AreEqual(2, _controller.ViewBag.UpcomingAppointments);
        }

        [Test]
        public async Task CreateAppointment_WhenSlotIsFree_ReturnsTrue()
        {
            _context.Specializations.Add(new Specialization { Id = 1, Name = "S" });
            _context.Users.Add(new User { Id = 2, Role = "Doctor", Login = "d", PasswordHash = "p", FullName = "D" });
            _context.Doctors.Add(new Doctor { Id = 2, LicenseNumber = "L", SpecializationId = 1 });
            await _context.SaveChangesAsync();

            _mockSchedule.Setup(s => s.IsTimeSlotAvailable(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync(true);

            var result = await _service.CreateAppointment(new Appointment
            {
                DoctorId = 2,
                AppointmentDateTime = DateTime.Now.AddDays(1),
                Reason = "Осмотр"
            });

            Assert.IsTrue(result);
            var appt = await _context.Appointments.FirstOrDefaultAsync();
            Assert.IsNotNull(appt);
            Assert.AreEqual(AppointmentStatus.Scheduled, appt.Status);
        }

        [Test]
        public async Task JoinWaitlist_AddsRequestToDatabase()
        {
            _context.Specializations.Add(new Specialization { Id = 1, Name = "S" });
            _context.Users.Add(new User { Id = 5, Role = "Doctor", Login = "doc5", PasswordHash = "p", FullName = "Doc 5" });
            _context.Doctors.Add(new Doctor { Id = 5, LicenseNumber = "L5", SpecializationId = 1 });
            await _context.SaveChangesAsync();

            var result = await _service.JoinWaitlist(5);

            Assert.IsTrue(result);
            var request = await _context.WaitlistRequests.FirstOrDefaultAsync();
            Assert.IsNotNull(request);
            Assert.AreEqual(1, request.PatientId);
            Assert.AreEqual(5, request.DoctorId);
            Assert.IsFalse(request.IsNotified);
        }
    }
}