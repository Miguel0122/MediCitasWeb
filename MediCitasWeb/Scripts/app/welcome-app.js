const { createApp } = Vue;

createApp({
    data() {
        return {
            mensajeBienvenida: 'Tu salud, a un clic de distancia',
            servicios: [
                {
                    titulo: 'Agendamiento Web',
                    descripcion: 'Reserva citas con especialistas en tiempo real.',
                    icono: 'https://cdn-icons-png.flaticon.com/512/2370/2370264.png'
                },
                {
                    titulo: 'Historial Clínico',
                    descripcion: 'Accede a tus diagnósticos y órdenes médicas.',
                    icono: 'https://cdn-icons-png.flaticon.com/512/2966/2966327.png'
                },
                {
                    titulo: 'Atención 24/7',
                    descripcion: 'Consulta disponibilidad de médicos en cualquier momento.',
                    icono: 'https://cdn-icons-png.flaticon.com/512/2972/2972531.png'
                }
            ]
        }
    },
    mounted() {
        console.log("Vue.js está activo en la página de Bienvenida");
    }
}).mount('#app');