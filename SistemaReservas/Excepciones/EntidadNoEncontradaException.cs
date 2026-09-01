using System;

namespace SistemaReservas.Excepciones
{
    /// <summary>
    /// Se lanza cuando se busca, modifica o elimina una entidad por Id y no existe
    /// en el repositorio.
    /// </summary>
    public class EntidadNoEncontradaException : SistemaReservasException
    {
        public string TipoEntidad { get; }
        public string IdBuscado { get; }

        // Constructor 1 - sin mensaje
        public EntidadNoEncontradaException() : base() { }

        // Constructor 2 - con mensaje personalizado
        public EntidadNoEncontradaException(string mensaje) : base(mensaje) { }

        // Constructor 3 - con mensaje + excepcion interna
        public EntidadNoEncontradaException(string mensaje, Exception inner) : base(mensaje, inner) { }

        // Constructor 4 - especifico del dominio
        public EntidadNoEncontradaException(string tipoEntidad, string idBuscado)
            : base($"No existe ningun/a {tipoEntidad} con el identificador '{idBuscado}'.")
        {
            TipoEntidad = tipoEntidad;
            IdBuscado = idBuscado;
        }
    }
}
