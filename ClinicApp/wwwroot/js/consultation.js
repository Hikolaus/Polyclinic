let medIndex = 0;
let recipeIndex = 0;

$(document).ready(function () {
    const $diagSelect = $('#diagnosisSelect');

    $diagSelect.select2({
        theme: "bootstrap-5",
        width: '100%',
        placeholder: "Введите код или название болезни...",
        allowClear: true,
        language: { noResults: function () { return "Диагноз не найден"; } }
    });

    $diagSelect.on('select2:select', function (e) {
        const selectedId = e.params.data.id;
        console.log("Выбран ID диагноза:", selectedId);

        if (window.diagnosesData && window.diagnosesData.length > 0) {
            const diagnosis = window.diagnosesData.find(d => {
                const id = (d.id !== undefined) ? d.id : d.Id;
                return id == selectedId;
            });

            if (diagnosis) {
                console.log("Данные найдены:", diagnosis);

                const treatText = diagnosis.defaultTreatment !== undefined ? diagnosis.defaultTreatment : diagnosis.DefaultTreatment;
                const recomText = diagnosis.defaultRecommendations !== undefined ? diagnosis.defaultRecommendations : diagnosis.DefaultRecommendations;

                $('#treatmentArea').val(treatText || "");
                $('#recomArea').val(recomText || "");

                if (treatText || recomText) {
                    $('#treatmentArea').addClass('is-valid');
                    setTimeout(() => $('#treatmentArea').removeClass('is-valid'), 800);
                }
            } else {
                console.warn("Диагноз не найден в кэше JavaScript");
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
    if ($(select).data('select2')) {
        $(select).select2('destroy');
    }
    row.remove();
}