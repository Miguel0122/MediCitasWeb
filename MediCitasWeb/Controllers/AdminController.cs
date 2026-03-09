using MediCitasWeb.Models;
using MediCitasWeb.Services.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    public class AdminController : Controller
    {
        MediCitasContext db = new MediCitasContext();

        public ActionResult PanelAdmin()
        {
            if (Session["usuario"] == null) return RedirectToAction("Login", "Auth");
            if (Session["rol"] as string != "Administrador") return RedirectToAction("Login", "Auth");

            ViewBag.TotalCitas = db.Citas.Count();
            ViewBag.CitasPendientes = db.Citas.Count(c => c.estado == "Activa");
            ViewBag.CitasCompletadas = db.Citas.Count(c => c.estado == "Completada");
            ViewBag.CitasCanceladas = db.Citas.Count(c => c.estado == "Cancelada");
            ViewBag.TotalPacientes = db.Paciente.Count();

            var usuarios = db.Usuario.ToList();
            return View(usuarios);
        }


        // ==============================
        // CREAR DOCTOR - Formulario
        // ==============================
        public ActionResult CrearDoctor()
        {
            if (Session["rol"] as string != "Administrador")
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // ==============================
        // CREAR DOCTOR - Guardar
        // ==============================
        [HttpPost]
        public ActionResult CrearDoctor(string nombres, string apellidos,
                                        string numero_documento, string correo,
                                        string password, string especialidad)
        {
            if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(especialidad))
            {
                ModelState.AddModelError("", "Todos los campos son obligatorios.");
                return View();
            }

            try
            {
                // 1. Crear el usuario con rol Doctor
                Usuario nuevoUsuario = new Usuario
                {
                    nombres_usuario = nombres,
                    apellidos_usuario = apellidos,
                    numero_documento = numero_documento,
                    correo_usuario = correo,
                    password_usuario = PasswordHasher.Hash(password),
                    rol_usuario = "Doctor",
                    fecha_registro = DateTime.Now
                };

                db.Usuario.Add(nuevoUsuario);
                db.SaveChanges(); // genera el id_usuario

                // 2. Crear el registro en tabla Doctor
                Doctor nuevoDoctor = new Doctor
                {
                    id_usuario = nuevoUsuario.id_usuario,
                    especialidad = especialidad
                };

                db.Doctor.Add(nuevoDoctor);
                db.SaveChanges();

                TempData["Exito"] = "Doctor creado correctamente.";
                return RedirectToAction("ListaUsuarios");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear doctor: " + ex.Message);
                return View();
            }
        }

        // ==============================
        // LISTA DE USUARIOS
        // ==============================
        public ActionResult ListaUsuarios()
        {
            if (Session["rol"] as string != "Administrador")
                return RedirectToAction("Login", "Auth");

            var usuarios = db.Usuario.ToList();
            return View(usuarios);
        }

        // ==============================
        // CERRAR SESIÓN
        // ==============================
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Auth");
        }
    }
}