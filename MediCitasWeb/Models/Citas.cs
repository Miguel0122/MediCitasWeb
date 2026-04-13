using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediCitasWeb.Models
{
    [Table("Citas")]
    public class Cita
    {
        [Key]
        public int id_cita { get; set; }
        public int id_paciente { get; set; }
        public int id_doctor { get; set; }
        public DateTime fecha_cita { get; set; }
        public TimeSpan hora_cita { get; set; }
        public string especialidad { get; set; }
        public string tipo_consulta { get; set; }
        public string estado { get; set; }
        
        // Navegación
        public virtual Paciente Paciente { get; set; }
        public virtual Doctor Doctor { get; set; }
    }
}