using System;

namespace SistemaReservas.Excepciones
{
    /// <summary>
    /// Se lanza cuando una linea de un archivo de datos no respeta el formato esperado
    /// (faltan campos, una fecha no se puede parsear, un numero es invalido...).
    /// Guarda el numero de linea y su contenido para que el usuario pueda corregir el .txt.
    /// </summary>
    public class ArchivoDatosCorruptoException : SistemaReservasException
    {
        public string NombreArchivo { get; }
        public int NumeroLinea { get; }
        public string ContenidoLinea { get; }

        // Constructor 1 - sin mensaje
        public ArchivoDatosCorruptoException() : base() { }

        // Constructor 2 - con mensaje personalizado
        public ArchivoDatosCorruptoException(string mensaje) : base(mensaje) { }

        // Constructor 3 - con mensaje + excepcion interna
        // (clave aca: envolvemos la FormatException original sin perderla)
        public ArchivoDatosCorruptoException(string mensaje, Exception inner)
            : base(mensaje, inner) { }

        // Constructor 4 - especifico del dominio
        public ArchivoDatosCorruptoException(string nombreArchivo, int numeroLinea,
                                             string contenidoLinea, Exception inner)
            : base($"Formato invalido en el archivo '{nombreArchivo}', linea {numeroLinea}: " +
                   $"\"{contenidoLinea}\"", inner)
        {
            NombreArchivo = nombreArchivo;
            NumeroLinea = numeroLinea;
            ContenidoLinea = contenidoLinea;
        }
    }
}
