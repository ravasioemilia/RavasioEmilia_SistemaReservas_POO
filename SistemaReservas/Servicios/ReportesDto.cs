using System;
using SistemaReservas.Modelos;

namespace SistemaReservas.Servicios
{
    /// <summary>
    /// Fila del reporte de uso por laboratorio.
    ///
    /// Se define una clase en lugar de usar un tipo anonimo (new { ... }) porque
    /// los tipos anonimos no se pueden devolver desde un metodo publico ni enlazar
    /// comodamente a un DataGridView. Las propiedades publicas se transforman
    /// automaticamente en las columnas de la grilla.
    /// </summary>
    public class UsoLaboratorio
    {
        public string IdLaboratorio { get; set; }
        public string NombreLaboratorio { get; set; }
        public int CantidadReservas { get; set; }
        public double HorasTotales { get; set; }
        public double PromedioHorasPorReserva { get; set; }
        public int CantidadCanceladas { get; set; }
    }

    /// <summary>Fila del ranking de usuarios que mas reservan.</summary>
    public class UsoUsuario
    {
        public string Legajo { get; set; }
        public string NombreCompleto { get; set; }
        public string Rol { get; set; }
        public int CantidadReservas { get; set; }
        public double HorasTotales { get; set; }
    }

    /// <summary>Fila del resumen por estado (Pendiente / Confirmada / Cancelada).</summary>
    public class ResumenEstado
    {
        public string Estado { get; set; }
        public int Cantidad { get; set; }
        public double PorcentajeSobreTotal { get; set; }
    }

    /// <summary>
    /// Fila lista para mostrar en la grilla de reservas.
    ///
    /// La grilla no se enlaza directamente a la lista de Reserva porque esa clase
    /// tiene muchas propiedades (EstaVencida, Duracion, EstaVigente...) y todas
    /// apareceran como columnas. Con este objeto se define exactamente que se ve
    /// y en que orden, y ademas se reemplazan los codigos por nombres.
    /// </summary>
    public class FilaReserva
    {
        public string Id { get; set; }
        public string Laboratorio { get; set; }
        public string Usuario { get; set; }
        public string Fecha { get; set; }
        public string Horario { get; set; }
        public string Duracion { get; set; }
        public string Motivo { get; set; }
        public string Estado { get; set; }

        /// <summary>
        /// La reserva original. No se muestra como columna (se oculta en el formulario),
        /// pero permite recuperar el objeto completo cuando el usuario hace clic en la fila.
        /// </summary>
        public Reserva Original { get; set; }
    }

    /// <summary>
    /// Fila de la vista tipo calendario: una franja de media hora del dia,
    /// con la indicacion de si esta libre u ocupada.
    /// </summary>
    public class FilaFranjaHoraria
    {
        public string Horario { get; set; }
        public string Estado { get; set; }
        public string Motivo { get; set; }
        public string Solicitante { get; set; }

        /// <summary>Se usa para pintar la fila. No se muestra como columna.</summary>
        public bool Ocupada { get; set; }
    }

    /// <summary>Franja horaria libre de un laboratorio en una fecha.</summary>
    public class FranjaLibre
    {
        public TimeSpan Desde { get; set; }
        public TimeSpan Hasta { get; set; }

        /// <summary>Propiedad calculada: duracion de la franja en horas.</summary>
        public double Horas
        {
            get { return (Hasta - Desde).TotalHours; }
        }

        public override string ToString()
        {
            return $"{Desde:hh\\:mm} a {Hasta:hh\\:mm} ({Horas:0.#} hs)";
        }
    }
}
