new Vue({
    el: '#chatApp',
    data: {
        entrada: '',
        mensajes: [],
        loading: false
    },
    methods: {
        horaActual() {
            const now = new Date();
            return now.getHours().toString().padStart(2, '0') + ':' +
                now.getMinutes().toString().padStart(2, '0');
        },
        agregarMensaje(texto, tipo) {
            this.mensajes.push({
                texto: texto,
                tipo: tipo,   // 'user' o 'bot'
                hora: this.horaActual()
            });
            this.$nextTick(() => {
                const cont = document.querySelector('.chat-messages');
                if (cont) cont.scrollTop = cont.scrollHeight;
            });
        },
        async enviarMensaje() {
            const mensaje = this.entrada.trim();
            if (!mensaje || this.loading) return;

            this.agregarMensaje(mensaje, 'user');
            this.entrada = '';
            this.loading = true;

            try {
                const resp = await fetch('/api/chatbot/consultar', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ mensaje: mensaje })
                });

                const text = await resp.text(); // leemos el cuerpo como texto primero

                if (!resp.ok) {
                    console.error('Respuesta no OK:', resp.status, text);
                    this.agregarMensaje(
                        'Error del servidor: ' + text,
                        'bot'
                    );
                    return;
                }

                const data = JSON.parse(text);

                const textoBot = data && (data.respuesta || data.mensaje)
                    ? (data.respuesta || data.mensaje)
                    : 'Lo siento, no pude procesar tu consulta.';

                this.agregarMensaje(textoBot, 'bot');
            } catch (err) {
                console.error(err);
                this.agregarMensaje(
                    'Ocurrió un error al conectar con el servidor. Intenta nuevamente.',
                    'bot'
                );
            } finally {
                this.loading = false;
            }
        },
        limpiarChat() {
            this.mensajes = [];
        }
    }
});
