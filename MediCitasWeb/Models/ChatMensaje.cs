using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCitasWeb.Models
{
    // Modelo que representa cada mensaje individual dentro de una sesión del chatbot
    // Aquí se guardan tanto los mensajes del usuario como las respuestas del bot
    [Table("ChatMensajes")]
    public class ChatMensaje
    {
        [Key] // Clave primaria de la tabla
        public int id_mensaje { get; set; }

        // ID de la sesión a la que pertenece este mensaje
        public int id_sesion { get; set; }

        // Indica quién envió el mensaje: "user" (paciente) o "assistant" (bot)
        public string rol { get; set; }

        // El texto del mensaje enviado o recibido
        public string contenido { get; set; }

        // Fecha y hora exacta en que se registró el mensaje
        public DateTime fecha_mensaje { get; set; }

        // Relación de navegación hacia la tabla ChatSesiones
        [ForeignKey("id_sesion")]
        public virtual ChatSesion ChatSesion { get; set; }
    }
}