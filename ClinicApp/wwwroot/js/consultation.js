let medIndex = 0;
let recipeIndex = 0;

$(document).ready(function () {
    $('#diagnosisSelect').select2({
        theme: "bootstrap-5",
        width: '100%',
        placeholder: "Введите код или название болезни...",
        allowClear: true,
        language: { noResults: function () { return "Диагноз не найден"; } }
    });

    $('#diagnosisSelect').on('select2:select', function (e) {
        var selectedId = e.params.data.id;

        if (window.diagnosesData) {
            var diagnosis = window.diagnosesData.find(d => d.id == selectedId);

            if (diagnosis) {
                var treatmentArea = $('#treatmentArea');
                var recomArea = $('#recomArea');

                var treatText = diagnosis.DefaultTreatment || diagnosis.defaultTreatment;
                var recomText = diagnosis.DefaultRecommendations || diagnosis.defaultRecommendations;

                if (treatText) {
                    treatmentArea.val(treatText);
                }

                if (recomText) {
                    recomArea.val(recomText);
                }
            }
        }
    });
});
function initSelect2(element) {
    $(element).select2({
        theme: "bootstrap-5",
        width: '100%',
        placeholder: "Введите название...",
        allowClear: true,
        language: { noResults: function () { return "Препарат не найден"; } }
    });
}

function addMedRow() {
    const container = document.getElementById('medsContainer');
    const template = document.getElementById('medRowTemplate');
    const clone = template.content.cloneNode(true);
    const row = clone.querySelector('.med-row');
    const select = row.querySelector('.med-select');
    select.name = `Meds[${medIndex}].MedicationId`;
    select.id = `med-select-${medIndex}`;
    row.querySelector('.med-dosage').name = `Meds[${medIndex}].Dosage`;
    row.querySelector('.med-instr').name = `Meds[${medIndex}].Instructions`;
    container.appendChild(clone);
    initSelect2(`#med-select-${medIndex}`);
    medIndex++;
}

function addRecipeRow() {
    const container = document.getElementById('recipesContainer');
    const template = document.getElementById('recipeRowTemplate');
    const clone = template.content.cloneNode(true);
    const row = clone.querySelector('.recipe-row');
    const select = row.querySelector('.recipe-select');
    select.name = `Recipes[${recipeIndex}].MedicationId`;
    select.id = `recipe-select-${recipeIndex}`;
    row.querySelector('.recipe-dosage').name = `Recipes[${recipeIndex}].Dosage`;
    row.querySelector('.recipe-instr').name = `Recipes[${recipeIndex}].Instructions`;
    container.appendChild(clone);
    initSelect2(`#recipe-select-${recipeIndex}`);
    recipeIndex++;
}

function removeRow(btn) {
    const row = btn.closest('.row');
    const select = row.querySelector('select');
    if ($(select).data('select2')) { $(select).select2('destroy'); }
    row.remove();
}