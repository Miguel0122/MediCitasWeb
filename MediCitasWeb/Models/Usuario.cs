using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCitasWeb.Models
{
    [Table("Usuario")]
    public class Usuario
    {
        [Key]
        public int id_usuario { get; set; }

        public string nombres_usuario { get; set; }

        public string apellidos_usuario { get; set; }

        public string numero_documento { get; set; }

        public string correo_usuario { get; set; }

        public string password_usuario { get; set; }

        public string rol_usuario { get; set; }

        public DateTime fecha_registro { get; set; }
    }
}