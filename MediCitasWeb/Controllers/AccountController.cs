using MediCitasWeb.Filters;
using MediCitasWeb.Models;
using MediCitasWeb.Services.Security; // Importante para usar PasswordHasher
using System;
using System.Linq;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    [SessionAuthorize(Roles = "Paciente,Doctor,Administrador")]
    public class AccountController : Controller
    {
        private MediCitasContext db = new MediCitasContext();

        // GET: Perfil
        public ActionResult Perfil()
        {
            if (Session["id_usuario"] == null) return RedirectToAction("Login", "Auth");

            int idUsuario = Convert.ToInt32(Session["id_usuario"]);
            var usuario = db.Usuario.Find(idUsuario);

            if (usuario == null) return HttpNotFound();

            return View(usuario);
        }

        // POST: Actualizar Datos (Correo)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarDatos(string correo, string telefono)
        {
            // 1. Obtener el ID de la sesión
            if (Session["id_usuario"] == null) return RedirectToAction("Login", "Auth");

            int idUsuario = Convert.ToInt32(Session["id_usuario"]);
            var usuario = db.Usuario.Find(idUsuario);

            if (usuario != null)
            {
                // 2. Validar que el correo no sea nulo o vacío
                if (string.IsNullOrWhiteSpace(correo))
                {
                    TempData["Error"] = "El correo electrónico es obligatorio.";
                    return RedirectToAction("Perfil");
                }

                // 3. Validar si el correo ya existe en otro usuario (para evitar conflictos de UNIQUE)
                var existeCorreo = db.Usuario.Any(u => u.correo_usuario == correo && u.id_usuario != idUsuario);

                if (existeCorreo)
                {
                    TempData["Error"] = "El correo ya está registrado por otro usuario.";
                }
                else
                {
                    // 4. Actualizar los campos
                    usuario.correo_usuario = correo.Trim();
                    usuario.telefono_usuario = telefono?.Trim(); // Puede ser nulo según tu nueva tabla

                    try
                    {
                        db.SaveChanges();
                        TempData["Exito"] = "Datos actualizados correctamente.";
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = "Error al guardar en la base de datos: " + ex.Message;
                    }
                }
            }

            return RedirectToAction("Perfil");
        }

        // POST: Cambiar Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarPassword(string passwordActual, string nuevaPassword)
        {
            int idUsuario = Convert.ToInt32(Session["id_usuario"]);
            var usuario = db.Usuario.Find(idUsuario);

            if (usuario == null) return RedirectToAction("Login", "Auth");

            // 1. Verificar si la contraseña actual es correcta usando tu PasswordHasher
            if (!PasswordHasher.Verify(passwordActual, usuario.password_usuario))
            {
                TempData["Error"] = "La contraseña actual es incorrecta.";
                return RedirectToAction("Perfil");
            }

            // 2. Validar longitud mínima
            if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 6)
            {
                TempData["Error"] = "La nueva contraseña debe tener al menos 6 caracteres.";
                return RedirectToAction("Perfil");
            }

            // 3. Hashear la nueva contraseña y guardar
            usuario.password_usuario = PasswordHasher.Hash(nuevaPassword);
            db.SaveChanges();

            TempData["Exito"] = "Contraseña actualizada con éxito.";
            return RedirectToAction("Perfil");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}