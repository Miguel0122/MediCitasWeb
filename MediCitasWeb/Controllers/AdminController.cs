using MediCitasWeb.Models;
using MediCitasWeb.Services.Security;
using MediCitasWeb.Filters;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SysColor = System.Drawing.Color;

namespace MediCitasWeb.Controllers
{
    [SessionAuthorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly MediCitasContext db = new MediCitasContext();

        // ════════════════════════════════════════════════════════════════════
        #region Panel Principal
        // ════════════════════════════════════════════════════════════════════

        public ActionResult PanelAdmin()
        {
            try
            {
                ViewBag.TotalCitas = db.Citas.Count();
                ViewBag.CitasPendientes = db.Citas.Count(c => c.estado == "Activa");
                ViewBag.CitasCompletadas = db.Citas.Count(c => c.estado == "Atendida");
                ViewBag.CitasCanceladas = db.Citas.Count(c => c.estado == "Cancelada");
                ViewBag.TotalPacientes = db.Paciente.Count();
                ViewBag.TotalDoctores = db.Doctor.Count();
                ViewBag.UsuariosNuevosHoy = db.Usuario.Count(u => u.fecha_registro >= DateTime.Today);
                ViewBag.UsuariosActivos = db.Usuario.Count(u => u.activo);
                ViewBag.UsuariosInactivos = db.Usuario.Count(u => !u.activo);

                return View(db.Usuario.ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en PanelAdmin: {ex.Message}");
                TempData["Error"] = "Error al cargar el panel.";
                return View(new List<Usuario>());
            }
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════
        #region Gestión de Doctores
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Crea un doctor desde formulario tradicional (no AJAX).</summary>
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

                var nuevoUsuario = new Usuario
                {
                    nombres_usuario = model.nombres,
                    apellidos_usuario = model.apellidos,
                    numero_documento = model.numero_documento,
                    correo_usuario = model.correo,
                    telefono_usuario = model.telefono,
                    activo = true,
                    password_usuario = PasswordHasher.Hash(model.password ?? "MediCitas2026"),
                    rol_usuario = "Doctor",
                    fecha_registro = DateTime.Now
                };

                db.Usuario.Add(nuevoUsuario);
                db.SaveChanges();

                db.Doctor.Add(new Doctor
                {
                    id_usuario = nuevoUsuario.id_usuario,
                    especialidad = model.especialidad
                });
                db.SaveChanges();

                LogAuditoria("Crear Doctor", $"ID: {nuevoUsuario.id_usuario}");
                TempData["Exito"] = $"Doctor {model.nombres} {model.apellidos} creado correctamente.";
                return RedirectToAction("PanelAdmin");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear doctor: " + ex.Message;
                return RedirectToAction("PanelAdmin");
            }
        }

        /// <summary>Crea un doctor vía AJAX desde el modal del panel.</summary>
        [HttpPost]
        public JsonResult CrearDoctorAjax(CrearDoctorViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errores = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Datos inválidos", errors = errores });
                }

                if (db.Usuario.Any(u => u.numero_documento == model.numero_documento))
                    return Json(new { success = false, message = "El número de documento ya existe.", field = "documento" });

                if (db.Usuario.Any(u => u.correo_usuario == model.correo))
                    return Json(new { success = false, message = "El correo ya está registrado.", field = "correo" });

                var nuevoUsuario = new Usuario
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

                db.Doctor.Add(new Doctor
                {
                    id_usuario = nuevoUsuario.id_usuario,
                    especialidad = model.especialidad
                });
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Doctor creado exitosamente.",
                    user = new
                    {
                        id = nuevoUsuario.id_usuario,
                        documento = nuevoUsuario.numero_documento,
                        nombre = $"{nuevoUsuario.nombres_usuario} {nuevoUsuario.apellidos_usuario}",
                        nombres = nuevoUsuario.nombres_usuario,
                        apellidos = nuevoUsuario.apellidos_usuario,
                        correo = nuevoUsuario.correo_usuario,
                        telefono = nuevoUsuario.telefono_usuario,
                        activo = nuevoUsuario.activo,
                        rol = "Doctor",
                        especialidad = model.especialidad,
                        fecha_registro = nuevoUsuario.fecha_registro.ToString("dd/MM/yyyy")
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error CrearDoctorAjax: {ex}");
                return Json(new { success = false, message = "Error interno del servidor. Intente nuevamente." });
            }
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════
        #region Gestión de Usuarios
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Activa o desactiva un usuario. No permite auto-desactivación.</summary>
        [HttpPost]
        public JsonResult CambiarEstadoUsuario(int id)
        {
            try
            {
                var usuario = db.Usuario.Find(id);
                if (usuario == null)
                    return Json(new { success = false, message = "Usuario no encontrado." });

                // Protección: el admin no puede desactivar su propia cuenta
                var sesionId = Session["id_usuario"];
                int idSesion = sesionId is int i ? i : (int.TryParse(sesionId?.ToString(), out int p) ? p : 0);
                if (idSesion == id)
                    return Json(new { success = false, message = "No puedes cambiar el estado de tu propia cuenta." });

                usuario.activo = !usuario.activo;
                db.SaveChanges();
                LogAuditoria("CambiarEstado", $"Usuario {id} → {(usuario.activo ? "Activo" : "Inactivo")}");

                return Json(new
                {
                    success = true,
                    message = $"Usuario {(usuario.activo ? "activado" : "desactivado")} correctamente.",
                    nuevoEstado = usuario.activo
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error CambiarEstado: {ex.Message}");
                return Json(new { success = false, message = "Error de servidor al cambiar estado." });
            }
        }

        /// <summary>Devuelve los detalles completos de un usuario para el modal.</summary>
        [HttpGet]
        public JsonResult ObtenerUsuario(int id)
        {
            try
            {
                var usuario = db.Usuario.AsNoTracking().FirstOrDefault(u => u.id_usuario == id);
                if (usuario == null)
                    return Json(new { success = false, message = "Usuario no encontrado." }, JsonRequestBehavior.AllowGet);

                string especialidad = "";
                if (usuario.rol_usuario == "Doctor")
                    especialidad = db.Doctor.FirstOrDefault(d => d.id_usuario == id)?.especialidad ?? "Sin especialidad";

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
                            .ToList()
                            .Select(c => (object)new
                            {
                                fecha = c.fecha_cita.ToString("dd/MM/yyyy"),
                                hora = c.hora_cita.ToString(),
                                estado = c.estado ?? "Pendiente",
                                especialidad = c.especialidad ?? "General"
                            }).ToList();
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
                        nombre = $"{usuario.nombres_usuario} {usuario.apellidos_usuario}",
                        documento = usuario.numero_documento ?? "",
                        correo = usuario.correo_usuario ?? "",
                        telefono = usuario.telefono_usuario ?? "No registrado",
                        rol = usuario.rol_usuario,
                        activo = usuario.activo,
                        especialidad = especialidad,
                        fechaRegistro = usuario.fecha_registro.ToString("dd/MM/yyyy")
                    },
                    historial
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>Actualiza los datos básicos de un usuario.</summary>
        [HttpPost]
        public JsonResult ActualizarUsuario(ActualizarUsuarioViewModel model)
        {
            try
            {
                if (model == null)
                    return Json(new { success = false, message = "Datos inválidos." });

                var usuario = db.Usuario.Find(model.id);
                if (usuario == null)
                    return Json(new { success = false, message = "Usuario no encontrado." });

                // Validar correo duplicado (excluyendo el propio)
                if (!string.IsNullOrEmpty(model.correo) &&
                    db.Usuario.Any(u => u.correo_usuario == model.correo && u.id_usuario != model.id))
                    return Json(new { success = false, message = "El correo ya está en uso por otro usuario." });

                usuario.nombres_usuario = model.nombres ?? usuario.nombres_usuario;
                usuario.apellidos_usuario = model.apellidos ?? usuario.apellidos_usuario;
                usuario.correo_usuario = model.correo ?? usuario.correo_usuario;
                usuario.telefono_usuario = model.telefono ?? usuario.telefono_usuario;

                if (usuario.rol_usuario == "Doctor" && !string.IsNullOrEmpty(model.especialidad))
                {
                    var doctor = db.Doctor.FirstOrDefault(d => d.id_usuario == usuario.id_usuario);
                    if (doctor != null) doctor.especialidad = model.especialidad;
                }

                db.SaveChanges();
                LogAuditoria("ActualizarUsuario", $"ID: {usuario.id_usuario}");

                return Json(new
                {
                    success = true,
                    message = "Usuario actualizado correctamente.",
                    user = new
                    {
                        id = usuario.id_usuario,
                        documento = usuario.numero_documento,
                        nombre = $"{usuario.nombres_usuario} {usuario.apellidos_usuario}",
                        nombres = usuario.nombres_usuario,
                        apellidos = usuario.apellidos_usuario,
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

        // ════════════════════════════════════════════════════════════════════
        #region Reportes
        // ════════════════════════════════════════════════════════════════════

        public ActionResult Reportes() => View();

        /// <summary>Devuelve datos en JSON para la previsualización interactiva.</summary>
        [HttpGet]
        public JsonResult ObtenerDatosReporte(
            string tipo, string fechaInicio, string fechaFin,
            string especialidad, string estadoCita, string rol,
            bool soloActivos = false)
        {
            try
            {
                var (inicio, fin) = ParsearFechas(fechaInicio, fechaFin);

                object result;
                // Switch tradicional para compatibilidad con C# 7.3
                switch (tipo)
                {
                    case "Usuarios": result = ObtenerDatosUsuarios(rol, soloActivos, inicio, fin); break;
                    case "Doctores": result = ObtenerDatosDoctores(especialidad); break;
                    default: result = ObtenerDatosCitas(inicio, fin, especialidad, estadoCita); break;
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>Descarga el reporte como PDF o Excel.</summary>
        [HttpGet]
        public ActionResult DescargarReporte(
            string tipo, string fechaInicio, string fechaFin,
            string especialidad, string estadoCita, string rol,
            bool soloActivos = false, string formato = "pdf")
        {
            try
            {
                var (inicio, fin) = ParsearFechas(fechaInicio, fechaFin);
                string periodoStr = $"{fechaInicio ?? "Inicio"} al {fechaFin ?? "Hoy"}";

                object reporte;
                string titulo;
                switch (tipo)
                {
                    case "Usuarios":
                        reporte = ObtenerDatosUsuarios(rol, soloActivos, inicio, fin);
                        titulo = "Directorio de Usuarios";
                        break;
                    case "Doctores":
                        reporte = ObtenerDatosDoctores(especialidad);
                        titulo = "Equipo Médico";
                        break;
                    default:
                        reporte = ObtenerDatosCitas(inicio, fin, especialidad, estadoCita);
                        titulo = "Informe de Citas Medicas";
                        break;
                }

                dynamic r = reporte;
                var filas = ConvertirFilas(reporte);
                var columnas = r.columnas;
                var kpis = r.kpis;

                return formato == "excel"
                    ? GenerarExcel(filas, columnas, titulo, periodoStr, kpis)
                    : (ActionResult)GenerarPDF(filas, columnas, titulo, periodoStr, kpis);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al generar reporte: " + ex.Message;
                return RedirectToAction("Reportes");
            }
        }

        // ── Builders de datos ────────────────────────────────────────────────

        private object ObtenerDatosCitas(DateTime? inicio, DateTime? fin, string especialidad, string estado)
        {
            var query = db.Citas.AsQueryable();
            if (inicio.HasValue) query = query.Where(c => c.fecha_cita >= inicio.Value);
            if (fin.HasValue) query = query.Where(c => c.fecha_cita <= fin.Value);
            if (!string.IsNullOrEmpty(especialidad)) query = query.Where(c => c.especialidad == especialidad);
            if (!string.IsNullOrEmpty(estado)) query = query.Where(c => c.estado == estado);

            var citas = query.OrderByDescending(c => c.fecha_cita).ToList();

            var filas = citas.Select(c =>
            {
                var nombrePaciente = db.Paciente
                    .Where(p => p.id_paciente == c.id_paciente)
                    .Join(db.Usuario, p => p.id_usuario, u => u.id_usuario, (p, u) => u.nombres_usuario + " " + u.apellidos_usuario)
                    .FirstOrDefault() ?? "N/A";

                var nombreDoctor = db.Doctor
                    .Where(d => d.id_doctor == c.id_doctor)
                    .Join(db.Usuario, d => d.id_usuario, u => u.id_usuario, (d, u) => u.nombres_usuario + " " + u.apellidos_usuario)
                    .FirstOrDefault() ?? "N/A";

                return new Dictionary<string, string>
                {
                    ["fecha"] = c.fecha_cita.ToString("dd/MM/yyyy"),
                    ["hora"] = c.hora_cita.ToString(@"hh\:mm"),
                    ["paciente"] = nombrePaciente,
                    ["doctor"] = "Dr. " + nombreDoctor,
                    ["especialidad"] = c.especialidad,
                    ["tipo"] = c.tipo_consulta,
                    ["estado"] = c.estado
                };
            }).ToList();

            return new
            {
                success = true,
                filas,
                columnas = new[]
                {
                    new { key = "fecha",        label = "Fecha",        tipo = "text"  },
                    new { key = "hora",          label = "Hora",         tipo = "text"  },
                    new { key = "paciente",      label = "Paciente",     tipo = "text"  },
                    new { key = "doctor",        label = "Doctor",       tipo = "text"  },
                    new { key = "especialidad",  label = "Especialidad", tipo = "text"  },
                    new { key = "tipo",          label = "Tipo",         tipo = "text"  },
                    new { key = "estado",        label = "Estado",       tipo = "badge" }
                },
                kpis = new object[]
                {
                    new { label = "Total",        valor = citas.Count },
                    new { label = "Activas",      valor = citas.Count(c => c.estado == "Activa") },
                    new { label = "Atendidas",    valor = citas.Count(c => c.estado == "Atendida") },
                    new { label = "Canceladas",   valor = citas.Count(c => c.estado == "Cancelada") },
                    new { label = "Inasistencia", valor = citas.Count(c => c.estado == "Inasistencia") }
                }
            };
        }

        private object ObtenerDatosUsuarios(string rol, bool soloActivos, DateTime? inicio, DateTime? fin)
        {
            var query = db.Usuario.AsQueryable();
            if (!string.IsNullOrEmpty(rol)) query = query.Where(u => u.rol_usuario == rol);
            if (soloActivos) query = query.Where(u => u.activo);
            if (inicio.HasValue) query = query.Where(u => u.fecha_registro >= inicio.Value);
            if (fin.HasValue) query = query.Where(u => u.fecha_registro <= fin.Value);

            var usuarios = query.OrderBy(u => u.apellidos_usuario).ToList();

            var filas = usuarios.Select(u =>
            {
                string esp = u.rol_usuario == "Doctor"
                    ? db.Doctor.FirstOrDefault(d => d.id_usuario == u.id_usuario)?.especialidad ?? "—"
                    : "—";

                return new Dictionary<string, string>
                {
                    ["documento"] = u.numero_documento,
                    ["nombre"] = u.nombres_usuario + " " + u.apellidos_usuario,
                    ["correo"] = u.correo_usuario,
                    ["telefono"] = u.telefono_usuario ?? "—",
                    ["rol"] = u.rol_usuario,
                    ["especialidad"] = esp,
                    ["estado"] = u.activo ? "Activo" : "Inactivo",
                    ["registro"] = u.fecha_registro.ToString("dd/MM/yyyy")
                };
            }).ToList();

            return new
            {
                success = true,
                filas,
                columnas = new[]
                {
                    new { key = "documento",    label = "Documento",    tipo = "text"  },
                    new { key = "nombre",        label = "Nombre",       tipo = "text"  },
                    new { key = "correo",        label = "Correo",       tipo = "text"  },
                    new { key = "rol",           label = "Rol",          tipo = "badge" },
                    new { key = "especialidad",  label = "Especialidad", tipo = "text"  },
                    new { key = "estado",        label = "Estado",       tipo = "badge" },
                    new { key = "registro",      label = "Registro",     tipo = "text"  }
                },
                kpis = new object[]
                {
                    new { label = "Total",      valor = usuarios.Count },
                    new { label = "Pacientes",  valor = usuarios.Count(u => u.rol_usuario == "Paciente") },
                    new { label = "Doctores",   valor = usuarios.Count(u => u.rol_usuario == "Doctor") },
                    new { label = "Activos",    valor = usuarios.Count(u => u.activo) },
                    new { label = "Inactivos",  valor = usuarios.Count(u => !u.activo) }
                }
            };
        }

        private object ObtenerDatosDoctores(string especialidad)
        {
            var query = db.Doctor.AsQueryable();
            if (!string.IsNullOrEmpty(especialidad)) query = query.Where(d => d.especialidad == especialidad);

            var doctores = query.ToList();

            var filas = doctores.Select(d =>
            {
                var u = db.Usuario.FirstOrDefault(x => x.id_usuario == d.id_usuario);
                int totalCitas = db.Citas.Count(c => c.id_doctor == d.id_doctor);
                int citasHoy = db.Citas.Count(c => c.id_doctor == d.id_doctor && c.fecha_cita == DateTime.Today);

                return new Dictionary<string, string>
                {
                    ["documento"] = u?.numero_documento ?? "—",
                    ["nombre"] = "Dr. " + (u?.nombres_usuario + " " + u?.apellidos_usuario),
                    ["correo"] = u?.correo_usuario ?? "—",
                    ["telefono"] = u?.telefono_usuario ?? "—",
                    ["especialidad"] = d.especialidad,
                    ["totalCitas"] = totalCitas.ToString(),
                    ["citasHoy"] = citasHoy.ToString(),
                    ["estado"] = (u?.activo ?? false) ? "Activo" : "Inactivo"
                };
            }).ToList();

            return new
            {
                success = true,
                filas,
                columnas = new[]
                {
                    new { key = "nombre",        label = "Doctor",       tipo = "text"  },
                    new { key = "especialidad",  label = "Especialidad", tipo = "text"  },
                    new { key = "correo",        label = "Correo",       tipo = "text"  },
                    new { key = "totalCitas",    label = "Total Citas",  tipo = "text"  },
                    new { key = "citasHoy",      label = "Citas Hoy",    tipo = "text"  },
                    new { key = "estado",        label = "Estado",       tipo = "badge" }
                },
                kpis = new object[]
                {
                    new { label = "Total doctores",  valor = doctores.Count },
                    new { label = "Especialidades",  valor = doctores.Select(d => d.especialidad).Distinct().Count() }
                }
            };
        }

        // ── Generadores de archivo ────────────────────────────────────────────

        private FileContentResult GenerarPDF(
            List<Dictionary<string, string>> filas,
            dynamic columnas, string titulo, string periodo, dynamic kpis)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                var doc = new Document(PageSize.A4.Rotate(), 30, 30, 40, 30);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // Colores corporativos
                var azul = new BaseColor(13, 110, 253);
                var azulOsc = new BaseColor(10, 88, 202);
                var blanco = BaseColor.WHITE;
                var grisClaro = new BaseColor(248, 249, 250);
                var grisTexto = new BaseColor(108, 117, 125);
                var negro = BaseColor.BLACK;

                // Fuentes
                var fTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, blanco);
                var fSubtit = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(220, 230, 255));
                var fKpiVal = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, blanco);
                var fKpiLbl = FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(200, 215, 255));
                var fHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, blanco);
                var fCelda = FontFactory.GetFont(FontFactory.HELVETICA, 8, negro);
                var fPie = FontFactory.GetFont(FontFactory.HELVETICA, 7, grisTexto);

                // ── Encabezado ───────────────────────────────────────────────
                var tHeader = new PdfPTable(1) { WidthPercentage = 100 };
                var cHeader = new PdfPCell { BackgroundColor = azul, Border = iTextSharp.text.Rectangle.NO_BORDER, Padding = 18 };

                var parTitulo = new Paragraph();
                // FIX CS0117: usar Chunk normal con fondo blanco usando SetBackground
                var chunkLogo = new Chunk("  MediCitas  ",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(13, 110, 253)));
                chunkLogo.SetBackground(blanco, 4, 2, 4, 2);
                parTitulo.Add(chunkLogo);
                parTitulo.Add(new Chunk("   " + titulo, fTitulo));
                cHeader.AddElement(parTitulo);
                cHeader.AddElement(new Paragraph($"Periodo: {periodo}   |   Generado: {DateTime.Now:dd/MM/yyyy HH:mm}", fSubtit) { SpacingBefore = 4 });

                // KPIs
                if (kpis != null)
                {
                    var kpiList = new System.Collections.Generic.List<dynamic>();
                    foreach (var k in kpis) kpiList.Add(k);

                    var tKpis = new PdfPTable(kpiList.Count) { WidthPercentage = 100 };
                    tKpis.DefaultCell.Border = iTextSharp.text.Rectangle.NO_BORDER;
                    foreach (var kpi in kpiList)
                    {
                        var kCell = new PdfPCell
                        {
                            Border = iTextSharp.text.Rectangle.NO_BORDER,
                            BackgroundColor = azulOsc,
                            Padding = 8,
                            PaddingTop = 10
                        };
                        kCell.AddElement(new Paragraph(kpi.valor.ToString(), fKpiVal) { Alignment = Element.ALIGN_CENTER });
                        kCell.AddElement(new Paragraph(kpi.label.ToString(), fKpiLbl) { Alignment = Element.ALIGN_CENTER });
                        tKpis.AddCell(kCell);
                    }
                    tKpis.SpacingBefore = 12;
                    cHeader.AddElement(tKpis);
                }
                tHeader.AddCell(cHeader);
                doc.Add(tHeader);

                // ── Tabla de datos ───────────────────────────────────────────
                var colList = new System.Collections.Generic.List<dynamic>();
                foreach (var c in columnas) colList.Add(c);

                var tData = new PdfPTable(colList.Count) { WidthPercentage = 100, SpacingBefore = 12 };

                foreach (var col in colList)
                    tData.AddCell(new PdfPCell(new Phrase(col.label.ToString().ToUpper(), fHeader))
                    {
                        BackgroundColor = azulOsc,
                        Border = iTextSharp.text.Rectangle.NO_BORDER,
                        Padding = 8,
                        HorizontalAlignment = Element.ALIGN_LEFT
                    });

                bool impar = true;
                foreach (var fila in filas)
                {
                    var bg = impar ? blanco : grisClaro;
                    foreach (var col in colList)
                    {
                        string val = fila.ContainsKey(col.key.ToString()) ? fila[col.key.ToString()] : "—";
                        tData.AddCell(new PdfPCell(new Phrase(val, fCelda))
                        {
                            BackgroundColor = bg,
                            BorderColor = new BaseColor(222, 226, 230),
                            BorderWidth = 0.3f,
                            Padding = 7
                        });
                    }
                    impar = !impar;
                }

                // Fila de total
                tData.AddCell(new PdfPCell(new Phrase($"Total: {filas.Count} registros", fHeader))
                {
                    Colspan = colList.Count,
                    BackgroundColor = azul,
                    Border = iTextSharp.text.Rectangle.NO_BORDER,
                    Padding = 8,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });
                doc.Add(tData);

                // Pie de página
                doc.Add(new Paragraph(
                    $"\nGenerado automáticamente por MediCitas · {DateTime.Now:yyyy}  |  Confidencial — Solo uso interno", fPie)
                { Alignment = Element.ALIGN_CENTER, SpacingBefore = 10 });

                doc.Close();

                string nombre = $"MediCitas_{titulo.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(ms.ToArray(), "application/pdf", nombre);
            }
        }

        private FileContentResult GenerarExcel(
            List<Dictionary<string, string>> filas,
            dynamic columnas, string titulo, string periodo, dynamic kpis)
        {
            // FIX CS0618: forma correcta para EPPlus 5+ y 8+
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var pkg = new ExcelPackage())
            {
                var ws = pkg.Workbook.Worksheets.Add(titulo);

                var azul = SysColor.FromArgb(13, 110, 253);
                var azulOsc = SysColor.FromArgb(10, 88, 202);
                var grisClaro = SysColor.FromArgb(248, 249, 250);

                var colList = new System.Collections.Generic.List<dynamic>();
                foreach (var c in columnas) colList.Add(c);
                int nCols = colList.Count;

                // Fila 1: Título
                ws.Cells[1, 1, 1, nCols].Merge = true;
                ws.Cells[1, 1].Value = $"MediCitas — {titulo}";
                ws.Cells[1, 1].Style.Font.Size = 16;
                ws.Cells[1, 1].Style.Font.Bold = true;
                ws.Cells[1, 1].Style.Font.Color.SetColor(SysColor.White);
                ws.Cells[1, 1, 1, nCols].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[1, 1, 1, nCols].Style.Fill.BackgroundColor.SetColor(azul);
                ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Rows[1].Height = 32;

                // Fila 2: Subtítulo
                ws.Cells[2, 1, 2, nCols].Merge = true;
                ws.Cells[2, 1].Value = $"Periodo: {periodo}  |  Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Cells[2, 1].Style.Font.Size = 10;
                ws.Cells[2, 1].Style.Font.Color.SetColor(SysColor.White);
                ws.Cells[2, 1, 2, nCols].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[2, 1, 2, nCols].Style.Fill.BackgroundColor.SetColor(azulOsc);
                ws.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Filas 3-4: KPIs
                int col = 1;
                if (kpis != null)
                    foreach (var kpi in kpis)
                    {
                        ws.Cells[3, col].Value = kpi.valor;
                        ws.Cells[3, col].Style.Font.Bold = true;
                        ws.Cells[3, col].Style.Font.Size = 14;
                        ws.Cells[3, col].Style.Font.Color.SetColor(azul);
                        ws.Cells[4, col].Value = kpi.label;
                        ws.Cells[4, col].Style.Font.Color.SetColor(SysColor.Gray);
                        ws.Cells[4, col].Style.Font.Size = 9;
                        col++;
                    }

                // Fila 6: Headers de tabla
                int fH = 6;
                for (int i = 0; i < nCols; i++)
                {
                    var c = ws.Cells[fH, i + 1];
                    c.Value = colList[i].label.ToString().ToUpper();
                    c.Style.Font.Bold = true;
                    c.Style.Font.Color.SetColor(SysColor.White);
                    c.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    c.Style.Fill.BackgroundColor.SetColor(azulOsc);
                    c.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    ws.Cells[fH, i + 1].Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                }
                ws.Rows[fH].Height = 22;

                // Filas de datos
                int fila = fH + 1;
                bool impar = true;
                foreach (var row in filas)
                {
                    for (int i = 0; i < nCols; i++)
                    {
                        string key = colList[i].key.ToString();
                        var c = ws.Cells[fila, i + 1];
                        c.Value = row.ContainsKey(key) ? row[key] : "—";
                        c.Style.Font.Size = 10;
                        if (!impar)
                        {
                            c.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            c.Style.Fill.BackgroundColor.SetColor(grisClaro);
                        }
                        c.Style.Border.Bottom.Style = ExcelBorderStyle.Hair;
                        c.Style.Border.Bottom.Color.SetColor(SysColor.LightGray);
                    }
                    fila++;
                    impar = !impar;
                }

                // Fila de total
                ws.Cells[fila, 1, fila, nCols].Merge = true;
                ws.Cells[fila, 1].Value = $"Total: {filas.Count} registros";
                ws.Cells[fila, 1].Style.Font.Bold = true;
                ws.Cells[fila, 1].Style.Font.Color.SetColor(SysColor.White);
                ws.Cells[fila, 1, fila, nCols].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[fila, 1, fila, nCols].Style.Fill.BackgroundColor.SetColor(azul);
                ws.Cells[fila, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                ws.Cells[ws.Dimension.Address].AutoFitColumns(10, 40);

                string nombre = $"MediCitas_{titulo.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(pkg.GetAsByteArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre);
            }
        }

        // ── Helpers privados ─────────────────────────────────────────────────

        private (DateTime? inicio, DateTime? fin) ParsearFechas(string fechaInicio, string fechaFin)
        {
            DateTime? inicio = string.IsNullOrEmpty(fechaInicio) ? (DateTime?)null : DateTime.Parse(fechaInicio);
            DateTime? fin = string.IsNullOrEmpty(fechaFin) ? (DateTime?)null : DateTime.Parse(fechaFin).AddDays(1).AddSeconds(-1);
            return (inicio, fin);
        }

        private List<Dictionary<string, string>> ConvertirFilas(dynamic reporte)
        {
            var lista = new List<Dictionary<string, string>>();
            foreach (var fila in reporte.filas)
                lista.Add(fila);
            return lista;
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════
        #region Sesión y Auditoría
        // ════════════════════════════════════════════════════════════════════

        public ActionResult Logout()
        {
            LogAuditoria("Logout", $"Usuario: {Session["usuario"]}");
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Auth");
        }

        private void LogAuditoria(string accion, string detalle)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AUDITORIA] {DateTime.Now:dd/MM/yyyy HH:mm:ss} | {accion} | {detalle} | Admin: {Session["id_usuario"]}");
            }
            catch { /* No interrumpir el flujo principal */ }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        #endregion
    }
}