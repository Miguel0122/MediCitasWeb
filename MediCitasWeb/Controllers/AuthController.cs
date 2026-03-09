using System;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using MediCitasWeb.Models;
using System.Linq;
using MediCitasWeb.Services.Security;

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
            return View();
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
        public ActionResult Login(string numero_documento, string password)
        {
            numero_documento = numero_documento?.Trim();
            password = password?.Trim();

            // VALIDACIONES
            if (string.IsNullOrWhiteSpace(numero_documento))
                ModelState.AddModelError("numero_documento", "El número de documento es obligatorio.");
            else if (!numero_documento.All(char.IsDigit))
                ModelState.AddModelError("numero_documento", "Solo números permitidos.");
            else if (numero_documento.Length < 6 || numero_documento.Length > 12)
                ModelState.AddModelError("numero_documento", "Debe tener entre 6 y 12 dígitos.");

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "La contraseña es obligatoria.");

            if (!ModelState.IsValid)
                return View();

            try
            {
                string conexion =
                    ConfigurationManager.ConnectionStrings["MediCitasDB"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conexion))
                {
                    con.Open();

                    string query = @"
                        SELECT id_usuario,
                               nombres_usuario,
                               password_usuario,
                               rol_usuario
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

                            // 🔐 VERIFICACIÓN SEGURA
                            bool passwordCorrecta =
                                PasswordHasher.Verify(password, passwordBD);

                            if (!passwordCorrecta)
                            {
                                ModelState.AddModelError("", "Documento o contraseña incorrectos.");
                                return View();
                            }

                            if (string.IsNullOrEmpty(rolUsuario))
                            {
                                ModelState.AddModelError("", "Usuario sin rol asignado.");
                                return View();
                            }

                            // SESSION
                            Session["usuario"] = nombreUsuario;
                            Session["rol"] = rolUsuario;
                            Session["documento"] = numero_documento;
                            Session["id_usuario"] = idUsuario;
                        }
                    }
                }

                // REDIRECCIÓN POR ROL
                switch (Session["rol"] as string)
                {
                    case "Administrador":
                        return RedirectToAction("PanelAdmin", "Admin");

                    case "Doctor":
                        return RedirectToAction("CitasDoctor", "Doctor");

                    case "Paciente":
                        return RedirectToAction("AgendarCita", "Paciente");

                    default:
                        ModelState.AddModelError("", "Rol no válido.");
                        return View();
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Ocurrió un error inesperado.");
                return View();
            }
        }

        // ===============================
        // REGISTRO POST (CON HASH)
        // ===============================
        [HttpPost]
        public ActionResult Registro(
            string nombres,
            string apellidos,
            string numero_documento,
            string correo,
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
                // 🔐 HASH PASSWORD
                string passwordHash = PasswordHasher.Hash(password);

                Usuario nuevoUsuario = new Usuario
                {
                    nombres_usuario = nombres,
                    apellidos_usuario = apellidos,
                    numero_documento = numero_documento,
                    correo_usuario = correo,
                    password_usuario = passwordHash,
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
                ModelState.AddModelError("", "Error al registrar: " + ex.Message);
                return View();
            }
        }
    }
}