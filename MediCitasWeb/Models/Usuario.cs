using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCitasWeb.Models
{
    // Esta línea indica que esta clase representa la tabla "Usuario"
    [Table("Usuario")]
    public class Usuario
    {
        // Clave primaria
        [Key]
        public int id_usuario { get; set; }

        // Nombre del usuario
        [Required]
        public string nombres_usuario { get; set; }

        [Required]
        public string numero_documento { get; set; }

        // Apellido del usuario
        [Required]
        public string apellidos_usuario { get; set; }

        // Correo (único)
        [Required]
        public string correo_usuario { get; set; }

        // Contraseña (luego la encriptaremos)
        [Required]
        public string password_usuario { get; set; }

        // Rol: Administrador, Doctor o Paciente
        [Required]
        public string rol_usuario { get; set; }

        // Fecha automática
        public DateTime fecha_registro { get; set; }
    }
}