namespace SistemaReservas.Modelos
{
    /// <summary>
    /// Estados posibles de una reserva. Se usa un enum en lugar de un string
    /// para que el compilador impida estados invalidos ("Confirmda", "CONFIRMADA"...).
    /// </summary>
    public enum EstadoReserva
    {
        Pendiente,
        Confirmada,
        Cancelada
    }
}
