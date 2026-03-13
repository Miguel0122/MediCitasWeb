using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MediCitasWeb.Filters
{
    /// <summary>
    /// Autorización basada en la sesión actual.
    /// Usa la propiedad Roles (separada por comas) para validar el rol almacenado en Session["rol"].
    /// </summary>
    public class SessionAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            var session = httpContext.Session;
            if (session == null) return false;

            var usuario = session["usuario"] as string;
            var rol = session["rol"] as string;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(rol))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(Roles))
            {
                // Solo requiere sesión activa
                return true;
            }

            var rolesPermitidos = Roles.Split(',')
                                       .Select(r => r.Trim())
                                       .Where(r => !string.IsNullOrEmpty(r))
                                       .ToArray();

            return rolesPermitidos.Contains(rol, StringComparer.OrdinalIgnoreCase);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));

            // Redirige siempre a Login de Auth si no está autorizado
            filterContext.Result = new RedirectToRouteResult(
                new System.Web.Routing.RouteValueDictionary(
                    new
                    {
                        controller = "Auth",
                        action = "Login"
                    }
                ));
        }
    }
}

