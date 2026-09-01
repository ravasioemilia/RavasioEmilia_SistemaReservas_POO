using System;

namespace SistemaReservas.Excepciones
{
    /// <summary>
    /// Se lanza cuando el laboratorio solicitado no existe, esta dado de baja
    /// o no tiene capacidad suficiente para la reserva pedida.
    /// </summary>
    public class LaboratorioNoDisponibleException : SistemaReservasException
    {
        public string IdLaboratorio { get; }
        public string Motivo { get; }

        // Constructor 1 - sin mensaje
        public LaboratorioNoDisponibleException() : base() { }

        // Constructor 2 - con mensaje personalizado
        public LaboratorioNoDisponibleException(string mensaje) : base(mensaje) { }

        // Constructor 3 - con mensaje + excepcion interna
        public LaboratorioNoDisponibleException(string mensaje, Exception inner)
            : base(mensaje, inner) { }

        // Constructor 4 - especifico del dominio
        public LaboratorioNoDisponibleException(string idLaboratorio, string motivo)
            : base($"El laboratorio {idLaboratorio} no esta disponible: {motivo}")
        {
            IdLaboratorio = idLaboratorio;
            Motivo = motivo;
        }
    }
}
