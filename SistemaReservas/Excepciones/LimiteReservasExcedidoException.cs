using System;

namespace SistemaReservas.Excepciones
{
    /// <summary>
    /// Se lanza cuando un usuario intenta reservar por encima de su limite semanal.
    /// El limite NO esta escrito aca: lo devuelve el propio usuario por polimorfismo
    /// (Usuario.ObtenerLimiteReservasSemanales).
    /// </summary>
    public class LimiteReservasExcedidoException : SistemaReservasException
    {
        public string LegajoUsuario { get; }
        public int LimiteSemanal { get; }
        public int ReservasActuales { get; }

        // Constructor 1 - sin mensaje
        public LimiteReservasExcedidoException() : base() { }

        // Constructor 2 - con mensaje personalizado
        public LimiteReservasExcedidoException(string mensaje) : base(mensaje) { }

        // Constructor 3 - con mensaje + excepcion interna
        public LimiteReservasExcedidoException(string mensaje, Exception inner)
            : base(mensaje, inner) { }

        // Constructor 4 - especifico del dominio
        public LimiteReservasExcedidoException(string legajo, int limite, int actuales)
            : base($"El usuario {legajo} alcanzo su limite de {limite} reserva(s) por semana " +
                   $"(actualmente tiene {actuales}).")
        {
            LegajoUsuario = legajo;
            LimiteSemanal = limite;
            ReservasActuales = actuales;
        }
    }
}
