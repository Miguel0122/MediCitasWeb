/**
 * Lógica de interacción para el Login de MediCitas
 */
document.addEventListener("DOMContentLoaded", function () {
    const inputDoc = document.getElementById("numDocLogin");
    const loginForm = document.getElementById("loginForm");

    // Impedir que escriban letras en el documento
    inputDoc.addEventListener("input", function () {
        this.value = this.value.replace(/[^0-9]/g, '');
    });

    // Pequeña animación visual al enviar
    loginForm.addEventListener("submit", function () {
        const btn = document.getElementById("btnLogin");
        btn.innerHTML = "Verificando...";
        btn.style.opacity = "0.7";
        btn.style.cursor = "not-allowed";
    });
});