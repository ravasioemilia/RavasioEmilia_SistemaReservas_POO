using System;

namespace SistemaReservas.Modelos
{
    /// <summary>
    /// Usuario con permisos totales: gestiona laboratorios y usuarios,
    /// y puede cancelar CUALQUIER reserva, no solo las propias.
    /// </summary>
    public class Administrador : Usuario
    {
        // ─── Constructores: encadenan con la clase base usando : base(...) ──

        public Administrador() : base() { }

        public Administrador(string legajo, string nombre, string apellido, string email)
            : base(legajo, nombre, apellido, email, true) { }

        public Administrador(string legajo, string nombre, string apellido, string email, bool activo)
            : base(legajo, nombre, apellido, email, activo) { }

        // ─── Implementacion de los miembros abstractos ──────────────────────

        public override string Rol
        {
            get { return "Administrador"; }
        }

        /// <summary>Sin limite practico de reservas.</summary>
        public override int ObtenerLimiteReservasSemanales()
        {
            return int.MaxValue;
        }

        /// <summary>Puede reservar con la anticipacion que quiera (1 año).</summary>
        public override int ObtenerAnticipacionMaximaDias()
        {
            return 365;
        }

        // ─── Sobrescritura de los miembros virtuales ────────────────────────

        /// <summary>A diferencia del resto, cancela cualquier reserva del sistema.</summary>
        public override bool PuedeCancelar(Reserva reserva)
        {
            return reserva != null;
        }

        public override bool PuedeGestionarLaboratorios()
        {
            return true;
        }

        public override string DescribirPermisos()
        {
            return "Administrador: acceso total. Gestiona laboratorios y usuarios, " +
                   "y puede cancelar cualquier reserva.";
        }
    }
}
