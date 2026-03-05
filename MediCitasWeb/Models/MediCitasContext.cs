using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;


namespace MediCitasWeb.Models
{
    public class MediCitasContext : DbContext
    {
        public MediCitasContext() : base("MediCitasDB")
        {
        }

        public DbSet<Usuario> Usuario { get; set; }

        public DbSet<Doctor> Doctor { get; set; }

        public DbSet<Paciente> Paciente { get; set; }

        public DbSet<Cita> Citas { get; set; }
    }
}