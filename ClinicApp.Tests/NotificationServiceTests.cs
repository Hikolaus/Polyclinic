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
    public class NotificationServiceTests
    {
        private ClinicContext _context;
        private NotificationService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ClinicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new ClinicContext(options);
            _service = new NotificationService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task NotifyAppointmentCreated_AddsNotification()
        {
            await _service.NotifyAppointmentCreated(new Appointment { PatientId = 1, AppointmentDateTime = DateTime.Now });
            Assert.AreEqual(1, await _context.Notifications.CountAsync());
        }

        [Test]
        public async Task NotifyAppointmentStatusChanged_AddsNotification()
        {
            await _service.NotifyAppointmentStatusChanged(new Appointment { PatientId = 1, Status = AppointmentStatus.InProgress, AppointmentDateTime = DateTime.Now }, "Scheduled");

            var notif = await _context.Notifications.FirstAsync();
            Assert.AreEqual(1, await _context.Notifications.CountAsync());
            Assert.That(notif.Message, Does.Contain("Ваш прием у врача начался"));
        }

        [Test]
        public async Task NotifyPrescriptionCreated_AddsNotification()
        {
            await _service.NotifyPrescriptionCreated(new Prescription { PatientId = 1, Medication = new Medication { Name = "A" } });
            Assert.AreEqual(1, await _context.Notifications.CountAsync());
        }

        [Test]
        public async Task GetUserNotifications_ReturnsList()
        {
            _context.Notifications.Add(new Notification { UserId = 1, Message = "A" });
            await _context.SaveChangesAsync();
            Assert.AreEqual(1, (await _service.GetUserNotifications(1)).Count);
        }

        [Test]
        public async Task MarkAsRead_UpdatesFlag()
        {
            _context.Notifications.Add(new Notification { Id = 1, UserId = 1, IsRead = false, Title = "T", Message = "M" });
            await _context.SaveChangesAsync();
            await _service.MarkAsRead(1);
            Assert.IsTrue((await _context.Notifications.FindAsync(1)).IsRead);
        }

        [Test]
        public async Task MarkAllAsRead_UpdatesAll()
        {
            _context.Notifications.Add(new Notification { UserId = 1, IsRead = false, Title = "T", Message = "M" });
            _context.Notifications.Add(new Notification { UserId = 1, IsRead = false, Title = "T2", Message = "M2" });
            await _context.SaveChangesAsync();

            await _service.MarkAllAsRead(1);

            Assert.IsFalse(await _context.Notifications.AnyAsync(n => !n.IsRead));
        }

        [Test]
        public async Task GetUnreadCount_ReturnsCorrectNumber()
        {
            _context.Notifications.Add(new Notification { UserId = 1, IsRead = false, Title = "T", Message = "M" });
            _context.Notifications.Add(new Notification { UserId = 1, IsRead = true, Title = "T2", Message = "M2" });
            await _context.SaveChangesAsync();
            Assert.AreEqual(1, await _service.GetUnreadCount(1));
        }
    }
}