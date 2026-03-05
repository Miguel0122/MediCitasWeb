using MediCitasWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    public class PagesController : Controller
    {
        MediCitasContext db = new MediCitasContext();

        public ActionResult AgendarCita()
        {
            if (Session["usuario"] == null) return RedirectToAction("Login", "Auth");

            // Join Doctor + Usuario para obtener el nombre completo del médico
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
            return View(usuario);
        }

        [HttpPost]
        public ActionResult GuardarCita(int idDoctor, string especialidad,
                                        string tipoConsulta, DateTime fechaCita,
                                        string horaCita)
        {
            if (Session["usuario"] == null)
                return RedirectToAction("Login", "Auth");

            int idUsuario = Convert.ToInt32(Session["id_usuario"]);

            var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == idUsuario);
            if (paciente == null)
                return RedirectToAction("Login", "Auth");

            TimeSpan hora = TimeSpan.Parse(horaCita);

            Cita cita = new Cita()
            {
                id_paciente = paciente.id_paciente,
                id_doctor = idDoctor,        // ✅ viene del <select>
                especialidad = especialidad,
                tipo_consulta = tipoConsulta,
                fecha_cita = fechaCita,
                hora_cita = hora,
                estado = "Activa"
            };

            db.Citas.Add(cita);
            db.SaveChanges();
            return RedirectToAction("MisCitas");
        }

        public ActionResult MisCitas()
        {
            if (Session["usuario"] == null) return RedirectToAction("Login", "Auth");

            int idUsuario = Convert.ToInt32(Session["id_usuario"]);

            var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == idUsuario);
            if (paciente == null) return View(new List<Cita>());

            var citas = db.Citas.Where(c => c.id_paciente == paciente.id_paciente).ToList();
            return View(citas);
        }
    }
}