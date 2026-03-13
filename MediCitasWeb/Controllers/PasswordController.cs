using MediCitasWeb.Models;
using MediCitasWeb.Services;
using MediCitasWeb.Services.Security;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Mvc;

namespace MediCitasWeb.Controllers
{
	public class PasswordController : Controller
	{
		private readonly EmailService _emailService = new EmailService();

		public ActionResult Recuperar()
		{
			return View();
		}

		// =====================================================
		// GENERAR CÓDIGO SEGURO
		// =====================================================
		private string GenerarCodigoSeguro()
		{
			byte[] bytes = new byte[4];

			using (var rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(bytes);
			}

			int numero = Math.Abs(BitConverter.ToInt32(bytes, 0));

			// Código de 6 dígitos
			return (numero % 900000 + 100000).ToString();
		}

		// =====================================================
		// ENVIAR CÓDIGO
		// =====================================================
		[HttpPost]
		public JsonResult EnviarCodigo(string correo)
		{
			correo = correo?.Trim().ToLower();

			if (string.IsNullOrWhiteSpace(correo))
				return Json(new { ok = false, mensaje = "Correo inválido." });

			using (var db = new MediCitasContext())
			{
				var usuario = db.Usuario
					.FirstOrDefault(u => u.correo_usuario == correo);

				if (usuario == null)
					return Json(new { ok = false, mensaje = "El correo no está registrado." });

				string codigo = GenerarCodigoSeguro();

				Session["CodigoRecuperacion"] = codigo;
				Session["CorreoRecuperacion"] = correo;
				Session["CodigoExpira"] = DateTime.Now.AddMinutes(10);

				try
				{
					_emailService.EnviarCodigoRecuperacion(correo, codigo);

					return Json(new
					{
						ok = true,
						mensaje = "Código enviado correctamente."
					});
				}
				catch (Exception ex)
				{
					return Json(new
					{
						ok = false,
						mensaje = "Error enviando correo: " + ex.Message
					});
				}
			}
		}

		// =====================================================
		// VALIDAR CÓDIGO
		// =====================================================
		[HttpPost]
		public JsonResult ValidarCodigo(string codigo)
		{
			string codigoSession = Session["CodigoRecuperacion"] as string;
			DateTime? expira = Session["CodigoExpira"] as DateTime?;

			if (codigoSession == null || expira == null)
				return Json(new { ok = false, mensaje = "La sesión expiró." });

			if (DateTime.Now > expira.Value)
				return Json(new { ok = false, mensaje = "El código expiró." });

			if (codigoSession.Trim() == codigo?.Trim())
				return Json(new { ok = true });

			return Json(new { ok = false, mensaje = "Código incorrecto." });
		}

		// =====================================================
		// CAMBIAR PASSWORD
		// =====================================================
		[HttpPost]
		public JsonResult CambiarPassword(string nueva)
		{
			string correo = Session["CorreoRecuperacion"] as string;

			if (correo == null)
				return Json(new { ok = false, mensaje = "Sesión expirada." });

			if (string.IsNullOrWhiteSpace(nueva) || nueva.Length < 6)
				return Json(new { ok = false, mensaje = "Contraseña inválida." });

			using (var db = new MediCitasContext())
			{
				var usuario = db.Usuario
					.FirstOrDefault(u => u.correo_usuario == correo);

				if (usuario == null)
					return Json(new { ok = false });

				// ✅ PASSWORD HASH unificado con el login/registro
				usuario.password_usuario = PasswordHasher.Hash(nueva);

				db.SaveChanges();
			}

			Session.Remove("CodigoRecuperacion");
			Session.Remove("CorreoRecuperacion");
			Session.Remove("CodigoExpira");

			return Json(new { ok = true });
		}
	}
}