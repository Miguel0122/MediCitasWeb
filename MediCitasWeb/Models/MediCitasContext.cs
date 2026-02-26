using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace MediCitasWeb.Models
{
    // Esta clase es el puente entre el proyecto y la base de datos
    public class MediCitasContext : DbContext
    {
        // Constructor que usa la cadena de conexión llamada "MediCitasDB"
        public MediCitasContext() : base("MediCitasDB")
        {
        }

        // Representa la tabla Usuario
        public DbSet<Usuario> Usuario { get; set; }
    }
}