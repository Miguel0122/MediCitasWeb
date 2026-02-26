using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    public class DoctorController : Controller
    {
        public ActionResult Dashboard()
        {
            return View();
        }

        public ActionResult MisCitas()
        {
            return View();
        }
    }
}