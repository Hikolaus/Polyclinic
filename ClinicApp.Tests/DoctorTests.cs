using ClinicApp.Controllers;
using ClinicApp.Data;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
using ClinicApp.Services.Core;
using ClinicApp.Services.DoctorService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Text;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class DoctorTests
    {
        private ClinicContext _context;
        private SqliteConnection _connection;
        private DoctorController _controller;

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<ClinicContext>().UseSqlite(_connection).Options;
            _context = new ClinicContext(options);
            _context.Database.EnsureCreated();

            var mockAuth = new Mock<IAuthService>();
            var mockNotif = new Mock<INotificationService>();
            var docService = new DoctorService(_context, mockAuth.Object, mockNotif.Object);

            _controller = new DoctorController(docService, _context, mockAuth.Object);

            var mockSession = new Mock<ISession>();
            byte[] roleBytes = Encoding.UTF8.GetBytes("Doctor");
            mockSession.Setup(s => s.TryGetValue("UserRole", out roleBytes)).Returns(true);

            var docUser = new User { Id = 20, Role = "Doctor", Login = "doc", PasswordHash = "1", FullName = "Dr. House" };
            mockAuth.Setup(a => a.GetCurrentUser()).Returns(docUser);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { Session = mockSession.Object }
            };
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            _context.Specializations.Add(new Specialization { Id = 1, Name = "Терапевт" });
            _context.Users.Add(docUser);
            _context.Doctors.Add(new Doctor { Id = 20, LicenseNumber = "L1", SpecializationId = 1 });

            var patUser = new User { Id = 10, Role = "Patient", Login = "p", PasswordHash = "1", FullName = "Patient Ivanov" };
            _context.Users.Add(patUser);
            _context.Patients.Add(new Patient { Id = 10, PolicyNumber = "12345" });

            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
            _connection?.Close();
        }

        [Test]
        public async Task CompleteConsultation_SavesAllData()
        {
            _context.Diagnoses.Add(new Diagnosis { Id = 5, Code = "J00", Name = "Test Diag" });
            _context.Medications.Add(new Medication { Id = 5, Name = "Аспирин" });
            await _context.SaveChangesAsync();

            var appt = new Appointment { Id = 1, PatientId = 10, DoctorId = 20, Status = AppointmentStatus.InProgress, AppointmentDateTime = DateTime.Now };
            _context.Appointments.Add(appt);
            await _context.SaveChangesAsync();

            var model = new ConsultationViewModel
            {
                AppointmentId = 1,
                Symptoms = "Температура 38",
                DiagnosisId = 5,
                Recipes = new List<PrescriptionItem> { new PrescriptionItem { MedicationId = 5, Dosage = "1 таб" } }
            };

            await _controller.CompleteConsultation(model);

            var dbAppt = await _context.Appointments.FindAsync(1);
            Assert.AreEqual(AppointmentStatus.Completed, dbAppt.Status);
            Assert.AreEqual(1, _context.Prescriptions.Count());
        }

        [Test]
        public async Task SearchPatients_ByName_ReturnsCorrectResult()
        {
            var result = await _controller.Patients("Ivanov") as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as List<Patient>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual("12345", model[0].PolicyNumber);
        }

        [Test]
        public async Task MarkNoShow_UpdatesStatus()
        {
            var appt = new Appointment { Id = 55, PatientId = 10, DoctorId = 20, Status = AppointmentStatus.Scheduled, AppointmentDateTime = DateTime.Now };
            _context.Appointments.Add(appt);
            await _context.SaveChangesAsync();

            await _controller.MarkNoShow(55);

            var dbAppt = await _context.Appointments.FindAsync(55);
            Assert.AreEqual(AppointmentStatus.NoShow, dbAppt.Status);
        }
    }
}