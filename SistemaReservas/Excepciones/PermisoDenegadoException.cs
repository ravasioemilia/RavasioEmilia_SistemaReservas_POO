using System;

namespace SistemaReservas.Excepciones
{
    /// <summary>
    /// Se lanza cuando un usuario intenta una accion que su rol no le permite
    /// (por ejemplo, un Alumno cancelando la reserva de otra persona).
    /// </summary>
    public class PermisoDenegadoException : SistemaReservasException
    {
        public string LegajoUsuario { get; }
        public string AccionIntentada { get; }

        // Constructor 1 - sin mensaje
        public PermisoDenegadoException() : base() { }

        // Constructor 2 - con mensaje personalizado
        public PermisoDenegadoException(string mensaje) : base(mensaje) { }

        // Constructor 3 - con mensaje + excepcion interna
        public PermisoDenegadoException(string mensaje, Exception inner) : base(mensaje, inner) { }

        // Constructor 4 - especifico del dominio
        public PermisoDenegadoException(string legajo, string accion)
            : base($"El usuario {legajo} no tiene permisos para {accion}.")
        {
            LegajoUsuario = legajo;
            AccionIntentada = accion;
        }
    }
}
