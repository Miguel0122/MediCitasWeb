document.addEventListener('DOMContentLoaded', function () {
    const txtEntrada = document.getElementById('txtEntrada');
    const btnEnviar = document.getElementById('btnEnviar');
    const btnLimpiar = document.getElementById('btnLimpiar');
    const chatMessages = document.getElementById('chatMessages');
    const typingIndicator = document.getElementById('typingIndicator');

    async function enviarMensaje() {
        const mensaje = txtEntrada.value.trim();
        if (!mensaje) return;

        // 1. Mostrar mensaje del usuario
        agregarMensaje(mensaje, 'user');
        txtEntrada.value = '';

        // 2. Bloquear UI y mostrar indicador de carga
        setLoading(true);

        try {
            const response = await fetch('/api/chatbot/consultar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ mensaje: mensaje })
            });

            if (!response.ok) throw new Error('Error en la respuesta del servidor');

            const data = await response.json();
            agregarMensaje(data.respuesta, 'bot');

        } catch (error) {
            console.error(error);
            agregarMensaje("Lo siento, hubo un problema al conectar con el servidor.", 'bot');
        } finally {
            setLoading(false);
        }
    }

    function agregarMensaje(texto, tipo) {
        const ahora = new Date();
        const horaStr = ahora.getHours().toString().padStart(2, '0') + ':' +
            ahora.getMinutes().toString().padStart(2, '0');

        const divMsg = document.createElement('div');
        divMsg.className = `message ${tipo}`;
        divMsg.innerHTML = `
            <div>${texto}</div>
            <div class="time">${horaStr}</div>
        `;

        chatMessages.appendChild(divMsg);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    function setLoading(isLoading) {
        txtEntrada.disabled = isLoading;
        btnEnviar.disabled = isLoading;
        typingIndicator.style.display = isLoading ? 'block' : 'none';
        if (isLoading) chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    // Eventos
    btnEnviar.onclick = enviarMensaje;
    txtEntrada.onkeyup = (e) => { if (e.key === 'Enter') enviarMensaje(); };
    btnLimpiar.onclick = () => { chatMessages.innerHTML = ''; };
});