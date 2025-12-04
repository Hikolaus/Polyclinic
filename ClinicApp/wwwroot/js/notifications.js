function markRead(id, btn) {

    btn.disabled = true;

    fetch('/Notifications/MarkAsRead', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(id)
    })
        .then(response => {
            if (response.ok) {
                moveCardToHistory(id, btn);

                if (typeof checkNotifications === 'function') {
                    checkNotifications();
                }
            } else {
                console.error('Ошибка сервера');
                btn.disabled = false;
            }
        })
        .catch(error => console.error('Ошибка сети:', error));
}
function moveCardToHistory(id, btn) {
    const card = document.getElementById('notif-' + id);
    if (!card) return;

    card.classList.add('fading-out');

    setTimeout(() => {

        btn.remove();

        card.classList.remove('border-primary', 'shadow-sm', 'fading-out');
        card.classList.add('is-read', 'bg-light');

        const historyContainer = document.getElementById('readContainer');
        const unreadContainer = document.getElementById('unreadContainer');

        historyContainer.insertBefore(card, historyContainer.querySelector('.card') || null);

        const badge = document.getElementById('unreadCounter');
        if (badge) {
            let count = parseInt(badge.innerText);
            count = Math.max(0, count - 1);
            badge.innerText = count;

            if (count === 0) {
                unreadContainer.innerHTML = '<div class="alert alert-light text-center text-muted">Новых уведомлений нет</div>';
            }
        }
    }, 300);
}