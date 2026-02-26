using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    // Controlador del panel administrativo
    public class AdminController : Controller
    {
        // Panel principal del administrador
        public ActionResult Index()
        {
            return View();
        }

        // Cerrar sesión (solo visual por ahora)
        public ActionResult Logout()
        {
            // Más adelante aquí limpiaremos sesión
            return RedirectToAction("Login", "Pages");
        }
        //public ActionResult Index(string estado, DateTime? fechaInicio, DateTime? fechaFin)
        //{
        //    var citas = db.Citas.AsQueryable();

        //    if (!string.IsNullOrEmpty(estado))
        //        citas = citas.Where(c => c.Estado == estado);

        //    if (fechaInicio.HasValue)
        //        citas = citas.Where(c => c.Fecha >= fechaInicio.Value);

        //    if (fechaFin.HasValue)
        //        citas = citas.Where(c => c.Fecha <= fechaFin.Value);

        //    var lista = citas.ToList();

        //    ViewBag.TotalCitas = lista.Count();
        //    ViewBag.CitasPendientes = lista.Count(c => c.Estado == "Pendiente");
        //    ViewBag.CitasCompletadas = lista.Count(c => c.Estado == "Completada");
        //    ViewBag.CitasCanceladas = lista.Count(c => c.Estado == "Cancelada");
        //    ViewBag.TotalPacientes = db.Usuarios.Count(u => u.Rol == "Paciente");

        //    return View();
        //}
    }

}