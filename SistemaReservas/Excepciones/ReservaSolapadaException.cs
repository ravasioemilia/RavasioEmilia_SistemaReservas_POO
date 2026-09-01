using System;

namespace SistemaReservas.Excepciones
{
    /// <summary>
    /// Se lanza cuando se intenta crear una reserva que pisa el horario de otra
    /// ya existente en el mismo laboratorio y la misma fecha.
    /// </summary>
    public class ReservaSolapadaException : SistemaReservasException
    {
        // Datos especificos del error: permiten que el formulario muestre un
        // mensaje util sin tener que parsear el texto de la excepcion.
        public string IdLaboratorio { get; }
        public DateTime Fecha { get; }
        public string IdReservaEnConflicto { get; }

        // Constructor 1 - sin mensaje
        public ReservaSolapadaException() : base() { }

        // Constructor 2 - con mensaje personalizado
        public ReservaSolapadaException(string mensaje) : base(mensaje) { }

        // Constructor 3 - con mensaje + excepcion interna
        public ReservaSolapadaException(string mensaje, Exception inner) : base(mensaje, inner) { }

        // Constructor 4 - especifico del dominio: arma solo el mensaje con los datos del conflicto
        public ReservaSolapadaException(string idLaboratorio, DateTime fecha, string idEnConflicto)
            : base($"El laboratorio {idLaboratorio} ya tiene una reserva que se superpone " +
                   $"el {fecha:dd/MM/yyyy} (reserva {idEnConflicto}).")
        {
            IdLaboratorio = idLaboratorio;
            Fecha = fecha;
            IdReservaEnConflicto = idEnConflicto;
        }
    }
}
