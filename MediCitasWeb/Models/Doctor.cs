using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace MediCitasWeb.Models
{
    [Table("Doctor")]
    public class Doctor
    {
        [Key]
        public int id_doctor { get; set; }

        public int id_usuario { get; set; }

        public string especialidad { get; set; }
    }
}