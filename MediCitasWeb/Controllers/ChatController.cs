using MediCitasWeb.Filters;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    [SessionAuthorize(Roles = "Paciente")]
    public class ChatController : Controller
    {
        public ActionResult ChatBot()
        {
            // Usa la vista existente en Views/ChatBot/ChatBot.cshtml
            return View("~/Views/ChatBot/ChatBot.cshtml");
        }
    }
}

