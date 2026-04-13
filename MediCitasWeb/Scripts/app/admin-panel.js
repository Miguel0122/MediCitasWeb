var canvas = document.getElementById('graficaCitas');

var pendientes = parseInt(canvas.getAttribute('data-pendientes')) || 0;
var completadas = parseInt(canvas.getAttribute('data-completadas')) || 0;
var canceladas = parseInt(canvas.getAttribute('data-canceladas')) || 0;

new Chart(canvas.getContext('2d'), {
    type: 'bar',
    data: {
        labels: ['Activas', 'Completadas', 'Canceladas'],
        datasets: [{
            label: 'Cantidad de Citas',
            data: [pendientes, completadas, canceladas],
            backgroundColor: ['#4fc3f7', '#81c784', '#e57373']
        }]
    },
    options: {
        responsive: true,
        plugins: { legend: { display: false } }
    }
});
