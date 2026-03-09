const { createApp } = Vue;

createApp({
    data() {
        return {
            loading: false,
            horasDisponibles: [],
            // Cargamos los doctores que vienen desde el servidor
            todosLosDoctores: window.listaDoctoresBase || [],
            form: {
                especialidad: '',
                tipoConsulta: 'Primera vez',
                fecha: '',
                hora: '',
                idDoctor: ''
            }
        }
    },
    computed: {
        // Esta es la magia: se ejecuta cada vez que cambia la especialidad
        doctoresFiltrados() {
            if (!this.form.especialidad) return [];

            return this.todosLosDoctores.filter(doc =>
                doc.Especialidad === this.form.especialidad
            );
        }
    },
    methods: {
        actualizarHoras() {
            this.horasDisponibles = [];
            this.form.hora = '';

            if (!this.form.fecha) return;

            const ahora = new Date();
            const hoyStr = ahora.toISOString().split('T')[0];
            const esHoy = (this.form.fecha === hoyStr);

            // Generar franjas de 8:00 a 17:00 cada 30 min
            for (let h = 8; h <= 17; h++) {
                for (let m of [0, 30]) {
                    let mostrar = true;

                    // Validación: Si es hoy, no mostrar horas que ya pasaron
                    if (esHoy) {
                        if (h < ahora.getHours() || (h === ahora.getHours() && m <= ahora.getMinutes())) {
                            mostrar = false;
                        }
                    }

                    if (mostrar) {
                        const hStr = h.toString().padStart(2, '0');
                        const mStr = m.toString().padStart(2, '0');

                        this.horasDisponibles.push({
                            valor: `${hStr}:${mStr}:00`,
                            texto: `${hStr}:${mStr} ${h < 12 ? 'AM' : 'PM'}`
                        });
                    }
                }
            }
        },
        validarFormulario(e) {
            this.loading = true;
            // Aquí puedes agregar validaciones extra antes del submit al servidor
            if (this.horasDisponibles.length === 0 && this.form.fecha) {
                alert("No hay horarios disponibles para la fecha seleccionata.");
                e.preventDefault();
                this.loading = false;
            }
        }
    },
    mounted() {
        // Si ya hay una fecha pre-seleccionada, cargar horas
        if (this.form.fecha) this.actualizarHoras();
    }
}).mount('#agendarApp');