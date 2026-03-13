using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCitasWeb.Models
{
    // Modelo que representa una sesión de conversación en el chatbot
    // Cada vez que un usuario abre el chat, se crea un registro aquí
    [Table("ChatSesiones")]
    public class ChatSesion
    {
        [Key] // Clave primaria de la tabla
        public int id_sesion { get; set; }

        // ID del usuario que inició la conversación (viene de la sesión activa)
        public int id_usuario { get; set; }

        // Fecha y hora en que el usuario abrió el chatbot
        public DateTime fecha_inicio { get; set; }

        // Fecha y hora en que el usuario cerró el chatbot (puede ser null si aún está activo)
        public DateTime? fecha_fin { get; set; }

        // Relación de navegación hacia la tabla Usuario
        [ForeignKey("id_usuario")]
        public virtual Usuario Usuario { get; set; }
        public virtual ICollection<ChatMensaje> Mensajes { get; set; }
    }
}