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

        public ActionResult CitasDoctor()
        {
            int idUsuario = Convert.ToInt32(Session["id_usuario"]);

            var doctor = db.Doctor.FirstOrDefault(d => d.id_usuario == idUsuario);
            if (doctor == null) return Content("Acceso denegado: No es un perfil de doctor.");

            var citas = (from c in db.Citas
                         join p in db.Paciente on c.id_paciente equals p.id_paciente
                         join u in db.Usuario on p.id_usuario equals u.id_usuario
                         where c.id_doctor == doctor.id_doctor
                         orderby c.fecha_cita, c.hora_cita
                         select new CitaPacienteViewModel
                         {
                             IdCita = c.id_cita,
                             NombrePaciente = u.nombres_usuario + " " + u.apellidos_usuario,
                             Documento = u.numero_documento,
                             Fecha = c.fecha_cita,
                             Hora = c.hora_cita,
                             Especialidad = c.especialidad,
                             Estado = c.estado,
                             Tipo = c.tipo_consulta
                         }).ToList();

            return View("PanelDoctor",citas);
        }

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
    }
}