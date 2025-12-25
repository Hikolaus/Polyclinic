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
            var notification = new Notification
            {
                UserId = appointment.PatientId,
                Title = "Запись создана",
                Message = $"Вы успешно записаны к врачу на {appointment.AppointmentDateTime:dd.MM.yyyy в HH:mm}.",
                Type = NotificationType.Appointment,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyAppointmentStatusChanged(Appointment appointment, string oldStatus)
        {
            string friendlyStatus = GetFriendlyStatusDescription(appointment.Status);
            string title = "Изменение статуса приема";

            if (appointment.Status == AppointmentStatus.InProgress)
            {
                title = "Прием начался";
            }
            else if (appointment.Status == AppointmentStatus.Completed)
            {
                title = "Прием завершен";
            }

            var notification = new Notification
            {
                UserId = appointment.PatientId,
                Title = title,
                Message = $"{friendlyStatus}. Дата: {appointment.AppointmentDateTime:dd.MM.yyyy HH:mm}",
                Type = NotificationType.Appointment,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyPrescriptionCreated(Prescription prescription)
        {
            var notification = new Notification
            {
                UserId = prescription.PatientId,
                Title = "Новый рецепт/назначение",
                Message = $"Врач выписал вам препарат: {prescription.Medication?.Name}. Проверьте раздел 'Лекарства' в медкарте.",
                Type = NotificationType.Prescription,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotifications(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();
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

        private string GetFriendlyStatusDescription(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Scheduled => "Ваша запись подтверждена",
                AppointmentStatus.Confirmed => "Врач подтвердил вашу запись",
                AppointmentStatus.InProgress => "Ваш прием у врача начался",
                AppointmentStatus.Completed => "Ваш прием завершен, результаты в медкарте",
                AppointmentStatus.Cancelled => "Запись была отменена",
                AppointmentStatus.NoShow => "Отмечена неявка на прием",
                _ => "Статус обновлен"
            };
        }
    }
}