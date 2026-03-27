using MediCitasWeb.Models;
using MediCitasWeb.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    [SessionAuthorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private MediCitasContext db = new MediCitasContext();

        // ── PANEL PRINCIPAL ──────────────────────────────────────────────────
        public ActionResult CitasDoctor()
        {
            int idUsuario = Convert.ToInt32(Session["id_usuario"]);

            var doctor = db.Doctor.FirstOrDefault(d => d.id_usuario == idUsuario);
            if (doctor == null)
                return Content("Acceso denegado: No es un perfil de doctor.");

            // ViewBag para el header
            var usuarioDoc = db.Usuario.FirstOrDefault(u => u.id_usuario == idUsuario);
            if (usuarioDoc != null)
            {
                ViewBag.NombreDoctor = $"{usuarioDoc.nombres_usuario} {usuarioDoc.apellidos_usuario}";
                ViewBag.Especialidad = doctor.especialidad;
            }

            var citas = (from c in db.Citas
                         join p in db.Paciente on c.id_paciente equals p.id_paciente
                         join u in db.Usuario on p.id_usuario equals u.id_usuario
                         where c.id_doctor == doctor.id_doctor
                         orderby c.fecha_cita, c.hora_cita
                         select new CitaPacienteViewModel
                         {
                             IdCita = c.id_cita,
                             IdPaciente = c.id_paciente,
                             NombrePaciente = u.nombres_usuario + " " + u.apellidos_usuario,
                             Documento = u.numero_documento,
                             Fecha = c.fecha_cita,
                             Hora = c.hora_cita,
                             Especialidad = c.especialidad,
                             Estado = c.estado,
                             Tipo = c.tipo_consulta
                         }).ToList();

            return View("PanelDoctor", citas);
        }

        // ── ALIAS PARA /Doctor/PanelDoctor (redirige a CitasDoctor) ─────────
        public ActionResult PanelDoctor()
        {
            return CitasDoctor();  // ← CORREGIDO: llama al método correcto
        }

        // ── CAMBIAR ESTADO (AJAX JSON) ───────────────────────────────────────
        [HttpPost]
        public JsonResult CambiarEstadoCita(int citaId, string estado)
        {
            try
            {
                if (Session["rol"] as string != "Doctor")
                    return Json(new { success = false, message = "No autorizado" });

                if (estado != "Atendida" && estado != "Cancelada")
                    return Json(new { success = false, message = "Estado no válido" });

                var cita = db.Citas.FirstOrDefault(c => c.id_cita == citaId);
                if (cita == null)
                    return Json(new { success = false, message = "Cita no encontrada" });

                cita.estado = estado;
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DoctorController] CambiarEstadoCita: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── CAMBIAR ESTADO (POST clásico) ─────────────────────────────────────
        [HttpPost]
        public ActionResult CambiarEstado(int idCita, string nuevoEstado)
        {
            if (Session["rol"] as string != "Doctor")
                return RedirectToAction("Login", "Auth");

            if (nuevoEstado != "Completada" && nuevoEstado != "Cancelada")
                return RedirectToAction("CitasDoctor");

            var cita = db.Citas.FirstOrDefault(c => c.id_cita == idCita);
            if (cita == null) return HttpNotFound();

            cita.estado = nuevoEstado;
            db.SaveChanges();

            return RedirectToAction("CitasDoctor");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}