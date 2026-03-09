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
    }
}