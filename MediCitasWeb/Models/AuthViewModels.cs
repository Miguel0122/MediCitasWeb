using System.ComponentModel.DataAnnotations;

namespace MediCitasWeb.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "Solo números permitidos.")]
        [StringLength(12, MinimumLength = 6, ErrorMessage = "Debe tener entre 6 y 12 dígitos.")]
        [Display(Name = "Número de documento")]
        public string numero_documento { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string password { get; set; }
    }

    public class RegistroViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "Nombres")]
        public string nombres { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Apellidos")]
        public string apellidos { get; set; }

        [Required]
        [RegularExpression("^[0-9]+$", ErrorMessage = "Solo números permitidos.")]
        [StringLength(20, MinimumLength = 6)]
        [Display(Name = "Número de documento")]
        public string numero_documento { get; set; }

        [Required, EmailAddress]
        [Display(Name = "Correo electrónico")]
        public string correo { get; set; }

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string password { get; set; }

        // NUEVO: Campo teléfono para registro de pacientes
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [StringLength(15, MinimumLength = 7, ErrorMessage = "El teléfono debe tener entre 7 y 15 dígitos")]
        [Display(Name = "Teléfono")]
        public string telefono { get; set; }
    }

    public class CrearDoctorViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres")]
        [Display(Name = "Nombres")]
        public string nombres { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 50 caracteres")]
        [Display(Name = "Apellidos")]
        public string apellidos { get; set; }

        [Required(ErrorMessage = "El número de documento es obligatorio")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "Solo números permitidos.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "El documento debe tener entre 6 y 20 dígitos")]
        [Display(Name = "Número de documento")]
        public string numero_documento { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [Display(Name = "Correo electrónico")]
        public string correo { get; set; }

        [Required(ErrorMessage = "La especialidad es obligatoria")]
        [Display(Name = "Especialidad")]
        public string especialidad { get; set; }

        // NUEVO: Campo teléfono para doctores (NO requerido porque en BD es NULL)
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [StringLength(15, MinimumLength = 7, ErrorMessage = "El teléfono debe tener entre 7 y 15 dígitos")]
        [Display(Name = "Teléfono")]
        public string telefono { get; set; }

        // NUEVO: Campo para contraseña (ahora es opcional porque tiene default)
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [Display(Name = "Contraseña")]
        public string password { get; set; }

        // NUEVO: Campo para estado activo (por defecto true)
        public bool activo { get; set; } = true;
    }

    public class ActualizarUsuarioViewModel
    {
        public int id { get; set; }

        [Required, StringLength(50)]
        public string nombres { get; set; }

        [Required, StringLength(50)]
        public string apellidos { get; set; }

        [Required, EmailAddress]
        public string correo { get; set; }

        [Phone]
        public string telefono { get; set; }

        public string especialidad { get; set; } // Solo para doctores
    }
}

