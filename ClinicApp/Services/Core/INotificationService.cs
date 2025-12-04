using ClinicApp.Models.Core;

namespace ClinicApp.Services.Core
{
    public interface INotificationService
    {
        Task NotifyAppointmentCreated(Appointment appointment);
        Task NotifyAppointmentStatusChanged(Appointment appointment, string oldStatus);
        Task NotifyPrescriptionCreated(Prescription prescription);
        Task<List<Notification>> GetUserNotifications(int userId);
        Task MarkAsRead(int notificationId);
        Task MarkAllAsRead(int userId);
        Task<int> GetUnreadCount(int userId);
    }
}