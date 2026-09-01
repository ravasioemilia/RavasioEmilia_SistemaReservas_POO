using System;

namespace SistemaReservas.Modelos
{
    /// <summary>
    /// Docente: reserva laboratorios para dictar clases.
    /// Tiene un limite semanal mas alto y mayor anticipacion que un Alumno,
    /// pero solo puede cancelar sus propias reservas (hereda esa regla de Usuario).
    /// </summary>
    public class Docente : Usuario
    {
        /// <summary>Limites propios del rol, declarados como constantes para poder citarlos en los mensajes.</summary>
        public const int LIMITE_SEMANAL = 10;
        public const int ANTICIPACION_DIAS = 60;

        // ─── Constructores ──────────────────────────────────────────────────

        public Docente() : base() { }

        public Docente(string legajo, string nombre, string apellido, string email)
            : base(legajo, nombre, apellido, email, true) { }

        public Docente(string legajo, string nombre, string apellido, string email, bool activo)
            : base(legajo, nombre, apellido, email, activo) { }

        // ─── Implementacion de los miembros abstractos ──────────────────────

        public override string Rol
        {
            get { return "Docente"; }
        }

        public override int ObtenerLimiteReservasSemanales()
        {
            return LIMITE_SEMANAL;
        }

        public override int ObtenerAnticipacionMaximaDias()
        {
            return ANTICIPACION_DIAS;
        }

        // NOTA: no se sobrescribe PuedeCancelar(). Se usa la version de la clase base
        // (solo reservas propias), que es exactamente la regla que corresponde al Docente.

        public override string DescribirPermisos()
        {
            return $"Docente: hasta {LIMITE_SEMANAL} reservas por semana, con hasta " +
                   $"{ANTICIPACION_DIAS} dias de anticipacion. Cancela solo sus propias reservas.";
        }
    }
}
