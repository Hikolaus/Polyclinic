console.log("Prescription script loaded");

const dateInput = document.querySelector('input[type="date"]');
if (dateInput) {
    dateInput.addEventListener('change', function () {
        const selected = new Date(this.value);
        const today = new Date();
        if (selected < today) {
            alert("Дата не может быть в прошлом!");
        }
    });
}