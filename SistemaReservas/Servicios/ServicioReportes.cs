using System;
using System.Collections.Generic;
using System.Linq;
using SistemaReservas.Modelos;

namespace SistemaReservas.Servicios
{
    /// <summary>
    /// Genera los reportes del sistema. Todas las consultas estan resueltas con LINQ.
    ///
    /// Se muestran a proposito las DOS sintaxis vistas en la cursada:
    ///  - Sintaxis de METODO (fluida): .Where(...).GroupBy(...).Select(...)
    ///  - Sintaxis de CONSULTA (declarativa): from ... group ... by ... select ...
    /// </summary>
    public class ServicioReportes
    {
        private readonly GestorReservas gestor;

        public ServicioReportes(GestorReservas gestor)
        {
            if (gestor == null)
                throw new ArgumentNullException(nameof(gestor));

            this.gestor = gestor;
        }

        // ─── Reporte 1: reservas de un dia ──────────────────────────────────

        /// <summary>
        /// Reservas vigentes de una fecha, ordenadas por laboratorio y hora.
        /// Sintaxis de CONSULTA (from / where / orderby / select).
        /// </summary>
        public List<Reserva> ReservasDelDia(DateTime fecha)
        {
            var consulta = from r in gestor.Reservas.Listar()
                           where r.Fecha.Date == fecha.Date
                              && r.Estado != EstadoReserva.Cancelada
                           orderby r.IdLaboratorio, r.HoraInicio
                           select r;

            return consulta.ToList();
        }

        /// <summary>Reservas de hoy. Atajo del anterior.</summary>
        public List<Reserva> ReservasDeHoy()
        {
            return ReservasDelDia(DateTime.Today);
        }

        // ─── Reporte 2: uso por laboratorio (GroupBy) ───────────────────────

        /// <summary>
        /// Agrupa las reservas por laboratorio y calcula cantidad, horas totales y
        /// promedio. Es el reporte con GroupBy que pide la consigna.
        ///
        /// Se hace un join contra la lista de laboratorios para poder mostrar el
        /// nombre y no solo el codigo.
        /// </summary>
        public List<UsoLaboratorio> UsoPorLaboratorio()
        {
            List<Laboratorio> laboratorios = gestor.Laboratorios.Listar();

            return gestor.Reservas.Listar()
                .GroupBy(r => r.IdLaboratorio)               // clave: el laboratorio
                .Select(grupo => new UsoLaboratorio
                {
                    IdLaboratorio = grupo.Key,

                    NombreLaboratorio = laboratorios
                        .Where(l => l.Id == grupo.Key)
                        .Select(l => l.Nombre)
                        .FirstOrDefault() ?? "(laboratorio eliminado)",

                    // Las canceladas no cuentan como uso real del laboratorio.
                    CantidadReservas = grupo.Count(r => r.Estado != EstadoReserva.Cancelada),

                    HorasTotales = grupo
                        .Where(r => r.Estado != EstadoReserva.Cancelada)
                        .Sum(r => r.DuracionHoras),

                    PromedioHorasPorReserva = grupo
                        .Where(r => r.Estado != EstadoReserva.Cancelada)
                        .Select(r => r.DuracionHoras)
                        .DefaultIfEmpty(0)                    // evita dividir por cero
                        .Average(),

                    CantidadCanceladas = grupo.Count(r => r.Estado == EstadoReserva.Cancelada)
                })
                .OrderByDescending(u => u.CantidadReservas)
                .ToList();
        }

        // ─── Reporte 3: ranking de laboratorios mas solicitados ─────────────

        /// <summary>
        /// Los N laboratorios con mas reservas. Reutiliza el reporte anterior:
        /// ya viene ordenado de mayor a menor, solo hay que quedarse con los primeros.
        /// </summary>
        public List<UsoLaboratorio> RankingLaboratorios(int cantidad)
        {
            if (cantidad <= 0) cantidad = 3;

            return UsoPorLaboratorio()
                .Where(u => u.CantidadReservas > 0)
                .Take(cantidad)
                .ToList();
        }

        // ─── Reporte 4: ranking de usuarios (GroupBy + join) ────────────────

        /// <summary>
        /// Agrupa las reservas por usuario y las cruza con la lista de usuarios
        /// para mostrar nombre y rol. Usa la sintaxis de consulta con join y group by.
        /// </summary>
        public List<UsoUsuario> RankingUsuarios()
        {
            var consulta = from r in gestor.Reservas.Listar()
                           where r.Estado != EstadoReserva.Cancelada
                           join u in gestor.Usuarios.Listar()
                                on r.LegajoUsuario equals u.Legajo
                           group new { r, u } by u into grupo
                           select new UsoUsuario
                           {
                               Legajo = grupo.Key.Legajo,
                               NombreCompleto = grupo.Key.NombreCompleto,
                               Rol = grupo.Key.Rol,
                               CantidadReservas = grupo.Count(),
                               HorasTotales = grupo.Sum(x => x.r.DuracionHoras)
                           };

            return consulta.OrderByDescending(u => u.CantidadReservas).ToList();
        }

        // ─── Reporte 5: resumen por estado ──────────────────────────────────

        /// <summary>Cuantas reservas hay en cada estado y que porcentaje representan.</summary>
        public List<ResumenEstado> ResumenPorEstado()
        {
            List<Reserva> todas = gestor.Reservas.Listar();
            int total = todas.Count;

            if (total == 0) return new List<ResumenEstado>();

            return todas
                .GroupBy(r => r.Estado)
                .Select(grupo => new ResumenEstado
                {
                    Estado = grupo.Key.ToString(),
                    Cantidad = grupo.Count(),
                    PorcentajeSobreTotal = Math.Round(grupo.Count() * 100.0 / total, 1)
                })
                .OrderByDescending(e => e.Cantidad)
                .ToList();
        }

        // ─── Consulta 6: franjas libres de un laboratorio ───────────────────

        /// <summary>
        /// Calcula los huecos libres de un laboratorio en una fecha, dentro del
        /// horario de la institucion (08:00 a 22:00).
        ///
        /// Se recorren las reservas ya ordenadas por hora y se van tomando los
        /// espacios que quedan entre una y la siguiente.
        /// </summary>
        public List<FranjaLibre> FranjasLibres(string idLaboratorio, DateTime fecha)
        {
            TimeSpan apertura = new TimeSpan(8, 0, 0);
            TimeSpan cierre = new TimeSpan(22, 0, 0);

            List<Reserva> ocupadas = gestor.ObtenerReservasDelLaboratorio(idLaboratorio, fecha);
            List<FranjaLibre> libres = new List<FranjaLibre>();

            TimeSpan cursor = apertura;

            foreach (Reserva r in ocupadas.OrderBy(r => r.HoraInicio))
            {
                if (r.HoraInicio > cursor)
                {
                    libres.Add(new FranjaLibre { Desde = cursor, Hasta = r.HoraInicio });
                }

                // Si dos reservas se pisaran, el cursor se queda en el fin mas tardio.
                if (r.HoraFin > cursor) cursor = r.HoraFin;
            }

            if (cursor < cierre)
                libres.Add(new FranjaLibre { Desde = cursor, Hasta = cierre });

            return libres;
        }

        // ─── Armado de filas para la grilla ─────────────────────────────────

        /// <summary>
        /// Convierte una lista de reservas en filas listas para la grilla, cruzando
        /// con laboratorios y usuarios para mostrar nombres en lugar de codigos.
        ///
        /// No se usa join porque un join deja afuera las reservas cuyo laboratorio o
        /// usuario ya no existen. Con FirstOrDefault se resuelve el caso: si no se
        /// encuentra, se muestra el codigo original y la reserva sigue apareciendo.
        /// </summary>
        public List<FilaReserva> ArmarFilas(List<Reserva> reservas)
        {
            if (reservas == null) return new List<FilaReserva>();

            List<Laboratorio> laboratorios = gestor.Laboratorios.Listar();
            List<Usuario> usuarios = gestor.Usuarios.Listar();

            return reservas.Select(r =>
            {
                Laboratorio lab = laboratorios
                    .FirstOrDefault(l => l.Id == r.IdLaboratorio);

                Usuario usu = usuarios
                    .FirstOrDefault(u => u.Legajo == r.LegajoUsuario);

                return new FilaReserva
                {
                    Id = r.Id,
                    Laboratorio = lab != null ? $"{lab.Id} - {lab.Nombre}" : r.IdLaboratorio,
                    Usuario = usu != null ? $"{usu.NombreCompleto} ({usu.Rol})" : r.LegajoUsuario,
                    Fecha = r.Fecha.ToString("dd/MM/yyyy"),
                    Horario = r.RangoHorario,
                    Duracion = $"{r.DuracionHoras:0.#} hs",
                    Motivo = r.Motivo,
                    Estado = r.Estado.ToString(),
                    Original = r
                };
            })
            .ToList();
        }

        // ─── Consulta 7: reservas proximas a vencer ─────────────────────────

        /// <summary>Reservas vigentes de los proximos N dias. Usa una lambda como filtro.</summary>
        public List<Reserva> ReservasProximas(int dias)
        {
            DateTime desde = DateTime.Today;
            DateTime hasta = DateTime.Today.AddDays(dias);

            return gestor.Reservas.Listar()
                .Where(r => r.Fecha.Date >= desde && r.Fecha.Date <= hasta)
                .Where(r => r.Estado != EstadoReserva.Cancelada)
                .OrderBy(r => r.Fecha)
                .ThenBy(r => r.HoraInicio)
                .ToList();
        }
    }
}
