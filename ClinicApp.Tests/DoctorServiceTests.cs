using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Microsoft.AspNetCore.Http;
using ClinicApp.Data;
using ClinicApp.Services.DoctorService;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class DoctorServiceTests
    {
        private ClinicContext _context;
        private DoctorService _service;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<ISession> _sessionMock;
        private Mock<HttpContext> _httpContextMock;
        private NotificationService _notifService;
        private AuthService _authService;

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
            _notifService = new NotificationService(_context);
            _service = new DoctorService(_context, _authService, _notifService);

            _context.Users.Add(new User { Id = 1, Login = "doc", Role = "Doctor", FullName = "Dr", IsActive = true });
            _context.Doctors.Add(new Doctor { Id = 1, LicenseNumber = "123", SpecializationId = 1 });
            _context.Specializations.Add(new Specialization { Id = 1, Name = "Spec" });
            _context.SaveChanges();

            byte[] idBytes = BitConverter.GetBytes(1);
            if (BitConverter.IsLittleEndian) Array.Reverse(idBytes);
            _sessionMock.Setup(s => s.TryGetValue("UserId", out idBytes)).Returns(true);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetCurrentDoctor_ReturnsDoc()
        {
            var doc = await _service.GetCurrentDoctor();
            Assert.IsNotNull(doc);
            Assert.AreEqual(1, doc.Id);
        }

        [Test]
        public async Task GetDoctorSchedule_ReturnsSchedule()
        {
            _context.Schedules.Add(new Schedule { DoctorId = 1, DayOfWeek = 1, StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromHours(1), IsActive = true });
            await _context.SaveChangesAsync();
            var s = await _service.GetDoctorSchedule();
            Assert.AreEqual(1, s.Count);
        }

        [Test]
        public async Task GetAppointments_FiltersCorrectly()
        {
            _context.Users.Add(new User { Id = 2, FullName = "Pat", Role = "Patient" });
            _context.Patients.Add(new Patient { Id = 2, PolicyNumber = "123", DateOfBirth = DateTime.Now, Gender = "M" });

            _context.Appointments.Add(new Appointment { DoctorId = 1, PatientId = 2, AppointmentDateTime = DateTime.Today.AddHours(12), Status = AppointmentStatus.Scheduled });
            await _context.SaveChangesAsync();

            var today = await _service.GetTodayAppointments();
            var all = await _service.GetAppointments();
            var upcoming = await _service.GetUpcomingAppointments();

            Assert.AreEqual(1, today.Count);
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual(1, upcoming.Count);
        }

        [Test]
        public async Task UpdateAppointmentStatus_UpdatesAndNotifies()
        {
            _context.Appointments.Add(new Appointment { Id = 1, DoctorId = 1, PatientId = 1, AppointmentDateTime = DateTime.Today, Status = AppointmentStatus.Scheduled });
            await _context.SaveChangesAsync();

            await _service.UpdateAppointmentStatus(1, AppointmentStatus.InProgress);

            var app = await _context.Appointments.FindAsync(1);
            Assert.AreEqual(AppointmentStatus.InProgress, app.Status);
            Assert.AreEqual(1, await _context.Notifications.CountAsync());
        }

        [Test]
        public async Task CreatePrescription_AddsToDb()
        {
            _context.Users.Add(new User { Id = 2, Role = "Patient", FullName = "P" });
            _context.Patients.Add(new Patient { Id = 2, PolicyNumber = "1", DateOfBirth = DateTime.Now, Gender = "M" });

            _context.Medications.Add(new Medication { Id = 1, Name = "M", PrescriptionRequired = true });
            await _context.SaveChangesAsync();

            var res = await _service.CreatePrescription(new Prescription { PatientId = 2, MedicationId = 1, ExpiryDate = DateTime.Now.AddDays(1), Dosage = "1" });
            Assert.IsTrue(res);

            var list = await _service.GetDoctorPrescriptions();
            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public async Task CompleteConsultationAsync_TransactionsWorks()
        {
            _context.Appointments.Add(new Appointment { Id = 10, DoctorId = 1, PatientId = 2, Status = AppointmentStatus.InProgress, AppointmentDateTime = DateTime.Now });
            _context.Diagnoses.Add(new Diagnosis { Id = 1, Code = "A", Name = "B" });
            await _context.SaveChangesAsync();

            var model = new ConsultationViewModel
            {
                AppointmentId = 10,
                DiagnosisId = 1,
                Meds = new List<PrescriptionItem> { new PrescriptionItem { MedicationId = 1, Dosage = "1" } }
            };

            var res = await _service.CompleteConsultationAsync(model);
            Assert.IsTrue(res);

            var app = await _context.Appointments.FindAsync(10);
            Assert.AreEqual(AppointmentStatus.Completed, app.Status);
            Assert.AreEqual(1, await _context.MedicalRecords.CountAsync());
        }

        [Test]
        public async Task SearchPatients_ReturnsResults()
        {
            _context.Users.Add(new User { Id = 2, FullName = "John Doe", Role = "Patient" });
            _context.Patients.Add(new Patient { Id = 2, PolicyNumber = "111", DateOfBirth = DateTime.Now, Gender = "M" });
            await _context.SaveChangesAsync();

            var res = await _service.SearchPatients("Doe");
            Assert.AreEqual(1, res.Count);
        }

        [Test]
        public async Task GetPatientDetails_ReturnsPatient()
        {
            _context.Users.Add(new User { Id = 2, FullName = "P", Role = "Patient" });
            _context.Patients.Add(new Patient { Id = 2, PolicyNumber = "1", DateOfBirth = DateTime.Now, Gender = "M" });
            await _context.SaveChangesAsync();

            var p = await _service.GetPatientDetails(2);
            Assert.IsNotNull(p);
        }

        [Test]
        public async Task GetConsultationData_ReturnsLists()
        {
            _context.Medications.Add(new Medication { Name = "A", PrescriptionRequired = false });
            _context.Medications.Add(new Medication { Name = "B", PrescriptionRequired = true });
            _context.Diagnoses.Add(new Diagnosis { Code = "A", Name = "B" });
            await _context.SaveChangesAsync();

            var (all, strict, diags) = await _service.GetConsultationData();
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual(1, strict.Count);
            Assert.AreEqual(1, diags.Count);
        }

        [Test]
        public async Task SearchHelpers_Work()
        {
            _context.Diagnoses.Add(new Diagnosis { Code = "A00", Name = "Flu" });
            _context.Medications.Add(new Medication { Name = "Asp" });
            await _context.SaveChangesAsync();

            var diags = await _service.SearchDiagnoses("Flu");
            var meds = await _service.SearchMedications("Asp");

            Assert.AreEqual(1, diags.Count);
            Assert.AreEqual(1, meds.Count);
        }

        [Test]
        public async Task GetAppointmentForConsultation_ReturnsApp()
        {
            _context.Appointments.Add(new Appointment { Id = 99, DoctorId = 1, PatientId = 2, AppointmentDateTime = DateTime.Now });
            _context.Users.Add(new User { Id = 2, Role = "Patient", FullName = "P" });
            _context.Patients.Add(new Patient { Id = 2, PolicyNumber = "1", DateOfBirth = DateTime.Now, Gender = "M" });
            await _context.SaveChangesAsync();

            var app = await _service.GetAppointmentForConsultation(99);
            Assert.IsNotNull(app);
        }
    }
}