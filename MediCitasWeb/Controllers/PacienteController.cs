using MediCitasWeb.Models;
using MediCitasWeb.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    [SessionAuthorize(Roles = "Paciente")]
    public class PacienteController : Controller
    {
        private readonly MediCitasContext db = new MediCitasContext();

        // ════════════════════════════════════════════════════════════════════
        // AGENDAR CITA (GET)
        // ════════════════════════════════════════════════════════════════════
        public ActionResult AgendarCita(int? idCita)
        {
            // Obtener lista de doctores con sus nombres
            var doctores = (from d in db.Doctor
                            join u in db.Usuario on d.id_usuario equals u.id_usuario
                            select new DoctorViewModel
                            {
                                IdDoctor = d.id_doctor,
                                NombreDoctor = u.nombres_usuario + " " + u.apellidos_usuario,
                                Especialidad = d.especialidad
                            }).ToList();

            ViewBag.Doctores = doctores;

            int idUsuario = Convert.ToInt32(Session["id_usuario"]);
            var usuario = db.Usuario.FirstOrDefault(u => u.id_usuario == idUsuario);

            // Verificar si es reprogramación
            if (idCita.HasValue)
            {
                var cita = db.Citas.FirstOrDefault(c => c.id_cita == idCita.Value);
                if (cita != null && cita.estado == "Activa")
                {
                    // Verificar que la cita pertenece al paciente actual
                    var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == idUsuario);
                    if (paciente != null && cita.id_paciente == paciente.id_paciente)
                    {
                        ViewBag.CitaEditar = cita;
                        ViewBag.EsReprogramacion = true;
                        ViewBag.CitaId = idCita.Value;
                        ViewBag.EspecialidadPrecargada = cita.especialidad;
                        ViewBag.DoctorPrecargado = cita.id_doctor;
                        ViewBag.FechaPrecargada = cita.fecha_cita.ToString("yyyy-MM-dd");
                        ViewBag.HoraPrecargada = cita.hora_cita.ToString(@"hh\:mm");
                        ViewBag.TipoPrecargado = cita.tipo_consulta;
                    }
                }
            }

            return View(usuario);
        }

        // ════════════════════════════════════════════════════════════════════
        // GUARDAR CITA (POST)
        // ════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarCita(int idDoctor, string especialidad,
                                        string tipoConsulta, DateTime fechaCita,
                                        string horaCita, int? idCita)
        {
            if (Session["usuario"] == null)
                return RedirectToAction("Login", "Auth");

            int idUsuario = Convert.ToInt32(Session["id_usuario"]);
            var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == idUsuario);

            if (paciente == null)
            {
                TempData["Error"] = "Paciente no encontrado.";
                return RedirectToAction("AgendarCita");
            }

            // VALIDACIÓN 1: Hora válida
            if (!TimeSpan.TryParse(horaCita, out TimeSpan hora))
            {
                TempData["Error"] = "Hora inválida. Use formato HH:MM.";
                return RedirectToAction("AgendarCita");
            }

            // VALIDACIÓN 2: Horario de atención (6:00 AM - 6:00 PM)
            var horaInicio = new TimeSpan(6, 0, 0);
            var horaFin = new TimeSpan(18, 0, 0);
            if (hora < horaInicio || hora > horaFin)
            {
                TempData["Error"] = "La hora debe estar entre 6:00 AM y 6:00 PM.";
                return RedirectToAction("AgendarCita");
            }

            // VALIDACIÓN 3: Fecha no puede ser anterior a hoy
            if (fechaCita.Date < DateTime.Today)
            {
                TempData["Error"] = "No se pueden agendar citas en fechas pasadas.";
                return RedirectToAction("AgendarCita");
            }

            // VALIDACIÓN 4: No se pueden agendar citas los domingos
            if (fechaCita.DayOfWeek == DayOfWeek.Sunday)
            {
                TempData["Error"] = "No hay atención los domingos. Seleccione lunes a sábado.";
                return RedirectToAction("AgendarCita");
            }

            // VALIDACIÓN 5: Verificar que el doctor existe y está activo
            var doctor = db.Doctor.Find(idDoctor);
            if (doctor == null)
            {
                TempData["Error"] = "Doctor no encontrado.";
                return RedirectToAction("AgendarCita");
            }

            var usuarioDoctor = db.Usuario.Find(doctor.id_usuario);
            if (usuarioDoctor == null || !usuarioDoctor.activo)
            {
                TempData["Error"] = "El doctor seleccionado no está disponible actualmente.";
                return RedirectToAction("AgendarCita");
            }

            // VALIDACIÓN 6: Evitar conflicto de horario con el mismo doctor
            var citaExistente = db.Citas.FirstOrDefault(c =>
                c.id_doctor == idDoctor &&
                c.fecha_cita == fechaCita &&
                c.hora_cita == hora &&
                c.estado == "Activa" &&
                (!idCita.HasValue || c.id_cita != idCita.Value));

            if (citaExistente != null)
            {
                TempData["Error"] = "El doctor ya tiene una cita agendada en ese horario. Por favor selecciona otra hora.";
                return RedirectToAction("AgendarCita");
            }

            // VALIDACIÓN 7: Evitar que el paciente tenga dos citas a la misma hora
            var citaPacienteExistente = db.Citas.FirstOrDefault(c =>
                c.id_paciente == paciente.id_paciente &&
                c.fecha_cita == fechaCita &&
                c.hora_cita == hora &&
                c.estado == "Activa" &&
                (!idCita.HasValue || c.id_cita != idCita.Value));

            if (citaPacienteExistente != null)
            {
                TempData["Error"] = "Ya tienes una cita agendada en ese horario.";
                return RedirectToAction("AgendarCita");
            }

            Cita cita;

            if (idCita.HasValue)
            {
                // REPROGRAMACIÓN
                cita = db.Citas.FirstOrDefault(c => c.id_cita == idCita.Value && c.id_paciente == paciente.id_paciente);
                if (cita == null)
                {
                    TempData["Error"] = "Cita no encontrada para reprogramar.";
                    return RedirectToAction("MisCitas");
                }

                if (cita.estado != "Activa")
                {
                    TempData["Error"] = "Solo se pueden reprogramar citas activas.";
                    return RedirectToAction("MisCitas");
                }

                // Guardar datos anteriores para auditoría
                var fechaAnterior = cita.fecha_cita;
                var horaAnterior = cita.hora_cita;

                cita.id_doctor = idDoctor;
                cita.especialidad = especialidad;
                cita.tipo_consulta = tipoConsulta;
                cita.fecha_cita = fechaCita;
                cita.hora_cita = hora;
                cita.estado = "Activa";
                cita.observaciones = $"Reprogramada desde {fechaAnterior:dd/MM/yyyy} {horaAnterior} el {DateTime.Now:dd/MM/yyyy HH:mm}";

                db.SaveChanges();

                TempData["Exito"] = $"✅ Cita reprogramada exitosamente para el {fechaCita:dd/MM/yyyy} a las {horaCita} en {especialidad}.";
            }
            else
            {
                // NUEVA CITA
                cita = new Cita
                {
                    id_paciente = paciente.id_paciente,
                    id_doctor = idDoctor,
                    especialidad = especialidad,
                    tipo_consulta = tipoConsulta,
                    fecha_cita = fechaCita,
                    hora_cita = hora,
                    estado = "Activa",
                    fecha_registro = DateTime.Now,
                    observaciones = $"Cita agendada el {DateTime.Now:dd/MM/yyyy HH:mm}"
                };

                db.Citas.Add(cita);
                db.SaveChanges();

                TempData["Exito"] = $"✅ Cita agendada exitosamente para el {fechaCita:dd/MM/yyyy} a las {horaCita} en {especialidad}.";
            }

            return RedirectToAction("MisCitas");
        }

        // ════════════════════════════════════════════════════════════════════
        // MIS CITAS (GET)
        // ════════════════════════════════════════════════════════════════════
        public ActionResult MisCitas()
        {
            if (Session["usuario"] == null)
                return RedirectToAction("Login", "Auth");

            int idUsuario = Convert.ToInt32(Session["id_usuario"]);
            var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == idUsuario);

            if (paciente == null)
                return View(new List<Cita>());

            var citas = db.Citas
                .Where(c => c.id_paciente == paciente.id_paciente)
                .OrderByDescending(c => c.fecha_cita)
                .ThenByDescending(c => c.hora_cita)
                .ToList();

            // Cargar nombres de doctores para mostrar
            var doctores = (from d in db.Doctor
                            join u in db.Usuario on d.id_usuario equals u.id_usuario
                            select new
                            {
                                d.id_doctor,
                                NombreDoctor = u.nombres_usuario + " " + u.apellidos_usuario
                            }).ToDictionary(d => d.id_doctor, d => d.NombreDoctor);

            ViewBag.Doctores = doctores;

            return View(citas);
        }

        // ════════════════════════════════════════════════════════════════════
        // CANCELAR CITA (AJAX)
        // ════════════════════════════════════════════════════════════════════
        [HttpPost]
        public JsonResult CancelarCita(int idCita)
        {
            try
            {
                int idUsuario = Convert.ToInt32(Session["id_usuario"]);
                var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == idUsuario);

                if (paciente == null)
                    return Json(new { success = false, message = "Paciente no encontrado." });

                var cita = db.Citas.FirstOrDefault(c => c.id_cita == idCita && c.id_paciente == paciente.id_paciente);

                if (cita == null)
                    return Json(new { success = false, message = "Cita no encontrada." });

                if (cita.estado != "Activa")
                    return Json(new { success = false, message = "Solo se pueden cancelar citas activas." });

                // VALIDACIÓN: Cancelación con mínimo 2 horas de anticipación
                if (cita.fecha_cita <= DateTime.Now.AddHours(2))
                {
                    return Json(new { success = false, message = "No se puede cancelar una cita con menos de 2 horas de anticipación." });
                }

                cita.estado = "Cancelada";
                cita.observaciones = $"Cancelada por el paciente el {DateTime.Now:dd/MM/yyyy HH:mm}";
                db.SaveChanges();

                return Json(new { success = true, message = "Cita cancelada exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // OBTENER DETALLE CITA (AJAX)
        // ════════════════════════════════════════════════════════════════════
        [HttpGet]
        public JsonResult ObtenerDetalleCita(int idCita)
        {
            try
            {
                int idUsuario = Convert.ToInt32(Session["id_usuario"]);
                var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == idUsuario);

                if (paciente == null)
                    return Json(new { success = false, message = "Paciente no encontrado." }, JsonRequestBehavior.AllowGet);

                var cita = db.Citas.FirstOrDefault(c => c.id_cita == idCita && c.id_paciente == paciente.id_paciente);

                if (cita == null)
                    return Json(new { success = false, message = "Cita no encontrada." }, JsonRequestBehavior.AllowGet);

                var doctor = db.Doctor.Find(cita.id_doctor);
                var doctorUsuario = doctor != null ? db.Usuario.Find(doctor.id_usuario) : null;

                return Json(new
                {
                    success = true,
                    cita = new
                    {
                        id = cita.id_cita,
                        especialidad = cita.especialidad,
                        fecha = cita.fecha_cita.ToString("dd/MM/yyyy"),
                        hora = cita.hora_cita.ToString(@"hh\:mm"),
                        tipo = cita.tipo_consulta,
                        estado = cita.estado,
                        doctor = doctorUsuario != null ? $"{doctorUsuario.nombres_usuario} {doctorUsuario.apellidos_usuario}" : "No asignado",
                        observaciones = cita.observaciones
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // REPROGRAMAR CITA (GET)
        // ════════════════════════════════════════════════════════════════════
        [HttpGet]
        public ActionResult ReprogramarCita(int idCita)
        {
            try
            {
                int idUsuario = Convert.ToInt32(Session["id_usuario"]);
                var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == idUsuario);

                if (paciente == null)
                {
                    TempData["Error"] = "Paciente no encontrado.";
                    return RedirectToAction("MisCitas");
                }

                var cita = db.Citas.FirstOrDefault(c => c.id_cita == idCita && c.id_paciente == paciente.id_paciente);

                if (cita == null)
                {
                    TempData["Error"] = "Cita no encontrada.";
                    return RedirectToAction("MisCitas");
                }

                if (cita.estado != "Activa")
                {
                    TempData["Error"] = "Solo se pueden reprogramar citas activas.";
                    return RedirectToAction("MisCitas");
                }

                return RedirectToAction("AgendarCita", new { idCita = idCita });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al reprogramar: " + ex.Message;
                return RedirectToAction("MisCitas");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}