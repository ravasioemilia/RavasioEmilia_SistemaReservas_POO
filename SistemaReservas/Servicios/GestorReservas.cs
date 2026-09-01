using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SistemaReservas.Excepciones;
using SistemaReservas.Interfaces;
using SistemaReservas.Modelos;
using SistemaReservas.Repositorios;

namespace SistemaReservas.Servicios
{
    /// <summary>
    /// Nucleo de la logica de negocio. Es el intermediario entre los formularios
    /// y los repositorios: NINGUN formulario toca archivos ni aplica reglas por su cuenta.
    ///
    /// TEMAS APLICADOS:
    ///  - EVENTOS: ReservaCreada y ReservaCancelada avisan a la interfaz que algo cambio,
    ///    sin que esta clase sepa que existen los formularios (desacoplamiento).
    ///  - POLIMORFISMO: las reglas por rol se consultan al objeto Usuario
    ///    (ObtenerLimiteReservasSemanales, ObtenerAnticipacionMaximaDias, PuedeCancelar).
    ///  - LINQ + LAMBDAS: deteccion de solapamientos, conteo semanal, disponibilidad.
    ///  - EXCEPCIONES PROPIAS: cada regla violada lanza su excepcion especifica.
    ///  - IDISPOSABLE: es dueño de los tres repositorios y los libera al cerrarse.
    /// </summary>
    public class GestorReservas : IDisposable
    {
        // ─── Repositorios (los crea y los administra esta clase) ────────────

        private readonly RepositorioArchivo<Laboratorio> repoLaboratorios;
        private readonly RepositorioArchivo<Usuario> repoUsuarios;
        private readonly RepositorioArchivo<Reserva> repoReservas;

        private bool disposed = false;

        // ─── Acceso para los formularios ────────────────────────────────────
        // Se exponen como IRepositorio<T> y no como la clase concreta: los formularios
        // dependen del CONTRATO, no de la implementacion con archivos.

        public IRepositorio<Laboratorio> Laboratorios
        {
            get { return repoLaboratorios; }
        }

        public IRepositorio<Usuario> Usuarios
        {
            get { return repoUsuarios; }
        }

        public IRepositorio<Reserva> Reservas
        {
            get { return repoReservas; }
        }

        // ─── EVENTOS ────────────────────────────────────────────────────────

        /// <summary>Se dispara cada vez que se registra una reserva nueva.</summary>
        public event EventHandler<ReservaEventArgs> ReservaCreada;

        /// <summary>Se dispara cada vez que se cancela una reserva.</summary>
        public event EventHandler<ReservaEventArgs> ReservaCancelada;

        /// <summary>
        /// Metodo protegido que dispara el evento. Se usa ?.Invoke para no fallar
        /// cuando todavia no hay ningun suscriptor.
        /// </summary>
        protected virtual void OnReservaCreada(ReservaEventArgs e)
        {
            ReservaCreada?.Invoke(this, e);
        }

        protected virtual void OnReservaCancelada(ReservaEventArgs e)
        {
            ReservaCancelada?.Invoke(this, e);
        }

        // ─── Constructores ──────────────────────────────────────────────────

        /// <summary>
        /// Constructor por defecto: usa las rutas estandar de la carpeta Datos.
        /// Encadena con : this(...) al constructor completo.
        /// </summary>
        public GestorReservas()
            : this(RutasDatos.ArchivoLaboratorios,
                   RutasDatos.ArchivoUsuarios,
                   RutasDatos.ArchivoReservas)
        {
        }

        /// <summary>
        /// Constructor completo: permite indicar otras rutas (util para pruebas).
        /// </summary>
        public GestorReservas(string rutaLaboratorios, string rutaUsuarios, string rutaReservas)
        {
            repoLaboratorios = new RepositorioArchivo<Laboratorio>(rutaLaboratorios);
            repoReservas = new RepositorioArchivo<Reserva>(rutaReservas);

            // Usuario es abstracta: se le pasa la fabrica que elige la clase concreta
            // segun el rol. Ese delegado es lo que permite reusar el repositorio generico.
            repoUsuarios = new RepositorioArchivo<Usuario>(rutaUsuarios, Usuario.CrearSegunRol);
        }

        // ─── Errores de carga (robustez ante archivos corruptos) ────────────

        /// <summary>
        /// Junta los errores de las tres cargas para mostrarlos de una sola vez al iniciar.
        /// Se usa SelectMany para aplanar las tres listas en una.
        /// </summary>
        public List<ArchivoDatosCorruptoException> ObtenerErroresDeCarga()
        {
            var listas = new List<List<ArchivoDatosCorruptoException>>
            {
                repoLaboratorios.ErroresDeCarga,
                repoUsuarios.ErroresDeCarga,
                repoReservas.ErroresDeCarga
            };

            return listas.SelectMany(lista => lista).ToList();
        }

        // ─── ALTA DE RESERVA ────────────────────────────────────────────────

        /// <summary>
        /// Registra una reserva nueva aplicando, en orden, todas las reglas del sistema.
        /// Si alguna falla lanza la excepcion correspondiente y NO se guarda nada.
        /// </summary>
        /// <returns>La reserva creada, ya persistida.</returns>
        public Reserva CrearReserva(Usuario solicitante, string idLaboratorio, DateTime fecha,
                                    TimeSpan horaInicio, TimeSpan horaFin, string motivo)
        {
            VerificarNoLiberado();

            if (solicitante == null)
                throw new ArgumentNullException(nameof(solicitante),
                    "Debe haber un usuario logueado para reservar.");

            // ── Regla 1: el laboratorio debe existir y estar activo ──────────
            Laboratorio laboratorio = repoLaboratorios.Obtener(idLaboratorio);

            if (laboratorio == null)
                throw new LaboratorioNoDisponibleException(idLaboratorio, "no existe en el sistema");

            if (!laboratorio.Activo)
                throw new LaboratorioNoDisponibleException(idLaboratorio,
                    "esta dado de baja y no admite reservas");

            // ── Regla 2: no se puede reservar en el pasado ───────────────────
            if (fecha.Date < DateTime.Today)
                throw new SistemaReservasException(
                    $"No se puede reservar en una fecha pasada ({fecha:dd/MM/yyyy}).");

            // ── Regla 3: anticipacion maxima segun el ROL (polimorfismo) ─────
            int anticipacionPermitida = solicitante.ObtenerAnticipacionMaximaDias();
            int diasDeAnticipacion = (fecha.Date - DateTime.Today).Days;

            if (diasDeAnticipacion > anticipacionPermitida)
                throw new SistemaReservasException(
                    $"Un {solicitante.Rol} puede reservar con hasta {anticipacionPermitida} dias " +
                    $"de anticipacion. La fecha elegida esta a {diasDeAnticipacion} dias.");

            // ── Regla 4: limite semanal segun el ROL (polimorfismo) ──────────
            int limiteSemanal = solicitante.ObtenerLimiteReservasSemanales();
            int reservasEnLaSemana = ContarReservasDeLaSemana(solicitante.Legajo, fecha);

            if (reservasEnLaSemana >= limiteSemanal)
                throw new LimiteReservasExcedidoException(
                    solicitante.Legajo, limiteSemanal, reservasEnLaSemana);

            // ── Se arma el objeto: sus propiedades validan las horas ─────────
            string nuevoId = GenerarIdReserva();

            // Se elige la sobrecarga del constructor segun haya motivo o no.
            Reserva nueva = string.IsNullOrWhiteSpace(motivo)
                ? new Reserva(nuevoId, laboratorio.Id, solicitante.Legajo, fecha, horaInicio, horaFin)
                : new Reserva(nuevoId, laboratorio.Id, solicitante.Legajo, fecha, horaInicio, horaFin, motivo);

            // ── Regla 5: no puede solaparse con otra reserva vigente ─────────
            Reserva enConflicto = BuscarSolapamiento(nueva);

            if (enConflicto != null)
                throw new ReservaSolapadaException(laboratorio.Id, fecha, enConflicto.Id);

            // ── Regla 6: validaciones propias de la entidad (IValidable) ─────
            List<string> errores = nueva.Validar();

            if (errores.Count > 0)
                throw new SistemaReservasException(
                    "La reserva no es valida:" + Environment.NewLine +
                    string.Join(Environment.NewLine, errores.Select(e => " - " + e)));

            // ── Persistencia + aviso a la interfaz ───────────────────────────
            repoReservas.Agregar(nueva);

            OnReservaCreada(new ReservaEventArgs(nueva, solicitante,
                $"Reserva {nueva.Id} creada en {laboratorio.Id} para el " +
                $"{nueva.Fecha:dd/MM/yyyy} de {nueva.RangoHorario}."));

            return nueva;
        }

        // ─── CANCELACION ────────────────────────────────────────────────────

        /// <summary>
        /// Cancela una reserva. Quien puede hacerlo lo decide el propio objeto Usuario
        /// mediante PuedeCancelar(): el Administrador cancela cualquiera, el Docente y
        /// el Alumno solo las propias. Aca NO hay ningun if sobre el tipo de usuario.
        /// </summary>
        public void CancelarReserva(Usuario solicitante, string idReserva)
        {
            VerificarNoLiberado();

            if (solicitante == null)
                throw new ArgumentNullException(nameof(solicitante));

            Reserva reserva = repoReservas.Obtener(idReserva);

            if (reserva == null)
                throw new EntidadNoEncontradaException("Reserva", idReserva);

            if (reserva.Estado == EstadoReserva.Cancelada)
                throw new SistemaReservasException(
                    $"La reserva {reserva.Id} ya se encuentra cancelada.");

            // POLIMORFISMO: cada rol responde distinto a la misma pregunta.
            if (!solicitante.PuedeCancelar(reserva))
                throw new PermisoDenegadoException(solicitante.Legajo,
                    $"cancelar la reserva {reserva.Id}");

            reserva.Estado = EstadoReserva.Cancelada;
            repoReservas.Actualizar(reserva);

            OnReservaCancelada(new ReservaEventArgs(reserva, solicitante,
                $"Reserva {reserva.Id} cancelada por {solicitante.NombreCompleto} " +
                $"({solicitante.Rol})."));
        }

        /// <summary>Confirma una reserva pendiente. Solo el Administrador puede hacerlo.</summary>
        public void ConfirmarReserva(Usuario solicitante, string idReserva)
        {
            VerificarNoLiberado();

            if (solicitante == null)
                throw new ArgumentNullException(nameof(solicitante));

            if (!solicitante.PuedeGestionarLaboratorios())
                throw new PermisoDenegadoException(solicitante.Legajo, "confirmar reservas");

            Reserva reserva = repoReservas.Obtener(idReserva);

            if (reserva == null)
                throw new EntidadNoEncontradaException("Reserva", idReserva);

            if (reserva.Estado == EstadoReserva.Cancelada)
                throw new SistemaReservasException(
                    $"La reserva {reserva.Id} esta cancelada y no puede confirmarse.");

            reserva.Estado = EstadoReserva.Confirmada;
            repoReservas.Actualizar(reserva);
        }

        // ─── CONSULTAS (LINQ + lambdas) ─────────────────────────────────────

        /// <summary>
        /// Devuelve la reserva vigente que se superpone con la indicada, o null si
        /// el horario esta libre. Reutiliza la logica de dominio Reserva.SeSuperponeCon().
        /// </summary>
        public Reserva BuscarSolapamiento(Reserva candidata)
        {
            VerificarNoLiberado();

            if (candidata == null) return null;

            return repoReservas.Listar()
                               .FirstOrDefault(existente => candidata.SeSuperponeCon(existente));
        }

        /// <summary>True si el laboratorio esta libre en esa fecha y franja horaria.</summary>
        public bool HayDisponibilidad(string idLaboratorio, DateTime fecha,
                                      TimeSpan horaInicio, TimeSpan horaFin)
        {
            VerificarNoLiberado();

            // Se arma una reserva "de prueba" con un Id que no existe, solo para
            // reutilizar la comparacion de solapamiento.
            Reserva consulta = new Reserva("CONSULTA", idLaboratorio, "CONSULTA",
                                           fecha, horaInicio, horaFin);

            return BuscarSolapamiento(consulta) == null;
        }

        /// <summary>
        /// Reservas vigentes de un laboratorio en una fecha, ordenadas por hora.
        /// Es lo que alimenta la grilla de la pantalla de disponibilidad.
        /// </summary>
        public List<Reserva> ObtenerReservasDelLaboratorio(string idLaboratorio, DateTime fecha)
        {
            VerificarNoLiberado();

            return repoReservas.Listar()
                .Where(r => string.Equals(r.IdLaboratorio, idLaboratorio,
                                          StringComparison.OrdinalIgnoreCase))
                .Where(r => r.Fecha.Date == fecha.Date)
                .Where(r => r.Estado != EstadoReserva.Cancelada)
                .OrderBy(r => r.HoraInicio)
                .ToList();
        }

        /// <summary>Todas las reservas de un usuario, de la mas reciente a la mas vieja.</summary>
        public List<Reserva> ObtenerReservasDelUsuario(string legajo)
        {
            VerificarNoLiberado();

            return repoReservas.Listar()
                .Where(r => string.Equals(r.LegajoUsuario, legajo, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.Fecha)
                .ThenBy(r => r.HoraInicio)
                .ToList();
        }

        /// <summary>
        /// Cuenta las reservas VIGENTES del usuario en la semana (lunes a domingo)
        /// a la que pertenece la fecha indicada. Se usa para el limite semanal.
        /// </summary>
        public int ContarReservasDeLaSemana(string legajo, DateTime fecha)
        {
            VerificarNoLiberado();

            DateTime lunes = ObtenerLunesDeLaSemana(fecha);
            DateTime domingo = lunes.AddDays(6);

            return repoReservas.Listar()
                .Count(r => string.Equals(r.LegajoUsuario, legajo, StringComparison.OrdinalIgnoreCase)
                            && r.Estado != EstadoReserva.Cancelada
                            && r.Fecha.Date >= lunes
                            && r.Fecha.Date <= domingo);
        }

        /// <summary>Laboratorios activos, para llenar los ComboBox.</summary>
        public List<Laboratorio> ObtenerLaboratoriosActivos()
        {
            VerificarNoLiberado();

            return repoLaboratorios.Listar()
                .Where(l => l.Activo)
                .OrderBy(l => l.Id)
                .ToList();
        }

        /// <summary>Usuarios activos, ordenados por rol y apellido.</summary>
        public List<Usuario> ObtenerUsuariosActivos()
        {
            VerificarNoLiberado();

            return repoUsuarios.Listar()
                .Where(u => u.Activo)
                .OrderBy(u => u.Rol)
                .ThenBy(u => u.Apellido)
                .ToList();
        }

        // ─── Metodos auxiliares ─────────────────────────────────────────────

        /// <summary>
        /// Genera el proximo Id de reserva (R001, R002...). Toma el numero mas alto
        /// existente y le suma uno, para no repetir aunque se hayan borrado reservas.
        /// </summary>
        private string GenerarIdReserva()
        {
            List<Reserva> todas = repoReservas.Listar();

            if (todas.Count == 0) return "R001";

            int maximo = todas
                .Select(r => r.Id)
                .Where(id => id.Length > 1 && id.StartsWith("R", StringComparison.OrdinalIgnoreCase))
                .Select(id =>
                {
                    int numero;
                    return int.TryParse(id.Substring(1), NumberStyles.Integer,
                                        CultureInfo.InvariantCulture, out numero) ? numero : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return "R" + (maximo + 1).ToString("D3", CultureInfo.InvariantCulture);
        }

        /// <summary>Devuelve el lunes de la semana a la que pertenece la fecha.</summary>
        public static DateTime ObtenerLunesDeLaSemana(DateTime fecha)
        {
            // DayOfWeek arranca en Domingo=0; se corrige para que la semana empiece el lunes.
            int diasDesdeLunes = ((int)fecha.DayOfWeek + 6) % 7;
            return fecha.Date.AddDays(-diasDesdeLunes);
        }

        // ─── IDisposable ────────────────────────────────────────────────────

        private void VerificarNoLiberado()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(GestorReservas));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Esta clase es dueña de los repositorios: le corresponde liberarlos.
                    if (repoLaboratorios != null) repoLaboratorios.Dispose();
                    if (repoUsuarios != null) repoUsuarios.Dispose();
                    if (repoReservas != null) repoReservas.Dispose();

                    // Se quitan los suscriptores para que los formularios cerrados
                    // no queden retenidos en memoria por el evento.
                    ReservaCreada = null;
                    ReservaCancelada = null;
                }

                disposed = true;
            }
        }

        /// <summary>Destructor de respaldo, por si nadie llamo a Dispose().</summary>
        ~GestorReservas()
        {
            Dispose(false);
        }
    }
}
