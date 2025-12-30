using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ClinicApp.Data;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class AdminServiceTests
    {
        private ClinicContext _context;
        private AdminService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ClinicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new ClinicContext(options);
            _service = new AdminService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetDashboardStats_ReturnsCorrectCounts()
        {
            _context.Patients.Add(new Patient { Id = 1, PolicyNumber = "1", DateOfBirth = DateTime.Now.AddYears(-20), Gender = "M" });
            _context.Doctors.Add(new Doctor { Id = 2, LicenseNumber = "L1", SpecializationId = 1 });
            _context.Appointments.Add(new Appointment { Id = 1, PatientId = 1, DoctorId = 2, AppointmentDateTime = DateTime.Today });
            _context.Diagnoses.Add(new Diagnosis { Id = 1, Code = "A", Name = "B" });
            _context.MedicalRecords.Add(new MedicalRecord { Id = 1, DiagnosisId = 1, AppointmentId = 1 });
            await _context.SaveChangesAsync();

            var stats = await _service.GetDashboardStats();

            Assert.AreEqual(1, stats["TotalPatients"]);
            Assert.AreEqual(1, stats["TotalDoctors"]);
            Assert.AreEqual(1, stats["TotalAppointments"]);
            Assert.IsNotNull(stats["ChartDates"]);
            Assert.IsNotNull(stats["AgeData"]);
        }

        [Test]
        public async Task GetUsers_FiltersBySearchAndRole()
        {
            _context.Users.Add(new User { Login = "admin", Role = "Administrator", FullName = "Admin User", IsActive = true });
            _context.Users.Add(new User { Login = "doc", Role = "Doctor", FullName = "Doctor Who", IsActive = true });
            await _context.SaveChangesAsync();

            var all = await _service.GetUsers("", "");
            var doctors = await _service.GetUsers("", "Doctor");
            var search = await _service.GetUsers("Who", "");

            Assert.AreEqual(2, all.Count);
            Assert.AreEqual(1, doctors.Count);
            Assert.AreEqual(1, search.Count);
        }

        [Test]
        public async Task ToggleUserStatus_SwitchesActiveState()
        {
            var user = new User { Login = "u", Role = "Patient", IsActive = true, FullName = "U" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.ToggleUserStatus(user.Id);
            var updated = await _context.Users.FindAsync(user.Id);
            Assert.IsFalse(updated.IsActive);

            await _service.ToggleUserStatus(user.Id);
            updated = await _context.Users.FindAsync(user.Id);
            Assert.IsTrue(updated.IsActive);
        }

        [Test]
        public async Task RegisterDoctor_Success_CreatesUserAndDoctor()
        {
            var result = await _service.RegisterDoctor("doc1", "pass", "Name", "email", "phone", 1, "Lic123", 5);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, await _context.Users.CountAsync());
            Assert.AreEqual(1, await _context.Doctors.CountAsync());
        }

        [Test]
        public async Task RegisterDoctor_LoginTaken_ReturnsError()
        {
            _context.Users.Add(new User { Login = "doc1", Role = "Patient", FullName = "Existing", IsActive = true });
            await _context.SaveChangesAsync();

            var result = await _service.RegisterDoctor("doc1", "pass", "Name", "email", "phone", 1, "Lic123", 5);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Логин занят", result.Error);
        }

        [Test]
        public async Task Medications_CRUD_Works()
        {
            await _service.AddMedication(new Medication { Name = "Med1" });
            var meds = await _service.GetMedications("");
            Assert.AreEqual(1, meds.Count);

            var med = meds.First();
            med.Name = "MedUpdated";
            await _service.UpdateMedication(med);
            var updated = await _context.Medications.FindAsync(med.Id);
            Assert.AreEqual("MedUpdated", updated.Name);

            var deleted = await _service.DeleteMedication(med.Id);
            Assert.IsTrue(deleted);
            Assert.AreEqual(0, await _context.Medications.CountAsync());
        }

        [Test]
        public async Task Specializations_CRUD_Works()
        {
            await _service.AddSpecialization(new Specialization { Name = "Spec1" });
            var specs = await _service.GetSpecializations();
            Assert.AreEqual(1, specs.Count);

            var spec = specs.First();
            await _service.UpdateSpecializationTime(spec.Id, 45);
            var updated = await _context.Specializations.FindAsync(spec.Id);
            Assert.AreEqual(45, updated.AverageConsultationTime);

            var deleted = await _service.DeleteSpecialization(spec.Id);
            Assert.IsTrue(deleted);
            Assert.AreEqual(0, await _context.Specializations.CountAsync());
        }

        [Test]
        public async Task Diagnoses_CRUD_Works()
        {
            var res = await _service.AddDiagnosis(new Diagnosis { Code = "A00", Name = "D1" });
            Assert.IsTrue(res.Success);

            var list = await _service.GetDiagnoses("");
            Assert.AreEqual(1, list.Count);

            var diag = list.First();
            diag.Name = "D2";
            await _service.UpdateDiagnosis(diag);
            Assert.AreEqual("D2", (await _context.Diagnoses.FindAsync(diag.Id)).Name);

            var deleted = await _service.DeleteDiagnosis(diag.Id);
            Assert.IsTrue(deleted);
        }

        [Test]
        public async Task Schedule_CRUD_Works()
        {
            _context.Doctors.Add(new Doctor { Id = 1, LicenseNumber = "1" });
            await _context.SaveChangesAsync();

            var schedule = new Schedule { DoctorId = 1, DayOfWeek = 1, StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromHours(1) };
            await _service.AddSchedule(schedule);

            var s = await _service.GetScheduleById(schedule.Id);
            Assert.IsNotNull(s);

            await _service.ToggleSchedule(s.Id);
            Assert.IsFalse((await _context.Schedules.FindAsync(s.Id)).IsActive);

            s.MaxPatients = 50;
            await _service.UpdateSchedule(s);
            Assert.AreEqual(50, (await _context.Schedules.FindAsync(s.Id)).MaxPatients);

            var deleted = await _service.DeleteSchedule(s.Id);
            Assert.IsTrue(deleted);
        }

        [Test]
        public async Task GenerateBulkSchedule_Works()
        {
            _context.Doctors.Add(new Doctor { Id = 1, LicenseNumber = "1" });
            await _context.SaveChangesAsync();

            await _service.GenerateBulkSchedule(1, new List<int> { 1, 2 }, TimeSpan.Zero, TimeSpan.FromHours(5), 30);
            Assert.AreEqual(2, await _context.Schedules.CountAsync());
        }

        [Test]
        public async Task GetDoctorsWithSchedules_ReturnsData()
        {
            _context.Specializations.Add(new Specialization { Id = 1, Name = "Spec" });
            _context.Users.Add(new User { Id = 1, FullName = "Doc", IsActive = true, Role = "Doctor" });
            _context.Doctors.Add(new Doctor { Id = 1, LicenseNumber = "1", SpecializationId = 1 });
            await _context.SaveChangesAsync();

            var docs = await _service.GetDoctorsWithSchedules();
            Assert.AreEqual(1, docs.Count);

            var doc = await _service.GetDoctorWithSchedule(1);
            Assert.IsNotNull(doc);
        }
    }
}