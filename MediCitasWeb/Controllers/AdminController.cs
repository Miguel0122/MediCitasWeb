using MediCitasWeb.Models;
using MediCitasWeb.Services.Security;
using MediCitasWeb.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    [SessionAuthorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private MediCitasContext db = new MediCitasContext();

        #region Panel Principal

        public ActionResult PanelAdmin()
        {
            try
            {
                // 1. Métricas de Citas
                ViewBag.TotalCitas = db.Citas.Count();
                ViewBag.CitasPendientes = db.Citas.Count(c => c.estado == "Activa");
                ViewBag.CitasCompletadas = db.Citas.Count(c => c.estado == "Completada");
                ViewBag.CitasCanceladas = db.Citas.Count(c => c.estado == "Cancelada");

                // 2. Métricas de Usuarios
                ViewBag.TotalPacientes = db.Paciente.Count();
                ViewBag.TotalDoctores = db.Doctor.Count();
                ViewBag.UsuariosNuevosHoy = db.Usuario.Count(u => u.fecha_registro >= DateTime.Today);
                ViewBag.UsuariosActivos = db.Usuario.Count(u => u.activo);
                ViewBag.UsuariosInactivos = db.Usuario.Count(u => !u.activo);

                // 3. Obtener lista completa para la tabla
                var usuarios = db.Usuario.ToList();

                // DEBUG: Verificar que hay usuarios
                System.Diagnostics.Debug.WriteLine($"Total usuarios cargados: {usuarios.Count}");

                return View(usuarios);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en PanelAdmin: {ex.Message}");
                TempData["Error"] = "Error al cargar el panel";
                return View(new List<Usuario>());
            }
        }

        #endregion

        #region Gestión de Doctores

        /// <summary>
        /// Crea un nuevo doctor (POST desde formulario tradicional)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearDoctor(CrearDoctorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor, corrija los errores en el formulario.";
                return RedirectToAction("PanelAdmin");
            }

            try
            {
                // Validaciones de negocio
                if (db.Usuario.Any(u => u.numero_documento == model.numero_documento))
                {
                    TempData["Error"] = "El número de documento ya está registrado.";
                    return RedirectToAction("PanelAdmin");
                }

                if (db.Usuario.Any(u => u.correo_usuario == model.correo))
                {
                    TempData["Error"] = "El correo electrónico ya está registrado.";
                    return RedirectToAction("PanelAdmin");
                }

                // 1. Crear el usuario con TODOS los campos
                Usuario nuevoUsuario = new Usuario
                {
                    nombres_usuario = model.nombres,
                    apellidos_usuario = model.apellidos,
                    numero_documento = model.numero_documento,
                    correo_usuario = model.correo,
                    telefono_usuario = model.telefono,           // ← NUEVO
                    activo = true,                                 // ← Siempre activo al crear
                    password_usuario = PasswordHasher.Hash(model.password ?? "MediCitas2026"),
                    rol_usuario = "Doctor",
                    fecha_registro = DateTime.Now
                };

                db.Usuario.Add(nuevoUsuario);
                db.SaveChanges();

                // 2. Crear el registro en tabla Doctor
                Doctor nuevoDoctor = new Doctor
                {
                    id_usuario = nuevoUsuario.id_usuario,
                    especialidad = model.especialidad
                };

                db.Doctor.Add(nuevoDoctor);
                db.SaveChanges();

                TempData["Exito"] = $"Doctor {model.nombres} {model.apellidos} creado correctamente.";

                // Registrar en log de auditoría (opcional)
                LogAuditoria("Crear Doctor", $"ID Usuario: {nuevoUsuario.id_usuario}");

                return RedirectToAction("PanelAdmin");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear doctor: " + ex.Message;
                return RedirectToAction("PanelAdmin");
            }
        }

        /// <summary>
        /// Crea un nuevo doctor vía AJAX (para el modal)
        /// </summary>

        [HttpPost]
        public JsonResult CrearDoctorAjax(CrearDoctorViewModel model)
        {
            try
            {
                // Validar modelo
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new
                    {
                        success = false,
                        message = "Datos inválidos",
                        errors = errors
                    });
                }

                // Validar documento duplicado
                if (db.Usuario.Any(u => u.numero_documento == model.numero_documento))
                {
                    return Json(new
                    {
                        success = false,
                        message = "El número de documento ya existe.",
                        field = "documento"
                    });
                }

                // Validar correo duplicado
                if (db.Usuario.Any(u => u.correo_usuario == model.correo))
                {
                    return Json(new
                    {
                        success = false,
                        message = "El correo electrónico ya está registrado.",
                        field = "correo"
                    });
                }

                // Crear usuario con TODOS los campos
                Usuario nuevoUsuario = new Usuario
                {
                    nombres_usuario = model.nombres,
                    apellidos_usuario = model.apellidos,
                    numero_documento = model.numero_documento,
                    correo_usuario = model.correo,
                    telefono_usuario = string.IsNullOrEmpty(model.telefono) ? null : model.telefono,
                    activo = model.activo,
                    password_usuario = PasswordHasher.Hash(string.IsNullOrEmpty(model.password) ? "MediCitas2026" : model.password),
                    rol_usuario = "Doctor",
                    fecha_registro = DateTime.Now
                };

                db.Usuario.Add(nuevoUsuario);
                db.SaveChanges();

                Doctor nuevoDoctor = new Doctor
                {
                    id_usuario = nuevoUsuario.id_usuario,
                    especialidad = model.especialidad
                };

                db.Doctor.Add(nuevoDoctor);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Doctor creado exitosamente",
                    user = new
                    {
                        id = nuevoUsuario.id_usuario,
                        documento = nuevoUsuario.numero_documento,
                        nombre = $"{nuevoUsuario.nombres_usuario} {nuevoUsuario.apellidos_usuario}",
                        correo = nuevoUsuario.correo_usuario,
                        telefono = nuevoUsuario.telefono_usuario,
                        activo = nuevoUsuario.activo,
                        rol = "Doctor",
                        fecha_registro = nuevoUsuario.fecha_registro.ToString("dd/MM/yyyy")
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CrearDoctorAjax: {ex}");
                return Json(new
                {
                    success = false,
                    message = "Error interno del servidor. Por favor intente nuevamente."
                });
            }
        }

        #endregion

        #region Gestión de Usuarios

        /// <summary>
        /// Cambiar estado de un usuario (activar/desactivar)
        /// </summary>


        [HttpPost]
        public JsonResult CambiarEstadoUsuario(int id)
        {
            try
            {
                var usuario = db.Usuario.Find(id);
                if (usuario == null)
                    return Json(new { success = false, message = "Usuario no encontrado" });

                // SOLUCIÓN: Conversión segura de Session para evitar "Conversión no válida"
                int? idSesion = Session["id_usuario"] as int?;

                if (idSesion.HasValue && idSesion.Value == id)
                    return Json(new { success = false, message = "No puedes cambiar el estado de tu propia cuenta" });

                usuario.activo = !usuario.activo;
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Usuario {(usuario.activo ? "activado" : "desactivado")} correctamente",
                    nuevoEstado = usuario.activo
                });
            }
            catch (Exception ex)
            {
                // Log para debug
                System.Diagnostics.Debug.WriteLine($"Error en CambiarEstado: {ex.Message}");
                return Json(new { success = false, message = "Error de servidor al cambiar estado" });
            }
        }

        /// <summary>
        /// Obtener detalles completos de un usuario
        /// </summary>
        public JsonResult ObtenerUsuario(int id)
        {
            try
            {
                var usuario = db.Usuario.AsNoTracking().FirstOrDefault(u => u.id_usuario == id);
                if (usuario == null)
                    return Json(new { success = false, message = "Usuario no encontrado" });

                // Obtener especialidad de forma segura
                string especialidadStr = "";
                if (usuario.rol_usuario == "Doctor")
                {
                    var doctor = db.Doctor.FirstOrDefault(d => d.id_usuario == id);
                    especialidadStr = doctor?.especialidad ?? "Sin especialidad";
                }

                // Historial con manejo de nulos y formato de strings manual
                var historial = new List<object>();
                if (usuario.rol_usuario == "Paciente")
                {
                    var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == id);
                    if (paciente != null)
                    {
                        historial = db.Citas
                            .Where(c => c.id_paciente == paciente.id_paciente)
                            .OrderByDescending(c => c.fecha_cita)
                            .Take(5)
                            .ToList() // Traemos a memoria para formatear sin errores de LINQ to Entities
                            .Select(c => new {
                                fecha = c.fecha_cita.ToString("dd/MM/yyyy"),
                                hora = c.hora_cita.ToString(),
                                estado = c.estado ?? "Pendiente",
                                especialidad = c.especialidad ?? "General"
                            }).Cast<object>().ToList();
                    }
                }

                return Json(new
                {
                    success = true,
                    user = new
                    {
                        id = usuario.id_usuario,
                        nombres = usuario.nombres_usuario ?? "",
                        apellidos = usuario.apellidos_usuario ?? "",
                        documento = usuario.numero_documento ?? "",
                        correo = usuario.correo_usuario ?? "",
                        telefono = usuario.telefono_usuario ?? "No registrado",
                        rol = usuario.rol_usuario,
                        activo = usuario.activo,
                        especialidad = especialidadStr,
                        fechaRegistro = usuario.fecha_registro.ToString("dd/MM/yyyy")
                    },
                    historial = historial
                }, JsonRequestBehavior.AllowGet); // Importante para peticiones GET
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }

        /// <summary>
        /// Actualizar datos de un usuario
        /// </summary>

        [HttpPost]
        public JsonResult ActualizarUsuario(ActualizarUsuarioViewModel model)
        {
            try
            {
                if (model == null)
                    return Json(new { success = false, message = "Datos inválidos" });

                var usuario = db.Usuario.Find(model.id);
                if (usuario == null)
                    return Json(new { success = false, message = "Usuario no encontrado" });

                // Actualizar campos básicos
                usuario.nombres_usuario = model.nombres ?? usuario.nombres_usuario;
                usuario.apellidos_usuario = model.apellidos ?? usuario.apellidos_usuario;
                usuario.correo_usuario = model.correo ?? usuario.correo_usuario;
                usuario.telefono_usuario = model.telefono ?? usuario.telefono_usuario;

                // Si es doctor y cambió especialidad
                if (usuario.rol_usuario == "Doctor" && !string.IsNullOrEmpty(model.especialidad))
                {
                    var doctor = db.Doctor.FirstOrDefault(d => d.id_usuario == usuario.id_usuario);
                    if (doctor != null)
                    {
                        doctor.especialidad = model.especialidad;
                    }
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Usuario actualizado correctamente",
                    user = new
                    {
                        id = usuario.id_usuario,
                        documento = usuario.numero_documento,
                        nombre = $"{usuario.nombres_usuario} {usuario.apellidos_usuario}",
                        correo = usuario.correo_usuario,
                        telefono = usuario.telefono_usuario,
                        rol = usuario.rol_usuario,
                        activo = usuario.activo
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        #endregion

        #region Reportes

        public ActionResult Reportes()
        {
            return View();
        }

        public ActionResult GenerarCrystalReport(string tipoReporte, DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                // Aquí iría la lógica de Crystal Reports
                // Por ahora simulamos una descarga

                TempData["Info"] = $"Generando reporte {tipoReporte} " +
                    $"desde {fechaInicio?.ToString("dd/MM/yyyy") ?? "siempre"} " +
                    $"hasta {fechaFin?.ToString("dd/MM/yyyy") ?? "ahora"}";

                return RedirectToAction("Reportes");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al generar reporte: " + ex.Message;
                return RedirectToAction("Reportes");
            }
        }

        #endregion

        #region Cerrar Sesión

        public ActionResult Logout()
        {
            LogAuditoria("Logout", $"Usuario: {Session["usuario"]}");

            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Auth");
        }

        #endregion

        #region Métodos Privados

        private void LogAuditoria(string accion, string detalle)
        {
            try
            {
                // Aquí puedes implementar un log en base de datos
                // Por ahora solo escribimos en debug
                System.Diagnostics.Debug.WriteLine($"[AUDITORÍA] {DateTime.Now}: {accion} - {detalle} - Admin: {Session["id_usuario"]}");
            }
            catch
            {
                // Silently fail - no interrumpir el flujo principal
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}