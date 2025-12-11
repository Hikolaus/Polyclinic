document.addEventListener('DOMContentLoaded', function () {
    console.log('Отчеты: страница готова');

    var printBtn = document.getElementById('printReport');
    if (printBtn) {
        printBtn.addEventListener('click', function () {
            window.print();
        });
    }

    initCharts();
});

function initCharts() {
    console.log('Графики инициализированы');
}