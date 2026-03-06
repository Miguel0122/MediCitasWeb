const { createApp } = Vue;

createApp({
    data() {
        return {
            form: {
                numero_documento: '',
                password: ''
            },
            error: null,
            loading: false
        }
    },
    methods: {
        handleLogin() {
            this.loading = true;
            this.error = null;

            // Enviamos el formulario manualmente
            // Nota: Aquí puedes usar fetch si quieres una API, 
            // pero para mantener tu controlador actual, lo haremos por submit normal
            event.target.submit();
        }
    }
}).mount('#loginApp');