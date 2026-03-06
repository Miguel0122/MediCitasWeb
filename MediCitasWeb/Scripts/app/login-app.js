const { createApp } = Vue;

createApp({
    data() {
        return {
            form: {
                numero_documento: '',
                password: ''
            },
            loading: false
        }
    },
    methods: {
        soloNumeros() {
            // Reemplaza cualquier cosa que no sea número (lo que hacía el otro JS)
            this.form.numero_documento = this.form.numero_documento.replace(/[^0-9]/g, '');
        },
        handleLogin(event) {
            // Si los campos están llenos, mostramos el "Cargando" y dejamos que el form siga su curso
            if (this.form.numero_documento && this.form.password) {
                this.loading = true;
                // No ponemos preventDefault para que el POST de ASP.NET funcione normal
            } else {
                event.preventDefault();
            }
        }
    }
}).mount('#loginApp');