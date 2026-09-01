using System;

namespace SistemaReservas.Modelos
{
    /// <summary>
    /// Alumno: reserva laboratorios para uso libre o trabajos practicos.
    /// Es el rol mas restringido: menos reservas semanales, menos anticipacion,
    /// y solo puede cancelar sus propias reservas.
    /// </summary>
    public class Alumno : Usuario
    {
        public const int LIMITE_SEMANAL = 3;
        public const int ANTICIPACION_DIAS = 14;

        // ─── Constructores ──────────────────────────────────────────────────

        public Alumno() : base() { }

        public Alumno(string legajo, string nombre, string apellido, string email)
            : base(legajo, nombre, apellido, email, true) { }

        public Alumno(string legajo, string nombre, string apellido, string email, bool activo)
            : base(legajo, nombre, apellido, email, activo) { }

        // ─── Implementacion de los miembros abstractos ──────────────────────

        public override string Rol
        {
            get { return "Alumno"; }
        }

        public override int ObtenerLimiteReservasSemanales()
        {
            return LIMITE_SEMANAL;
        }

        public override int ObtenerAnticipacionMaximaDias()
        {
            return ANTICIPACION_DIAS;
        }

        // ─── Sobrescritura ──────────────────────────────────────────────────

        /// <summary>
        /// Regla adicional del Alumno: ademas de ser la reserva propia (regla heredada),
        /// no puede cancelar una reserva que ya empezo o vencio.
        /// Se reutiliza la logica del padre con base.PuedeCancelar(...).
        /// </summary>
        public override bool PuedeCancelar(Reserva reserva)
        {
            if (!base.PuedeCancelar(reserva)) return false;
            return !reserva.EstaVencida;
        }

        public override string DescribirPermisos()
        {
            return $"Alumno: hasta {LIMITE_SEMANAL} reservas por semana, con hasta " +
                   $"{ANTICIPACION_DIAS} dias de anticipacion. Cancela solo sus reservas no vencidas.";
        }
    }
}
