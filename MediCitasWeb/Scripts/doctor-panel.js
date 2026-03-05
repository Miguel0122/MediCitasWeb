function toggleDetalle(btn) {
    const fila = btn.closest("tr");
    const detalle = fila.nextElementSibling;
    detalle.style.display =
        detalle.style.display === "table-row" ? "none" : "table-row";
}

function cerrarDetalle(btn) {
    const panel = btn.closest(".detalle-row");
    panel.style.display = "none";
}

function finalizarCita(btn) {
    const fila = btn.closest("tr");
    const estado = fila.querySelector(".estado");
    estado.textContent = "Atendida";
    estado.classList.remove("activa");
    estado.classList.add("atendida");
    btn.remove();
    actualizarContador();
}

function actualizarContador() {
    const activas = document.querySelectorAll(".estado.activa").length;
    document.getElementById("contadorCitas").textContent =
        "Citas pendientes: " + activas;
}

// FILTRO COMBINADO (DOCUMENTO + FECHA)
const buscador = document.getElementById("buscador");
const filtroFecha = document.getElementById("filtroFecha");

if (buscador && filtroFecha) {
    buscador.addEventListener("keyup", filtrarCitas);
    filtroFecha.addEventListener("change", filtrarCitas);
}

function filtrarCitas() {
    const textoDoc = buscador.value.toLowerCase();
    const fechaSeleccionada = filtroFecha.value; // yyyy-MM-dd

    const filas = document.querySelectorAll(".fila-cita");

    filas.forEach(fila => {
        const doc = fila.querySelector(".doc").textContent.toLowerCase();
        const fecha = fila.querySelector(".fecha").textContent; // yyyy-MM-dd

        const coincideDoc = doc.includes(textoDoc);
        const coincideFecha = fechaSeleccionada === "" || fecha === fechaSeleccionada;

        if (coincideDoc && coincideFecha) {
            fila.style.display = "";
        } else {
            fila.style.display = "none";
            if (fila.nextElementSibling) {
                fila.nextElementSibling.style.display = "none";
            }
        }
    });
}

function limpiarFiltros() {
    buscador.value = "";
    filtroFecha.value = "";
    filtrarCitas();
}
