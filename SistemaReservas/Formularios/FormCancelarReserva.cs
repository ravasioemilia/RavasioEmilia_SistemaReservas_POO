using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SistemaReservas.Excepciones;
using SistemaReservas.Modelos;
using SistemaReservas.Servicios;

namespace SistemaReservas.Formularios
{
    /// <summary>
    /// Cancelacion de reservas.
    ///
    /// La lista que se muestra se arma con un filtro polimorfico:
    ///     .Where(r =&gt; usuarioActual.PuedeCancelar(r))
    ///
    /// Un Administrador ve todas las reservas; un Docente o un Alumno solo las suyas.
    /// El formulario es el mismo para los tres: la diferencia la pone el objeto Usuario.
    /// </summary>
    public class FormCancelarReserva : Form
    {
        private readonly GestorReservas gestor;
        private readonly ServicioReportes reportes;
        private readonly Usuario usuarioActual;

        private Label lblTitulo;
        private Label lblExplicacion;
        private DataGridView dgvReservas;
        private Button btnCancelarReserva;
        private Button btnCerrar;

        public FormCancelarReserva(GestorReservas gestor, Usuario usuarioActual)
        {
            if (gestor == null) throw new ArgumentNullException(nameof(gestor));
            if (usuarioActual == null) throw new ArgumentNullException(nameof(usuarioActual));

            this.gestor = gestor;
            this.usuarioActual = usuarioActual;
            this.reportes = new ServicioReportes(gestor);

            InicializarComponentes();
            CargarReservasCancelables();
        }

        private void InicializarComponentes()
        {
            this.Text = "Cancelar reserva";
            this.Size = new Size(900, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(700, 450);

            lblTitulo = new Label();
            lblTitulo.Text = "Reservas que usted puede cancelar";
            lblTitulo.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTitulo.Location = new Point(20, 20);
            lblTitulo.Size = new Size(600, 25);

            lblExplicacion = new Label();
            lblExplicacion.Location = new Point(20, 48);
            lblExplicacion.Size = new Size(840, 20);
            lblExplicacion.ForeColor = Color.DimGray;
            lblExplicacion.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            dgvReservas = new DataGridView();
            dgvReservas.Location = new Point(20, 78);
            dgvReservas.Size = new Size(840, 320);
            dgvReservas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                                 AnchorStyles.Left | AnchorStyles.Right;
            dgvReservas.ReadOnly = true;
            dgvReservas.AllowUserToAddRows = false;
            dgvReservas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReservas.MultiSelect = false;
            dgvReservas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReservas.RowHeadersVisible = false;

            btnCancelarReserva = new Button();
            btnCancelarReserva.Text = "Cancelar la reserva seleccionada";
            btnCancelarReserva.Location = new Point(20, 410);
            btnCancelarReserva.Size = new Size(240, 38);
            btnCancelarReserva.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCancelarReserva.Click += BtnCancelarReserva_Click;

            btnCerrar = new Button();
            btnCerrar.Text = "Cerrar";
            btnCerrar.Location = new Point(760, 410);
            btnCerrar.Size = new Size(100, 38);
            btnCerrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCerrar.Click += (s, e) => this.Close();

            this.CancelButton = btnCerrar;

            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblExplicacion);
            this.Controls.Add(dgvReservas);
            this.Controls.Add(btnCancelarReserva);
            this.Controls.Add(btnCerrar);
        }

        /// <summary>
        /// Filtra las reservas preguntandole al usuario si puede cancelar cada una.
        /// Esta es la aplicacion directa del polimorfismo dentro de una lambda.
        /// </summary>
        private void CargarReservasCancelables()
        {
            try
            {
                List<Reserva> cancelables = gestor.Reservas.Listar()
                    .Where(r => r.Estado != EstadoReserva.Cancelada)
                    .Where(r => usuarioActual.PuedeCancelar(r))
                    .OrderBy(r => r.Fecha)
                    .ThenBy(r => r.HoraInicio)
                    .ToList();

                dgvReservas.DataSource = reportes.ArmarFilas(cancelables);

                if (dgvReservas.Columns["Original"] != null)
                    dgvReservas.Columns["Original"].Visible = false;

                lblExplicacion.Text = usuarioActual.PuedeGestionarLaboratorios()
                    ? $"Como Administrador puede cancelar cualquier reserva del sistema. " +
                      $"Se encontraron {cancelables.Count}."
                    : $"Como {usuarioActual.Rol} solo puede cancelar sus propias reservas. " +
                      $"Se encontraron {cancelables.Count}.";

                btnCancelarReserva.Enabled = cancelables.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar las reservas:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancelarReserva_Click(object sender, EventArgs e)
        {
            if (dgvReservas.CurrentRow == null)
            {
                MessageBox.Show("Seleccione una reserva de la lista.", "Atencion",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            FilaReserva fila = dgvReservas.CurrentRow.DataBoundItem as FilaReserva;

            if (fila == null) return;

            Reserva reserva = fila.Original;

            DialogResult respuesta = MessageBox.Show(
                $"Confirma la cancelacion de la reserva {reserva.Id}?\n\n" +
                $"Laboratorio: {fila.Laboratorio}\n" +
                $"Solicitante: {fila.Usuario}\n" +
                $"Fecha: {fila.Fecha} {fila.Horario}\n\n" +
                "Esta accion no se puede deshacer.",
                "Confirmar cancelacion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (respuesta != DialogResult.Yes) return;

            try
            {
                gestor.CancelarReserva(usuarioActual, reserva.Id);

                MessageBox.Show($"La reserva {reserva.Id} fue cancelada.", "Listo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                CargarReservasCancelables();   // se recarga la lista sin la cancelada
            }
            catch (PermisoDenegadoException ex)
            {
                MessageBox.Show(ex.Message, "Permiso denegado",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (SistemaReservasException ex)
            {
                MessageBox.Show(ex.Message, "No se pudo cancelar",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inesperado:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
