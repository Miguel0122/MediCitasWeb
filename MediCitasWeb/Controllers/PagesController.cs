using System;
using System.Linq;
using System.Web.Mvc;
using MediCitasWeb.Models;

namespace MediCitasWeb.Controllers
{
    public class PagesController : Controller
    {
        MediCitasContext db = new MediCitasContext();

        // ==============================
        // PAGINA AGENDAR
        // ==============================
        public ActionResult AgendarCita()
        {
            if (Session["usuario"] == null)
                return RedirectToAction("Login", "Auth");

            int idUsuario = Convert.ToInt32(Session["usuario"]);

            var usuario = db.Usuario.FirstOrDefault(u => u.id_usuario == idUsuario);

            if (usuario == null)
                return RedirectToAction("Login", "Auth");

            return View(usuario);
        }

        // ==============================
        // GUARDAR CITA
        // ==============================
        [HttpPost]
        public ActionResult GuardarCita(string especialidad, string tipoConsulta, DateTime fechaCita, string horaCita)
        {
            if (Session["usuario"] == null)
                return RedirectToAction("Login", "Auth");

            int idUsuario = Convert.ToInt32(Session["usuario"]);

            var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == idUsuario);

            if (paciente == null)
                return RedirectToAction("Login", "Auth");

            TimeSpan hora = TimeSpan.Parse(horaCita);

            Cita cita = new Cita()
            {
                id_paciente = paciente.id_paciente,
                id_doctor = 1,
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

        // ==============================
        // VER MIS CITAS
        // ==============================
        public ActionResult MisCitas()
        {
            if (Session["usuario"] == null)
                return RedirectToAction("Login", "Auth");

            int idUsuario = Convert.ToInt32(Session["usuario"]);

            var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == idUsuario);

            var citas = db.Citas
                .Where(c => c.id_paciente == paciente.id_paciente)
                .ToList();

            return View(citas);
        }
    }
}