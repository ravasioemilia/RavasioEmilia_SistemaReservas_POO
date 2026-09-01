using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SistemaReservas.Excepciones;
using SistemaReservas.Modelos;
using SistemaReservas.Servicios;

namespace SistemaReservas.Formularios
{
    /// <summary>
    /// Alta de una reserva.
    ///
    /// El formulario NO decide nada: junta los datos y se los pasa a
    /// gestor.CrearReserva(). Todas las reglas (solapamiento, limite semanal,
    /// anticipacion) estan en la capa de negocio. Aca solo se atrapan las
    /// excepciones y se muestran como mensajes entendibles.
    /// </summary>
    public class FormNuevaReserva : Form
    {
        private readonly GestorReservas gestor;
        private readonly Usuario usuarioActual;

        // ─── Controles ──────────────────────────────────────────────────────
        private Label lblTitulo;
        private Label lblLaboratorio;
        private ComboBox cmbLaboratorio;
        private Label lblFecha;
        private DateTimePicker dtpFecha;
        private Label lblDesde;
        private ComboBox cmbHoraInicio;
        private Label lblHasta;
        private ComboBox cmbHoraFin;
        private Label lblMotivo;
        private TextBox txtMotivo;

        private GroupBox grpOcupado;
        private ListBox lstOcupado;

        private Label lblAviso;
        private Button btnVerificar;
        private Button btnConfirmar;
        private Button btnCerrar;

        // ─── Constructor ────────────────────────────────────────────────────

        public FormNuevaReserva(GestorReservas gestor, Usuario usuarioActual)
        {
            if (gestor == null) throw new ArgumentNullException(nameof(gestor));
            if (usuarioActual == null) throw new ArgumentNullException(nameof(usuarioActual));

            this.gestor = gestor;
            this.usuarioActual = usuarioActual;

            InicializarComponentes();
            CargarLaboratorios();
            CargarHorarios();
            ActualizarPanelOcupacion();
        }

        private void InicializarComponentes()
        {
            this.Text = "Nueva reserva";
            this.Size = new Size(660, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblTitulo = new Label();
            lblTitulo.Text = $"Reserva a nombre de: {usuarioActual.NombreCompleto} ({usuarioActual.Rol})";
            lblTitulo.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblTitulo.Location = new Point(20, 20);
            lblTitulo.Size = new Size(600, 22);

            // ── Laboratorio ──
            lblLaboratorio = new Label();
            lblLaboratorio.Text = "Laboratorio:";
            lblLaboratorio.Location = new Point(20, 60);
            lblLaboratorio.Size = new Size(100, 20);

            cmbLaboratorio = new ComboBox();
            cmbLaboratorio.Location = new Point(130, 57);
            cmbLaboratorio.Size = new Size(480, 24);
            cmbLaboratorio.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLaboratorio.SelectedIndexChanged += Seleccion_Cambiada;

            // ── Fecha ──
            lblFecha = new Label();
            lblFecha.Text = "Fecha:";
            lblFecha.Location = new Point(20, 95);
            lblFecha.Size = new Size(100, 20);

            dtpFecha = new DateTimePicker();
            dtpFecha.Location = new Point(130, 92);
            dtpFecha.Size = new Size(200, 24);
            dtpFecha.Format = DateTimePickerFormat.Short;
            dtpFecha.MinDate = DateTime.Today;
            // El tope lo define el ROL del usuario (polimorfismo aplicado a la interfaz).
            dtpFecha.MaxDate = DateTime.Today.AddDays(usuarioActual.ObtenerAnticipacionMaximaDias());
            dtpFecha.ValueChanged += Seleccion_Cambiada;

            // ── Horarios ──
            lblDesde = new Label();
            lblDesde.Text = "Desde:";
            lblDesde.Location = new Point(20, 130);
            lblDesde.Size = new Size(100, 20);

            cmbHoraInicio = new ComboBox();
            cmbHoraInicio.Location = new Point(130, 127);
            cmbHoraInicio.Size = new Size(100, 24);
            cmbHoraInicio.DropDownStyle = ComboBoxStyle.DropDownList;

            lblHasta = new Label();
            lblHasta.Text = "Hasta:";
            lblHasta.Location = new Point(250, 130);
            lblHasta.Size = new Size(50, 20);

            cmbHoraFin = new ComboBox();
            cmbHoraFin.Location = new Point(300, 127);
            cmbHoraFin.Size = new Size(100, 24);
            cmbHoraFin.DropDownStyle = ComboBoxStyle.DropDownList;

            // ── Motivo ──
            lblMotivo = new Label();
            lblMotivo.Text = "Motivo:";
            lblMotivo.Location = new Point(20, 165);
            lblMotivo.Size = new Size(100, 20);

            txtMotivo = new TextBox();
            txtMotivo.Location = new Point(130, 162);
            txtMotivo.Size = new Size(480, 24);
            txtMotivo.MaxLength = 100;

            // ── Horarios ya ocupados ──
            grpOcupado = new GroupBox();
            grpOcupado.Text = "Horarios ya ocupados en ese laboratorio y fecha";
            grpOcupado.Location = new Point(20, 200);
            grpOcupado.Size = new Size(590, 175);

            lstOcupado = new ListBox();
            lstOcupado.Location = new Point(15, 25);
            lstOcupado.Size = new Size(560, 135);

            grpOcupado.Controls.Add(lstOcupado);

            // ── Aviso y botones ──
            lblAviso = new Label();
            lblAviso.Location = new Point(20, 385);
            lblAviso.Size = new Size(590, 40);
            lblAviso.ForeColor = Color.DarkBlue;

            btnVerificar = new Button();
            btnVerificar.Text = "Verificar disponibilidad";
            btnVerificar.Location = new Point(20, 430);
            btnVerificar.Size = new Size(170, 35);
            btnVerificar.Click += BtnVerificar_Click;

            btnConfirmar = new Button();
            btnConfirmar.Text = "Confirmar reserva";
            btnConfirmar.Location = new Point(340, 430);
            btnConfirmar.Size = new Size(150, 35);
            btnConfirmar.Click += BtnConfirmar_Click;

            btnCerrar = new Button();
            btnCerrar.Text = "Cerrar";
            btnCerrar.Location = new Point(500, 430);
            btnCerrar.Size = new Size(110, 35);
            btnCerrar.Click += (s, e) => this.Close();

            this.CancelButton = btnCerrar;

            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblLaboratorio);
            this.Controls.Add(cmbLaboratorio);
            this.Controls.Add(lblFecha);
            this.Controls.Add(dtpFecha);
            this.Controls.Add(lblDesde);
            this.Controls.Add(cmbHoraInicio);
            this.Controls.Add(lblHasta);
            this.Controls.Add(cmbHoraFin);
            this.Controls.Add(lblMotivo);
            this.Controls.Add(txtMotivo);
            this.Controls.Add(grpOcupado);
            this.Controls.Add(lblAviso);
            this.Controls.Add(btnVerificar);
            this.Controls.Add(btnConfirmar);
            this.Controls.Add(btnCerrar);
        }

        // ─── Carga de datos ─────────────────────────────────────────────────

        private void CargarLaboratorios()
        {
            try
            {
                List<Laboratorio> activos = gestor.ObtenerLaboratoriosActivos();

                if (activos.Count == 0)
                {
                    MessageBox.Show("No hay laboratorios activos para reservar.", "Atencion",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnConfirmar.Enabled = false;
                    return;
                }

                cmbLaboratorio.DataSource = activos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los laboratorios:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Carga los horarios de 08:00 a 22:00 cada 30 minutos.</summary>
        private void CargarHorarios()
        {
            for (int minutos = 8 * 60; minutos <= 22 * 60; minutos += 30)
            {
                TimeSpan hora = TimeSpan.FromMinutes(minutos);
                string texto = hora.ToString(@"hh\:mm");

                cmbHoraInicio.Items.Add(texto);
                cmbHoraFin.Items.Add(texto);
            }

            // Valores iniciales: 08:00 a 10:00
            cmbHoraInicio.SelectedIndex = 0;
            cmbHoraFin.SelectedIndex = 4;
        }

        private void Seleccion_Cambiada(object sender, EventArgs e)
        {
            ActualizarPanelOcupacion();
        }

        /// <summary>Muestra que horarios estan tomados y cuales quedan libres.</summary>
        private void ActualizarPanelOcupacion()
        {
            lstOcupado.Items.Clear();

            Laboratorio lab = cmbLaboratorio.SelectedItem as Laboratorio;

            if (lab == null) return;

            try
            {
                List<Reserva> ocupadas = gestor.ObtenerReservasDelLaboratorio(lab.Id, dtpFecha.Value);

                if (ocupadas.Count == 0)
                {
                    lstOcupado.Items.Add("El laboratorio esta completamente libre ese dia.");
                }
                else
                {
                    foreach (Reserva r in ocupadas)
                        lstOcupado.Items.Add($"{r.RangoHorario}   {r.Motivo}   [{r.Estado}]");
                }

                // Franjas libres calculadas con LINQ en el servicio de reportes.
                ServicioReportes reportes = new ServicioReportes(gestor);
                List<FranjaLibre> libres = reportes.FranjasLibres(lab.Id, dtpFecha.Value);

                lstOcupado.Items.Add("");
                lstOcupado.Items.Add("--- Franjas libres ---");

                foreach (FranjaLibre franja in libres)
                    lstOcupado.Items.Add(franja.ToString());
            }
            catch (Exception ex)
            {
                lstOcupado.Items.Add("Error al consultar: " + ex.Message);
            }
        }

        // ─── Lectura de los controles ───────────────────────────────────────

        private TimeSpan LeerHora(ComboBox combo)
        {
            return TimeSpan.Parse(combo.SelectedItem.ToString());
        }

        // ─── Acciones ───────────────────────────────────────────────────────

        private void BtnVerificar_Click(object sender, EventArgs e)
        {
            Laboratorio lab = cmbLaboratorio.SelectedItem as Laboratorio;

            if (lab == null) return;

            TimeSpan inicio = LeerHora(cmbHoraInicio);
            TimeSpan fin = LeerHora(cmbHoraFin);

            if (fin <= inicio)
            {
                lblAviso.ForeColor = Color.Firebrick;
                lblAviso.Text = "La hora de fin debe ser posterior a la de inicio.";
                return;
            }

            bool libre = gestor.HayDisponibilidad(lab.Id, dtpFecha.Value, inicio, fin);

            lblAviso.ForeColor = libre ? Color.DarkGreen : Color.Firebrick;
            lblAviso.Text = libre
                ? $"Disponible: {lab.Id} esta libre el {dtpFecha.Value:dd/MM/yyyy} de " +
                  $"{inicio:hh\\:mm} a {fin:hh\\:mm}."
                : "Ese horario se superpone con una reserva existente. Elija otra franja.";
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            Laboratorio lab = cmbLaboratorio.SelectedItem as Laboratorio;

            if (lab == null)
            {
                MessageBox.Show("Debe seleccionar un laboratorio.", "Atencion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                TimeSpan inicio = LeerHora(cmbHoraInicio);
                TimeSpan fin = LeerHora(cmbHoraFin);

                // Toda la logica esta del otro lado. Si algo no cumple, viene una excepcion.
                Reserva creada = gestor.CrearReserva(usuarioActual, lab.Id, dtpFecha.Value,
                                                     inicio, fin, txtMotivo.Text);

                MessageBox.Show(
                    $"Reserva {creada.Id} registrada correctamente.\n\n" +
                    $"Laboratorio: {lab.Nombre}\n" +
                    $"Fecha: {creada.Fecha:dd/MM/yyyy}\n" +
                    $"Horario: {creada.RangoHorario}\n" +
                    $"Estado: {creada.Estado}",
                    "Reserva creada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Se limpia el motivo y se refresca el panel para una proxima carga.
                txtMotivo.Clear();
                ActualizarPanelOcupacion();
            }
            catch (ReservaSolapadaException ex)
            {
                MessageBox.Show(ex.Message, "Horario ocupado",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ActualizarPanelOcupacion();
            }
            catch (LimiteReservasExcedidoException ex)
            {
                MessageBox.Show(ex.Message, "Limite semanal alcanzado",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (LaboratorioNoDisponibleException ex)
            {
                MessageBox.Show(ex.Message, "Laboratorio no disponible",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (SistemaReservasException ex)
            {
                // Cualquier otra regla de negocio incumplida.
                MessageBox.Show(ex.Message, "No se pudo crear la reserva",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (ArgumentException ex)
            {
                // Validaciones de las propiedades de Reserva.
                MessageBox.Show(ex.Message, "Datos invalidos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inesperado:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // El finally se ejecuta siempre, haya o no excepcion.
                this.Cursor = Cursors.Default;
            }
        }
    }
}
