using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCitasWeb.Models
{
    // Modelo que representa una pregunta frecuente que el bot usa como referencia
    // Se carga desde la BD para enriquecer el system prompt de Claude
    [Table("ChatFAQ")]
    public class ChatFAQ
    {
        [Key]
        public int id_faq { get; set; }

        // La pregunta frecuente que un paciente podría hacer
        public string pregunta { get; set; }

        // La respuesta predefinida que el bot debe usar
        public string respuesta { get; set; }

        // Permite activar o desactivar preguntas sin borrarlas de la BD
        public bool activo { get; set; }
    }
}