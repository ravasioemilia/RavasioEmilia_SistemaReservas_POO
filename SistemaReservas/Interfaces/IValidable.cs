using System;
using System.Collections.Generic;

namespace SistemaReservas.Interfaces
{
    /// <summary>
    /// Contrato para entidades que pueden auto-validarse antes de guardarse.
    /// Permite que el formulario pregunte "estas bien?" a cualquier objeto,
    /// sin saber de que tipo es.
    /// </summary>
    public interface IValidable
    {
        /// <summary>
        /// Devuelve la lista de errores encontrados. Lista vacia = el objeto es valido.
        /// </summary>
        List<string> Validar();
    }
}
