using System;

namespace SistemaReservas.Excepciones
{
    /// <summary>
    /// Clase BASE de toda la jerarquia de excepciones propias del sistema.
    /// Sirve para poder capturar de un solo golpe cualquier error de negocio:
    ///
    ///     catch (SistemaReservasException ex) { ... }   // atrapa todas las nuestras
    ///     catch (Exception ex)                { ... }   // atrapa el resto
    ///
    /// Los cuatro constructores siguen el patron visto en la cursada (apunte 08).
    /// </summary>
    public class SistemaReservasException : Exception
    {
        /// <summary>Momento exacto en que se produjo el error (util para el log).</summary>
        public DateTime FechaHoraError { get; }

        // Constructor 1 - sin mensaje
        public SistemaReservasException() : base()
        {
            FechaHoraError = DateTime.Now;
        }

        // Constructor 2 - con mensaje personalizado
        public SistemaReservasException(string mensaje) : base(mensaje)
        {
            FechaHoraError = DateTime.Now;
        }

        // Constructor 3 - con mensaje + excepcion interna (para no perder la causa original)
        public SistemaReservasException(string mensaje, Exception inner) : base(mensaje, inner)
        {
            FechaHoraError = DateTime.Now;
        }
    }
}
