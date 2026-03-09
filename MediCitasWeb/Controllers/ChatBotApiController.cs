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
        public async Task<IHttpActionResult> Consultar([FromBody] ConsultaDto datos)
        {
            if (datos == null || string.IsNullOrWhiteSpace(datos.mensaje))
                return BadRequest("El mensaje no puede estar vacío.");

            try
            {
                // 1. VALIDACIÓN DE SESIÓN PROFESIONAL
                var context = HttpContext.Current;
                if (context?.Session?["id_usuario"] == null)
                    return Unauthorized();

                int idUsuario = Convert.ToInt32(context.Session["id_usuario"]);
                var usuario = await db.Usuario.FirstOrDefaultAsync(u => u.id_usuario == idUsuario);

                if (usuario == null) return NotFound();

                // 2. RECOPILACIÓN DE CONTEXTO (Citas y FAQs)
                var citas = await db.Citas
                    .Where(c => c.id_paciente == (db.Paciente.FirstOrDefault(p => p.id_usuario == idUsuario).id_paciente))
                    .OrderByDescending(c => c.fecha_cita)
                    .Take(5) // Solo las 5 más recientes para no saturar el prompt
                    .Select(c => new { c.fecha_cita, c.hora_cita, c.especialidad, c.estado })
                    .ToListAsync();

                var faqs = await db.ChatFAQ.Where(f => f.activo).ToListAsync();

                // 3. CONSTRUCCIÓN DEL PROMPT (Estructura XML-like para mejor entendimiento de la IA)
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("Eres MediBot, el asistente virtual inteligente de la clínica MediCitas.");
                promptBuilder.AppendLine("Tu objetivo es ayudar al paciente con información de sus citas y dudas generales.");
                promptBuilder.AppendLine($"Contexto del Usuario: {usuario.nombres_usuario} {usuario.apellidos_usuario}");

                promptBuilder.AppendLine("<CITAS_RECIENTES>");
                if (!citas.Any()) promptBuilder.AppendLine("No hay citas registradas.");
                else foreach (var c in citas)
                    promptBuilder.AppendLine($"- {c.fecha_cita:yyyy-MM-dd} a las {c.hora_cita} | Especialidad: {c.especialidad} | Estado: {c.estado}");
                promptBuilder.AppendLine("</CITAS_RECIENTES>");

                promptBuilder.AppendLine("<CONOCIMIENTO_FAQ>");
                foreach (var f in faqs) promptBuilder.AppendLine($"Q: {f.pregunta} A: {f.respuesta}");
                promptBuilder.AppendLine("</CONOCIMIENTO_FAQ>");

                promptBuilder.AppendLine("Instrucciones: Responde de forma cordial, breve y en español. Si te preguntan algo fuera de MediCitas, di amablemente que solo puedes ayudar con temas de la clínica.");

                // 4. LLAMADA A GROQ API
                var apiKey = ConfigurationManager.AppSettings["GroqApiKey"];
                if (string.IsNullOrEmpty(apiKey)) return InternalServerError(new Exception("Error de configuración: API Key faltante."));

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                    var requestBody = new
                    {
                        model = "llama-3.3-70b-versatile",
                        messages = new[]
                        {
                            new { role = "system", content = promptBuilder.ToString() },
                            new { role = "user", content = datos.mensaje }
                        },
                        temperature = 0.6,
                        max_tokens = 500
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        return Content(response.StatusCode, $"Error en IA: {errorContent}");
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var groqData = JObject.Parse(jsonResponse);
                    string botRespuesta = groqData["choices"]?[0]?["message"]?["content"]?.ToString();

                    return Ok(new { respuesta = botRespuesta, exito = true });
                }
            }
            catch (Exception ex)
            {
                // Loguear el error (opcional)
                return InternalServerError(new Exception("Lo siento, tuve un problema interno al procesar tu solicitud."));
            }
        }
    }
}