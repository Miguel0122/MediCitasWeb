const { createApp } = Vue;

createApp({

    data() {
        return {
            step: 1,

            correo: '',
            codigo: '',
            nueva: '',
            confirmar: '',

            loading: false,

            mensaje: '',
            tipoMensaje: 'success'
        }
    },

    methods: {

        mostrarMensaje(texto, tipo) {
            this.mensaje = texto;
            this.tipoMensaje = tipo;
        },

        // ================= PASO 1 =================
        async enviarCodigo() {

            if (!this.correo) {
                this.mostrarMensaje("Ingrese un correo.", "danger");
                return;
            }

            this.loading = true;

            try {

                const formData = new FormData();
                formData.append("correo", this.correo);

                const response = await fetch("/Password/EnviarCodigo", {
                    method: "POST",
                    body: formData
                });

                const data = await response.json();

                if (data.ok) {
                    this.step = 2;
                    this.mostrarMensaje("Código enviado correctamente.", "success");
                } else {
                    this.mostrarMensaje(data.mensaje, "danger");
                }

            } catch {
                this.mostrarMensaje("Error de conexión.", "danger");
            }

            this.loading = false;
        },

        // ================= PASO 2 =================
        async validarCodigo() {

            if (!this.codigo) {
                this.mostrarMensaje("Ingrese el código.", "danger");
                return;
            }

            this.loading = true;

            const formData = new FormData();
            formData.append("codigo", this.codigo);

            const response = await fetch("/Password/ValidarCodigo", {
                method: "POST",
                body: formData
            });

            const data = await response.json();

            if (data.ok) {
                this.step = 3;
                this.mostrarMensaje("Código correcto.", "success");
            } else {
                this.mostrarMensaje(data.mensaje, "danger");
            }

            this.loading = false;
        },

        // ================= PASO 3 =================
        async cambiarPassword() {

            if (this.nueva.length < 6) {
                this.mostrarMensaje("Mínimo 6 caracteres.", "danger");
                return;
            }

            if (this.nueva !== this.confirmar) {
                this.mostrarMensaje("Las contraseñas no coinciden.", "danger");
                return;
            }

            this.loading = true;

            const formData = new FormData();
            formData.append("nueva", this.nueva);

            const response = await fetch("/Password/CambiarPassword", {
                method: "POST",
                body: formData
            });

            const data = await response.json();

            if (data.ok) {

                this.mostrarMensaje(
                    "Contraseña actualizada. Redirigiendo...",
                    "primary"
                );

                setTimeout(() => {
                    window.location.href = "/Auth/Login";
                }, 2000);

            } else {
                this.mostrarMensaje(data.mensaje, "danger");
            }

            this.loading = false;
        }
    }

}).mount("#recuperarApp");