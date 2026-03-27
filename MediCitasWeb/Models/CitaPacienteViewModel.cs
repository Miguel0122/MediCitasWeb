using System;

namespace MediCitasWeb.Models
{
    public class CitaPacienteViewModel
    {
        public int IdCita { get; set; }
        public int IdPaciente { get; set; }          // ← AGREGADO: requerido por PanelDoctor.cshtml (JS riesgos)
        public string NombrePaciente { get; set; }
        public string Documento { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string Especialidad { get; set; }
        public string Estado { get; set; }
        public string Tipo { get; set; }
    }
}
