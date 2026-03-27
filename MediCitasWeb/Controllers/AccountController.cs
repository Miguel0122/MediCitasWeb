using MediCitasWeb.Filters;
using MediCitasWeb.Models;
using MediCitasWeb.Services.Security; // Importante para usar PasswordHasher
using System;
using System.Data.Entity;
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
        public JsonResult ActualizarDatos(string correo, string telefono)
        {
            try
            {
                if (Session["id_usuario"] == null)
                    return Json(new { success = false, message = "Sesión expirada." });

                int idUsuario = Convert.ToInt32(Session["id_usuario"]);
                var usuario = db.Usuario.Find(idUsuario);

                if (usuario == null)
                    return Json(new { success = false, message = "Usuario no encontrado." });

                // Validaciones
                if (string.IsNullOrWhiteSpace(correo))
                    return Json(new { success = false, message = "El correo es obligatorio." });

                // Formato básico de correo
                if (!System.Text.RegularExpressions.Regex.IsMatch(correo,
                        @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return Json(new { success = false, message = "El correo no tiene un formato válido." });
                }

                // Teléfono opcional, pero si viene, validar longitud
                if (!string.IsNullOrWhiteSpace(telefono) && telefono.Length < 7)
                {
                    return Json(new { success = false, message = "El teléfono es demasiado corto." });
                }

                // (Opcional) validar correo único en la BD
                bool correoEnUso = db.Usuario.Any(u => u.id_usuario != idUsuario
                                                    && u.correo_usuario == correo);
                if (correoEnUso)
                {
                    return Json(new { success = false, message = "Ese correo ya está registrado." });
                }

                // Actualización de campos
                usuario.correo_usuario = correo;
                usuario.telefono_usuario = telefono;

                db.Entry(usuario).State = EntityState.Modified;
                db.SaveChanges();

                Session["correo"] = correo;

                return Json(new { success = true, message = "Datos actualizados correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CambiarPassword(string passwordActual, string nuevaPassword)
        {
            try
            {
                if (Session["id_usuario"] == null)
                    return Json(new { success = false, message = "Sesión expirada." });

                int idUsuario = Convert.ToInt32(Session["id_usuario"]);
                var usuario = db.Usuario.Find(idUsuario);

                if (usuario == null)
                    return Json(new { success = false, message = "Usuario no encontrado." });

                // Verificar contraseña actual
                if (!PasswordHasher.Verify(passwordActual, usuario.password_usuario))
                    return Json(new { success = false, message = "La contraseña actual es incorrecta." });

                if (string.IsNullOrWhiteSpace(nuevaPassword) || nuevaPassword.Length < 6)
                    return Json(new { success = false, message = "La nueva contraseña debe tener al menos 6 caracteres." });

                usuario.password_usuario = PasswordHasher.Hash(nuevaPassword);
                db.SaveChanges();

                return Json(new { success = true, message = "Contraseña actualizada con éxito." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}