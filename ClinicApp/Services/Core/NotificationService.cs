using Microsoft.EntityFrameworkCore;
using ClinicApp.Data;
using ClinicApp.Models.Core;

namespace ClinicApp.Services.Core
{
    public class NotificationService : INotificationService
    {
        private readonly ClinicContext _context;

        public NotificationService(ClinicContext context)
        {
            _context = context;
        }

        public async Task NotifyAppointmentCreated(Appointment appointment)
        {
            var notification = new Notification { UserId = appointment.PatientId, Title = "Новая запись", Message = $"Вы записаны к врачу на {appointment.AppointmentDateTime:dd.MM HH:mm}", Type = NotificationType.Appointment, CreatedAt = DateTime.Now };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyAppointmentStatusChanged(Appointment appointment, string oldStatus)
        {
            var notification = new Notification { UserId = appointment.PatientId, Title = "Статус изменен", Message = $"Статус записи изменен на {appointment.Status}", Type = NotificationType.Appointment, CreatedAt = DateTime.Now };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyPrescriptionCreated(Prescription prescription)
        {
            var notification = new Notification { UserId = prescription.PatientId, Title = "Новый рецепт", Message = $"Выписан препарат: {prescription.Medication?.Name}", Type = NotificationType.Prescription, CreatedAt = DateTime.Now };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotifications(int userId)
        {
            return await _context.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync();
        }

        public async Task MarkAsRead(int notificationId)
        {
            var n = await _context.Notifications.FindAsync(notificationId);
            if (n != null) { n.IsRead = true; await _context.SaveChangesAsync(); }
        }

        public async Task MarkAllAsRead(int userId)
        {
            var list = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            foreach (var n in list) n.IsRead = true;
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCount(int userId)
        {
            return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }
}