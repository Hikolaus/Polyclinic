using Microsoft.AspNetCore.Mvc;
using ClinicApp.Services.Core;

namespace ClinicApp.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly IAuthService _authService;

        public NotificationsController(INotificationService notificationService, IAuthService authService)
        {
            _notificationService = notificationService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Auth");

            if (currentUser.Role != "Patient") return RedirectToAction("NotAuthorized", "Auth");

            var notifications = await _notificationService.GetUserNotifications(currentUser.Id);
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser != null && currentUser.Role == "Patient")
            {
                await _notificationService.MarkAllAsRead(currentUser.Id);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] int id)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser != null && currentUser.Role == "Patient")
            {
                await _notificationService.MarkAsRead(id);
                return Ok();
            }
            return Unauthorized();
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null || currentUser.Role != "Patient") return Json(0);

            var count = await _notificationService.GetUnreadCount(currentUser.Id);
            return Json(count);
        }
    }
}