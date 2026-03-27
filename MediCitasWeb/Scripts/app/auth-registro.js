function mostrar(id) {
    const elemento = document.getElementById(id);
    if (elemento && elemento.style.display !== "block") {
        elemento.style.display = "block";
        elemento.scrollIntoView({ behavior: 'smooth' });
    }
}

document.addEventListener("DOMContentLoaded", function () {
    // 1. Nombres -> Muestra Apellidos (Paso 2)
    document.getElementById("nombre").addEventListener("input", function () {
        let regex = /^[A-Za-záéíóúñÑ\s]+$/;
        if (regex.test(this.value) && this.value.length > 2) {
            mostrar("paso1");
        }
    });

    // 2. Apellidos -> Muestra Documento (Paso 3)
    document.getElementById("apellidos").addEventListener("input", function () {
        let regex = /^[A-Za-záéíóúñÑ\s]+$/;
        if (regex.test(this.value) && this.value.length > 2) {
            mostrar("paso2");
        }
    });

    // 3. Documento -> Muestra Correo (Paso 4)
    document.getElementById("numDoc").addEventListener("input", function () {
        this.value = this.value.replace(/[^0-9]/g, '').slice(0, 10);
        if (this.value.length >= 6) {
            mostrar("paso3");
        }
    });

    // 4. Correo -> Muestra Teléfono (Paso 5) Y Contraseña (Paso 6) simultáneamente
    document.getElementById("correo").addEventListener("input", function () {
        let regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (regex.test(this.value)) {
            mostrar("paso4"); // Muestra Teléfono
        }
    });

    // 5. Teléfono (Opcional)
    const telefonoInput = document.getElementById("telefono");
    if (telefonoInput) {
        telefonoInput.addEventListener("input", function () {
            this.value = this.value.replace(/[^0-9]/g, '').slice(0, 10);
        });
    }

    // 6. Contraseña -> Muestra Botón Final
    document.getElementById("password").addEventListener("input", function () {
        if (this.value.length >= 5) {
            mostrar("pasoFinal");
        }
    });
});