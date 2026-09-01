using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SistemaReservas.Modelos;
using SistemaReservas.Servicios;

namespace SistemaReservas.Formularios
{
    /// <summary>
    /// Vista tipo calendario: muestra el dia de un laboratorio dividido en franjas
    /// de media hora, de 08:00 a 22:00, pintando en rojo las ocupadas y en verde
    /// las libres. Es la "indicacion visual de horarios ocupados" que pide la consigna.
    /// </summary>
    public class FormDisponibilidad : Form
    {
        private readonly GestorReservas gestor;
        private readonly ServicioReportes reportes;
        private readonly Usuario usuarioActual;

        private static readonly TimeSpan Apertura = new TimeSpan(8, 0, 0);
        private static readonly TimeSpan Cierre = new TimeSpan(22, 0, 0);

        private Label lblTitulo;
        private Label lblLaboratorio;
        private ComboBox cmbLaboratorio;
        private Label lblFecha;
        private DateTimePicker dtpFecha;
        private Button btnConsultar;
        private Label lblResumen;
        private DataGridView dgvFranjas;
        private Button btnCerrar;

        public FormDisponibilidad(GestorReservas gestor, Usuario usuarioActual)
        {
            if (gestor == null) throw new ArgumentNullException(nameof(gestor));

            this.gestor = gestor;
            this.usuarioActual = usuarioActual;
            this.reportes = new ServicioReportes(gestor);

            InicializarComponentes();
            CargarLaboratorios();
            ConsultarDisponibilidad();
        }

        private void InicializarComponentes()
        {
            this.Text = "Consulta de disponibilidad";
            this.Size = new Size(820, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(700, 500);

            lblTitulo = new Label();
            lblTitulo.Text = "Disponibilidad por laboratorio y fecha";
            lblTitulo.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTitulo.Location = new Point(20, 20);
            lblTitulo.Size = new Size(500, 25);

            lblLaboratorio = new Label();
            lblLaboratorio.Text = "Laboratorio:";
            lblLaboratorio.Location = new Point(20, 60);
            lblLaboratorio.Size = new Size(80, 20);

            cmbLaboratorio = new ComboBox();
            cmbLaboratorio.Location = new Point(105, 57);
            cmbLaboratorio.Size = new Size(330, 24);
            cmbLaboratorio.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLaboratorio.SelectedIndexChanged += (s, e) => ConsultarDisponibilidad();

            lblFecha = new Label();
            lblFecha.Text = "Fecha:";
            lblFecha.Location = new Point(450, 60);
            lblFecha.Size = new Size(50, 20);

            dtpFecha = new DateTimePicker();
            dtpFecha.Location = new Point(500, 57);
            dtpFecha.Size = new Size(150, 24);
            dtpFecha.Format = DateTimePickerFormat.Short;
            dtpFecha.ValueChanged += (s, e) => ConsultarDisponibilidad();

            btnConsultar = new Button();
            btnConsultar.Text = "Consultar";
            btnConsultar.Location = new Point(665, 55);
            btnConsultar.Size = new Size(110, 28);
            btnConsultar.Click += (s, e) => ConsultarDisponibilidad();

            lblResumen = new Label();
            lblResumen.Location = new Point(20, 95);
            lblResumen.Size = new Size(755, 40);
            lblResumen.ForeColor = Color.DarkBlue;
            lblResumen.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            dgvFranjas = new DataGridView();
            dgvFranjas.Location = new Point(20, 140);
            dgvFranjas.Size = new Size(755, 360);
            dgvFranjas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                                AnchorStyles.Left | AnchorStyles.Right;
            dgvFranjas.ReadOnly = true;
            dgvFranjas.AllowUserToAddRows = false;
            dgvFranjas.RowHeadersVisible = false;
            dgvFranjas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFranjas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvFranjas.DataBindingComplete += DgvFranjas_DataBindingComplete;

            btnCerrar = new Button();
            btnCerrar.Text = "Cerrar";
            btnCerrar.Location = new Point(675, 512);
            btnCerrar.Size = new Size(100, 35);
            btnCerrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCerrar.Click += (s, e) => this.Close();

            this.CancelButton = btnCerrar;

            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblLaboratorio);
            this.Controls.Add(cmbLaboratorio);
            this.Controls.Add(lblFecha);
            this.Controls.Add(dtpFecha);
            this.Controls.Add(btnConsultar);
            this.Controls.Add(lblResumen);
            this.Controls.Add(dgvFranjas);
            this.Controls.Add(btnCerrar);
        }

        private void CargarLaboratorios()
        {
            try
            {
                // Aca se listan TODOS (incluso los inactivos): es una consulta, no un alta.
                List<Laboratorio> todos = gestor.Laboratorios.Listar()
                    .OrderBy(l => l.Id)
                    .ToList();

                if (todos.Count == 0) return;

                cmbLaboratorio.DataSource = todos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los laboratorios:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Arma la vista por franjas de media hora. Para cada franja busca con LINQ
        /// si hay una reserva que la cubra.
        /// </summary>
        private void ConsultarDisponibilidad()
        {
            Laboratorio lab = cmbLaboratorio.SelectedItem as Laboratorio;

            if (lab == null) return;

            try
            {
                List<Reserva> ocupadas = gestor.ObtenerReservasDelLaboratorio(lab.Id, dtpFecha.Value);
                List<Usuario> usuarios = gestor.Usuarios.Listar();
                List<FilaFranjaHoraria> filas = new List<FilaFranjaHoraria>();

                for (TimeSpan hora = Apertura; hora < Cierre; hora = hora.Add(TimeSpan.FromMinutes(30)))
                {
                    TimeSpan finFranja = hora.Add(TimeSpan.FromMinutes(30));

                    // Una franja esta ocupada si alguna reserva la cubre, aunque sea en parte.
                    Reserva reserva = ocupadas.FirstOrDefault(
                        r => r.HoraInicio < finFranja && hora < r.HoraFin);

                    if (reserva == null)
                    {
                        filas.Add(new FilaFranjaHoraria
                        {
                            Horario = $"{hora:hh\\:mm} - {finFranja:hh\\:mm}",
                            Estado = "LIBRE",
                            Motivo = "",
                            Solicitante = "",
                            Ocupada = false
                        });
                    }
                    else
                    {
                        Usuario solicitante = usuarios
                            .FirstOrDefault(u => u.Legajo == reserva.LegajoUsuario);

                        filas.Add(new FilaFranjaHoraria
                        {
                            Horario = $"{hora:hh\\:mm} - {finFranja:hh\\:mm}",
                            Estado = "OCUPADO (" + reserva.Id + ")",
                            Motivo = reserva.Motivo,
                            Solicitante = solicitante != null
                                ? $"{solicitante.NombreCompleto} ({solicitante.Rol})"
                                : reserva.LegajoUsuario,
                            Ocupada = true
                        });
                    }
                }

                dgvFranjas.DataSource = filas;

                if (dgvFranjas.Columns["Ocupada"] != null)
                    dgvFranjas.Columns["Ocupada"].Visible = false;

                // Resumen con LINQ
                int franjasLibres = filas.Count(f => !f.Ocupada);
                double horasLibres = franjasLibres * 0.5;

                lblResumen.Text =
                    $"{lab.Nombre} ({lab.CapacidadPCs} PCs) - {dtpFecha.Value:dddd dd/MM/yyyy}\n" +
                    $"{ocupadas.Count} reserva(s) vigente(s). Quedan {horasLibres:0.#} horas libres " +
                    $"entre las 08:00 y las 22:00." +
                    (lab.Activo ? "" : "   [LABORATORIO DADO DE BAJA - no admite reservas]");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al consultar la disponibilidad:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Pinta de verde las franjas libres y de rojo las ocupadas.</summary>
        private void DgvFranjas_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow fila in dgvFranjas.Rows)
            {
                FilaFranjaHoraria datos = fila.DataBoundItem as FilaFranjaHoraria;

                if (datos == null) continue;

                if (datos.Ocupada)
                {
                    fila.DefaultCellStyle.BackColor = Color.MistyRose;
                    fila.DefaultCellStyle.ForeColor = Color.DarkRed;
                }
                else
                {
                    fila.DefaultCellStyle.BackColor = Color.Honeydew;
                    fila.DefaultCellStyle.ForeColor = Color.DarkGreen;
                }
            }
        }
    }
}
