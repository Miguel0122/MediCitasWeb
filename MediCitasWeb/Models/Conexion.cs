using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Data.SqlClient;

namespace MediCitasWeb.Models
{
    public class Conexion
    {
        // 👇 CORREGIDO: ahora usa el nombre REAL del Web.config
        private string cadena = ConfigurationManager.ConnectionStrings["MediCitasDB"].ConnectionString;

        public SqlConnection ObtenerConexion()
        {
            SqlConnection con = new SqlConnection(cadena);
            con.Open();
            return con;
        }
    }
}