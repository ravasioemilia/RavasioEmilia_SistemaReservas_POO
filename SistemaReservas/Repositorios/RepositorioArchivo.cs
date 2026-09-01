using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SistemaReservas.Excepciones;
using SistemaReservas.Interfaces;

namespace SistemaReservas.Repositorios
{
    /// <summary>
    /// UNICA clase de acceso a datos del sistema. Persiste cualquier entidad en un
    /// archivo de texto delimitado por '|', sin duplicar codigo de lectura/escritura.
    ///
    /// Se instancia una vez por entidad:
    ///     new RepositorioArchivo&lt;Laboratorio&gt;("laboratorios.txt")
    ///     new RepositorioArchivo&lt;Reserva&gt;("reservas.txt")
    ///     new RepositorioArchivo&lt;Usuario&gt;("usuarios.txt", Usuario.CrearSegunRol)
    ///
    /// TEMAS APLICADOS:
    ///  - GENERICOS: una sola clase T para las tres entidades, con restriccion de tipo.
    ///  - INTERFACES: implementa IRepositorio&lt;T&gt; e IDisposable.
    ///  - IDISPOSABLE + DESTRUCTOR: patron Dispose(bool) del apunte 02.
    ///  - EXCEPCIONES: errores de archivo envueltos en excepciones propias.
    ///  - DELEGADOS: la fabrica de objetos se recibe como Func&lt;string, T&gt;.
    ///  - LINQ y LAMBDAS: en Obtener, Existe y Buscar.
    ///  - SYSTEM.IO: StreamReader / StreamWriter dentro de bloques using.
    /// </summary>
    /// <typeparam name="T">
    /// Entidad a persistir. La restriccion exige que sea una clase que cumpla IEntidad,
    /// es decir, que sepa convertirse a linea de texto y reconstruirse desde una.
    /// </typeparam>
    public class RepositorioArchivo<T> : IRepositorio<T>, IDisposable where T : class, IEntidad
    {
        // ─── Campos privados ────────────────────────────────────────────────

        private readonly string rutaArchivo;

        /// <summary>Cache en memoria: se lee el archivo una vez y se trabaja sobre la lista.</summary>
        private readonly List<T> entidades;

        /// <summary>
        /// DELEGADO fabrica: dice como crear un objeto vacio de tipo T a partir de la linea.
        /// Para Laboratorio y Reserva alcanza con el constructor sin parametros; para
        /// Usuario (que es abstracta) hay que mirar el rol, y por eso se puede inyectar.
        /// </summary>
        private readonly Func<string, T> fabrica;

        /// <summary>Bandera del patron Dispose: evita la doble liberacion.</summary>
        private bool disposed = false;

        /// <summary>True si hay altas/bajas/modificaciones que todavia no se escribieron a disco.</summary>
        private bool hayCambiosPendientes = false;

        // ─── Propiedades publicas ───────────────────────────────────────────

        /// <summary>Ruta completa del archivo administrado.</summary>
        public string RutaArchivo
        {
            get { return rutaArchivo; }
        }

        /// <summary>Nombre del tipo administrado ("Laboratorio", "Reserva"...). Para los mensajes de error.</summary>
        public string NombreEntidad
        {
            get { return typeof(T).Name; }
        }

        /// <summary>Cantidad de entidades actualmente en memoria.</summary>
        public int Cantidad
        {
            get { return entidades.Count; }
        }

        /// <summary>
        /// Lineas que no se pudieron interpretar durante la carga.
        /// El sistema NO se cae por una linea corrupta: la saltea, la registra aca,
        /// y sigue cargando el resto. La pantalla principal puede avisarle al administrador.
        /// </summary>
        public List<ArchivoDatosCorruptoException> ErroresDeCarga { get; private set; }

        /// <summary>True si al abrir el archivo hubo al menos una linea invalida.</summary>
        public bool HuboErroresDeCarga
        {
            get { return ErroresDeCarga.Count > 0; }
        }

        // ─── Constructores (sobrecarga + encadenamiento con : this) ─────────

        /// <summary>
        /// Constructor para entidades CONCRETAS (Laboratorio, Reserva).
        /// Usa como fabrica el constructor sin parametros del propio tipo.
        /// Encadena con : this(...) al constructor completo.
        /// </summary>
        public RepositorioArchivo(string rutaArchivo)
            : this(rutaArchivo, linea => Activator.CreateInstance<T>())
        {
        }

        /// <summary>
        /// Constructor completo. Recibe la fabrica como delegado, lo que permite
        /// persistir jerarquias completas: para Usuario se pasa Usuario.CrearSegunRol,
        /// que devuelve un Alumno, un Docente o un Administrador segun la linea.
        /// </summary>
        public RepositorioArchivo(string rutaArchivo, Func<string, T> fabrica)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo))
                throw new ArgumentException("Debe indicarse la ruta del archivo de datos.");
            if (fabrica == null)
                throw new ArgumentNullException(nameof(fabrica),
                    "Debe indicarse como construir las entidades leidas del archivo.");

            this.rutaArchivo = rutaArchivo;
            this.fabrica = fabrica;
            this.entidades = new List<T>();
            this.ErroresDeCarga = new List<ArchivoDatosCorruptoException>();

            CargarDesdeArchivo();
        }

        // ─── Lectura y escritura del archivo (System.IO) ────────────────────

        /// <summary>
        /// Lee el archivo completo y reconstruye los objetos en memoria.
        /// Si el archivo no existe (primer uso del sistema) lo crea vacio en lugar de fallar.
        /// </summary>
        public void CargarDesdeArchivo()
        {
            VerificarNoLiberado();

            entidades.Clear();
            ErroresDeCarga.Clear();

            try
            {
                // Primer uso: no hay archivo todavia. Se crea vacio y se sigue.
                if (!File.Exists(rutaArchivo))
                {
                    string carpeta = Path.GetDirectoryName(rutaArchivo);
                    if (!string.IsNullOrEmpty(carpeta) && !Directory.Exists(carpeta))
                        Directory.CreateDirectory(carpeta);

                    using (File.Create(rutaArchivo)) { }   // se cierra de inmediato
                    return;
                }

                // using garantiza que el StreamReader se cierre aunque haya excepcion.
                using (StreamReader lector = new StreamReader(rutaArchivo, Encoding.UTF8))
                {
                    string linea;
                    int numeroLinea = 0;

                    while ((linea = lector.ReadLine()) != null)
                    {
                        numeroLinea++;

                        // Se ignoran lineas en blanco y comentarios que empiezan con #
                        if (string.IsNullOrWhiteSpace(linea)) continue;
                        if (linea.TrimStart().StartsWith("#")) continue;

                        try
                        {
                            T entidad = fabrica(linea);      // delegado: crea el objeto adecuado
                            entidad.CargarDesdeLinea(linea); // el objeto se hidrata a si mismo
                            entidades.Add(entidad);
                        }
                        catch (ArchivoDatosCorruptoException ex)
                        {
                            // Linea invalida: se registra y se continua con la siguiente.
                            ErroresDeCarga.Add(new ArchivoDatosCorruptoException(
                                Path.GetFileName(rutaArchivo), numeroLinea, linea, ex));
                        }
                        catch (Exception ex)
                        {
                            ErroresDeCarga.Add(new ArchivoDatosCorruptoException(
                                Path.GetFileName(rutaArchivo), numeroLinea, linea, ex));
                        }
                    }
                }

                hayCambiosPendientes = false;
            }
            catch (IOException ex)
            {
                throw new SistemaReservasException(
                    $"No se pudo leer el archivo de {NombreEntidad}: {rutaArchivo}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new SistemaReservasException(
                    $"Sin permisos para leer el archivo de {NombreEntidad}: {rutaArchivo}", ex);
            }
        }

        /// <summary>
        /// Escribe TODAS las entidades al archivo, pisando el contenido anterior.
        /// Antes de pisar se hace una copia .bak, para no perder los datos si algo falla.
        /// </summary>
        public void GuardarEnArchivo()
        {
            VerificarNoLiberado();

            try
            {
                // Respaldo previo: si el guardado falla a mitad de camino, el .bak salva los datos.
                if (File.Exists(rutaArchivo))
                    File.Copy(rutaArchivo, rutaArchivo + ".bak", true);

                using (StreamWriter escritor = new StreamWriter(rutaArchivo, false, Encoding.UTF8))
                {
                    foreach (T entidad in entidades)
                    {
                        escritor.WriteLine(entidad.ConvertirALinea());
                    }
                }   // Dispose() del StreamWriter aca: cierra y vacia el buffer a disco

                hayCambiosPendientes = false;
            }
            catch (IOException ex)
            {
                throw new SistemaReservasException(
                    $"No se pudo guardar el archivo de {NombreEntidad}: {rutaArchivo}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new SistemaReservasException(
                    $"Sin permisos para escribir el archivo de {NombreEntidad}: {rutaArchivo}", ex);
            }
        }

        // ─── IRepositorio<T> ────────────────────────────────────────────────

        public void Agregar(T entidad)
        {
            VerificarNoLiberado();

            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            if (Existe(entidad.Id))
                throw new EntidadDuplicadaException(NombreEntidad, entidad.Id);

            entidades.Add(entidad);
            hayCambiosPendientes = true;
            GuardarEnArchivo();
        }

        /// <summary>Busca por Id con LINQ. Devuelve null si no existe.</summary>
        public T Obtener(string id)
        {
            VerificarNoLiberado();

            if (string.IsNullOrWhiteSpace(id)) return null;

            return entidades.FirstOrDefault(
                e => string.Equals(e.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Devuelve una COPIA de la lista. Se copia a proposito: si se devolviera la
        /// lista interna, cualquier formulario podria agregar o borrar elementos
        /// salteandose las validaciones del repositorio.
        /// </summary>
        public List<T> Listar()
        {
            VerificarNoLiberado();
            return new List<T>(entidades);
        }

        public void Actualizar(T entidad)
        {
            VerificarNoLiberado();

            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            int indice = entidades.FindIndex(
                e => string.Equals(e.Id, entidad.Id, StringComparison.OrdinalIgnoreCase));

            if (indice < 0)
                throw new EntidadNoEncontradaException(NombreEntidad, entidad.Id);

            entidades[indice] = entidad;
            hayCambiosPendientes = true;
            GuardarEnArchivo();
        }

        public void Eliminar(string id)
        {
            VerificarNoLiberado();

            T entidad = Obtener(id);

            if (entidad == null)
                throw new EntidadNoEncontradaException(NombreEntidad, id);

            entidades.Remove(entidad);
            hayCambiosPendientes = true;
            GuardarEnArchivo();
        }

        // ─── Consultas adicionales (lambdas) ────────────────────────────────

        /// <summary>True si ya hay una entidad con ese Id.</summary>
        public bool Existe(string id)
        {
            VerificarNoLiberado();

            if (string.IsNullOrWhiteSpace(id)) return false;

            return entidades.Any(
                e => string.Equals(e.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Filtra con la condicion que le pase el llamador, en forma de expresion lambda:
        ///     repoReservas.Buscar(r =&gt; r.Estado == EstadoReserva.Confirmada)
        /// </summary>
        public List<T> Buscar(Func<T, bool> condicion)
        {
            VerificarNoLiberado();

            if (condicion == null)
                throw new ArgumentNullException(nameof(condicion));

            return entidades.Where(condicion).ToList();
        }

        // ─── Patron IDisposable (apunte 02) ─────────────────────────────────

        /// <summary>Impide seguir usando el repositorio despues de liberarlo.</summary>
        private void VerificarNoLiberado()
        {
            if (disposed)
                throw new ObjectDisposedException($"RepositorioArchivo<{NombreEntidad}>");
        }

        /// <summary>
        /// Liberacion DETERMINISTICA: se llama sola al salir de un bloque using,
        /// o de forma explicita al cerrar la aplicacion.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);   // ya se libero: el GC no necesita llamar al destructor
        }

        /// <param name="disposing">
        /// true  = lo llamo el programador (hay que liberar recursos administrados).
        /// false = lo llamo el GC desde el destructor (los administrados ya pueden no existir).
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Recursos administrados: se vuelca a disco lo que quedo pendiente.
                    try
                    {
                        if (hayCambiosPendientes)
                            GuardarEnArchivo();
                    }
                    catch (Exception)
                    {
                        // Nunca se propaga una excepcion desde Dispose: dejaria la
                        // aplicacion sin poder cerrarse. Los datos quedan en el .bak.
                    }

                    entidades.Clear();
                    ErroresDeCarga.Clear();
                }

                // Recursos NO administrados: en esta clase no hay handles propios,
                // porque los StreamReader/StreamWriter se cierran con using apenas se usan.

                disposed = true;
            }
        }

        /// <summary>
        /// DESTRUCTOR - red de seguridad. Solo lo ejecuta el Garbage Collector si
        /// alguien se olvido de llamar a Dispose(). Recibe false porque en ese momento
        /// no es seguro tocar otros objetos administrados.
        /// </summary>
        ~RepositorioArchivo()
        {
            Dispose(false);
        }
    }
}
