using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    // Controlador encargado de mostrar las páginas del sistema
    public class PagesController : Controller
    {
        // Muestra la vista para agendar una cita médica
        public ActionResult AgendarCita()
        {
            return View();
        }
        // Muestra la lista de citas del usuario
        public ActionResult MisCitas()
        {
            return View();
        }


    }
}