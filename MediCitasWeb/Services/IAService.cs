using MediCitasWeb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace MediCitasWeb.Services
{
    /// <summary>
    /// Servicio centralizado de IA para MediCitas.
    /// Todos los módulos del sistema consumen este servicio.
    /// </summary>
    public class IAService
    {
        private readonly string _groqKey;
        private readonly string _modelo = "llama-3.3-70b-versatile";

        public IAService()
        {
            _groqKey = WebConfigurationManager.AppSettings["GroqApiKey"];
        }

        // ═══════════════════════════════════════════════════════════════════
        // MÉTODO PRIVADO BASE — todas las llamadas pasan por aquí
        // ═══════════════════════════════════════════════════════════════════
        public async Task<string> LlamarGroq(string systemPrompt, string userPrompt, double temperature = 0.4)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _groqKey);
                    client.Timeout = TimeSpan.FromSeconds(15);

                    var payload = new
                    {
                        model = _modelo,
                        temperature,
                        max_tokens = 400,
                        messages = new[]
                        {
                            new { role = "system", content = systemPrompt },
                            new { role = "user",   content = userPrompt   }
                        }
                    };

                    var content = new StringContent(
                        JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(
                        "https://api.groq.com/openai/v1/chat/completions", content);

                    if (!response.IsSuccessStatusCode) return null;

                    var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                    return json["choices"]?[0]?["message"]?["content"]?.ToString()?.Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IAService] Error: {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 1. RECOMENDADOR DE ESPECIALIDAD
        //    Paciente describe síntomas → IA sugiere especialidad
        // ═══════════════════════════════════════════════════════════════════
        public async Task<RecomendacionEspecialidadDto> RecomendarEspecialidad(string sintomas)
        {
            if (string.IsNullOrWhiteSpace(sintomas))
                return null;

            string system = @"Eres un asistente médico de triaje del sistema MediCitas en Colombia.
Tu única tarea es analizar síntomas y recomendar la especialidad más apropiada.
Las especialidades disponibles son EXACTAMENTE estas: Medicina General, Pediatría, Cardiología, Odontología, Dermatología, Ginecología.
Responde SOLO con un JSON válido sin texto adicional, con este formato exacto:
{""especialidad"": ""Cardiología"", ""confianza"": ""alta"", ""razon"": ""Los síntomas descritos sugieren...""}
El campo confianza puede ser: alta, media, baja.
La razón debe ser máximo 1 oración corta y amigable.";

            string user = $"El paciente describe: {sintomas}";

            string respuesta = await LlamarGroq(system, user, 0.2);
            if (respuesta == null) return null;

            try
            {
                // Limpiar posibles bloques de código markdown
                respuesta = respuesta.Replace("```json", "").Replace("```", "").Trim();
                var dto = JsonConvert.DeserializeObject<RecomendacionEspecialidadDto>(respuesta);
                return dto;
            }
            catch
            {
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 2. PREDICCIÓN DE RIESGO DE INASISTENCIA
        //    Doctor ve su agenda → IA evalúa riesgo por paciente
        // ═══════════════════════════════════════════════════════════════════
        public async Task<RiesgoInasistenciaDto> PredecirRiesgoInasistencia(
            string nombrePaciente, int totalCitas, int inasistencias, int cancelaciones, int diasHastaCita)
        {
            string system = @"Eres un sistema de análisis de riesgo médico para MediCitas.
Analiza el historial del paciente y predice la probabilidad de inasistencia a su próxima cita.
Responde SOLO con JSON válido sin texto adicional:
{""nivel"": ""alto"", ""porcentaje"": 78, ""mensaje"": ""Este paciente ha cancelado...""}
Niveles posibles: alto (>60%), medio (30-60%), bajo (<30%).
El mensaje debe ser máximo 1 oración corta y accionable para el doctor.";

            string user = $@"Paciente: {nombrePaciente}
Total de citas históricas: {totalCitas}
Inasistencias: {inasistencias}
Cancelaciones: {cancelaciones}
Días hasta la próxima cita: {diasHastaCita}";

            string respuesta = await LlamarGroq(system, user, 0.3);
            if (respuesta == null)
                return new RiesgoInasistenciaDto { Nivel = "bajo", Porcentaje = 10, Mensaje = "Sin datos suficientes" };

            try
            {
                respuesta = respuesta.Replace("```json", "").Replace("```", "").Trim();
                var dto = JsonConvert.DeserializeObject<RiesgoInasistenciaDto>(respuesta);
                return dto ?? new RiesgoInasistenciaDto { Nivel = "bajo", Porcentaje = 10, Mensaje = "Sin datos suficientes" };
            }
            catch
            {
                return new RiesgoInasistenciaDto { Nivel = "bajo", Porcentaje = 10, Mensaje = "Sin datos suficientes" };
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 3. RESUMEN DE HISTORIAL DEL PACIENTE
        //    Doctor abre detalle de paciente → IA genera resumen en 2 seg
        // ═══════════════════════════════════════════════════════════════════
        public async Task<string> ResumirHistorialPaciente(
    string nombrePaciente, int totalCitas,
    List<string> especialidades, string ultimaCita,
    int inasistencias, int cancelaciones)
        {
            string system = @"Eres un asistente médico que genera resúmenes clínicos breves para doctores.
El resumen debe ser profesional, en español, máximo 3 oraciones.
No uses bullets ni listas. Texto corrido, directo y útil para el médico.
No uses markdown. Solo texto plano.
Menciona si el paciente ha cancelado citas o ha tenido inasistencias.";

            string especialidadesStr = especialidades != null && especialidades.Any()
                ? string.Join(", ", especialidades.GroupBy(e => e).Select(g => $"{g.Key} ({g.Count()}x)"))
                : "sin registros";

            string user = $@"Genera un resumen clínico del siguiente paciente:
Nombre: {nombrePaciente}
Total de citas: {totalCitas}
Especialidades consultadas: {especialidadesStr}
Última consulta: {ultimaCita ?? "sin registro"}
Inasistencias: {inasistencias}
Cancelaciones: {cancelaciones}";

            string respuesta = await LlamarGroq(system, user, 0.5);

            if (string.IsNullOrEmpty(respuesta))
            {
                // Fallback local
                string fallback = $"{nombrePaciente} ha tenido {totalCitas} citas en total.";
                if (cancelaciones > 0)
                    fallback += $" Ha cancelado {cancelaciones} citas.";
                if (inasistencias > 0)
                    fallback += $" Tiene {inasistencias} inasistencias registradas.";
                if (!string.IsNullOrEmpty(ultimaCita) && ultimaCita != "sin registro")
                    fallback += $" Su última consulta fue el {ultimaCita}.";
                return fallback;
            }

            return respuesta;
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4. ALERTAS INTELIGENTES PARA EL ADMIN
        //    PanelAdmin → IA analiza métricas y genera alertas accionables
        // ═══════════════════════════════════════════════════════════════════
        public async Task<List<AlertaAdminDto>> GenerarAlertasAdmin(MetricasAdminDto metricas)
        {
            string system = @"Eres un sistema de inteligencia de negocios para una clínica médica colombiana.
Analiza las métricas del sistema y genera entre 2 y 4 alertas accionables para el administrador.
Responde SOLO con un array JSON válido, sin texto adicional:
[{""tipo"": ""warning"", ""icono"": ""bi-exclamation-triangle"", ""titulo"": ""..."", ""mensaje"": ""...""}]
Tipos posibles: warning (amarillo), danger (rojo), success (verde), info (azul).
Iconos de Bootstrap Icons. Títulos máximo 5 palabras. Mensaje máximo 1 oración.
Sé específico con los números, no genérico.";

            string user = $@"Métricas actuales del sistema MediCitas:
- Citas activas hoy: {metricas.CitasHoy}
- Citas canceladas esta semana: {metricas.CanceladasSemana}
- Tasa de inasistencia promedio: {metricas.TasaInasistencia}%
- Especialidad con más citas: {metricas.EspecialidadTop}
- Doctor con más citas hoy: {metricas.DoctorMasCargado} ({metricas.CitasDoctorTop} citas)
- Nuevos pacientes esta semana: {metricas.NuevosPacientesSemana}
- Pacientes sin cita en 30 días: {metricas.PacientesSinCita}
- Total doctores activos: {metricas.TotalDoctores}";

            string respuesta = await LlamarGroq(system, user, 0.6);
            if (respuesta == null) return new List<AlertaAdminDto>();

            try
            {
                respuesta = respuesta.Replace("```json", "").Replace("```", "").Trim();
                return JsonConvert.DeserializeObject<List<AlertaAdminDto>>(respuesta)
                    ?? new List<AlertaAdminDto>();
            }
            catch
            {
                return new List<AlertaAdminDto>();
            }
        }

        public async Task<RecomendacionConversacionalDto> RecomendarEspecialidadConversacional(string sintomas)
        {
            if (string.IsNullOrWhiteSpace(sintomas))
                return null;

            string system = @"Eres un asistente médico amigable de MediCitas en Colombia.
Tu tarea es analizar los síntomas del paciente y darle una recomendación cálida y útil.

RESPONDE COMO UN MÉDICO AMIGABLE:
- Saluda al paciente
- Muestra empatía por sus síntomas
- Explica por qué recomiendas esa especialidad
- Menciona que hay doctores disponibles
- Ofrece ayuda para agendar

EJEMPLO DE RESPUESTA:
'¡Hola! Entiendo que tienes dolor en el pecho y dificultad para respirar. 😟 
Estos síntomas están relacionados con el sistema cardiovascular. 
Por eso te recomiendo agendar una cita con **Cardiología** - un especialista del corazón podrá evaluarte adecuadamente. 
Contamos con cardiólogos disponibles esta semana. ¿Quieres que te ayude a agendar tu cita?'

IMPORTANTE:
- La especialidad debe estar entre: Medicina General, Pediatría, Cardiología, Odontología, Dermatología, Ginecología
- Sé cálido, usa emojis con moderación
- No uses JSON, solo texto natural";

            string user = $"El paciente describe: {sintomas}";

            string respuesta = await LlamarGroq(system, user, 0.7);

            if (string.IsNullOrEmpty(respuesta))
            {
                // Fallback con lógica local
                return GenerarRecomendacionLocal(sintomas);
            }

            return new RecomendacionConversacionalDto
            {
                Exito = true,
                Mensaje = respuesta,
                Especialidad = ExtraerEspecialidadDeRespuesta(respuesta)
            };
        }

        private RecomendacionConversacionalDto GenerarRecomendacionLocal(string sintomas)
        {
            sintomas = sintomas.ToLower();
            string especialidad = "Medicina General";
            string razon = "No se detectaron síntomas específicos.";

            if (sintomas.Contains("pecho") || sintomas.Contains("corazón") || sintomas.Contains("palpitaciones"))
            {
                especialidad = "Cardiología";
                razon = "tus síntomas están relacionados con el corazón y el sistema cardiovascular";
            }
            else if (sintomas.Contains("gripe") || sintomas.Contains("tos") || sintomas.Contains("fiebre"))
            {
                especialidad = "Medicina General";
                razon = "tienes síntomas generales que requieren evaluación primaria";
            }
            else if (sintomas.Contains("niño") || sintomas.Contains("bebé") || sintomas.Contains("pediatría"))
            {
                especialidad = "Pediatría";
                razon = "los síntomas descritos son de un paciente pediátrico";
            }
            else if (sintomas.Contains("muela") || sintomas.Contains("diente") || sintomas.Contains("dental"))
            {
                especialidad = "Odontología";
                razon = "tienes síntomas relacionados con salud dental";
            }
            else if (sintomas.Contains("piel") || sintomas.Contains("mancha") || sintomas.Contains("acné"))
            {
                especialidad = "Dermatología";
                razon = "tus síntomas están relacionados con la piel";
            }

            var nombre = System.Web.HttpContext.Current?.Session["usuario"]?.ToString()?.Split(' ')[0] ?? "";
            var saludo = string.IsNullOrEmpty(nombre) ? "¡Hola!" : $"¡Hola {nombre}!";

            var mensaje = $@"{saludo} 😊

He analizado tus síntomas y según lo que describes, {razon}.

Por eso te recomiendo agendar una cita con **{especialidad}**. Un especialista podrá evaluarte mejor y darte un diagnóstico preciso.

Contamos con doctores disponibles en esta especialidad. ¿Quieres que te ayude a agendar tu cita? 📅";

            return new RecomendacionConversacionalDto
            {
                Exito = true,
                Mensaje = mensaje,
                Especialidad = especialidad
            };
        }

        private string ExtraerEspecialidadDeRespuesta(string respuesta)
        {
            var especialidades = new[] { "Medicina General", "Pediatría", "Cardiología", "Odontología", "Dermatología", "Ginecología" };
            foreach (var esp in especialidades)
            {
                if (respuesta.Contains(esp))
                    return esp;
            }
            return "Medicina General";
        }
    }



    // ═══════════════════════════════════════════════════════════════════════
    // DTOs
    // ═══════════════════════════════════════════════════════════════════════  

    public class RecomendacionConversacionalDto
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public string Especialidad { get; set; }
    }

    public class RecomendacionEspecialidadDto
    {
        [JsonProperty("especialidad")]
        public string Especialidad { get; set; }
        [JsonProperty("confianza")]
        public string Confianza { get; set; }
        [JsonProperty("razon")]
        public string Razon { get; set; }
    }

    public class RiesgoInasistenciaDto
    {
        [JsonProperty("nivel")]
        public string Nivel { get; set; }        // alto, medio, bajo
        [JsonProperty("porcentaje")]
        public int Porcentaje { get; set; }
        [JsonProperty("mensaje")]
        public string Mensaje { get; set; }
    }

    public class AlertaAdminDto
    {
        [JsonProperty("tipo")]
        public string Tipo { get; set; }         // warning, danger, success, info
        [JsonProperty("icono")]
        public string Icono { get; set; }
        [JsonProperty("titulo")]
        public string Titulo { get; set; }
        [JsonProperty("mensaje")]
        public string Mensaje { get; set; }
    }

    public class MetricasAdminDto
    {
        public int CitasHoy { get; set; }
        public int CanceladasSemana { get; set; }
        public double TasaInasistencia { get; set; }
        public string EspecialidadTop { get; set; }
        public string DoctorMasCargado { get; set; }
        public int CitasDoctorTop { get; set; }
        public int NuevosPacientesSemana { get; set; }
        public int PacientesSinCita { get; set; }
        public int TotalDoctores { get; set; }
    }
}