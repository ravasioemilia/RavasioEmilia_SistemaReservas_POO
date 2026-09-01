using System;   // Necesario para acceder a EventArgs
using SistemaReservas.Modelos;

namespace SistemaReservas.Servicios
{
    /// <summary>
    /// Datos que viajan con los eventos ReservaCreada y ReservaCancelada.
    /// Hereda de System.EventArgs, como exige la convencion de .NET.
    ///
    /// Gracias a esta clase, el formulario que escucha el evento recibe toda la
    /// informacion que necesita para refrescar la pantalla sin volver a consultar nada.
    /// </summary>
    public class ReservaEventArgs : EventArgs
    {
        /// <summary>La reserva afectada por la operacion.</summary>
        public Reserva Reserva { get; }

        /// <summary>Usuario que realizo la accion (puede no ser el dueño de la reserva).</summary>
        public Usuario Responsable { get; }

        /// <summary>Momento exacto de la operacion.</summary>
        public DateTime FechaHora { get; }

        /// <summary>Texto listo para mostrar en la barra de estado o en el log.</summary>
        public string Descripcion { get; }

        public ReservaEventArgs(Reserva reserva, Usuario responsable, string descripcion)
        {
            Reserva = reserva;
            Responsable = responsable;
            Descripcion = descripcion;
            FechaHora = DateTime.Now;
        }
    }
}
