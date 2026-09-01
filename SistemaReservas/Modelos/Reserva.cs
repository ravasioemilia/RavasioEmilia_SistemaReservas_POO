using System;
using System.Collections.Generic;
using System.Globalization;
using SistemaReservas.Excepciones;
using SistemaReservas.Interfaces;

namespace SistemaReservas.Modelos
{
    /// <summary>
    /// Representa la solicitud de uso de un laboratorio en una fecha y franja horaria.
    ///
    /// TEMAS APLICADOS:
    ///  - Propiedades con validacion cruzada (HoraFin no puede ser anterior a HoraInicio).
    ///  - Propiedades calculadas: Duracion, EstaVencida, RangoHorario.
    ///  - DOS constructores sobrecargados encadenados con : this(...)  (uno con motivo,
    ///    otro sin motivo), tal como pide la consigna.
    ///  - Metodo de dominio SeSuperponeCon(), reutilizado por LINQ para detectar solapamientos.
    /// </summary>
    public class Reserva : IEntidad, IValidable
    {
        /// <summary>Formato unico de fecha en los archivos. Invariante = no depende del idioma de la PC.</summary>
        public const string FormatoFecha = "yyyy-MM-dd";

        // ─── Campos privados ────────────────────────────────────────────────
        private string id;
        private string idLaboratorio;
        private string legajoUsuario;
        private DateTime fecha;
        private TimeSpan horaInicio;
        private TimeSpan horaFin;
        private string motivo;
        private EstadoReserva estado;

        // ─── Propiedades con validacion ─────────────────────────────────────

        public string Id
        {
            get { return id; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El identificador de la reserva no puede estar vacio.");
                if (value.Contains("|"))
                    throw new ArgumentException("El identificador no puede contener el caracter '|'.");
                id = value.Trim().ToUpper();
            }
        }

        public string IdLaboratorio
        {
            get { return idLaboratorio; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Debe indicarse el laboratorio a reservar.");
                idLaboratorio = value.Trim().ToUpper();
            }
        }

        public string LegajoUsuario
        {
            get { return legajoUsuario; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Debe indicarse el usuario que realiza la reserva.");
                legajoUsuario = value.Trim().ToUpper();
            }
        }

        /// <summary>Fecha de la reserva. Se guarda solo la parte de fecha (sin hora).</summary>
        public DateTime Fecha
        {
            get { return fecha; }
            set { fecha = value.Date; }
        }

        /// <summary>
        /// Hora de inicio. Valida que quede antes de HoraFin.
        /// La comparacion se hace solo si HoraFin ya fue cargada (distinta de cero),
        /// para que el orden de asignacion en el constructor no dispare un error falso.
        /// </summary>
        public TimeSpan HoraInicio
        {
            get { return horaInicio; }
            set
            {
                if (value < TimeSpan.Zero || value >= TimeSpan.FromHours(24))
                    throw new ArgumentOutOfRangeException(nameof(HoraInicio),
                        "La hora de inicio debe estar entre 00:00 y 23:59.");

                if (horaFin != TimeSpan.Zero && value >= horaFin)
                    throw new ArgumentException(
                        "La hora de inicio debe ser anterior a la hora de fin.");

                horaInicio = value;
            }
        }

        /// <summary>Hora de fin. Valida que quede despues de HoraInicio.</summary>
        public TimeSpan HoraFin
        {
            get { return horaFin; }
            set
            {
                // El tope es 23:59 y no 24:00 porque el formato de guardado es "hh:mm":
                // un TimeSpan de 24 horas se guardaria como "00:00" y corromperia el archivo.
                if (value <= TimeSpan.Zero || value >= TimeSpan.FromHours(24))
                    throw new ArgumentOutOfRangeException(nameof(HoraFin),
                        "La hora de fin debe estar entre 00:01 y 23:59.");

                if (horaInicio != TimeSpan.Zero && value <= horaInicio)
                    throw new ArgumentException(
                        "La hora de fin no puede ser anterior o igual a la hora de inicio.");

                horaFin = value;
            }
        }

        /// <summary>Motivo de la reserva. Es opcional: si no se indica, queda un texto por defecto.</summary>
        public string Motivo
        {
            get { return motivo; }
            set
            {
                string v = string.IsNullOrWhiteSpace(value) ? "Sin motivo especificado" : value.Trim();
                if (v.Contains("|"))
                    throw new ArgumentException("El motivo no puede contener el caracter '|'.");
                motivo = v;
            }
        }

        public EstadoReserva Estado
        {
            get { return estado; }
            set { estado = value; }
        }

        // ─── Propiedades CALCULADAS ─────────────────────────────────────────

        /// <summary>Duracion de la reserva. No se guarda: se deduce de las dos horas.</summary>
        public TimeSpan Duracion
        {
            get { return HoraFin - HoraInicio; }
        }

        /// <summary>Duracion expresada en horas, para los reportes (2,5 hs).</summary>
        public double DuracionHoras
        {
            get { return Duracion.TotalHours; }
        }

        /// <summary>True si la franja horaria ya paso respecto del momento actual.</summary>
        public bool EstaVencida
        {
            get { return Fecha.Add(HoraFin) < DateTime.Now; }
        }

        /// <summary>True si la reserva sigue ocupando el laboratorio (no fue cancelada ni vencio).</summary>
        public bool EstaVigente
        {
            get { return Estado != EstadoReserva.Cancelada && !EstaVencida; }
        }

        /// <summary>Texto "14:00 - 16:00" para mostrar en la grilla.</summary>
        public string RangoHorario
        {
            get { return $"{HoraInicio:hh\\:mm} - {HoraFin:hh\\:mm}"; }
        }

        // ─── Constructores ──────────────────────────────────────────────────

        /// <summary>Constructor sin parametros, requerido por la restriccion where T : new().</summary>
        public Reserva()
        {
            id = string.Empty;
            idLaboratorio = string.Empty;
            legajoUsuario = string.Empty;
            motivo = "Sin motivo especificado";
            fecha = DateTime.Today;
            horaInicio = TimeSpan.Zero;
            horaFin = TimeSpan.Zero;
            estado = EstadoReserva.Pendiente;
        }

        /// <summary>
        /// SOBRECARGA 1 - reserva SIN motivo declarado.
        /// Delega en el constructor completo con : this(...) para no duplicar validaciones.
        /// </summary>
        public Reserva(string id, string idLaboratorio, string legajoUsuario,
                       DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin)
            : this(id, idLaboratorio, legajoUsuario, fecha, horaInicio, horaFin,
                   "Sin motivo especificado", EstadoReserva.Pendiente)
        {
        }

        /// <summary>
        /// SOBRECARGA 2 - reserva CON motivo, estado inicial Pendiente.
        /// Tambien encadena al constructor completo.
        /// </summary>
        public Reserva(string id, string idLaboratorio, string legajoUsuario,
                       DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin, string motivo)
            : this(id, idLaboratorio, legajoUsuario, fecha, horaInicio, horaFin,
                   motivo, EstadoReserva.Pendiente)
        {
        }

        /// <summary>CONSTRUCTOR COMPLETO - el unico que asigna realmente.</summary>
        public Reserva(string id, string idLaboratorio, string legajoUsuario,
                       DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin,
                       string motivo, EstadoReserva estado)
        {
            this.motivo = "Sin motivo especificado";   // valor por defecto antes de validar

            Id = id;
            IdLaboratorio = idLaboratorio;
            LegajoUsuario = legajoUsuario;
            Fecha = fecha;

            // Se asigna primero HoraFin y despues HoraInicio? No hace falta:
            // las validaciones cruzadas contemplan que la otra todavia valga cero.
            HoraInicio = horaInicio;
            HoraFin = horaFin;

            Motivo = motivo;
            Estado = estado;
        }

        // ─── Logica de dominio ──────────────────────────────────────────────

        /// <summary>
        /// Determina si esta reserva pisa el horario de otra.
        /// Dos franjas se superponen si: inicioA &lt; finB  Y  inicioB &lt; finA.
        /// Que una termine justo cuando la otra empieza (14-16 y 16-18) NO es solapamiento.
        /// </summary>
        public bool SeSuperponeCon(Reserva otra)
        {
            if (otra == null) return false;

            // Distinto laboratorio o distinta fecha: nunca chocan.
            if (!string.Equals(IdLaboratorio, otra.IdLaboratorio, StringComparison.OrdinalIgnoreCase))
                return false;
            if (Fecha.Date != otra.Fecha.Date)
                return false;

            // Una reserva cancelada libera el horario.
            if (Estado == EstadoReserva.Cancelada || otra.Estado == EstadoReserva.Cancelada)
                return false;

            // Es la misma reserva (caso de modificacion): no choca consigo misma.
            if (string.Equals(Id, otra.Id, StringComparison.OrdinalIgnoreCase))
                return false;

            return HoraInicio < otra.HoraFin && otra.HoraInicio < HoraFin;
        }

        // ─── IValidable ─────────────────────────────────────────────────────

        public List<string> Validar()
        {
            List<string> errores = new List<string>();

            if (string.IsNullOrWhiteSpace(Id)) errores.Add("El identificador es obligatorio.");
            if (string.IsNullOrWhiteSpace(IdLaboratorio)) errores.Add("Debe seleccionar un laboratorio.");
            if (string.IsNullOrWhiteSpace(LegajoUsuario)) errores.Add("Debe indicarse el usuario solicitante.");
            if (HoraFin <= HoraInicio) errores.Add("La hora de fin debe ser posterior a la de inicio.");
            if (Duracion > TimeSpan.FromHours(6)) errores.Add("Una reserva no puede durar mas de 6 horas.");

            return errores;
        }

        // ─── IEntidad: persistencia en texto ────────────────────────────────
        // Formato: R001|LAB01|A-2024-015|2026-07-15|14:00|16:00|Trabajo Practico N3|Confirmada

        public string ConvertirALinea()
        {
            return string.Join("|",
                Id,
                IdLaboratorio,
                LegajoUsuario,
                Fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture),
                HoraInicio.ToString(@"hh\:mm"),
                HoraFin.ToString(@"hh\:mm"),
                Motivo,
                Estado.ToString());
        }

        public void CargarDesdeLinea(string linea)
        {
            string[] campos = linea.Split('|');

            if (campos.Length != 8)
                throw new ArchivoDatosCorruptoException(
                    $"Se esperaban 8 campos y se encontraron {campos.Length} en la linea: \"{linea}\"");

            try
            {
                Id = campos[0];
                IdLaboratorio = campos[1];
                LegajoUsuario = campos[2];
                Fecha = DateTime.ParseExact(campos[3], FormatoFecha, CultureInfo.InvariantCulture);

                // Se limpian las horas antes de asignar para que la validacion cruzada
                // no compare contra los valores viejos del objeto.
                horaInicio = TimeSpan.Zero;
                horaFin = TimeSpan.Zero;
                HoraInicio = TimeSpan.ParseExact(campos[4], @"hh\:mm", CultureInfo.InvariantCulture);
                HoraFin = TimeSpan.ParseExact(campos[5], @"hh\:mm", CultureInfo.InvariantCulture);

                Motivo = campos[6];
                Estado = (EstadoReserva)Enum.Parse(typeof(EstadoReserva), campos[7], true);
            }
            catch (Exception ex) when (ex is FormatException ||
                                       ex is ArgumentException ||
                                       ex is OverflowException)
            {
                throw new ArchivoDatosCorruptoException(
                    $"No se pudo interpretar la linea de reserva: \"{linea}\"", ex);
            }
        }

        public override string ToString()
        {
            return $"{Id} | {IdLaboratorio} | {Fecha:dd/MM/yyyy} | {RangoHorario} | {Estado}";
        }
    }
}
