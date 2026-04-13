using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    public class HomeController : Controller
    {
        // Esta acción hará que se cargue la vista Views/Home/Index.cshtml
        public ActionResult Index()
        {
            return View();
        }
    }
}