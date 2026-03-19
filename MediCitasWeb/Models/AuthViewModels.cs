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
        [Required]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Solo letras y espacios.")]
        public string nombres { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Solo letras y espacios.")]
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
    }

    public class CrearDoctorViewModel
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

        [Required]
        [Display(Name = "Especialidad")]
        public string especialidad { get; set; }
    }
}

