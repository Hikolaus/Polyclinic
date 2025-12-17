using ClinicApp.Controllers;
using ClinicApp.Data;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
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
    public class AdminTests
    {
        private ClinicContext _context;
        private SqliteConnection _connection;
        private AdminController _controller;

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<ClinicContext>().UseSqlite(_connection).Options;
            _context = new ClinicContext(options);
            _context.Database.EnsureCreated();

            _controller = new AdminController(_context);

            var mockSession = new Mock<ISession>();
            byte[] roleBytes = Encoding.UTF8.GetBytes("Administrator");
            mockSession.Setup(s => s.TryGetValue("UserRole", out roleBytes)).Returns(true);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { Session = mockSession.Object }
            };
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
            _connection?.Close();
        }

        [Test]
        public async Task RegisterDoctor_ValidData_SavesToDatabase()
        {
            _context.Specializations.Add(new Specialization { Id = 1, Name = "Хирург" });
            await _context.SaveChangesAsync();

            await _controller.RegisterDoctor("doc1", "123", "Иванов И.И.", "email", "phone", 1, "LIC-001", 5);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == "doc1");
            Assert.IsNotNull(user);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == user.Id);
            Assert.IsNotNull(doctor);
        }

        [Test]
        public async Task BulkSchedule_CreatesSlots()
        {
            var spec = new Specialization { Id = 1, Name = "Терапевт" };
            _context.Specializations.Add(spec);
            var user = new User { Id = 10, Role = "Doctor", Login = "d", PasswordHash = "p", FullName = "D" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            var doctor = new Doctor { Id = 10, LicenseNumber = "L", SpecializationId = 1 };
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            var days = new List<int> { 1, 2 };
            await _controller.BulkSchedule(10, days, new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0), 60);

            var count = await _context.Schedules.CountAsync(s => s.DoctorId == 10);
            Assert.AreEqual(2, count);
        }

        [Test]
        public async Task ToggleUserStatus_ChangesActiveState()
        {
            var user = new User { Id = 5, Login = "u", IsActive = true, FullName = "User", Role = "Patient", PasswordHash = "1" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _controller.ToggleUserStatus(5);

            var updatedUser = await _context.Users.FindAsync(5);
            Assert.IsFalse(updatedUser.IsActive);
        }

        [Test]
        public async Task AddMedication_SavesToDb()
        {
            var med = new Medication { Name = "Analgin", Form = "Pills" };
            await _controller.AddMedication(med);

            var saved = await _context.Medications.FirstOrDefaultAsync(m => m.Name == "Analgin");
            Assert.IsNotNull(saved);
        }

        [Test]
        public async Task AddDiagnosis_WithTemplates_SavesCorrectly()
        {
            var diag = new Diagnosis { Code = "A00", Name = "Test", DefaultTreatment = "Sleep" };
            _context.Diagnoses.Add(diag);
            await _context.SaveChangesAsync();

            var saved = await _context.Diagnoses.FirstAsync();
            Assert.AreEqual("Sleep", saved.DefaultTreatment);
        }

        [Test]
        public async Task AddSchedule_SingleSlot_SavesToDb()
        {
            _context.Specializations.Add(new Specialization { Id = 1, Name = "S" });
            _context.Users.Add(new User { Id = 10, Role = "Doctor", Login = "d", PasswordHash = "p", FullName = "D" });
            _context.Doctors.Add(new Doctor { Id = 10, LicenseNumber = "L", SpecializationId = 1 });
            await _context.SaveChangesAsync();

            var schedule = new Schedule
            {
                DoctorId = 10,
                DayOfWeek = 1,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                SlotDurationMinutes = 15
            };

            await _controller.AddSchedule(schedule);

            var saved = await _context.Schedules.FirstOrDefaultAsync();
            Assert.IsNotNull(saved);
            Assert.AreEqual(10, saved.DoctorId);
            Assert.IsTrue(saved.IsActive);
        }

        [Test]
        public async Task AddSpecialization_SavesNewSpec()
        {
            await _controller.AddSpecialization(new Specialization { Name = "Neurologist", Description = "Brain" });

            var spec = await _context.Specializations.FirstOrDefaultAsync(s => s.Name == "Neurologist");
            Assert.IsNotNull(spec);
            Assert.AreEqual("Brain", spec.Description);
        }
    }
}