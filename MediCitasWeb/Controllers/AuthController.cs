using System;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using MediCitasWeb.Models;
using System.Linq;
// Permite usar clases para enviar correos
using System.Net;
using System.Net.Mail;
namespace MediCitasWeb.Controllers
{
    public class AuthController : Controller
    {

        // ============================================================
        // ==================== VISTAS (GET) ==========================
        // ============================================================

        // Muestra la vista de Login
        public ActionResult Login()
        {
            return View();
        }

        // Muestra la vista de Registro
        public ActionResult Registro()
        {
            return View();
        }

        // Muestra la vista de Recuperar contraseña
        public ActionResult Recuperar()
        {
            return View();
        }

        // ============================================================
        // ==================== LOGIN (POST) ==========================
        // ============================================================

        [HttpPost]
        public ActionResult Login(string numero_documento, string password)
        {
            // ============================================================
            // LIMPIAR ESPACIOS EN BLANCO (evita errores por espacios)
            // ============================================================
            numero_documento = numero_documento?.Trim();
            password = password?.Trim();

            // ============================================================
            // VALIDACIÓN: NÚMERO DE DOCUMENTO
            // ============================================================

            // 1️⃣ Campo obligatorio
            if (string.IsNullOrWhiteSpace(numero_documento))
                ModelState.AddModelError("numero_documento", "El número de documento es obligatorio.");

            // 2️⃣ Solo números (evita letras, puntos, símbolos)
            else if (!numero_documento.All(char.IsDigit))
                ModelState.AddModelError("numero_documento", "El número de documento solo debe contener números.");

            // 3️⃣ Longitud válida (ejemplo entre 6 y 12)
            else if (numero_documento.Length < 6 || numero_documento.Length > 12)
                ModelState.AddModelError("numero_documento", "El número de documento debe tener entre 6 y 12 dígitos.");



            // ============================================================
            // VALIDACIÓN: CONTRASEÑA
            // ============================================================

            // 1️⃣ Campo obligatorio
            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "La contraseña es obligatoria.");

            // 2️⃣ Longitud mínima de seguridad
            else if (password.Length < 6)
                ModelState.AddModelError("password", "La contraseña debe tener mínimo 6 caracteres.");



            // ============================================================
            // SI ALGUNA VALIDACIÓN FALLA → NO CONSULTA LA BD
            // ============================================================

            if (!ModelState.IsValid)
                return View();



            try
            {
                string conexion = ConfigurationManager
                                  .ConnectionStrings["MediCitasDB"]
                                  .ConnectionString;

                using (SqlConnection con = new SqlConnection(conexion))
                {
                    con.Open();

                    // ============================================================
                    // BUSCAR USUARIO POR DOCUMENTO
                    // ============================================================

                    string query = @"SELECT nombres_usuario, password_usuario, rol_usuario
                             FROM Usuario
                             WHERE numero_documento = @Documento";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Documento", numero_documento);

                        SqlDataReader dr = cmd.ExecuteReader();

                        // ============================================================
                        // VALIDAR SI EL USUARIO EXISTE
                        // ============================================================

                        if (!dr.Read())
                        {
                            // Mensaje general por seguridad
                            ModelState.AddModelError("", "Documento o contraseña incorrectos.");
                            return View();
                        }

                        string passwordBD = dr["password_usuario"].ToString();
                        string rolUsuario = dr["rol_usuario"]?.ToString();
                        string nombreUsuario = dr["nombres_usuario"].ToString();



                        // ============================================================
                        // VALIDAR CONTRASEÑA
                        // ============================================================

                        if (passwordBD != password)
                        {
                            // Mensaje general por seguridad
                            ModelState.AddModelError("", "Documento o contraseña incorrectos.");
                            return View();
                        }



                        // ============================================================
                        // VALIDAR QUE EL ROL EXISTA
                        // ============================================================

                        if (string.IsNullOrEmpty(rolUsuario))
                        {
                            ModelState.AddModelError("", "El usuario no tiene un rol asignado.");
                            return View();
                        }



                        // ============================================================
                        // CREAR SESIÓN
                        // ============================================================

                        Session["usuario"] = nombreUsuario;
                        Session["rol"] = rolUsuario;
                        Session["documento"] = numero_documento;



                        // ============================================================
                        // REDIRECCIÓN SEGÚN ROL
                        // ============================================================

                        switch (rolUsuario)
                        {
                            case "Administrador":
                                return RedirectToAction("Index", "Admin");

                            case "Doctor":
                                return RedirectToAction("MisCitas", "Doctor");

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
                // No mostramos detalles técnicos por seguridad
                ModelState.AddModelError("", "Ocurrió un error inesperado. Intente nuevamente.");
                return View();
            }
        }
        [HttpPost]
        public ActionResult Registro(string nombres,
                             string apellidos,
                             string numero_documento,
                             string correo,
                             string password,
                             string rol)
        {
            // =====================================================
            // VALIDACIONES DE CAMPOS
            // =====================================================

            // -------------------------
            // VALIDAR NOMBRES
            // -------------------------
            // Verifica que no esté vacío
            if (string.IsNullOrWhiteSpace(nombres))
                ModelState.AddModelError("nombres", "El nombre es obligatorio.");

            // Verifica que solo contenga letras y espacios
            // char.IsLetter → solo letras
            // char.IsWhiteSpace → permite espacios
            else if (!nombres.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
                ModelState.AddModelError("nombres", "El nombre solo debe contener letras.");


            // -------------------------
            // VALIDAR APELLIDOS
            // -------------------------
            if (string.IsNullOrWhiteSpace(apellidos))
                ModelState.AddModelError("apellidos", "Los apellidos son obligatorios.");

            else if (!apellidos.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
                ModelState.AddModelError("apellidos", "Los apellidos solo deben contener letras.");


            // -------------------------
            // VALIDAR DOCUMENTO
            // -------------------------
            // Verifica que no esté vacío
            if (string.IsNullOrWhiteSpace(numero_documento))
                ModelState.AddModelError("numero_documento", "El documento es obligatorio.");

            // Verifica que solo tenga números
            // char.IsDigit → solo permite 0-9
            else if (!numero_documento.All(char.IsDigit))
                ModelState.AddModelError("numero_documento", "El documento solo debe contener números.");


            // -------------------------
            // VALIDAR CORREO
            // -------------------------
            if (string.IsNullOrWhiteSpace(correo))
                ModelState.AddModelError("correo", "El correo es obligatorio.");

            // Validación básica de formato
            // Debe contener @ y .
            else if (!correo.Contains("@") || !correo.Contains("."))
                ModelState.AddModelError("correo", "Debe ingresar un correo válido.");


            // -------------------------
            // VALIDAR CONTRASEÑA
            // -------------------------
            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "La contraseña es obligatoria.");

            // Mínimo 6 caracteres
            else if (password.Length < 6)
                ModelState.AddModelError("password", "La contraseña debe tener mínimo 6 caracteres.");


            // -------------------------
            // VALIDAR ROL
            // -------------------------
            if (string.IsNullOrWhiteSpace(rol))
                ModelState.AddModelError("rol", "Debe seleccionar un rol.");


            // =====================================================
            // SI HAY ERRORES DE VALIDACIÓN
            // =====================================================
            // ModelState.IsValid verifica si hay errores agregados arriba.
            // Si hay errores → regresa a la vista y muestra los mensajes.
            if (!ModelState.IsValid)
                return View();


            try
            {
                // Obtener cadena de conexión desde Web.config
                string conexion = ConfigurationManager
                                  .ConnectionStrings["MediCitasDB"]
                                  .ConnectionString;

                // Crear conexión a la base de datos
                using (SqlConnection con = new SqlConnection(conexion))
                {
                    con.Open();


                    // =====================================================
                    // VALIDAR SI EL USUARIO YA EXISTE
                    // =====================================================
                    // Evita duplicar documento o correo
                    string checkQuery = @"SELECT COUNT(*) FROM Usuario
                                  WHERE numero_documento = @Documento
                                  OR correo_usuario = @Correo";

                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        // Enviar parámetros seguros
                        checkCmd.Parameters.AddWithValue("@Documento", numero_documento);
                        checkCmd.Parameters.AddWithValue("@Correo", correo);

                        // ExecuteScalar devuelve el número de registros encontrados
                        int existe = (int)checkCmd.ExecuteScalar();

                        // Si ya existe, mostrar error
                        if (existe > 0)
                        {
                            ModelState.AddModelError("", "El documento o correo ya están registrados.");
                            return View();
                        }
                    }


                    // =====================================================
                    // INSERTAR USUARIO EN LA BASE DE DATOS
                    // =====================================================
                    string insertQuery = @"INSERT INTO Usuario
                (nombres_usuario, apellidos_usuario, numero_documento, correo_usuario, password_usuario, rol_usuario)
                VALUES
                (@Nombres, @Apellidos, @Documento, @Correo, @Password, @Rol)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                    {
                        // Enviar valores al INSERT
                        cmd.Parameters.AddWithValue("@Nombres", nombres);
                        cmd.Parameters.AddWithValue("@Apellidos", apellidos);
                        cmd.Parameters.AddWithValue("@Documento", numero_documento);
                        cmd.Parameters.AddWithValue("@Correo", correo);
                        cmd.Parameters.AddWithValue("@Password", password);
                        cmd.Parameters.AddWithValue("@Rol", rol);

                        // Ejecutar INSERT
                        cmd.ExecuteNonQuery();
                    }
                }

                // Mensaje temporal de éxito
                TempData["Exito"] = "Usuario registrado correctamente.";

                // Redirigir al Login
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // Si ocurre un error del sistema (ej: base caída)
                ModelState.AddModelError("", "Error del sistema: " + ex.Message);
                return View();
            }
        }



        // ------------------------------------------------------------
        // PASO 1: Verificar si el correo existe y generar código
        // ------------------------------------------------------------
        // =====================================================
        // MÉTODO PARA ENVIAR CÓDIGO DE RECUPERACIÓN POR CORREO
        // =====================================================
        [HttpPost] // Indica que este método recibe datos vía POST (AJAX)
        public JsonResult EnviarCodigo(string correo)
        {
            // Crear instancia del contexto de base de datos
            using (var db = new MediCitasContext())
            {
                // =====================================================
                // 1️⃣ BUSCAR USUARIO POR CORREO
                // =====================================================
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

                    // =====================================================
                    // 6️⃣ CONFIGURAR SERVIDOR SMTP (GMAIL)
                    // =====================================================
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


        // ------------------------------------------------------------
        // PASO 2: Validar que el código ingresado sea correcto
        // ------------------------------------------------------------
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
        // ------------------------------------------------------------
        // PASO 3: Cambiar contraseña en la base de datos
        // ------------------------------------------------------------
        [HttpPost]
        public JsonResult CambiarPassword(string nueva)
        {
            // =====================================================
            // OBTENER CORREO GUARDADO EN SESSION
            // =====================================================
            // Se recupera el correo del usuario que está
            // realizando el proceso de recuperación.
            string correo = Session["CorreoRecuperacion"] as string;

            // =====================================================
            // VALIDAR QUE LA SESSION NO HAYA EXPIRADO
            // =====================================================
            // Si no existe el correo en sesión,
            // significa que el proceso expiró.
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
                // =====================================================
                // BUSCAR USUARIO POR CORREO
                // =====================================================
                var usuario = db.Usuario
                                .FirstOrDefault(u => u.correo_usuario == correo);

                // Si por alguna razón no existe el usuario
                if (usuario == null)
                {
                    return Json(new { ok = false });
                }

                // =====================================================
                // ACTUALIZAR CONTRASEÑA
                // =====================================================
                // Se reemplaza la contraseña antigua por la nueva.
                usuario.password_usuario = nueva;

                // Guardar cambios en la base de datos
                db.SaveChanges();
            }

            // =====================================================
            // LIMPIAR DATOS DE SESSION
            // =====================================================
            // Por seguridad se eliminan los datos temporales.
            Session.Remove("CodigoRecuperacion");
            Session.Remove("CorreoRecuperacion");

            // Respuesta exitosa
            return Json(new { ok = true });
        }
    }
}