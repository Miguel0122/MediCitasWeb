// Función para mostrar el siguiente paso
function mostrar(id) {
    const elemento = document.getElementById(id);
    if (elemento) {
        elemento.style.display = "block";
        elemento.scrollIntoView({ behavior: 'smooth' });
    }
}

// Event Listeners para validaciones en tiempo real
document.addEventListener("DOMContentLoaded", function () {

    // Validar Nombres
    document.getElementById("nombre").addEventListener("input", function () {
        let regex = /^[A-Za-zÁÉÍÓÚáéíóúñÑ\s]+$/;
        if (regex.test(this.value) && this.value.length > 2) {
            mostrar("paso2");
        }
    });

    // Validar Apellidos
    document.getElementById("apellidos").addEventListener("input", function () {
        let regex = /^[A-Za-zÁÉÍÓÚáéíóúñÑ\s]+$/;
        if (regex.test(this.value) && this.value.length > 2) {
            mostrar("paso3");
        }
    });

    // Validar Documento (Solo números)
    document.getElementById("numDoc").addEventListener("input", function () {
        this.value = this.value.replace(/[^0-9]/g, ''); // Evita letras
        if (this.value.length >= 6) {
            mostrar("paso4");
        }
    });

    // Validar Correo
    document.getElementById("correo").addEventListener("input", function () {
        let regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (regex.test(this.value)) {
            mostrar("paso5");
        }
    });

    // Validar Password
    document.getElementById("password").addEventListener("input", function () {
        if (this.value.length >= 6) {
            mostrar("pasoFinal");
        }
    });
});