using MediCitasWeb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace MediCitasWeb.Controllers.Api
{
    [RoutePrefix("api/chatbot")]
    public class ChatBotApiController : ApiController
    {
        private readonly MediCitasContext db = new MediCitasContext();

        public class ConsultaDto
        {
            public string mensaje { get; set; }
        }


        [HttpPost]
        [Route("consultar")]
        [ActionName("consultar")]
        public async Task<IHttpActionResult> Consultar([FromBody] ConsultaDto datos)
        {
            if (datos == null || string.IsNullOrWhiteSpace(datos.mensaje))
                return BadRequest("Mensaje vacío.");

            try
            {
                var context = HttpContext.Current;
                if (context?.Session?["id_usuario"] == null) return Unauthorized();
                int idUsuario = Convert.ToInt32(context.Session["id_usuario"]);

                // 1. Sesión de Chat
                var hoy = DateTime.Today;
                var sesion = await db.ChatSesiones
                    .FirstOrDefaultAsync(s => s.id_usuario == idUsuario && DbFunctions.TruncateTime(s.fecha_inicio) == hoy);

                if (sesion == null)
                {
                    sesion = new ChatSesion { id_usuario = idUsuario, fecha_inicio = DateTime.Now };
                    db.ChatSesiones.Add(sesion);
                    await db.SaveChangesAsync();
                }

                // 2. Guardar mensaje (USANDO TUS NOMBRES: remitente y fecha_envio)
                db.ChatMensajes.Add(new ChatMensaje
                {
                    id_sesion = sesion.id_sesion,
                    remitente = "user",  // <-- Tu campo
                    contenido = datos.mensaje,
                    fecha_envio = DateTime.Now // <-- Tu campo
                });

                // 3. Llamada a Groq
                var apiKey = ConfigurationManager.AppSettings["GroqApiKey"];
                string botRespuesta = await EjecutarLlamadaGroq(datos.mensaje, apiKey);

                // 4. Guardar respuesta del Bot
                db.ChatMensajes.Add(new ChatMensaje
                {
                    id_sesion = sesion.id_sesion,
                    remitente = "assistant",
                    contenido = botRespuesta,
                    fecha_envio = DateTime.Now
                });

                await db.SaveChangesAsync();
                return Ok(new { respuesta = botRespuesta });
            }
            catch (Exception ex)
            {
                // Esto te ayudará a ver el error real en la consola si algo falla en el C#
                return InternalServerError(ex);
            }
        }

        private async Task<string> EjecutarLlamadaGroq(string mensajeUsuario, string apiKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var requestBody = new
                {
                    model = "llama-3.3-70b-versatile",
                    messages = new[]
                    {
                new { role = "system", content = "Eres MediBot, asistente de la clínica MediCitas. Responde de forma breve y amable en español." },
                new { role = "user", content = mensajeUsuario }
            },
                    temperature = 0.7
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode) return "Lo siento, tuve un problema al conectar con mi cerebro virtual.";

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var groqData = JObject.Parse(jsonResponse);
                return groqData["choices"]?[0]?["message"]?["content"]?.ToString() ?? "No recibí respuesta.";
            }
        }
    }
}