using MediCitasWeb.Models;
using MediCitasWeb.Services.Security;
using MediCitasWeb.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
    [SessionAuthorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        MediCitasContext db = new MediCitasContext();

        public ActionResult PanelAdmin(
                string documento,
                string estado,
                DateTime? fechaInicio,
                string buscarUsuario,
                string rol)
        {
            var citas = db.Citas
                .Include("Paciente.Usuario")
                .Include("Doctor.Usuario")
                .AsQueryable();

            if (!string.IsNullOrEmpty(documento))
            {
                citas = citas.Where(c =>
                    c.Paciente.Usuario.numero_documento.Contains(documento));
            }

            if (!string.IsNullOrEmpty(estado))
            {
                citas = citas.Where(c => c.estado == estado);
            }

            if (fechaInicio.HasValue)
            {
                citas = citas.Where(c => c.fecha_cita >= fechaInicio.Value);
            }

            var listaCitas = citas.ToList();

            ViewBag.TotalCitas = db.Citas.Count();
            ViewBag.CitasPendientes = db.Citas.Count(c => c.estado == "Pendiente");
            ViewBag.CitasCompletadas = db.Citas.Count(c => c.estado == "Completada");
            ViewBag.CitasCanceladas = db.Citas.Count(c => c.estado == "Cancelada");
            ViewBag.TotalPacientes = db.Paciente.Count();

            ViewBag.Citas = listaCitas;

            // FILTRO USUARIOS
            var usuarios = db.Usuario.AsQueryable();

            if (!string.IsNullOrEmpty(buscarUsuario))
            {
                usuarios = usuarios.Where(u =>
                    u.numero_documento.Contains(buscarUsuario) ||
                    u.nombres_usuario.Contains(buscarUsuario) ||
                    u.apellidos_usuario.Contains(buscarUsuario));
            }

            if (!string.IsNullOrEmpty(rol))
            {
                usuarios = usuarios.Where(u => u.rol_usuario == rol);
            }

            return View(usuarios.ToList());
        }

        // ==============================
        // CREAR DOCTOR - Formulario
        // ==============================
        public ActionResult CrearDoctor()
        {
            return View();
        }

        // ==============================
        // CREAR DOCTOR - Guardar
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearDoctor(CrearDoctorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // 1. Crear el usuario con rol Doctor
                Usuario nuevoUsuario = new Usuario
                {
                    nombres_usuario = model.nombres,
                    apellidos_usuario = model.apellidos,
                    numero_documento = model.numero_documento,
                    correo_usuario = model.correo,
                    password_usuario = PasswordHasher.Hash(model.password),
                    rol_usuario = "Doctor",
                    fecha_registro = DateTime.Now
                };

                db.Usuario.Add(nuevoUsuario);
                db.SaveChanges(); // genera el id_usuario

                // 2. Crear el registro en tabla Doctor
                Doctor nuevoDoctor = new Doctor
                {
                    id_usuario = nuevoUsuario.id_usuario,
                    especialidad = model.especialidad
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