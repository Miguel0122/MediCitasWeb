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
        [Key]
        public int id_mensaje { get; set; }
        public int id_sesion { get; set; }

        // Cambiamos 'rol' por 'remitente' para que sea más claro (Usuario/Bot)
        public string remitente { get; set; }

        public string contenido { get; set; }

        // Cambiamos 'fecha_mensaje' por 'fecha_envio'
        public DateTime fecha_envio { get; set; }

        [ForeignKey("id_sesion")]
        public virtual ChatSesion ChatSesion { get; set; }
    }
}