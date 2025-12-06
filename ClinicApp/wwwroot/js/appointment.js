document.addEventListener('DOMContentLoaded', function () {
    const specSelect = document.getElementById('specSelect');
    const docSelect = document.getElementById('docSelect');
    const inputDoctorId = document.getElementById('selectedDoctorId');

    const calendarWrapper = document.getElementById('calendarWrapper');
    const selectDoctorHint = document.getElementById('selectDoctorHint');
    const monthYearDisplay = document.getElementById('monthYearDisplay');
    const calendarDays = document.getElementById('calendarDays');
    const prevMonthBtn = document.getElementById('prevMonthBtn');
    const nextMonthBtn = document.getElementById('nextMonthBtn');

    const slotsContainer = document.getElementById('slotsContainer');
    const slotsList = document.getElementById('slotsList');
    const slotsLoader = document.getElementById('slotsLoader');
    const selectedDateText = document.getElementById('selectedDateText');
    const inputDateTime = document.getElementById('selectedDateTime');
    const reasonBlock = document.getElementById('reasonBlock');
    const waitlistContainer = document.getElementById('waitlistContainer');
    const btnJoinWaitlist = document.getElementById('btnJoinWaitlist');

    let currentYear = new Date().getFullYear();
    let currentMonth = new Date().getMonth() + 1;
    let availabilityCache = [];

    specSelect.addEventListener('change', async function () {
        docSelect.innerHTML = '<option>Загрузка...</option>';
        docSelect.disabled = true;
        resetCalendar();

        if (!this.value) return;

        try {
            const response = await fetch('/Patient/GetDoctorsBySpec?specId=' + this.value);
            const doctors = await response.json();

            docSelect.innerHTML = '<option value="">-- Выберите врача --</option>';
            doctors.forEach(d => {
                docSelect.innerHTML += `<option value="${d.id}">${d.name}</option>`;
            });
            docSelect.disabled = false;
        } catch (e) { console.error(e); }
    });

    docSelect.addEventListener('change', function () {
        inputDoctorId.value = this.value;

        if (this.value) {
            calendarWrapper.classList.remove('d-none');
            selectDoctorHint.classList.add('d-none');

            if (btnJoinWaitlist) {
                btnJoinWaitlist.disabled = false;
                btnJoinWaitlist.className = "btn btn-warning text-dark fw-bold px-4 shadow-sm";
                btnJoinWaitlist.innerHTML = '<i class="fas fa-bell me-2"></i> Сообщить о свободном месте';
            }
            loadMonthAvailability();
        } else {
            resetCalendar();
        }
    });

    if (btnJoinWaitlist) {
        btnJoinWaitlist.addEventListener('click', async function () {
            const docId = docSelect.value;
            if (!docId) return;

            try {
                const originalText = this.innerHTML;
                this.disabled = true;
                this.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Сохранение...';

                const response = await fetch('/Patient/JoinWaitlist', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: `doctorId=${docId}`
                });

                const result = await response.json();

                if (result.success) {
                    this.className = "btn btn-success fw-bold px-4";
                    this.innerHTML = '<i class="fas fa-check me-2"></i> Заявка принята!';
                } else {
                    alert("Ошибка или вы уже оставили заявку");
                    this.innerHTML = originalText;
                    this.disabled = false;
                }
            } catch (e) {
                console.error(e);
                alert("Ошибка сети");
            }
        });
    }

    prevMonthBtn.addEventListener('click', () => changeMonth(-1));
    nextMonthBtn.addEventListener('click', () => changeMonth(1));

    function changeMonth(delta) {
        let newDate = new Date(currentYear, currentMonth - 1 + delta, 1);
        currentYear = newDate.getFullYear();
        currentMonth = newDate.getMonth() + 1;
        loadMonthAvailability();
    }

    async function loadMonthAvailability() {
        const dateObj = new Date(currentYear, currentMonth - 1, 1);
        monthYearDisplay.innerText = dateObj.toLocaleDateString('ru-RU', { month: 'long', year: 'numeric' });
        calendarDays.innerHTML = '<div style="grid-column: 1/-1; text-align:center; padding:10px;">Загрузка...</div>';

        waitlistContainer.classList.add('d-none');

        const docId = docSelect.value;

        try {
            const response = await fetch(`/Patient/GetMonthAvailability?doctorId=${docId}&year=${currentYear}&month=${currentMonth}`);
            availabilityCache = await response.json();

            renderCalendarGrid();

            const hasAnyOpenSlot = availabilityCache.some(x => !x.isFull);
            if (!hasAnyOpenSlot && availabilityCache.length === 0) {
                waitlistContainer.classList.remove('d-none');
            }

        } catch (e) {
            console.error(e);
            calendarDays.innerHTML = '<div style="grid-column: 1/-1; text-align:center; color:red;">Ошибка</div>';
        }
    }

    function renderCalendarGrid() {
        calendarDays.innerHTML = '';

        const daysInMonth = new Date(currentYear, currentMonth, 0).getDate();
        let firstDayIndex = new Date(currentYear, currentMonth - 1, 1).getDay();
        let startCol = (firstDayIndex === 0) ? 6 : firstDayIndex - 1;

        for (let i = 0; i < startCol; i++) {
            const emptyCell = document.createElement('div');
            emptyCell.className = 'calendar-day disabled';
            calendarDays.appendChild(emptyCell);
        }

        for (let day = 1; day <= daysInMonth; day++) {
            const cell = document.createElement('div');
            const monthStr = String(currentMonth).padStart(2, '0');
            const dayStr = String(day).padStart(2, '0');
            const dateStr = `${currentYear}-${monthStr}-${dayStr}`;

            cell.innerText = day;
            cell.className = 'calendar-day';

            const info = availabilityCache.find(x => x.day === day);

            if (info && info.available) {

                if (info.isFull) {
                    cell.classList.add('full');
                    cell.title = "Все время занято";
                } else {
                    cell.classList.add('available');
                    const indicator = document.createElement('div');
                    indicator.className = 'slot-indicator';
                    cell.appendChild(indicator);
                }

                cell.onclick = () => {
                    document.querySelectorAll('.calendar-day').forEach(c => c.classList.remove('selected'));
                    cell.classList.add('selected');
                    loadTimeSlots(dateStr);
                };
            } else {
                cell.classList.add('disabled');
            }

            calendarDays.appendChild(cell);
        }
    }

    async function loadTimeSlots(dateStr) {
        slotsContainer.classList.remove('d-none');
        slotsList.innerHTML = '';
        slotsLoader.classList.remove('d-none');
        reasonBlock.classList.add('d-none');
        waitlistContainer.classList.add('d-none');

        selectedDateText.innerText = dateStr.split('-').reverse().join('.');
        const docId = docSelect.value;

        try {
            const response = await fetch(`/Patient/GetSlots?doctorId=${docId}&date=${dateStr}`);
            const times = await response.json();

            slotsLoader.classList.add('d-none');

            if (times.length === 0) {
                slotsList.innerHTML = '<span class="text-danger fw-bold">На этот день свободного времени нет.</span>';

                waitlistContainer.classList.remove('d-none');
                return;
            }

            times.forEach(time => {
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'btn btn-outline-primary px-3';
                btn.innerText = time;

                btn.onclick = function () {
                    document.querySelectorAll('#slotsList button').forEach(b => {
                        b.classList.remove('btn-primary', 'text-white');
                        b.classList.add('btn-outline-primary');
                    });
                    btn.classList.remove('btn-outline-primary');
                    btn.classList.add('btn-primary', 'text-white');

                    const fullDateTime = `${dateStr}T${time}:00`;
                    inputDateTime.value = fullDateTime;
                    reasonBlock.classList.remove('d-none');
                    waitlistContainer.classList.add('d-none');
                    reasonBlock.scrollIntoView({ behavior: 'smooth' });
                };

                slotsList.appendChild(btn);
            });
        } catch (e) {
            console.error(e);
            slotsList.innerHTML = 'Ошибка';
        }
    }

    function resetCalendar() {
        calendarWrapper.classList.add('d-none');
        selectDoctorHint.classList.remove('d-none');
        slotsContainer.classList.add('d-none');
        reasonBlock.classList.add('d-none');
        waitlistContainer.classList.add('d-none');
    }
});