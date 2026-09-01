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
    /// Pantalla principal. Muestra las reservas y da acceso al resto del sistema.
    ///
    /// DOS TEMAS SE VEN ACA CON CLARIDAD:
    ///
    /// 1) EVENTOS: este formulario se SUSCRIBE a ReservaCreada y ReservaCancelada del
    ///    gestor. Cuando otra pantalla da de alta una reserva, esta grilla se refresca
    ///    sola. El gestor no conoce este formulario: solo dispara el evento.
    ///
    /// 2) POLIMORFISMO EN EL MENU: las opciones se habilitan segun lo que responda el
    ///    propio usuario (PuedeGestionarLaboratorios), sin preguntar de que tipo es.
    /// </summary>
    public class FormPrincipal : Form
    {
        private readonly GestorReservas gestor;
        private readonly ServicioReportes reportes;
        private readonly Usuario usuarioActual;

        // ─── Controles ──────────────────────────────────────────────────────
        private MenuStrip menu;
        private ToolStripMenuItem menuReservas;
        private ToolStripMenuItem menuGestion;
        private ToolStripMenuItem menuReportes;
        private ToolStripMenuItem menuSesion;

        private Label lblBienvenida;
        private Label lblPermisos;

        private GroupBox grpFiltros;
        private Label lblFiltroEstado;
        private ComboBox cmbFiltroEstado;
        private CheckBox chkSoloMisReservas;
        private Button btnActualizar;

        private DataGridView dgvReservas;

        private Button btnNuevaReserva;
        private Button btnCancelarReserva;

        private StatusStrip barraEstado;
        private ToolStripStatusLabel lblEstado;

        // ─── Constructor ────────────────────────────────────────────────────

        public FormPrincipal(GestorReservas gestor, Usuario usuarioActual)
        {
            if (gestor == null) throw new ArgumentNullException(nameof(gestor));
            if (usuarioActual == null) throw new ArgumentNullException(nameof(usuarioActual));

            this.gestor = gestor;
            this.usuarioActual = usuarioActual;
            this.reportes = new ServicioReportes(gestor);

            InicializarComponentes();
            AplicarPermisosDelRol();
            SuscribirseAEventos();
            RefrescarGrilla();
        }

        // ─── Construccion de la interfaz ────────────────────────────────────

        private void InicializarComponentes()
        {
            this.Text = "Sistema de Reservas de Laboratorios";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(850, 550);

            // ── Menu superior ──
            menu = new MenuStrip();

            menuReservas = new ToolStripMenuItem("&Reservas");
            menuReservas.DropDownItems.Add("Nueva reserva...", null, MenuNuevaReserva_Click);
            menuReservas.DropDownItems.Add("Cancelar reserva...", null, MenuCancelarReserva_Click);
            menuReservas.DropDownItems.Add(new ToolStripSeparator());
            menuReservas.DropDownItems.Add("Consultar disponibilidad...", null, MenuDisponibilidad_Click);

            menuGestion = new ToolStripMenuItem("&Gestion");
            menuGestion.DropDownItems.Add("Laboratorios...", null, MenuLaboratorios_Click);
            menuGestion.DropDownItems.Add("Usuarios...", null, MenuUsuarios_Click);

            menuReportes = new ToolStripMenuItem("Re&portes");
            menuReportes.DropDownItems.Add("Ver reportes...", null, MenuReportes_Click);

            menuSesion = new ToolStripMenuItem("&Sesion");
            menuSesion.DropDownItems.Add("Salir", null, MenuSalir_Click);

            menu.Items.Add(menuReservas);
            menu.Items.Add(menuGestion);
            menu.Items.Add(menuReportes);
            menu.Items.Add(menuSesion);

            this.MainMenuStrip = menu;

            // ── Encabezado ──
            lblBienvenida = new Label();
            lblBienvenida.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblBienvenida.Location = new Point(20, 35);
            lblBienvenida.Size = new Size(700, 25);

            lblPermisos = new Label();
            lblPermisos.Location = new Point(20, 62);
            lblPermisos.Size = new Size(940, 20);
            lblPermisos.ForeColor = Color.DimGray;
            lblPermisos.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // ── Filtros ──
            grpFiltros = new GroupBox();
            grpFiltros.Text = "Filtros";
            grpFiltros.Location = new Point(20, 92);
            grpFiltros.Size = new Size(940, 70);
            grpFiltros.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            lblFiltroEstado = new Label();
            lblFiltroEstado.Text = "Estado:";
            lblFiltroEstado.Location = new Point(15, 30);
            lblFiltroEstado.Size = new Size(55, 20);

            cmbFiltroEstado = new ComboBox();
            cmbFiltroEstado.Location = new Point(72, 27);
            cmbFiltroEstado.Size = new Size(160, 24);
            cmbFiltroEstado.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFiltroEstado.Items.Add("(Todos)");
            // Se cargan los estados desde el enum: si mañana se agrega uno, aparece solo.
            foreach (EstadoReserva estado in Enum.GetValues(typeof(EstadoReserva)))
                cmbFiltroEstado.Items.Add(estado.ToString());
            cmbFiltroEstado.SelectedIndex = 0;
            cmbFiltroEstado.SelectedIndexChanged += Filtro_Cambiado;

            chkSoloMisReservas = new CheckBox();
            chkSoloMisReservas.Text = "Ver solo mis reservas";
            chkSoloMisReservas.Location = new Point(260, 28);
            chkSoloMisReservas.Size = new Size(170, 22);
            chkSoloMisReservas.CheckedChanged += Filtro_Cambiado;

            btnActualizar = new Button();
            btnActualizar.Text = "Actualizar";
            btnActualizar.Location = new Point(450, 25);
            btnActualizar.Size = new Size(100, 28);
            btnActualizar.Click += (s, e) => RefrescarGrilla();   // lambda como manejador

            grpFiltros.Controls.Add(lblFiltroEstado);
            grpFiltros.Controls.Add(cmbFiltroEstado);
            grpFiltros.Controls.Add(chkSoloMisReservas);
            grpFiltros.Controls.Add(btnActualizar);

            // ── Grilla ──
            dgvReservas = new DataGridView();
            dgvReservas.Location = new Point(20, 172);
            dgvReservas.Size = new Size(940, 350);
            dgvReservas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                                 AnchorStyles.Left | AnchorStyles.Right;
            dgvReservas.ReadOnly = true;
            dgvReservas.AllowUserToAddRows = false;
            dgvReservas.AllowUserToDeleteRows = false;
            dgvReservas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReservas.MultiSelect = false;
            dgvReservas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReservas.RowHeadersVisible = false;
            // Se pinta cada fila segun su estado (ver el manejador).
            dgvReservas.DataBindingComplete += DgvReservas_DataBindingComplete;

            // ── Botones inferiores ──
            btnNuevaReserva = new Button();
            btnNuevaReserva.Text = "Nueva reserva";
            btnNuevaReserva.Location = new Point(20, 535);
            btnNuevaReserva.Size = new Size(140, 35);
            btnNuevaReserva.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNuevaReserva.Click += MenuNuevaReserva_Click;

            btnCancelarReserva = new Button();
            btnCancelarReserva.Text = "Cancelar la seleccionada";
            btnCancelarReserva.Location = new Point(170, 535);
            btnCancelarReserva.Size = new Size(180, 35);
            btnCancelarReserva.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCancelarReserva.Click += BtnCancelarSeleccionada_Click;

            // ── Barra de estado ──
            barraEstado = new StatusStrip();
            lblEstado = new ToolStripStatusLabel("Listo.");
            barraEstado.Items.Add(lblEstado);

            // ── Se agregan a la ventana ──
            this.Controls.Add(lblBienvenida);
            this.Controls.Add(lblPermisos);
            this.Controls.Add(grpFiltros);
            this.Controls.Add(dgvReservas);
            this.Controls.Add(btnNuevaReserva);
            this.Controls.Add(btnCancelarReserva);
            this.Controls.Add(barraEstado);
            this.Controls.Add(menu);   // el menu se agrega ultimo para que quede arriba
        }

        // ─── Permisos segun el rol (polimorfismo) ───────────────────────────

        /// <summary>
        /// Habilita o deshabilita opciones preguntandole al objeto Usuario.
        /// No hay ningun "if (usuario is Administrador)".
        /// </summary>
        private void AplicarPermisosDelRol()
        {
            lblBienvenida.Text = $"Bienvenido/a, {usuarioActual.NombreCompleto}";
            lblPermisos.Text = usuarioActual.DescribirPermisos();

            // El menu Gestion solo esta disponible para quien pueda gestionar.
            menuGestion.Enabled = usuarioActual.PuedeGestionarLaboratorios();

            if (!menuGestion.Enabled)
                menuGestion.ToolTipText = "Solo disponible para el rol Administrador.";

            // Un usuario sin permisos de gestion arranca viendo solo sus reservas.
            chkSoloMisReservas.Checked = !usuarioActual.PuedeGestionarLaboratorios();
        }

        // ─── Suscripcion a los eventos del gestor ───────────────────────────

        private void SuscribirseAEventos()
        {
            gestor.ReservaCreada += Gestor_ReservaCreada;
            gestor.ReservaCancelada += Gestor_ReservaCancelada;

            // IMPORTANTE: al cerrar la ventana hay que DESUSCRIBIRSE. Si no, el gestor
            // sigue guardando una referencia a este formulario y el Garbage Collector
            // no puede liberarlo (perdida de memoria por eventos).
            this.FormClosed += (s, e) =>
            {
                gestor.ReservaCreada -= Gestor_ReservaCreada;
                gestor.ReservaCancelada -= Gestor_ReservaCancelada;
            };
        }

        /// <summary>
        /// Manejador del evento. Firma estandar de .NET: (object sender, TEventArgs e).
        /// La grilla se actualiza sola sin que la pantalla de alta tenga que avisarle.
        /// </summary>
        private void Gestor_ReservaCreada(object sender, ReservaEventArgs e)
        {
            RefrescarGrilla();
            lblEstado.Text = e.Descripcion;
        }

        private void Gestor_ReservaCancelada(object sender, ReservaEventArgs e)
        {
            RefrescarGrilla();
            lblEstado.Text = e.Descripcion;
        }

        // ─── Carga de la grilla ─────────────────────────────────────────────

        /// <summary>
        /// Vuelve a leer las reservas y aplica los filtros elegidos.
        /// Los filtros se arman con expresiones lambda encadenadas.
        /// </summary>
        private void RefrescarGrilla()
        {
            try
            {
                IEnumerable<Reserva> consulta = gestor.Reservas.Listar();

                // Filtro 1: solo las del usuario logueado
                if (chkSoloMisReservas.Checked)
                    consulta = consulta.Where(r => r.LegajoUsuario == usuarioActual.Legajo);

                // Filtro 2: por estado
                string estadoElegido = cmbFiltroEstado.SelectedItem as string;

                if (!string.IsNullOrEmpty(estadoElegido) && estadoElegido != "(Todos)")
                {
                    EstadoReserva estado = (EstadoReserva)Enum.Parse(
                        typeof(EstadoReserva), estadoElegido);

                    consulta = consulta.Where(r => r.Estado == estado);
                }

                List<Reserva> resultado = consulta
                    .OrderBy(r => r.Fecha)
                    .ThenBy(r => r.HoraInicio)
                    .ToList();

                dgvReservas.DataSource = reportes.ArmarFilas(resultado);

                // La columna Original guarda el objeto Reserva: se usa por codigo, no se muestra.
                if (dgvReservas.Columns["Original"] != null)
                    dgvReservas.Columns["Original"].Visible = false;

                lblEstado.Text = $"{resultado.Count} reserva(s) en pantalla.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo actualizar la lista de reservas:\n\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Filtro_Cambiado(object sender, EventArgs e)
        {
            RefrescarGrilla();
        }

        /// <summary>Colorea las filas segun el estado, para que se lean de un vistazo.</summary>
        private void DgvReservas_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow fila in dgvReservas.Rows)
            {
                FilaReserva datos = fila.DataBoundItem as FilaReserva;

                if (datos == null) continue;

                if (datos.Original.Estado == EstadoReserva.Cancelada)
                {
                    fila.DefaultCellStyle.ForeColor = Color.Gray;
                    fila.DefaultCellStyle.Font = new Font(dgvReservas.Font, FontStyle.Strikeout);
                }
                else if (datos.Original.Estado == EstadoReserva.Pendiente)
                {
                    fila.DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
                }
                else if (datos.Original.EstaVencida)
                {
                    fila.DefaultCellStyle.ForeColor = Color.DarkGray;
                }
            }
        }

        /// <summary>Devuelve la reserva seleccionada en la grilla, o null.</summary>
        private Reserva ObtenerReservaSeleccionada()
        {
            if (dgvReservas.CurrentRow == null) return null;

            FilaReserva fila = dgvReservas.CurrentRow.DataBoundItem as FilaReserva;

            return fila != null ? fila.Original : null;
        }

        // ─── Opciones del menu ──────────────────────────────────────────────

        private void MenuNuevaReserva_Click(object sender, EventArgs e)
        {
            using (FormNuevaReserva form = new FormNuevaReserva(gestor, usuarioActual))
            {
                form.ShowDialog(this);
            }
            // No hace falta refrescar aca: el evento ReservaCreada ya lo hizo.
        }

        private void MenuCancelarReserva_Click(object sender, EventArgs e)
        {
            using (FormCancelarReserva form = new FormCancelarReserva(gestor, usuarioActual))
            {
                form.ShowDialog(this);
            }
        }

        private void MenuDisponibilidad_Click(object sender, EventArgs e)
        {
            using (FormDisponibilidad form = new FormDisponibilidad(gestor, usuarioActual))
            {
                form.ShowDialog(this);
            }
        }

        private void MenuLaboratorios_Click(object sender, EventArgs e)
        {
            using (FormLaboratorios form = new FormLaboratorios(gestor, usuarioActual))
            {
                form.ShowDialog(this);
            }
        }

        private void MenuUsuarios_Click(object sender, EventArgs e)
        {
            using (FormUsuarios form = new FormUsuarios(gestor, usuarioActual))
            {
                form.ShowDialog(this);
            }
        }

        private void MenuReportes_Click(object sender, EventArgs e)
        {
            using (FormReportes form = new FormReportes(gestor))
            {
                form.ShowDialog(this);
            }
        }

        private void MenuSalir_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ─── Cancelacion rapida desde la grilla ─────────────────────────────

        private void BtnCancelarSeleccionada_Click(object sender, EventArgs e)
        {
            Reserva seleccionada = ObtenerReservaSeleccionada();

            if (seleccionada == null)
            {
                MessageBox.Show("Seleccione una reserva de la lista.", "Atencion",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult respuesta = MessageBox.Show(
                $"Confirma la cancelacion de la reserva {seleccionada.Id}?\n\n" +
                $"Laboratorio: {seleccionada.IdLaboratorio}\n" +
                $"Fecha: {seleccionada.Fecha:dd/MM/yyyy} {seleccionada.RangoHorario}",
                "Cancelar reserva", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (respuesta != DialogResult.Yes) return;

            try
            {
                // Las reglas de quien puede cancelar estan en el gestor y en el usuario,
                // no en este formulario.
                gestor.CancelarReserva(usuarioActual, seleccionada.Id);

                MessageBox.Show("La reserva fue cancelada.", "Listo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Excepciones.PermisoDenegadoException ex)
            {
                MessageBox.Show(ex.Message, "Permiso denegado",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Excepciones.SistemaReservasException ex)
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
