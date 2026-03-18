using MediCitasWeb.Filters;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Dashboard()
        {
            return View();
        }
    }
}