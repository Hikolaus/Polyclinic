document.addEventListener('DOMContentLoaded', function () {

    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    if (document.getElementById('notificationLink')) {
        setInterval(checkNotifications, 30000);
    }
});

function checkNotifications() {
    const notifLink = document.getElementById('notificationLink');
    if (!notifLink) return;

    fetch('/Notifications/GetUnreadCount')
        .then(response => {
            if (response.ok) return response.json();
            throw new Error('Network response was not ok');
        })
        .then(count => {
            updateNotificationUI(count);
        })
        .catch(error => console.log('Ошибка проверки уведомлений (возможно, сессия истекла)'));
}

function updateNotificationUI(count) {
    const link = document.getElementById('notificationLink');
    const icon = link.querySelector('i');

    let badge = document.getElementById('notificationBadge');

    if (count > 0) {

        link.classList.add('text-warning', 'fw-bold', 'notification-active');
        icon.classList.add('bell-shake');

        if (badge) {
            badge.textContent = count;
        } else {

            badge = document.createElement('span');
            badge.id = 'notificationBadge';
            badge.className = 'position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger';
            badge.style.cssText = 'font-size: 0.65rem; margin-top: 10px; margin-left: -5px;';
            badge.textContent = count;
            link.appendChild(badge);
        }
    } else {

        link.classList.remove('text-warning', 'fw-bold', 'notification-active');
        icon.classList.remove('bell-shake');
        if (badge) badge.remove();
    }
}