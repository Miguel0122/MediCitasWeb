using System;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using MediCitasWeb.Models;
using System.Linq;
using System.Net;
using System.Net.Mail;
namespace MediCitasWeb.Controllers
{
    public class AuthController : Controller
    {
        private MediCitasContext db = new MediCitasContext();

        // 3. Login corregido
        public ActionResult Login()
        {
            return View();
        }

        // 1. Muestra el formulario de registro (GET)
        public ActionResult Registro()
        {
            return View();
        }

        // Muestra la vista de Recuperar contraseña
        public ActionResult Recuperar()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string numero_documento, string password)
        {
            numero_documento = numero_documento?.Trim();
            password = password?.Trim();

            if (string.IsNullOrWhiteSpace(numero_documento))
                ModelState.AddModelError("numero_documento", "El número de documento es obligatorio.");
            else if (!numero_documento.All(char.IsDigit))
                ModelState.AddModelError("numero_documento", "El número de documento solo debe contener números.");
            else if (numero_documento.Length < 6 || numero_documento.Length > 12)
                ModelState.AddModelError("numero_documento", "El número de documento debe tener entre 6 y 12 dígitos.");

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "La contraseña es obligatoria.");
            else if (password.Length < 6)
                ModelState.AddModelError("password", "La contraseña debe tener mínimo 6 caracteres.");

            if (!ModelState.IsValid)
                return View();

            try
            {
                string conexion = ConfigurationManager
                    .ConnectionStrings["MediCitasDB"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conexion))
                {
                    con.Open();

                    string query = @"SELECT id_usuario, nombres_usuario, password_usuario, rol_usuario
                             FROM Usuario
                             WHERE numero_documento = @Documento";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Documento", numero_documento);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                ModelState.AddModelError("", "Documento o contraseña incorrectos.");
                                return View();
                            }

                            string passwordBD = dr["password_usuario"].ToString();
                            string rolUsuario = dr["rol_usuario"]?.ToString();
                            string nombreUsuario = dr["nombres_usuario"].ToString();
                            string idUsuario = dr["id_usuario"].ToString();

                            if (passwordBD != password)
                            {
                                ModelState.AddModelError("", "Documento o contraseña incorrectos.");
                                return View();
                            }

                            if (string.IsNullOrEmpty(rolUsuario))
                            {
                                ModelState.AddModelError("", "El usuario no tiene un rol asignado.");
                                return View();
                            }

                            Session["usuario"] = nombreUsuario;
                            Session["rol"] = rolUsuario;
                            Session["documento"] = numero_documento;
                            Session["id_usuario"] = idUsuario;
                        }

                        switch (Session["rol"] as string)
                        {
                            case "Administrador":
                                return RedirectToAction("PanelAdmin", "Admin");
                            case "Doctor":
                                return RedirectToAction("CitasDoctor", "Doctor");
                            case "Paciente":
                                return RedirectToAction("AgendarCita", "Pages");
                            default:
                                ModelState.AddModelError("", "Rol no válido.");
                                return View();
                        }
                    }
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Ocurrió un error inesperado. Intente nuevamente.");
                return View();
            }
        }

        // He unificado los parámetros para que coincidan con tu base de datos
        [HttpPost]
        public ActionResult Registro(string nombres, string apellidos,
                             string numero_documento, string correo,
                             string password)
        {
            if (string.IsNullOrWhiteSpace(nombres) ||
                string.IsNullOrWhiteSpace(apellidos) ||
                string.IsNullOrWhiteSpace(numero_documento) ||
                string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Todos los campos son obligatorios.");
                return View();
            }

            try
            {
                Usuario nuevoUsuario = new Usuario
                {
                    nombres_usuario = nombres,
                    apellidos_usuario = apellidos,
                    numero_documento = numero_documento,
                    correo_usuario = correo,
                    password_usuario = password,
                    rol_usuario = "Paciente",
                    fecha_registro = DateTime.Now
                };

                db.Usuario.Add(nuevoUsuario);
                db.SaveChanges();

                Paciente nuevoPaciente = new Paciente
                {
                    id_usuario = nuevoUsuario.id_usuario
                };
                db.Paciente.Add(nuevoPaciente);
                db.SaveChanges();

                TempData["Exito"] = "Usuario registrado correctamente.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // Muestra el mensaje concreto mientras pruebas
                ModelState.AddModelError("", "Error al registrar: " + ex.Message);
                return View();
            }
        }

        [HttpPost] // Indica que este método recibe datos vía POST (AJAX)
        public JsonResult EnviarCodigo(string correo)
        {
            // Crear instancia del contexto de base de datos
            using (var db = new MediCitasContext())
            {

                var usuario = db.Usuario
                                .FirstOrDefault(u => u.correo_usuario == correo);

                // Si el correo no existe en la base de datos
                if (usuario == null)
                {
                    return Json(new
                    {
                        ok = false,
                        mensaje = "El correo no está registrado."
                    });
                }

                // =====================================================
                // 2️⃣ GENERAR CÓDIGO ALEATORIO DE 6 DÍGITOS
                // =====================================================
                Random rnd = new Random();
                string codigo = rnd.Next(100000, 999999).ToString();

                // =====================================================
                // 3️⃣ GUARDAR CÓDIGO Y CORREO EN SESSION
                // =====================================================
                // Se guardan temporalmente para validarlo después
                Session["CodigoRecuperacion"] = codigo;
                Session["CorreoRecuperacion"] = correo;

                try
                {
                    // =====================================================
                    // 4️⃣ CREAR Y CONFIGURAR MENSAJE DE CORREO
                    // =====================================================
                    MailMessage mensaje = new MailMessage();

                    // Correo desde el cual se enviará el mensaje
                    mensaje.From = new MailAddress("estoesparaia73@gmail.com");

                    // Correo destino (usuario que solicitó recuperación)
                    mensaje.To.Add(correo);

                    // Asunto del correo
                    mensaje.Subject = "Código de recuperación - MediCitas";

                    // =====================================================
                    // IMPORTANTE: INDICAR QUE EL CUERPO SERÁ HTML
                    // =====================================================
                    mensaje.IsBodyHtml = true;

                    // =====================================================
                    // 5️⃣ CUERPO DEL CORREO CON DISEÑO HTML
                    // =====================================================
                    // Se usa $@ para poder insertar la variable {codigo}
                    mensaje.Body = $@"
                                    <!DOCTYPE html>
                                    <html>
                                    <head>
                                    <meta charset='UTF-8'>
                                    </head>

                                    <body style='margin:0; padding:0; background-color:#e3f2fd; font-family:Arial, sans-serif;'>

                                        <!-- Contenedor con fondo degradado -->
                                        <div style='padding:50px 0; background:linear-gradient(135deg,#4fc3f7,#81d4fa);'>

                                            <!-- Tarjeta blanca central -->
                                            <div style='max-width:500px; margin:auto; background:white; 
                                                        border-radius:15px; 
                                                        box-shadow:0 10px 30px rgba(0,0,0,0.2); 
                                                        padding:40px; 
                                                        text-align:center;'>

                                                <!-- Título -->
                                                <h2 style='color:#0277bd; margin-bottom:25px;'>
                                                    Recuperación de Contraseña - MediCitas
                                                </h2>

                                                <!-- Mensaje informativo -->
                                                <p style='font-size:16px; color:#555; margin-bottom:30px; line-height:1.6;'>
                                                    Hola,<br><br>
                                                    Recibimos una solicitud para restablecer tu contraseña en 
                                                    <b>MediCitas</b>.
                                                </p>

                                                <!-- Código destacado -->
                                                <div style='background:#e1f5fe; 
                                                            padding:20px; 
                                                            border-radius:10px; 
                                                            font-size:32px; 
                                                            font-weight:bold; 
                                                            letter-spacing:6px; 
                                                            color:#01579b; 
                                                            margin-bottom:30px;'>

                                                    {codigo}

                                                </div>

                                                <!-- Mensaje final -->
                                                <p style='font-size:14px; color:#777; line-height:1.5;'>
                                                    Este código es válido por unos minutos.<br>
                                                    Si no solicitaste este cambio, puedes ignorar este mensaje.
                                                </p>

                                            </div>

                                        </div>

                                    </body>
                                    </html>";

                    SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);

                    // Credenciales del correo emisor (contraseña de aplicación)
                    smtp.Credentials = new NetworkCredential(
                        "estoesparaia73@gmail.com",
                        "rlwccoeikalgjwbi" // SIN espacios ni saltos de línea
                    );

                    // Activar conexión segura SSL
                    smtp.EnableSsl = true;

                    // =====================================================
                    // 7️⃣ ENVIAR CORREO
                    // =====================================================
                    smtp.Send(mensaje);

                    // =====================================================
                    // RESPUESTA EXITOSA
                    // =====================================================
                    return Json(new
                    {
                        ok = true,
                        mensaje = "Código enviado correctamente al correo."
                    });
                }
                catch (Exception ex)
                {
                    // =====================================================
                    // MANEJO DE ERRORES
                    // =====================================================
                    return Json(new
                    {
                        ok = false,
                        mensaje = "Error al enviar el correo: " + ex.Message
                    });
                }
            }
        }

        [HttpPost]
        public JsonResult ValidarCodigo(string codigo)
        {
            // Obtener código guardado en sesión
            string codigoSession = Session["CodigoRecuperacion"] as string;

            // Validar que no sea null
            if (string.IsNullOrEmpty(codigoSession))
            {
                return Json(new
                {
                    ok = false,
                    mensaje = "La sesión expiró. Solicita un nuevo código."
                });
            }

            // Limpiar espacios por seguridad
            codigo = codigo?.Trim();
            codigoSession = codigoSession?.Trim();

            // Comparar códigos
            if (codigoSession.Equals(codigo))
            {
                return Json(new { ok = true });
            }

            return Json(new
            {
                ok = false,
                mensaje = "Código incorrecto."
            });
        }

        [HttpPost]
        public JsonResult CambiarPassword(string nueva)
        {

            string correo = Session["CorreoRecuperacion"] as string;

            if (correo == null)
            {
                return Json(new
                {
                    ok = false,
                    mensaje = "Sesión expirada. Intente nuevamente."
                });
            }

            // Crear conexión con base de datos
            using (var db = new MediCitasContext())
            {
                var usuario = db.Usuario
                                .FirstOrDefault(u => u.correo_usuario == correo);

                // Si por alguna razón no existe el usuario
                if (usuario == null)
                {
                    return Json(new { ok = false });
                }

                usuario.password_usuario = nueva;

                // Guardar cambios en la base de datos
                db.SaveChanges();
            }

            Session.Remove("CodigoRecuperacion");
            Session.Remove("CorreoRecuperacion");

            // Respuesta exitosa
            return Json(new { ok = true });
        }
    }
}