using MediCitasWeb.Models;
using MediCitasWeb.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MediCitasWeb.Controllers
{
    [SessionAuthorize]
    public class ChatController : Controller
    {
        private MediCitasContext db = new MediCitasContext();

        private readonly string GroqApiKey =
            System.Web.Configuration.WebConfigurationManager.AppSettings["GroqApiKey"];

        // ─── VISTA PRINCIPAL ───────────────────────────────────────────────────
        public ActionResult ChatBot()
        {
            try
            {
                if (Session["id_usuario"] == null)
                    return RedirectToAction("Login", "Auth");

                int usuarioId = ObtenerUsuarioId();
                if (usuarioId == 0)
                    return RedirectToAction("Login", "Auth");

                var sesionActiva = db.ChatSesiones
                    .FirstOrDefault(s => s.id_usuario == usuarioId && s.fecha_fin == null);

                if (sesionActiva == null)
                {
                    sesionActiva = new ChatSesion
                    {
                        id_usuario = usuarioId,
                        fecha_inicio = DateTime.Now
                    };
                    db.ChatSesiones.Add(sesionActiva);
                    db.SaveChanges();
                }

                ViewBag.SesionId = sesionActiva.id_sesion;
                ViewBag.ContextoUsuario = ConstruirContextoUsuario(usuarioId);
                ViewBag.NombreUsuario = Session["usuario"]?.ToString() ?? "";

                return View("~/Views/ChatBot/ChatBot.cshtml");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ChatBot: {ex.Message}");
                ViewBag.SesionId = 0;
                ViewBag.ContextoUsuario = "";
                ViewBag.NombreUsuario = "";
                return View("~/Views/ChatBot/ChatBot.cshtml");
            }
        }

        // ─── ENVIAR MENSAJE (llama a Groq + guarda en BD) ─────────────────────
        [HttpPost]
        public async Task<JsonResult> EnviarMensaje(int sesionId, string mensaje)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mensaje))
                    return Json(new { success = false, message = "Mensaje vacio" });

                if (Session["id_usuario"] == null)
                    return Json(new { success = false, message = "Sesion no valida" });

                int usuarioId = ObtenerUsuarioId();

                // 1. Historial previo para dar contexto a Groq
                var historial = db.ChatMensajes
                    .Where(m => m.id_sesion == sesionId)
                    .OrderByDescending(m => m.fecha_envio)
                    .Take(10)
                    .OrderBy(m => m.fecha_envio)
                    .Select(m => new { m.remitente, m.contenido })
                    .ToList();

                // 2. Guardar mensaje del usuario
                db.ChatMensajes.Add(new ChatMensaje
                {
                    id_sesion = sesionId,
                    remitente = "user",
                    contenido = mensaje,
                    fecha_envio = DateTime.Now
                });
                db.SaveChanges();

                // 3. Construir contexto del usuario
                string contexto = ConstruirContextoUsuario(usuarioId);

                // 4. Llamar a Groq (gratis)
                string respuesta = await LlamarGroq(contexto, historial
                    .Select(h => new {
                        role = h.remitente == "user" ? "user" : "assistant",
                        content = h.contenido
                    }).ToList<dynamic>(), mensaje);

                // 5. Fallback si Groq falla
                if (string.IsNullOrEmpty(respuesta))
                    respuesta = GenerarRespuestaLocal(mensaje);

                // 6. Guardar respuesta del bot
                db.ChatMensajes.Add(new ChatMensaje
                {
                    id_sesion = sesionId,
                    remitente = "bot",
                    contenido = respuesta,
                    fecha_envio = DateTime.Now
                });
                db.SaveChanges();

                return Json(new { success = true, respuesta, hora = DateTime.Now.ToString("HH:mm") });
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.InnerException?.Message ?? dbEx.InnerException?.Message ?? dbEx.Message;
                return Json(new { success = false, message = "DB Error: " + inner });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ─── LLAMADA A GROQ (GRATIS) ───────────────────────────────────────────
        private async Task<string> LlamarGroq(string contexto, List<dynamic> historial, string mensajeActual)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", GroqApiKey);
                    client.Timeout = TimeSpan.FromSeconds(20);

                    string systemPrompt = $@"Eres MediBot, el asistente virtual de MediCitas, sistema de citas medicas en Colombia.

DATOS DEL USUARIO:
{contexto}

INSTRUCCIONES:
- Responde SIEMPRE en espanol, de forma amigable y personalizada usando el nombre del usuario.
- Si el usuario escribe con errores tipograficos o palabras incompletas, deduce la intencion y responde con normalidad.
- Usa los datos del usuario (citas, doctor, especialidad) para personalizar cada respuesta.
- Pacientes: ayuda con agendar, cancelar o consultar citas, horarios, especialistas.
- Doctores: agenda del dia, citas programadas, horarios.
- Administradores: estadisticas del sistema, usuarios, reportes.
- Horarios: Lunes-Viernes 6AM-6PM | Sabados 7AM-2PM | Domingos cerrado.
- Respuestas cortas (3-5 lineas). Sin emojis para evitar problemas de codificacion.
- Si preguntan algo fuera del sistema medico, redirige amablemente.";

                    // Armar mensajes: system + historial + mensaje actual
                    var messages = new List<object>
                    {
                        new { role = "system", content = systemPrompt }
                    };

                    foreach (var msg in historial)
                    {
                        string role = (string)msg.role;
                        string content = (string)msg.content;
                        if (role == "bot") role = "assistant";
                        messages.Add(new { role, content });
                    }

                    messages.Add(new { role = "user", content = mensajeActual });

                    var payload = new
                    {
                        model = "llama-3.3-70b-versatile",
                        messages = messages,
                        temperature = 0.7,
                        max_tokens = 500
                    };

                    var httpContent = new StringContent(
                        JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(
                        "https://api.groq.com/openai/v1/chat/completions", httpContent);

                    var body = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Groq Error {response.StatusCode}: {body}");
                        return null;
                    }

                    var json = JObject.Parse(body);
                    string texto = json["choices"]?[0]?["message"]?["content"]?.ToString();
                    System.Diagnostics.Debug.WriteLine($"Groq OK: {texto?.Substring(0, Math.Min(60, texto?.Length ?? 0))}");
                    return texto;
                }
            }
            catch (TaskCanceledException) { System.Diagnostics.Debug.WriteLine("Groq timeout"); return null; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Groq ex: {ex.Message}"); return null; }
        }

        // ─── CONTEXTO DEL USUARIO ─────────────────────────────────────────────
        private string ConstruirContextoUsuario(int usuarioId)
        {
            try
            {
                var usuario = db.Usuario.Find(usuarioId);
                if (usuario == null) return "";

                string rol = usuario.rol_usuario;
                string nombre = $"{usuario.nombres_usuario} {usuario.apellidos_usuario}";
                var sb = new StringBuilder();
                sb.AppendLine($"Usuario: {nombre} | Rol: {rol} | Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}");

                if (rol == "Paciente")
                {
                    var paciente = db.Paciente.FirstOrDefault(p => p.id_usuario == usuarioId);
                    if (paciente != null)
                    {
                        var citas = db.Citas
                            .Where(c => c.id_paciente == paciente.id_paciente
                                     && c.estado == "Activa"
                                     && c.fecha_cita >= DateTime.Today)
                            .OrderBy(c => c.fecha_cita).Take(3).ToList()
                            .Select(c => new {
                                c.fecha_cita,
                                c.hora_cita,
                                c.especialidad,
                                c.tipo_consulta,
                                Doctor = db.Usuario
                                    .Where(u => db.Doctor.Where(d => d.id_doctor == c.id_doctor)
                                        .Select(d => d.id_usuario).Contains(u.id_usuario))
                                    .Select(u => u.nombres_usuario + " " + u.apellidos_usuario)
                                    .FirstOrDefault() ?? "N/A"
                            }).ToList();

                        if (citas.Any())
                        {
                            sb.AppendLine("Citas proximas:");
                            foreach (var c in citas)
                                sb.AppendLine($"  - {c.fecha_cita:dd/MM/yyyy} {c.hora_cita} con Dr. {c.Doctor} ({c.especialidad}, {c.tipo_consulta})");
                        }
                        else sb.AppendLine("Sin citas activas proximas.");

                        var ultima = db.Citas
                            .Where(c => c.id_paciente == paciente.id_paciente && c.fecha_cita < DateTime.Today)
                            .OrderByDescending(c => c.fecha_cita)
                            .Select(c => new { c.fecha_cita, c.especialidad }).FirstOrDefault();
                        if (ultima != null)
                            sb.AppendLine($"Ultima consulta: {ultima.fecha_cita:dd/MM/yyyy} en {ultima.especialidad}.");
                    }
                }
                else if (rol == "Doctor")
                {
                    var doctor = db.Doctor.FirstOrDefault(d => d.id_usuario == usuarioId);
                    if (doctor != null)
                    {
                        var hoy = DateTime.Today;
                        int citasHoy = db.Citas.Count(c => c.id_doctor == doctor.id_doctor && c.fecha_cita == hoy && c.estado == "Activa");
                        int citasSemana = db.Citas.Count(c => c.id_doctor == doctor.id_doctor && c.fecha_cita >= hoy && c.fecha_cita <= hoy.AddDays(7) && c.estado == "Activa");
                        sb.AppendLine($"Especialidad: {doctor.especialidad}. Citas hoy: {citasHoy} | Proximos 7 dias: {citasSemana}.");
                    }
                }
                else if (rol == "Administrador")
                {
                    int activas = db.Citas.Count(c => c.estado == "Activa");
                    int usuarios = db.Usuario.Count(u => u.activo);
                    int doctores = db.Doctor.Count();
                    sb.AppendLine($"Sistema: Citas activas: {activas} | Usuarios activos: {usuarios} | Doctores: {doctores}.");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error contexto: {ex.Message}");
                return "";
            }
        }

        // ─── HISTORIAL ─────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult ObtenerHistorial(int sesionId)
        {
            try
            {
                var mensajes = db.ChatMensajes
                    .Where(m => m.id_sesion == sesionId)
                    .OrderBy(m => m.fecha_envio)
                    .Select(m => new {
                        id = m.id_mensaje,
                        remitente = m.remitente,
                        contenido = m.contenido,
                        hora = m.fecha_envio.ToString("HH:mm")
                    }).ToList();

                return Json(new { success = true, mensajes }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensajes = new object[0], message = ex.Message },
                            JsonRequestBehavior.AllowGet);
            }
        }

        // ─── CERRAR SESION ─────────────────────────────────────────────────────
        [HttpPost]
        public JsonResult CerrarSesion(int sesionId)
        {
            try
            {
                var sesion = db.ChatSesiones.Find(sesionId);
                if (sesion != null) { sesion.fecha_fin = DateTime.Now; db.SaveChanges(); }
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // ─── FALLBACK LOCAL ────────────────────────────────────────────────────
        private string GenerarRespuestaLocal(string mensaje)
        {
            mensaje = mensaje.ToLower().Trim();
            try
            {
                var faq = db.ChatFAQ.FirstOrDefault(f => f.activo &&
                    (mensaje.Contains(f.pregunta.ToLower().Substring(0, Math.Min(10, f.pregunta.Length))) ||
                     f.pregunta.ToLower().Contains(mensaje)));
                if (faq != null) return faq.respuesta;
            }
            catch { }

            if (mensaje.Contains("agendar") || mensaje.Contains("reservar"))
                return "Para agendar ve al menu principal y selecciona Agendar Cita.";
            if (mensaje.Contains("cancelar"))
                return "Para cancelar ve a Mis Citas y haz clic en Cancelar. Puedes hacerlo hasta 2 horas antes.";
            if (mensaje.Contains("horario") || mensaje.Contains("atencion"))
                return "Horarios: Lun-Vie 6AM-6PM | Sab 7AM-2PM | Dom cerrado.";
            if (mensaje.Contains("hola") || mensaje.Contains("buenas"))
                return "Hola! Soy MediBot. Puedo ayudarte con citas, horarios o especialistas.";
            if (mensaje.Contains("gracias"))
                return "Con gusto! Hay algo mas en lo que pueda ayudarte?";
            return "Puedo ayudarte con citas, horarios o especialistas. Que necesitas?";
        }

        private int ObtenerUsuarioId()
        {
            var v = Session["id_usuario"];
            if (v is int i) return i;
            if (v is string s && int.TryParse(s, out int p)) return p;
            return 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}