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
        [Key]
        public int id_sesion { get; set; }
        public int id_usuario { get; set; }
        public DateTime fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public string estado { get; set; } // Agregamos este campo que usamos en el Controller

        [ForeignKey("id_usuario")]
        public virtual Usuario Usuario { get; set; }
        public virtual ICollection<ChatMensaje> Mensajes { get; set; }

        public ChatSesion()
        {
            Mensajes = new HashSet<ChatMensaje>();
        }
    }
}