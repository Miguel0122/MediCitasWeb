using MediCitasWeb.Models;
using MediCitasWeb.Services.Security;
using System;
using System.Linq;

namespace MediCitasWeb.Services
{
    public static class SetupService
    {
        public static void EnsureAdminExists()
        {
            using (var db = new MediCitasContext())
            {
                bool existeAdmin = db.Usuario
                    .Any(u => u.rol_usuario == "Administrador");

                if (existeAdmin) return;

                var admin = new Usuario
                {
                    nombres_usuario = "Administrador",
                    apellidos_usuario = "Sistema",
                    numero_documento = "000000",
                    correo_usuario = "admin@medicitas.com",
                    password_usuario = PasswordHasher.Hash("Admin123*"),
                    rol_usuario = "Administrador",
                    fecha_registro = DateTime.Now
                };

                db.Usuario.Add(admin);
                db.SaveChanges();
            }
        }
    }
}