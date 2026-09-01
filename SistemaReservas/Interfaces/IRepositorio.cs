using System;
using System.Collections.Generic;

namespace SistemaReservas.Interfaces
{
    /// <summary>
    /// Contrato de acceso a datos (patron Repositorio), independiente de DONDE se
    /// guarden los datos. Hoy la implementacion es RepositorioArchivo&lt;T&gt; (archivos .txt);
    /// si mañana hubiera que pasar a base de datos, se escribe otra implementacion
    /// y el resto del sistema no se toca. Eso es programar contra la interfaz.
    ///
    /// La restriccion "where T : IEntidad" garantiza que cualquier tipo que se use
    /// tenga Id, ConvertirALinea() y CargarDesdeLinea().
    /// </summary>
    /// <typeparam name="T">Tipo de entidad que administra el repositorio.</typeparam>
    public interface IRepositorio<T> where T : IEntidad
    {
        /// <summary>Da de alta una entidad. Lanza EntidadDuplicadaException si el Id ya existe.</summary>
        void Agregar(T entidad);

        /// <summary>Devuelve la entidad con ese Id, o null si no existe.</summary>
        T Obtener(string id);

        /// <summary>Devuelve todas las entidades cargadas.</summary>
        List<T> Listar();

        /// <summary>Reemplaza la entidad existente con el mismo Id. Lanza EntidadNoEncontradaException.</summary>
        void Actualizar(T entidad);

        /// <summary>Elimina la entidad con ese Id. Lanza EntidadNoEncontradaException.</summary>
        void Eliminar(string id);
    }
}
