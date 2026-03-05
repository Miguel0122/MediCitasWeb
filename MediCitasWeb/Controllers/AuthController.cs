using System;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;

namespace MediCitasWeb.Controllers
{
    public class AuthController : Controller
    {

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
                        // Guardamos el ID de Usuario por si acaso
                        Session["id_usuario"] = dr["id_usuario"].ToString();

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

        [HttpPost]
        public ActionResult Registro(string nombres, string apellidos, string numero_documento, string correo, string password)
        {
            string conexion = ConfigurationManager.ConnectionStrings["MediCitasDB"].ConnectionString; // Ojo: Asegúrate que coincida con Web.config

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();
                // Iniciamos una transacción: o se guardan los dos, o no se guarda ninguno (seguridad de datos)
                SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    // 1. Insertar en Usuario y recuperar el ID generado (SCOPE_IDENTITY)
                    string queryUsuario = @"INSERT INTO Usuario
                            (nombres_usuario, apellidos_usuario, numero_documento, correo_usuario, password_usuario, rol_usuario)
                            VALUES
                            (@Nombres, @Apellidos, @Documento, @Correo, @Password, 'Paciente');
                            SELECT SCOPE_IDENTITY();"; // Esto nos devuelve el ID nuevo

                    int idUsuarioNuevo = 0;

                    using (SqlCommand cmd = new SqlCommand(queryUsuario, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Nombres", nombres);
                        cmd.Parameters.AddWithValue("@Apellidos", apellidos);
                        cmd.Parameters.AddWithValue("@Documento", numero_documento);
                        cmd.Parameters.AddWithValue("@Correo", correo);
                        cmd.Parameters.AddWithValue("@Password", password);

                        // Ejecutamos y obtenemos el ID del usuario recién creado
                        idUsuarioNuevo = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 2. Insertar automáticamente en la tabla Paciente
                    string queryPaciente = "INSERT INTO Paciente (id_usuario) VALUES (@IdUsuario)";
                    using (SqlCommand cmd2 = new SqlCommand(queryPaciente, con, transaction))
                    {
                        cmd2.Parameters.AddWithValue("@IdUsuario", idUsuarioNuevo);
                        cmd2.ExecuteNonQuery();
                    }

                    // Si todo salió bien, confirmamos los cambios
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // Si algo falla, deshacemos todo para no dejar datos basura
                    transaction.Rollback();
                    ViewBag.Error = "Error al registrar: " + ex.Message;
                    return View();
                }
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        public ActionResult Recuperar(string correo)
        {
            ViewBag.Mensaje = "Si el correo existe, se enviará un código de recuperación.";
            return View();
        }
    }
}