using System;

namespace SistemaReservas.Interfaces
{
    /// <summary>
    /// Contrato que debe cumplir toda entidad que se quiera persistir en un archivo de texto.
    /// Es la restriccion de tipo que usa RepositorioArchivo&lt;T&gt;: gracias a estos tres
    /// miembros, el repositorio generico puede leer y escribir CUALQUIER entidad sin
    /// conocer sus campos concretos.
    /// </summary>
    public interface IEntidad
    {
        /// <summary>Identificador unico de la entidad (LAB01, A-2024-015, R001...).</summary>
        string Id { get; }

        /// <summary>Convierte el objeto a una linea de texto delimitada por '|'.</summary>
        string ConvertirALinea();

        /// <summary>
        /// Carga los datos del objeto a partir de una linea del archivo.
        /// Debe lanzar ArchivoDatosCorruptoException si la linea no tiene el formato esperado.
        /// </summary>
        void CargarDesdeLinea(string linea);
    }
}
