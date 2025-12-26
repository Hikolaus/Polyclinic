using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using ClinicApp.Data;
using ClinicApp.Services.DoctorService;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class DoctorTests
    {
        private ClinicContext _context;
        private DoctorService _doctorService;
        private Mock<IAuthService> _authServiceMock;
        private Mock<INotificationService> _notifMock;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ClinicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ClinicContext(options);
            _authServiceMock = new Mock<IAuthService>();
            _notifMock = new Mock<INotificationService>();

            _doctorService = new DoctorService(_context, _authServiceMock.Object, _notifMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task CompleteConsultation_Logic_UpdatesStatusAndCreatesRecords()
        {
            int appId = 55;
            int docId = 10;
            int patId = 20;

            _context.Appointments.Add(new Appointment
            {
                Id = appId,
                DoctorId = docId,
                PatientId = patId,
                Status = AppointmentStatus.InProgress,
                AppointmentDateTime = DateTime.Now,
                Reason = "Test"
            });
            _context.Diagnoses.Add(new Diagnosis { Id = 1, Code = "J00", Name = "Test Diag" });
            await _context.SaveChangesAsync();

            var model = new ConsultationViewModel
            {
                AppointmentId = appId,
                DiagnosisId = 1,
                Symptoms = "Cough",
                Treatment = "Water",
                Recommendations = "Sleep",
                Meds = new List<PrescriptionItem>
                {
                    new PrescriptionItem { MedicationId = 1, Dosage = "1 tab" }
                }
            };

            bool result = await _doctorService.CompleteConsultationAsync(model);

            Assert.IsTrue(result);

            var updatedApp = await _context.Appointments.FindAsync(appId);
            Assert.AreEqual(AppointmentStatus.Completed, updatedApp.Status, "Статус должен смениться на Completed");

            var record = await _context.MedicalRecords.FirstOrDefaultAsync(m => m.AppointmentId == appId);
            Assert.IsNotNull(record, "Должна создаться запись в медкарте");
            Assert.AreEqual("Cough", record.Symptoms);

            var prescription = await _context.Prescriptions.FirstOrDefaultAsync(p => p.AppointmentId == appId);
            Assert.IsNotNull(prescription, "Должен создаться рецепт");
            Assert.AreEqual("1 tab", prescription.Dosage);
        }
    }
}