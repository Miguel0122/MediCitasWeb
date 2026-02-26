using System;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;

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

        /*
            Este método ahora:
            ✔ Valida por numero_documento
            ✔ Valida contraseña
            ✔ Valida rol
            ✔ Guarda sesión
            ✔ Redirige según rol
        */
        [HttpPost]
        public ActionResult Login(string numero_documento, string password, string rol)
        {
            string conexion = ConfigurationManager
                              .ConnectionStrings["MediCitasDB"]
                              .ConnectionString;

            using (SqlConnection con = new SqlConnection(conexion))
            {
                string query = @"SELECT nombres_usuario, rol_usuario
                         FROM Usuario
                         WHERE numero_documento = @Documento
                         AND password_usuario = @Password
                         AND rol_usuario = @Rol";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Documento", numero_documento);
                    cmd.Parameters.AddWithValue("@Password", password);
                    cmd.Parameters.AddWithValue("@Rol", rol);

                    con.Open();

                    SqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        Session["usuario"] = dr["nombres_usuario"].ToString();
                        Session["rol"] = dr["rol_usuario"].ToString();
                        Session["documento"] = numero_documento;

                        string rolUsuario = dr["rol_usuario"].ToString();

                        if (rolUsuario == "Administrador")
                            return RedirectToAction("Index", "Admin");

                        if (rolUsuario == "Doctor")
                            return RedirectToAction("MisCitas", "Doctor");

                        if (rolUsuario == "Paciente")
                            return RedirectToAction("AgendarCita", "Pages"); 
                    }
                }
            }

            ViewBag.Error = "Documento, contraseña o rol incorrectos.";
            return View();
        }



        // ============================================================
        // ==================== REGISTRO (POST) =======================
        // ============================================================

        /*
            Registro ahora:
            ✔ Guarda numero_documento
            ✔ Guarda nombres
            ✔ Guarda apellidos
            ✔ Guarda correo
            ✔ Guarda contraseña
            ✔ Rol por defecto: Paciente
        */
        [HttpPost]
        public ActionResult Registro(string nombres, string apellidos, string numero_documento, string correo, string password)
        {
            string conexion = ConfigurationManager
                              .ConnectionStrings["conexion"]
                              .ConnectionString;

            using (SqlConnection con = new SqlConnection(conexion))
            {
                /*
                    INSERT completo incluyendo numero_documento
                */
                string query = @"INSERT INTO Usuario
                                (nombres_usuario, apellidos_usuario, numero_documento, correo_usuario, password_usuario, rol_usuario)
                                VALUES
                                (@Nombres, @Apellidos, @Documento, @Correo, @Password, @Rol)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Nombres", nombres);
                    cmd.Parameters.AddWithValue("@Apellidos", apellidos);
                    cmd.Parameters.AddWithValue("@Documento", numero_documento);
                    cmd.Parameters.AddWithValue("@Correo", correo);
                    cmd.Parameters.AddWithValue("@Password", password);

                    // Rol automático
                    cmd.Parameters.AddWithValue("@Rol", "Paciente");

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // Redirige al login después de registrarse
            return RedirectToAction("Login");
        }



        // ============================================================
        // ================= RECUPERAR CONTRASEÑA =====================
        // ============================================================

        /*
            Por ahora solo muestra mensaje.
            Luego puedes agregar código de recuperación.
        */
        [HttpPost]
        public ActionResult Recuperar(string correo)
        {
            ViewBag.Mensaje = "Si el correo existe, se enviará un código de recuperación.";
            return View();
        }
    }
}