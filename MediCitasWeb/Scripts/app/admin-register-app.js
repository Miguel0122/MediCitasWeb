const { createApp } = Vue;

createApp({

    data() {
        return {
            form: {
                nombres: '',
                apellidos: '',
                documento: '',
                correo: '',
                password: '',
                especialidad: ''
            },

            loading: false,
            mensaje: ''
        }
    },

    methods: {

        // Solo números documento
        soloNumeros() {
            this.form.documento =
                this.form.documento.replace(/[^0-9]/g, '');
        },

        validarFormulario() {

            if (!this.form.nombres ||
                !this.form.apellidos ||
                !this.form.documento ||
                !this.form.correo ||
                !this.form.password ||
                !this.form.especialidad) {

                this.mensaje = "Todos los campos son obligatorios.";
                return false;
            }

            if (this.form.documento.length < 6) {
                this.mensaje = "Documento inválido.";
                return false;
            }

            if (this.form.password.length < 6) {
                this.mensaje = "La contraseña debe tener mínimo 6 caracteres.";
                return false;
            }

            this.mensaje = "";
            return true;
        },

        handleSubmit(e) {

            if (!this.validarFormulario()) {
                e.preventDefault();
                return;
            }

            // activa loader y deja continuar POST MVC
            this.loading = true;
        }
    }

}).mount("#crearDoctorApp");