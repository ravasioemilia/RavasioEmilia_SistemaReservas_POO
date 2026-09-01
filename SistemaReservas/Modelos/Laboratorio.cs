using System;
using System.Collections.Generic;
using System.Globalization;
using SistemaReservas.Excepciones;
using SistemaReservas.Interfaces;

namespace SistemaReservas.Modelos
{
    /// <summary>
    /// Representa un laboratorio de informatica fisico de la institucion.
    ///
    /// TEMAS APLICADOS:
    ///  - Campos privados + propiedades con validacion (encapsulamiento).
    ///  - Propiedad calculada (Descripcion, EsAptoParaGrupos).
    ///  - Constructores sobrecargados con encadenamiento : this(...).
    ///  - Implementa IEntidad (persistencia) e IValidable (auto-validacion).
    /// </summary>
    public class Laboratorio : IEntidad, IValidable
    {
        // ─── Campos privados ────────────────────────────────────────────────
        private string id;
        private string nombre;
        private string ubicacion;
        private int capacidadPCs;
        private string equipamiento;
        private bool activo;

        // ─── Propiedades con validacion ─────────────────────────────────────

        /// <summary>Identificador unico, por ejemplo "LAB01". No puede estar vacio.</summary>
        public string Id
        {
            get { return id; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El identificador del laboratorio no puede estar vacio.");
                if (value.Contains("|"))
                    throw new ArgumentException("El identificador no puede contener el caracter '|'.");
                id = value.Trim().ToUpper();
            }
        }

        public string Nombre
        {
            get { return nombre; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("El nombre del laboratorio no puede estar vacio.");
                if (value.Contains("|"))
                    throw new ArgumentException("El nombre no puede contener el caracter '|'.");
                nombre = value.Trim();
            }
        }

        public string Ubicacion
        {
            get { return ubicacion; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("La ubicacion no puede estar vacia.");
                if (value.Contains("|"))
                    throw new ArgumentException("La ubicacion no puede contener el caracter '|'.");
                ubicacion = value.Trim();
            }
        }

        /// <summary>Cantidad de PCs. Debe ser un valor razonable (1 a 200).</summary>
        public int CapacidadPCs
        {
            get { return capacidadPCs; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(CapacidadPCs),
                        "La capacidad debe ser mayor a cero.");
                if (value > 200)
                    throw new ArgumentOutOfRangeException(nameof(CapacidadPCs),
                        "La capacidad no puede superar las 200 PCs.");
                capacidadPCs = value;
            }
        }

        /// <summary>Equipamiento disponible (proyector, aire, etc.). Puede estar vacio.</summary>
        public string Equipamiento
        {
            get { return equipamiento; }
            set
            {
                string v = value == null ? string.Empty : value.Trim();
                if (v.Contains("|"))
                    throw new ArgumentException("El equipamiento no puede contener el caracter '|'.");
                equipamiento = v;
            }
        }

        /// <summary>Un laboratorio dado de baja no aparece para reservar, pero no se borra.</summary>
        public bool Activo
        {
            get { return activo; }
            set { activo = value; }
        }

        // ─── Propiedades CALCULADAS (no se guardan en el archivo) ───────────

        /// <summary>Texto listo para mostrar en un ComboBox.</summary>
        public string Descripcion
        {
            get { return $"{Id} - {Nombre} ({CapacidadPCs} PCs)"; }
        }

        /// <summary>Regla simple del dominio: sirve para comisiones numerosas.</summary>
        public bool EsAptoParaGrupos
        {
            get { return CapacidadPCs >= 20; }
        }

        // ─── Constructores (sobrecarga + encadenamiento con : this) ─────────

        /// <summary>
        /// Constructor sin parametros. Es OBLIGATORIO porque el repositorio generico
        /// tiene la restriccion where T : new() y necesita poder hacer new T().
        /// </summary>
        public Laboratorio()
        {
            id = string.Empty;
            nombre = string.Empty;
            ubicacion = string.Empty;
            equipamiento = string.Empty;
            capacidadPCs = 1;
            activo = true;
        }

        /// <summary>
        /// Constructor basico: laboratorio sin equipamiento declarado y activo.
        /// Encadena al constructor completo con : this(...) para no repetir codigo.
        /// </summary>
        public Laboratorio(string id, string nombre, string ubicacion, int capacidadPCs)
            : this(id, nombre, ubicacion, capacidadPCs, string.Empty, true)
        {
        }

        /// <summary>Constructor completo. Es el unico que realmente asigna.</summary>
        public Laboratorio(string id, string nombre, string ubicacion, int capacidadPCs,
                           string equipamiento, bool activo)
        {
            // Se asigna por las PROPIEDADES (no por los campos) para que corran las validaciones.
            Id = id;
            Nombre = nombre;
            Ubicacion = ubicacion;
            CapacidadPCs = capacidadPCs;
            Equipamiento = equipamiento;
            Activo = activo;
        }

        // ─── IValidable ─────────────────────────────────────────────────────

        public List<string> Validar()
        {
            List<string> errores = new List<string>();

            if (string.IsNullOrWhiteSpace(Id)) errores.Add("El identificador es obligatorio.");
            if (string.IsNullOrWhiteSpace(Nombre)) errores.Add("El nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(Ubicacion)) errores.Add("La ubicacion es obligatoria.");
            if (CapacidadPCs <= 0) errores.Add("La capacidad debe ser mayor a cero.");

            return errores;
        }

        // ─── IEntidad: persistencia en texto ────────────────────────────────
        // Formato: LAB01|Laboratorio de Redes|Planta Baja - Ala Norte|25|Proyector, Switch Cisco|True

        public string ConvertirALinea()
        {
            return string.Join("|", Id, Nombre, Ubicacion,
                               CapacidadPCs.ToString(CultureInfo.InvariantCulture),
                               Equipamiento, Activo.ToString());
        }

        public void CargarDesdeLinea(string linea)
        {
            string[] campos = linea.Split('|');

            if (campos.Length != 6)
                throw new ArchivoDatosCorruptoException(
                    $"Se esperaban 6 campos y se encontraron {campos.Length} en la linea: \"{linea}\"");

            try
            {
                Id = campos[0];
                Nombre = campos[1];
                Ubicacion = campos[2];
                CapacidadPCs = int.Parse(campos[3], CultureInfo.InvariantCulture);
                Equipamiento = campos[4];
                Activo = bool.Parse(campos[5]);
            }
            catch (Exception ex) when (ex is FormatException ||
                                       ex is ArgumentException ||
                                       ex is OverflowException)
            {
                // Se envuelve la excepcion original (inner) para no perder la causa real.
                throw new ArchivoDatosCorruptoException(
                    $"No se pudo interpretar la linea de laboratorio: \"{linea}\"", ex);
            }
        }

        /// <summary>Se sobrescribe para que los ComboBox y ListBox muestren algo legible.</summary>
        public override string ToString()
        {
            return Descripcion;
        }
    }
}
