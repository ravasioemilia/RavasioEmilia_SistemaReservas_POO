using System;

namespace SistemaReservas.Excepciones
{
    /// <summary>
    /// Se lanza al intentar dar de alta una entidad cuyo Id ya existe en el repositorio
    /// (por ejemplo, dos laboratorios con el codigo LAB01).
    /// </summary>
    public class EntidadDuplicadaException : SistemaReservasException
    {
        public string TipoEntidad { get; }
        public string IdDuplicado { get; }

        // Constructor 1 - sin mensaje
        public EntidadDuplicadaException() : base() { }

        // Constructor 2 - con mensaje personalizado
        public EntidadDuplicadaException(string mensaje) : base(mensaje) { }

        // Constructor 3 - con mensaje + excepcion interna
        public EntidadDuplicadaException(string mensaje, Exception inner) : base(mensaje, inner) { }

        // Constructor 4 - especifico del dominio
        public EntidadDuplicadaException(string tipoEntidad, string idDuplicado)
            : base($"Ya existe un/a {tipoEntidad} con el identificador '{idDuplicado}'.")
        {
            TipoEntidad = tipoEntidad;
            IdDuplicado = idDuplicado;
        }
    }
}
