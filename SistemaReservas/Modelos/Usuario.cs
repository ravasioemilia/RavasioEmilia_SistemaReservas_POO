using System;
using System.Collections.Generic;
using SistemaReservas.Excepciones;
using SistemaReservas.Interfaces;

namespace SistemaReservas.Modelos
{
    /// <summary>
    /// Clase BASE ABSTRACTA de la jerarquia de usuarios.
    ///
    /// Es abstracta porque "un usuario" a secas no existe en el sistema: siempre es
    /// un Administrador, un Docente o un Alumno. No tiene sentido poder hacer
    /// new Usuario(), y el compilador lo impide.
    ///
    /// PUNTO CLAVE DE LA CONSIGNA: las reglas de cada rol NO se resuelven con
    /// if / switch sobre un campo "TipoUsuario", sino sobrescribiendo estos metodos
    /// en cada clase derivada. El resto del sistema llama siempre a
    /// usuario.ObtenerLimiteReservasSemanales() sin preguntar de que tipo es.
    /// </summary>
    public abstract class Usuario : IEntidad, IValidable
    {
        // ─── Campos privados ────────────────────────────────────────────────
        private string legajo;
        private string nombre;
        private string apellido;
        private string email;
        private bool activo;

        // ─── Propiedades con validacion ─────────────────────────────────────

        /// <summary>Legajo: identificador unico del usuario (A-2024-015, D-2024-003...).</summary>
        public string Legajo
        {
            get { return legajo; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El legajo no puede estar vacio.");
                if (value.Contains("|"))
                    throw new ArgumentException("El legajo no puede contener el caracter '|'.");
                legajo = value.Trim().ToUpper();
            }
        }

        /// <summary>Implementacion de IEntidad.Id: para un usuario, el Id es su legajo.</summary>
        public string Id
        {
            get { return Legajo; }
        }

        public string Nombre
        {
            get { return nombre; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El nombre no puede estar vacio.");
                if (value.Contains("|"))
                    throw new ArgumentException("El nombre no puede contener el caracter '|'.");
                nombre = value.Trim();
            }
        }

        public string Apellido
        {
            get { return apellido; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El apellido no puede estar vacio.");
                if (value.Contains("|"))
                    throw new ArgumentException("El apellido no puede contener el caracter '|'.");
                apellido = value.Trim();
            }
        }

        /// <summary>Email con una validacion minima de formato.</summary>
        public string Email
        {
            get { return email; }
            set
            {
                string v = value == null ? string.Empty : value.Trim();

                if (string.IsNullOrWhiteSpace(v))
                    throw new ArgumentException("El email no puede estar vacio.");
                if (v.Contains("|"))
                    throw new ArgumentException("El email no puede contener el caracter '|'.");
                if (!v.Contains("@") || !v.Contains("."))
                    throw new ArgumentException($"El email '{v}' no tiene un formato valido.");

                email = v;
            }
        }

        public bool Activo
        {
            get { return activo; }
            set { activo = value; }
        }

        // ─── Propiedad calculada ────────────────────────────────────────────

        public string NombreCompleto
        {
            get { return $"{Apellido}, {Nombre}"; }
        }

        // ─── Miembros ABSTRACTOS: cada rol los resuelve a su manera ─────────

        /// <summary>Nombre del rol. Se usa para persistirlo y para mostrarlo en pantalla.</summary>
        public abstract string Rol { get; }

        /// <summary>Cuantas reservas puede tener el usuario por semana.</summary>
        public abstract int ObtenerLimiteReservasSemanales();

        /// <summary>Con cuantos dias de anticipacion como maximo puede reservar.</summary>
        public abstract int ObtenerAnticipacionMaximaDias();

        // ─── Miembros VIRTUALES: hay un comportamiento por defecto ──────────

        /// <summary>
        /// Regla por defecto (Docente y Alumno): solo se pueden cancelar las reservas propias.
        /// Administrador la sobrescribe para poder cancelar cualquiera.
        /// </summary>
        public virtual bool PuedeCancelar(Reserva reserva)
        {
            if (reserva == null) return false;
            return string.Equals(reserva.LegajoUsuario, Legajo, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Solo el Administrador gestiona el ABM de laboratorios y usuarios.</summary>
        public virtual bool PuedeGestionarLaboratorios()
        {
            return false;
        }

        /// <summary>Texto explicativo de los permisos del rol, para mostrar en la pantalla principal.</summary>
        public virtual string DescribirPermisos()
        {
            return $"{Rol}: hasta {ObtenerLimiteReservasSemanales()} reserva(s) por semana, " +
                   $"con un maximo de {ObtenerAnticipacionMaximaDias()} dias de anticipacion.";
        }

        // ─── Constructores ──────────────────────────────────────────────────

        /// <summary>
        /// Constructor sin parametros. Aunque la clase sea abstracta, las derivadas
        /// lo invocan implicitamente, y el repositorio necesita poder crear el objeto vacio.
        /// </summary>
        protected Usuario()
        {
            legajo = string.Empty;
            nombre = string.Empty;
            apellido = string.Empty;
            email = string.Empty;
            activo = true;
        }

        /// <summary>Constructor completo. Las derivadas lo llaman con : base(...).</summary>
        protected Usuario(string legajo, string nombre, string apellido, string email, bool activo)
        {
            Legajo = legajo;
            Nombre = nombre;
            Apellido = apellido;
            Email = email;
            Activo = activo;
        }

        // ─── IValidable ─────────────────────────────────────────────────────

        public virtual List<string> Validar()
        {
            List<string> errores = new List<string>();

            if (string.IsNullOrWhiteSpace(Legajo)) errores.Add("El legajo es obligatorio.");
            if (string.IsNullOrWhiteSpace(Nombre)) errores.Add("El nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(Apellido)) errores.Add("El apellido es obligatorio.");
            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@"))
                errores.Add("El email es obligatorio y debe tener formato valido.");

            return errores;
        }

        // ─── IEntidad: persistencia en texto ────────────────────────────────
        // Formato: A-2024-015|Alumno|Perez|Juan|juan.perez@alumnos.edu.ar|True
        //
        // El campo Rol se escribe leyendo la propiedad polimorfica Rol: cada
        // derivada devuelve el suyo. Por eso este metodo NO necesita override.

        public string ConvertirALinea()
        {
            return string.Join("|", Legajo, Rol, Apellido, Nombre, Email, Activo.ToString());
        }

        public void CargarDesdeLinea(string linea)
        {
            string[] campos = linea.Split('|');

            if (campos.Length != 6)
                throw new ArchivoDatosCorruptoException(
                    $"Se esperaban 6 campos y se encontraron {campos.Length} en la linea: \"{linea}\"");

            try
            {
                Legajo = campos[0];
                // campos[1] es el rol: ya se uso en CrearSegunRol para elegir la clase concreta.
                Apellido = campos[2];
                Nombre = campos[3];
                Email = campos[4];
                Activo = bool.Parse(campos[5]);
            }
            catch (Exception ex) when (ex is FormatException ||
                                       ex is ArgumentException ||
                                       ex is OverflowException)
            {
                throw new ArchivoDatosCorruptoException(
                    $"No se pudo interpretar la linea de usuario: \"{linea}\"", ex);
            }
        }

        // ─── FABRICA ────────────────────────────────────────────────────────

        /// <summary>
        /// Crea la instancia CONCRETA que corresponde segun el campo "rol" de la linea.
        ///
        /// Este es el unico lugar de todo el sistema donde se decide en base al nombre del
        /// rol, y es inevitable: alguien tiene que traducir el texto del archivo a un objeto.
        /// A partir de aca todo el resto del codigo trabaja con la clase base Usuario y
        /// deja que el polimorfismo haga el trabajo.
        ///
        /// Se pasa como delegado (Func&lt;string, Usuario&gt;) al repositorio generico.
        /// </summary>
        public static Usuario CrearSegunRol(string linea)
        {
            string[] campos = linea.Split('|');

            if (campos.Length < 2)
                throw new ArchivoDatosCorruptoException(
                    $"La linea de usuario no tiene el campo rol: \"{linea}\"");

            string rol = campos[1].Trim();

            switch (rol.ToLower())
            {
                case "administrador": return new Administrador();
                case "docente": return new Docente();
                case "alumno": return new Alumno();
                default:
                    throw new ArchivoDatosCorruptoException(
                        $"Rol de usuario desconocido: '{rol}' en la linea \"{linea}\".");
            }
        }

        public override string ToString()
        {
            return $"{NombreCompleto} ({Rol} - {Legajo})";
        }
    }
}
