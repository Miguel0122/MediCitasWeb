let sesionId = 0;
let isLoading = false;
let contextoUsuario = '';
let historialChat = [];

document.addEventListener('DOMContentLoaded', function () {
    sesionId = parseInt(document.body.getAttribute('data-sesion-id') || '0');
    try {
        contextoUsuario = decodeURIComponent(document.body.getAttribute('data-contexto') || '');
    } catch (e) {
        contextoUsuario = document.body.getAttribute('data-contexto') || '';
    }

    const txtEntrada = document.getElementById('txtEntrada');
    const btnEnviar = document.getElementById('btnEnviar');
    const btnLimpiar = document.getElementById('btnLimpiar');
    const chatMessages = document.getElementById('chatMessages');
    const typingIndicator = document.getElementById('typingIndicator');

    if (sesionId > 0) cargarHistorial();

    // ─── ENVIAR MENSAJE ──────────────────────────────────────────────────────
    async function enviarMensaje() {
        const mensaje = txtEntrada.value.trim();
        if (!mensaje || isLoading) return;

        agregarMensaje(mensaje, 'user');
        txtEntrada.value = '';
        setLoading(true);

        try {
            // El servidor llama a Groq, guarda en BD y devuelve la respuesta
            const response = await fetch('/Chat/EnviarMensaje', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'sesionId=' + encodeURIComponent(sesionId) + '&mensaje=' + encodeURIComponent(mensaje)
            });

            if (!response.ok) throw new Error('HTTP ' + response.status);
            const data = await response.json();

            if (data.success) {
                agregarMensaje(data.respuesta, 'bot', data.hora);
            } else {
                agregarMensaje(generarRespuestaFallback(mensaje), 'bot');
            }
        } catch (error) {
            console.error('Error:', error);
            agregarMensaje(generarRespuestaFallback(mensaje), 'bot');
        } finally {
            setLoading(false);
        }
    }

    // ─── FALLBACK LOCAL con contexto del usuario ─────────────────────────────
    function generarRespuestaFallback(mensaje) {
        const m = mensaje.toLowerCase();
        const nombreMatch = contextoUsuario.match(/Usuario:\s*([^|]+)\|/);
        const nombre = nombreMatch ? nombreMatch[1].trim().split(' ')[0] : '';
        const saludo = nombre ? nombre + ', ' : '';

        const tieneCitas = contextoUsuario.includes('Citas proximas:');
        const sinCitas = contextoUsuario.includes('Sin citas activas');
        const ultimaMatch = contextoUsuario.match(/Ultima consulta:\s*([^\n]+)/);
        const ultimaCita = ultimaMatch ? ultimaMatch[1].trim() : null;

        if (m.includes('quien soy') || m.includes('mis datos') || m.includes('mi perfil')) {
            if (nombre) {
                let r = 'Eres ' + nombre + ', registrado como Paciente en MediCitas.';
                if (ultimaCita) r += ' Tu ultima consulta fue: ' + ultimaCita + '.';
                if (sinCitas) r += ' No tienes citas proximas agendadas.';
                return r;
            }
        }
        if (m.includes('mis citas') || (m.includes('cita') && (m.includes('tengo') || m.includes('proxim')))) {
            if (tieneCitas) {
                const lineas = contextoUsuario.split('\n').filter(l => l.trim().startsWith('-')).join('\n');
                return saludo + 'tus citas proximas son:\n' + lineas;
            }
            if (sinCitas) return saludo + 'no tienes citas activas proximas. Quieres agendar una?';
        }
        if (m.includes('agendar') || m.includes('reservar'))
            return saludo + 'para agendar ve al menu principal y selecciona Agendar Cita.';
        if (m.includes('cancelar'))
            return saludo + 'para cancelar ve a Mis Citas y haz clic en Cancelar. Tienes hasta 2 horas antes.';
        if (m.includes('horario') || m.includes('atencion'))
            return 'Horarios: Lun-Vie 6AM-6PM | Sabados 7AM-2PM | Domingos cerrado.';
        if (m.includes('medicitas') || m.includes('que es') || m.includes('sistema'))
            return 'MediCitas es tu plataforma de gestion de citas medicas en Colombia.';
        if (m.includes('doctor') || m.includes('medico') || m.includes('especialidad'))
            return 'Especialidades: Medicina General, Pediatria, Cardiologia, Odontologia, Dermatologia, Ginecologia.';
        if (m.includes('hola') || m.includes('buenas'))
            return 'Hola ' + saludo + 'soy MediBot. Puedo ayudarte con citas, horarios o especialistas.';
        if (m.includes('gracias'))
            return 'Con gusto ' + saludo + 'hay algo mas en lo que pueda ayudarte?';
        return saludo + 'puedo ayudarte con citas, horarios o especialistas. Que necesitas?';
    }

    // ─── CARGAR HISTORIAL ────────────────────────────────────────────────────
    async function cargarHistorial() {
        try {
            const response = await fetch('/Chat/ObtenerHistorial?sesionId=' + sesionId);
            const data = await response.json();
            if (data.success && data.mensajes && data.mensajes.length > 0) {
                chatMessages.innerHTML = '';
                data.mensajes.forEach(msg => agregarMensaje(msg.contenido, msg.remitente, msg.hora));
            } else {
                agregarBienvenida();
            }
        } catch (e) {
            agregarBienvenida();
        }
    }

    function agregarBienvenida() {
        if (chatMessages.children.length === 0) {
            const nombre = document.body.getAttribute('data-nombre') || '';
            const txt = nombre
                ? 'Hola ' + nombre + '! Soy MediBot, tu asistente de MediCitas. En que puedo ayudarte?'
                : 'Hola! Soy MediBot, tu asistente de MediCitas. En que puedo ayudarte?';
            agregarMensaje(txt, 'bot');
        }
    }

    // ─── UI HELPERS ──────────────────────────────────────────────────────────
    function agregarMensaje(texto, tipo, horaPersonalizada) {
        const ahora = horaPersonalizada ||
            new Date().toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit' });
        const div = document.createElement('div');
        div.className = 'message ' + tipo;
        div.innerHTML = '<div>' + escapeHtml(texto) + '</div><div class="time">' + ahora + '</div>';
        chatMessages.appendChild(div);
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    function setLoading(loading) {
        isLoading = loading;
        txtEntrada.disabled = loading;
        btnEnviar.disabled = loading;
        typingIndicator.style.display = loading ? 'flex' : 'none';
        if (loading) chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML.replace(/\n/g, '<br>');
    }

    // ─── EVENTOS ─────────────────────────────────────────────────────────────
    btnEnviar.onclick = enviarMensaje;
    txtEntrada.addEventListener('keydown', e => {
        if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); enviarMensaje(); }
    });
    btnLimpiar.onclick = () => {
        if (confirm('Limpiar conversacion?')) {
            chatMessages.innerHTML = '';
            agregarBienvenida();
        }
    };
});