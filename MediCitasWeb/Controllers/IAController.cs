using MediCitasWeb.Filters;
using MediCitasWeb.Models;
using MediCitasWeb.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    [SessionAuthorize]
    public class IAController : Controller
    {
        private readonly IAService _ia = new IAService();
        private MediCitasContext db = new MediCitasContext();

        // ── 1. RECOMENDAR ESPECIALIDAD ───────────────────────────────────
        [HttpPost]
        public async Task<JsonResult> RecomendarEspecialidad(string sintomas)
        {
            if (string.IsNullOrWhiteSpace(sintomas))
                return Json(new { success = false, message = "Describe tus síntomas para poder ayudarte." });

            var rec = await _ia.RecomendarEspecialidadConversacional(sintomas);

            if (rec == null || !rec.Exito)
            {
                return Json(new
                {
                    success = true,
                    recomendacion = new
                    {
                        especialidad = "Medicina General",
                        mensaje = "No pude analizar tus síntomas en este momento. Te recomiendo agendar una cita con Medicina General para una evaluación inicial. ¿Quieres que te ayude a agendar?",
                        confianza = "media"
                    }
                });
            }

            return Json(new
            {
                success = true,
                recomendacion = new
                {
                    especialidad = rec.Especialidad,
                    mensaje = rec.Mensaje,
                    confianza = "alta"
                }
            });
        }

        // ── 2. RIESGOS BATCH (para la tabla del doctor) ──────────────────
        [HttpPost]
        public async Task<JsonResult> RiesgosBatch(List<CitaRiesgoRequest> items)
        {
            if (items == null || !items.Any())
                return Json(new { success = false });

            var resultados = new List<object>();

            foreach (var item in items)
            {
                try
                {
                    var paciente = db.Paciente.FirstOrDefault(p => p.id_paciente == item.PacienteId);
                    if (paciente == null) continue;

                    var usuario = db.Usuario.FirstOrDefault(u => u.id_usuario == paciente.id_usuario);
                    var todasCitas = db.Citas.Where(c => c.id_paciente == item.PacienteId).ToList();

                    int total = todasCitas.Count;
                    int inasistencias = todasCitas.Count(c => c.estado == "Inasistencia");
                    int cancelaciones = todasCitas.Count(c => c.estado == "Cancelada");

                    // Solo analizar con IA si tiene historial suficiente
                    if (total < 2)
                    {
                        resultados.Add(new
                        {
                            citaId = item.CitaId,
                            riesgo = new { nivel = "bajo", porcentaje = 5, mensaje = "Sin historial suficiente" }
                        });
                        continue;
                    }

                    var citaActual = db.Citas.Find(item.CitaId);
                    int diasHasta = citaActual != null
                        ? Math.Max(0, (int)(citaActual.fecha_cita - DateTime.Today).TotalDays)
                        : 1;

                    string nombre = usuario != null
                        ? $"{usuario.nombres_usuario} {usuario.apellidos_usuario}"
                        : "Paciente";

                    var riesgo = await _ia.PredecirRiesgoInasistencia(
                        nombre, total, inasistencias, cancelaciones, diasHasta);

                    resultados.Add(new { citaId = item.CitaId, riesgo });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[IAController] RiesgosBatch error: {ex.Message}");
                }
            }

            return Json(new { success = true, riesgos = resultados });
        }

        // ── 3. RESUMEN DE PACIENTE ───────────────────────────────────────

        [HttpGet]
        public async Task<JsonResult> ResumenPaciente(int pacienteId)
        {
            try
            {
                var paciente = db.Paciente.FirstOrDefault(p => p.id_paciente == pacienteId);
                if (paciente == null)
                    return Json(new { success = false, message = "Paciente no encontrado" }, JsonRequestBehavior.AllowGet);

                var usuario = db.Usuario.FirstOrDefault(u => u.id_usuario == paciente.id_usuario);

                // CORRECCIÓN: Filtrar solo citas con fecha <= hoy
                var citas = db.Citas
                    .Where(c => c.id_paciente == pacienteId && c.fecha_cita <= DateTime.Today)
                    .OrderByDescending(c => c.fecha_cita)
                    .ToList();

                string nombre = usuario != null
                    ? $"{usuario.nombres_usuario} {usuario.apellidos_usuario}"
                    : "Paciente";
                int total = citas.Count;
                int inasistencias = citas.Count(c => c.estado == "Inasistencia");
                int cancelaciones = citas.Count(c => c.estado == "Cancelada");
                var especialidades = citas.Select(c => c.especialidad).ToList();

                // CORRECCIÓN: Última cita real (no futura)
                var ultimaCitaReal = citas.FirstOrDefault();
                string ultimaCita = ultimaCitaReal != null
                    ? ultimaCitaReal.fecha_cita.ToString("dd/MM/yyyy")
                    : "sin registro";

                string resumen = await _ia.ResumirHistorialPaciente(
                    nombre, total, especialidades, ultimaCita, inasistencias, cancelaciones);

                return Json(new { success = true, resumen }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ── 4. ALERTAS PARA EL ADMIN ─────────────────────────────────────
        [HttpGet]
        public async Task<JsonResult> AlertasAdmin()
        {
            try
            {
                var hoy = DateTime.Today;
                var semanaAnt = hoy.AddDays(-7);

                // Doctor con más citas hoy
                var doctorTop = db.Citas
                    .Where(c => c.fecha_cita == hoy && c.estado == "Activa")
                    .GroupBy(c => c.id_doctor)
                    .Select(g => new { id = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .FirstOrDefault();

                string doctorNombre = "N/A";
                int doctorCitas = 0;
                if (doctorTop != null)
                {
                    doctorCitas = doctorTop.count;
                    var docUsuario = db.Doctor
                        .Where(d => d.id_doctor == doctorTop.id)
                        .Join(db.Usuario, d => d.id_usuario, u => u.id_usuario,
                              (d, u) => u.nombres_usuario + " " + u.apellidos_usuario)
                        .FirstOrDefault();
                    doctorNombre = docUsuario ?? "N/A";
                }

                // Especialidad top
                var espTop = db.Citas
                    .Where(c => c.fecha_cita >= semanaAnt)
                    .GroupBy(c => c.especialidad)
                    .Select(g => new { esp = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .FirstOrDefault();

                // Tasa de inasistencia
                var totalCitas = db.Citas.Count(c => c.fecha_cita >= semanaAnt);
                var inasistencias = db.Citas.Count(c => c.fecha_cita >= semanaAnt && c.estado == "Inasistencia");
                double tasa = totalCitas > 0 ? Math.Round((double)inasistencias / totalCitas * 100, 1) : 0;

                // Pacientes sin cita en 30 días
                var hace30 = hoy.AddDays(-30);
                var pacientesIds = db.Citas.Where(c => c.fecha_cita >= hace30).Select(c => c.id_paciente).Distinct();
                int sinCita = db.Paciente.Count(p => !pacientesIds.Contains(p.id_paciente));

                var metricas = new MetricasAdminDto
                {
                    CitasHoy = db.Citas.Count(c => c.fecha_cita == hoy && c.estado == "Activa"),
                    CanceladasSemana = db.Citas.Count(c => c.fecha_cita >= semanaAnt && c.estado == "Cancelada"),
                    TasaInasistencia = tasa,
                    EspecialidadTop = espTop?.esp ?? "N/A",
                    DoctorMasCargado = doctorNombre,
                    CitasDoctorTop = doctorCitas,
                    NuevosPacientesSemana = db.Usuario.Count(u => u.fecha_registro >= semanaAnt && u.rol_usuario == "Paciente"),
                    PacientesSinCita = sinCita,
                    TotalDoctores = db.Doctor.Count()
                };

                var alertas = await _ia.GenerarAlertasAdmin(metricas);
                return Json(new { success = true, alertas }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IAController] AlertasAdmin: {ex.Message}");
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        // En IAController.cs
        [HttpGet]
        public JsonResult ObtenerHorasOcupadas(int idDoctor, string fecha)
        {
            try
            {
                var fechaCita = DateTime.Parse(fecha);
                var horasOcupadas = db.Citas
                    .Where(c => c.id_doctor == idDoctor &&
                                c.fecha_cita == fechaCita &&
                                c.estado == "Activa")
                    .Select(c => c.hora_cita.ToString(@"hh\:mm"))
                    .ToList();

                return Json(new { success = true, horasOcupadas }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> GenerarInsightsReporte(string tipo, List<object> datos, List<object> kpis, int totalRegistros)
        {
            try
            {
                string systemPrompt = @"Eres un analista de datos experto. Genera un resumen ejecutivo breve (máximo 3 oraciones) sobre los datos del reporte.
        Responde en español, de forma profesional pero accesible. Destaca los hallazgos más importantes y ofrece una recomendación.
        No uses emojis excesivos. Sé conciso.";

                string userPrompt = $@"Tipo de reporte: {tipo}
        Total de registros: {totalRegistros}
        KPIs principales: {JsonConvert.SerializeObject(kpis)}
        Muestra de datos (primeros 5): {JsonConvert.SerializeObject(datos?.Take(5))}";

                var insights = await _ia.LlamarGroq(systemPrompt, userPrompt, 0.5);

                if (string.IsNullOrEmpty(insights))
                {
                    insights = $"📊 Se encontraron {totalRegistros} registros en el reporte de {tipo}. Revise los KPIs para más detalles.";
                }

                return Json(new { success = true, insights });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    public class CitaRiesgoRequest
    {
        public int CitaId { get; set; }
        public int PacienteId { get; set; }
    }
}