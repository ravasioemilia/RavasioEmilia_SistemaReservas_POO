using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SistemaReservas.Excepciones;
using SistemaReservas.Formularios;
using SistemaReservas.Modelos;
using SistemaReservas.Servicios;

namespace SistemaReservas
{
    /// <summary>
    /// Punto de entrada de la aplicacion.
    ///
    /// El GestorReservas se crea dentro de un bloque using: cuando la aplicacion
    /// termina, se llama automaticamente a Dispose(), que guarda los cambios
    /// pendientes y libera los tres repositorios. Es el patron IDisposable
    /// funcionando de punta a punta.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// STAThread es obligatorio en WinForms: define el modelo de hilos que
        /// necesitan los controles de Windows.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // using => al salir del bloque se ejecuta gestor.Dispose()
                using (GestorReservas gestor = new GestorReservas())
                {
                    AvisarErroresDeCarga(gestor);

                    // 1) Pantalla de login
                    using (FormLogin login = new FormLogin(gestor))
                    {
                        if (login.ShowDialog() != DialogResult.OK)
                            return;   // el usuario cerro o apreto Salir

                        // 2) Pantalla principal, con el usuario ya identificado
                        Usuario usuario = login.UsuarioSeleccionado;

                        Application.Run(new FormPrincipal(gestor, usuario));
                    }
                }
            }
            catch (SistemaReservasException ex)
            {
                // Errores propios del sistema: se muestran con su mensaje.
                MessageBox.Show(
                    "No se pudo iniciar el sistema:\n\n" + ex.Message,
                    "Error del sistema", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                // Red de seguridad final: cualquier error no previsto.
                MessageBox.Show(
                    "Error inesperado:\n\n" + ex.Message +
                    "\n\nDetalle tecnico:\n" + ex.GetType().Name,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Si algun archivo de datos tenia lineas mal formadas, se avisa al iniciar.
        /// El sistema igual arranca: las lineas invalidas se ignoraron.
        /// </summary>
        private static void AvisarErroresDeCarga(GestorReservas gestor)
        {
            List<ArchivoDatosCorruptoException> errores = gestor.ObtenerErroresDeCarga();

            if (errores.Count == 0) return;

            string detalle = string.Join(Environment.NewLine,
                errores.Take(10).Select(e => " - " + e.Message));

            if (errores.Count > 10)
                detalle += Environment.NewLine + $" ... y {errores.Count - 10} error(es) mas.";

            MessageBox.Show(
                $"Se encontraron {errores.Count} linea(s) con formato invalido en los " +
                "archivos de datos.\n\nEsas lineas fueron ignoradas y el resto de la " +
                "informacion se cargo normalmente.\n\n" + detalle,
                "Advertencia al cargar los datos",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
