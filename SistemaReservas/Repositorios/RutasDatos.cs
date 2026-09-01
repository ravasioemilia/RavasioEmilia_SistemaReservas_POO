using System;
using System.IO;

namespace SistemaReservas.Repositorios
{
    /// <summary>
    /// Centraliza las rutas de los archivos de datos.
    ///
    /// Se resuelven en tiempo de ejecucion a partir de la carpeta del ejecutable
    /// (bin\Debug\net8.0-windows\Datos\), no con rutas absolutas del estilo
    /// "C:\Users\...\Datos". Asi el proyecto funciona en la PC de cualquier integrante
    /// del grupo y en la del profesor.
    ///
    /// Los .txt estan marcados en el .csproj con CopyToOutputDirectory, por lo que
    /// Visual Studio los copia junto al .exe en cada compilacion.
    /// </summary>
    public static class RutasDatos
    {
        /// <summary>Nombre de la subcarpeta que contiene los archivos de datos.</summary>
        public const string CARPETA_DATOS = "Datos";

        /// <summary>Carpeta Datos ubicada junto al ejecutable. Se crea si no existe.</summary>
        public static string CarpetaDatos
        {
            get
            {
                string carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CARPETA_DATOS);

                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                return carpeta;
            }
        }

        public static string ArchivoLaboratorios
        {
            get { return Path.Combine(CarpetaDatos, "laboratorios.txt"); }
        }

        public static string ArchivoUsuarios
        {
            get { return Path.Combine(CarpetaDatos, "usuarios.txt"); }
        }

        public static string ArchivoReservas
        {
            get { return Path.Combine(CarpetaDatos, "reservas.txt"); }
        }
    }
}
