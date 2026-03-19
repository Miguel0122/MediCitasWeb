using MediCitasWeb.Filters;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    [SessionAuthorize]
    public class ChatController : Controller
    {
        public ActionResult ChatBot()
        {
            return View("~/Views/ChatBot/ChatBot.cshtml");
        }
    }
}

