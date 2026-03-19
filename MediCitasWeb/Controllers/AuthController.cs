using System;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using MediCitasWeb.Models;
using System.Linq;
using MediCitasWeb.Services.Security;
using System.Web.Helpers;

namespace MediCitasWeb.Controllers
{
    public class AuthController : Controller
    {
        private MediCitasContext db = new MediCitasContext();

        // ===============================
        // LOGIN VIEW
        // ===============================
        public ActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // ===============================
        // REGISTRO VIEW
        // ===============================
        public ActionResult Registro()
        {
            return View();
        }

        // ===============================
        // LOGIN POST (SEGURO)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                string conexion = ConfigurationManager.ConnectionStrings["MediCitasDB"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conexion))
                {
                    con.Open();

                    // 1. Agregamos "activo" al SELECT para validar el estado de la cuenta
                    string query = @"
                SELECT id_usuario, 
                       nombres_usuario, 
                       numero_documento, 
                       password_usuario, 
                       rol_usuario,
                       activo
                FROM Usuario 
                WHERE numero_documento = @Documento";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Documento", model.numero_documento);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                ModelState.AddModelError("", "Documento o contraseña incorrectos.");
                                return View(model);
                            }

                            // 2. Validación de Cuenta Activa (Nuevo campo de la BD)
                            bool estaActivo = dr["activo"] != DBNull.Value && Convert.ToBoolean(dr["activo"]);
                            if (!estaActivo)
                            {
                                ModelState.AddModelError("", "Tu cuenta se encuentra desactivada. Contacta al administrador.");
                                return View(model);
                            }

                            string passwordBD = dr["password_usuario"].ToString();
                            string rolUsuario = dr["rol_usuario"]?.ToString();
                            string nombreUsuario = dr["nombres_usuario"].ToString();
                            string idUsuario = dr["id_usuario"].ToString();

                            // 🔐 VERIFICACIÓN SEGURA
                            bool passwordCorrecta;
                            try
                            {
                                passwordCorrecta = PasswordHasher.Verify(model.password, passwordBD);
                            }
                            catch (FormatException)
                            {
                                passwordCorrecta = System.Web.Helpers.Crypto.VerifyHashedPassword(passwordBD, model.password);
                            }

                            if (!passwordCorrecta)
                            {
                                ModelState.AddModelError("", "Documento o contraseña incorrectos.");
                                return View(model);
                            }

                            if (string.IsNullOrEmpty(rolUsuario))
                            {
                                ModelState.AddModelError("", "Usuario sin rol asignado.");
                                return View(model);
                            }

                            // 3. SET DE SESIÓN
                            Session["usuario"] = nombreUsuario;
                            Session["rol"] = rolUsuario;
                            Session["documento"] = dr["numero_documento"].ToString();
                            Session["id_usuario"] = idUsuario;
                        }
                    }
                }

                // 4. REDIRECCIÓN UNIFICADA AL DASHBOARD
                // Ahora todos van al Index de Home, que es donde está tu nuevo Dashboard dinámico.
                return RedirectToAction("Dashboard", "Home");
            }
            catch (Exception ex)
            {
                // Es buena práctica loguear 'ex' en desarrollo
                ModelState.AddModelError("", "Ocurrió un error al intentar iniciar sesión.");
                return View(model);
            }
        }

        // ===============================
        // REGISTRO POST (CON HASH)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registro(RegistroViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // 🔐 HASH PASSWORD
                string passwordHash = PasswordHasher.Hash(model.password);

                Usuario nuevoUsuario = new Usuario
                {
                    nombres_usuario = model.nombres,
                    apellidos_usuario = model.apellidos,
                    numero_documento = model.numero_documento,
                    correo_usuario = model.correo,
                    password_usuario = passwordHash,
                    rol_usuario = "Paciente",
                    activo = true,
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
                ModelState.AddModelError("", "Error al registrar: " + ex.Message);
                return View();
            }
        }
    }
}