using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace MediCitasWeb.Services
{
    public class EmailService
    {
        private readonly string _emailUser;
        private readonly string _emailPass;

        public EmailService()
        {
            _emailUser = ConfigurationManager.AppSettings["EmailUser"];
            _emailPass = ConfigurationManager.AppSettings["EmailPass"];
        }

        public void EnviarCodigoRecuperacion(string destino, string codigo)
        {
            MailMessage mensaje = new MailMessage
            {
                From = new MailAddress(_emailUser),
                Subject = "Código de recuperación - MediCitas",
                IsBodyHtml = true,
                Body = CrearTemplate(codigo)
            };

            mensaje.To.Add(destino);

            using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
            {
                smtp.Credentials = new NetworkCredential(_emailUser, _emailPass);
                smtp.EnableSsl = true;
                smtp.Send(mensaje);
            }
        }

        private string CrearTemplate(string codigo)
        {
            return $@"
                    <!DOCTYPE html>
                    <html>
                    <body style='background:#e3f2fd;font-family:Arial;padding:40px'>
                    <div style='max-width:500px;margin:auto;background:white;
                    padding:40px;border-radius:15px;text-align:center'>

                    <h2 style='color:#0277bd'>Recuperación de Contraseña - MediCitas</h2>

                    <p>Recibimos una solicitud para restablecer tu contraseña.</p>

                    <div style='background:#e1f5fe;
                    padding:20px;
                    font-size:32px;
                    font-weight:bold;
                    letter-spacing:6px;
                    color:#01579b'>
                    {codigo}
                    </div>

                    <p style='color:#777'>Este código expirará en unos minutos.</p>

                    </div>
                    </body>
                    </html>";
        }
    }
}