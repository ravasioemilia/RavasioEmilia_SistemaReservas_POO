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
    /// Pantalla de reportes. Cada solapa muestra una consulta LINQ distinta
    /// resuelta en ServicioReportes.
    ///
    /// El formulario no calcula nada: pide el reporte y lo enlaza a la grilla.
    /// </summary>
    public class FormReportes : Form
    {
        private readonly ServicioReportes reportes;

        private TabControl tabs;

        private TabPage tabUsoLaboratorio;
        private DataGridView dgvUsoLaboratorio;

        private TabPage tabRanking;
        private DataGridView dgvRanking;
        private Label lblRanking;

        private TabPage tabDelDia;
        private DateTimePicker dtpDia;
        private Label lblDia;
        private DataGridView dgvDelDia;
        private Label lblResumenDia;

        private TabPage tabUsuarios;
        private DataGridView dgvUsuarios;

        private TabPage tabEstados;
        private DataGridView dgvEstados;
        private Label lblTotalReservas;

        private Button btnActualizar;
        private Button btnCerrar;

        public FormReportes(GestorReservas gestor)
        {
            if (gestor == null) throw new ArgumentNullException(nameof(gestor));

            this.reportes = new ServicioReportes(gestor);

            InicializarComponentes();
            CargarTodosLosReportes();
        }

        private void InicializarComponentes()
        {
            this.Text = "Reportes del sistema";
            this.Size = new Size(950, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 550);

            tabs = new TabControl();
            tabs.Location = new Point(15, 15);
            tabs.Size = new Size(905, 505);
            tabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                          AnchorStyles.Left | AnchorStyles.Right;

            // ── Solapa 1: uso por laboratorio (GroupBy) ──
            tabUsoLaboratorio = new TabPage("Uso por laboratorio");
            dgvUsoLaboratorio = CrearGrilla();
            tabUsoLaboratorio.Controls.Add(dgvUsoLaboratorio);

            // ── Solapa 2: ranking ──
            tabRanking = new TabPage("Ranking de laboratorios");

            lblRanking = new Label();
            lblRanking.Text = "Los laboratorios mas solicitados, ordenados por cantidad de reservas.";
            lblRanking.Location = new Point(15, 12);
            lblRanking.Size = new Size(700, 20);
            lblRanking.ForeColor = Color.DimGray;

            dgvRanking = CrearGrilla();
            dgvRanking.Location = new Point(15, 40);
            dgvRanking.Size = new Size(860, 400);

            tabRanking.Controls.Add(lblRanking);
            tabRanking.Controls.Add(dgvRanking);

            // ── Solapa 3: reservas del dia ──
            tabDelDia = new TabPage("Reservas del dia");

            lblDia = new Label();
            lblDia.Text = "Fecha:";
            lblDia.Location = new Point(15, 18);
            lblDia.Size = new Size(50, 20);

            dtpDia = new DateTimePicker();
            dtpDia.Location = new Point(70, 15);
            dtpDia.Size = new Size(160, 24);
            dtpDia.Format = DateTimePickerFormat.Short;
            dtpDia.ValueChanged += (s, e) => CargarReporteDelDia();

            lblResumenDia = new Label();
            lblResumenDia.Location = new Point(250, 18);
            lblResumenDia.Size = new Size(600, 20);
            lblResumenDia.ForeColor = Color.DarkBlue;

            dgvDelDia = CrearGrilla();
            dgvDelDia.Location = new Point(15, 50);
            dgvDelDia.Size = new Size(860, 390);

            tabDelDia.Controls.Add(lblDia);
            tabDelDia.Controls.Add(dtpDia);
            tabDelDia.Controls.Add(lblResumenDia);
            tabDelDia.Controls.Add(dgvDelDia);

            // ── Solapa 4: ranking de usuarios ──
            tabUsuarios = new TabPage("Usuarios que mas reservan");
            dgvUsuarios = CrearGrilla();
            tabUsuarios.Controls.Add(dgvUsuarios);

            // ── Solapa 5: resumen por estado ──
            tabEstados = new TabPage("Resumen por estado");

            lblTotalReservas = new Label();
            lblTotalReservas.Location = new Point(15, 12);
            lblTotalReservas.Size = new Size(700, 20);
            lblTotalReservas.ForeColor = Color.DimGray;

            dgvEstados = CrearGrilla();
            dgvEstados.Location = new Point(15, 40);
            dgvEstados.Size = new Size(860, 400);

            tabEstados.Controls.Add(lblTotalReservas);
            tabEstados.Controls.Add(dgvEstados);

            tabs.TabPages.Add(tabUsoLaboratorio);
            tabs.TabPages.Add(tabRanking);
            tabs.TabPages.Add(tabDelDia);
            tabs.TabPages.Add(tabUsuarios);
            tabs.TabPages.Add(tabEstados);

            // ── Botones ──
            btnActualizar = new Button();
            btnActualizar.Text = "Actualizar";
            btnActualizar.Location = new Point(15, 535);
            btnActualizar.Size = new Size(120, 38);
            btnActualizar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnActualizar.Click += (s, e) => CargarTodosLosReportes();

            btnCerrar = new Button();
            btnCerrar.Text = "Cerrar";
            btnCerrar.Location = new Point(800, 535);
            btnCerrar.Size = new Size(120, 38);
            btnCerrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCerrar.Click += (s, e) => this.Close();

            this.CancelButton = btnCerrar;

            this.Controls.Add(tabs);
            this.Controls.Add(btnActualizar);
            this.Controls.Add(btnCerrar);
        }

        /// <summary>Crea una grilla con la configuracion comun a todas las solapas.</summary>
        private DataGridView CrearGrilla()
        {
            DataGridView grilla = new DataGridView();
            grilla.Location = new Point(15, 15);
            grilla.Size = new Size(860, 425);
            grilla.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                            AnchorStyles.Left | AnchorStyles.Right;
            grilla.ReadOnly = true;
            grilla.AllowUserToAddRows = false;
            grilla.RowHeadersVisible = false;
            grilla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grilla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            return grilla;
        }

        // ─── Carga de los reportes ──────────────────────────────────────────

        private void CargarTodosLosReportes()
        {
            try
            {
                // Reporte 1 y 2: GroupBy por laboratorio
                dgvUsoLaboratorio.DataSource = reportes.UsoPorLaboratorio();
                dgvRanking.DataSource = reportes.RankingLaboratorios(5);

                // Reporte 3: reservas del dia
                CargarReporteDelDia();

                // Reporte 4: ranking de usuarios
                dgvUsuarios.DataSource = reportes.RankingUsuarios();

                // Reporte 5: resumen por estado
                List<ResumenEstado> estados = reportes.ResumenPorEstado();
                dgvEstados.DataSource = estados;
                lblTotalReservas.Text =
                    $"Total de reservas registradas: {estados.Sum(e => e.Cantidad)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar los reportes:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarReporteDelDia()
        {
            try
            {
                List<Reserva> delDia = reportes.ReservasDelDia(dtpDia.Value);

                dgvDelDia.DataSource = reportes.ArmarFilas(delDia);

                if (dgvDelDia.Columns["Original"] != null)
                    dgvDelDia.Columns["Original"].Visible = false;

                double horas = delDia.Sum(r => r.DuracionHoras);

                lblResumenDia.Text = delDia.Count == 0
                    ? "No hay reservas para esa fecha."
                    : $"{delDia.Count} reserva(s), {horas:0.#} horas de uso en total.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el reporte del dia:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
