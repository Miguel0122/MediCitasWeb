using MediCitasWeb.Filters;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    public class HomeController : Controller
    {
        // Esta es la acción que carga el Dashboard
        [SessionAuthorize] // Opcional: para que solo entren logueados
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