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
    /// ABM de laboratorios: alta, baja, modificacion y listado.
    /// Solo accesible para el Administrador (se verifica en el constructor).
    ///
    /// Se apoya directamente en IRepositorio&lt;Laboratorio&gt;: Agregar, Actualizar,
    /// Eliminar y Listar son los cinco metodos de la interfaz generica.
    /// </summary>
    public class FormLaboratorios : Form
    {
        private readonly GestorReservas gestor;
        private readonly Usuario usuarioActual;

        /// <summary>Id del laboratorio en edicion. Vacio = se esta dando de alta uno nuevo.</summary>
        private string idEnEdicion = string.Empty;

        private Label lblTitulo;
        private DataGridView dgvLaboratorios;

        private GroupBox grpDatos;
        private Label lblId;
        private TextBox txtId;
        private Label lblNombre;
        private TextBox txtNombre;
        private Label lblUbicacion;
        private TextBox txtUbicacion;
        private Label lblCapacidad;
        private NumericUpDown numCapacidad;
        private Label lblEquipamiento;
        private TextBox txtEquipamiento;
        private CheckBox chkActivo;

        private Button btnNuevo;
        private Button btnGuardar;
        private Button btnEliminar;
        private Button btnCerrar;

        public FormLaboratorios(GestorReservas gestor, Usuario usuarioActual)
        {
            if (gestor == null) throw new ArgumentNullException(nameof(gestor));
            if (usuarioActual == null) throw new ArgumentNullException(nameof(usuarioActual));

            // Control de acceso: se le pregunta al usuario, no se mira su tipo.
            if (!usuarioActual.PuedeGestionarLaboratorios())
                throw new PermisoDenegadoException(usuarioActual.Legajo, "gestionar laboratorios");

            this.gestor = gestor;
            this.usuarioActual = usuarioActual;

            InicializarComponentes();
            CargarGrilla();
            PrepararAlta();
        }

        private void InicializarComponentes()
        {
            this.Text = "Gestion de laboratorios";
            this.Size = new Size(950, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(850, 600);

            lblTitulo = new Label();
            lblTitulo.Text = "Laboratorios registrados";
            lblTitulo.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTitulo.Location = new Point(20, 15);
            lblTitulo.Size = new Size(500, 25);

            dgvLaboratorios = new DataGridView();
            dgvLaboratorios.Location = new Point(20, 45);
            dgvLaboratorios.Size = new Size(890, 230);
            dgvLaboratorios.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvLaboratorios.ReadOnly = true;
            dgvLaboratorios.AllowUserToAddRows = false;
            dgvLaboratorios.RowHeadersVisible = false;
            dgvLaboratorios.MultiSelect = false;
            dgvLaboratorios.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvLaboratorios.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvLaboratorios.SelectionChanged += DgvLaboratorios_SelectionChanged;

            // ── Panel de datos ──
            grpDatos = new GroupBox();
            grpDatos.Text = "Datos del laboratorio";
            grpDatos.Location = new Point(20, 285);
            grpDatos.Size = new Size(890, 220);
            grpDatos.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            lblId = new Label();
            lblId.Text = "Codigo:";
            lblId.Location = new Point(20, 35);
            lblId.Size = new Size(90, 20);

            txtId = new TextBox();
            txtId.Location = new Point(115, 32);
            txtId.Size = new Size(150, 24);
            txtId.MaxLength = 10;
            txtId.CharacterCasing = CharacterCasing.Upper;

            lblNombre = new Label();
            lblNombre.Text = "Nombre:";
            lblNombre.Location = new Point(300, 35);
            lblNombre.Size = new Size(70, 20);

            txtNombre = new TextBox();
            txtNombre.Location = new Point(375, 32);
            txtNombre.Size = new Size(480, 24);
            txtNombre.MaxLength = 60;

            lblUbicacion = new Label();
            lblUbicacion.Text = "Ubicacion:";
            lblUbicacion.Location = new Point(20, 75);
            lblUbicacion.Size = new Size(90, 20);

            txtUbicacion = new TextBox();
            txtUbicacion.Location = new Point(115, 72);
            txtUbicacion.Size = new Size(400, 24);
            txtUbicacion.MaxLength = 60;

            lblCapacidad = new Label();
            lblCapacidad.Text = "Capacidad:";
            lblCapacidad.Location = new Point(540, 75);
            lblCapacidad.Size = new Size(80, 20);

            numCapacidad = new NumericUpDown();
            numCapacidad.Location = new Point(625, 72);
            numCapacidad.Size = new Size(80, 24);
            numCapacidad.Minimum = 1;
            numCapacidad.Maximum = 200;
            numCapacidad.Value = 20;

            lblEquipamiento = new Label();
            lblEquipamiento.Text = "Equipamiento:";
            lblEquipamiento.Location = new Point(20, 115);
            lblEquipamiento.Size = new Size(90, 20);

            txtEquipamiento = new TextBox();
            txtEquipamiento.Location = new Point(115, 112);
            txtEquipamiento.Size = new Size(740, 24);
            txtEquipamiento.MaxLength = 120;

            chkActivo = new CheckBox();
            chkActivo.Text = "Laboratorio activo (disponible para reservar)";
            chkActivo.Location = new Point(115, 150);
            chkActivo.Size = new Size(320, 24);
            chkActivo.Checked = true;

            grpDatos.Controls.Add(lblId);
            grpDatos.Controls.Add(txtId);
            grpDatos.Controls.Add(lblNombre);
            grpDatos.Controls.Add(txtNombre);
            grpDatos.Controls.Add(lblUbicacion);
            grpDatos.Controls.Add(txtUbicacion);
            grpDatos.Controls.Add(lblCapacidad);
            grpDatos.Controls.Add(numCapacidad);
            grpDatos.Controls.Add(lblEquipamiento);
            grpDatos.Controls.Add(txtEquipamiento);
            grpDatos.Controls.Add(chkActivo);

            // ── Botones ──
            btnNuevo = new Button();
            btnNuevo.Text = "Nuevo";
            btnNuevo.Location = new Point(20, 520);
            btnNuevo.Size = new Size(120, 38);
            btnNuevo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNuevo.Click += (s, e) => PrepararAlta();

            btnGuardar = new Button();
            btnGuardar.Text = "Guardar";
            btnGuardar.Location = new Point(150, 520);
            btnGuardar.Size = new Size(120, 38);
            btnGuardar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnGuardar.Click += BtnGuardar_Click;

            btnEliminar = new Button();
            btnEliminar.Text = "Eliminar";
            btnEliminar.Location = new Point(280, 520);
            btnEliminar.Size = new Size(120, 38);
            btnEliminar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnEliminar.Click += BtnEliminar_Click;

            btnCerrar = new Button();
            btnCerrar.Text = "Cerrar";
            btnCerrar.Location = new Point(790, 520);
            btnCerrar.Size = new Size(120, 38);
            btnCerrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCerrar.Click += (s, e) => this.Close();

            this.CancelButton = btnCerrar;

            this.Controls.Add(lblTitulo);
            this.Controls.Add(dgvLaboratorios);
            this.Controls.Add(grpDatos);
            this.Controls.Add(btnNuevo);
            this.Controls.Add(btnGuardar);
            this.Controls.Add(btnEliminar);
            this.Controls.Add(btnCerrar);
        }

        // ─── Carga y seleccion ──────────────────────────────────────────────

        private void CargarGrilla()
        {
            try
            {
                dgvLaboratorios.DataSource = gestor.Laboratorios.Listar()
                    .OrderBy(l => l.Id)
                    .ToList();

                // Se ocultan las propiedades calculadas: no aportan a la grilla.
                OcultarColumna("Descripcion");
                OcultarColumna("EsAptoParaGrupos");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los laboratorios:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OcultarColumna(string nombre)
        {
            if (dgvLaboratorios.Columns[nombre] != null)
                dgvLaboratorios.Columns[nombre].Visible = false;
        }

        private void DgvLaboratorios_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvLaboratorios.CurrentRow == null) return;

            Laboratorio lab = dgvLaboratorios.CurrentRow.DataBoundItem as Laboratorio;

            if (lab == null) return;

            // Pasa a modo MODIFICACION: el codigo no se puede cambiar.
            idEnEdicion = lab.Id;

            txtId.Text = lab.Id;
            txtId.Enabled = false;
            txtNombre.Text = lab.Nombre;
            txtUbicacion.Text = lab.Ubicacion;
            numCapacidad.Value = lab.CapacidadPCs;
            txtEquipamiento.Text = lab.Equipamiento;
            chkActivo.Checked = lab.Activo;

            btnEliminar.Enabled = true;
            grpDatos.Text = $"Modificando el laboratorio {lab.Id}";
        }

        /// <summary>Limpia el panel para cargar un laboratorio nuevo.</summary>
        private void PrepararAlta()
        {
            idEnEdicion = string.Empty;

            txtId.Enabled = true;
            txtId.Clear();
            txtNombre.Clear();
            txtUbicacion.Clear();
            txtEquipamiento.Clear();
            numCapacidad.Value = 20;
            chkActivo.Checked = true;

            dgvLaboratorios.ClearSelection();
            btnEliminar.Enabled = false;
            grpDatos.Text = "Datos del laboratorio (alta nueva)";

            txtId.Focus();
        }

        // ─── Alta y modificacion ────────────────────────────────────────────

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            try
            {
                // Las propiedades del objeto validan solas: si algo esta mal,
                // el constructor lanza ArgumentException y se atrapa mas abajo.
                Laboratorio lab = new Laboratorio(
                    txtId.Text,
                    txtNombre.Text,
                    txtUbicacion.Text,
                    (int)numCapacidad.Value,
                    txtEquipamiento.Text,
                    chkActivo.Checked);

                // Validacion adicional con IValidable.
                List<string> errores = lab.Validar();

                if (errores.Count > 0)
                {
                    MessageBox.Show("Corrija los siguientes datos:\n\n - " +
                        string.Join("\n - ", errores), "Datos incompletos",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(idEnEdicion))
                {
                    gestor.Laboratorios.Agregar(lab);       // ALTA
                    MessageBox.Show($"Laboratorio {lab.Id} dado de alta.", "Listo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    gestor.Laboratorios.Actualizar(lab);    // MODIFICACION
                    MessageBox.Show($"Laboratorio {lab.Id} actualizado.", "Listo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                CargarGrilla();
                PrepararAlta();
            }
            catch (EntidadDuplicadaException ex)
            {
                MessageBox.Show(ex.Message, "Codigo repetido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (ArgumentException ex)
            {
                // Viene de las validaciones de las propiedades de Laboratorio.
                MessageBox.Show(ex.Message, "Datos invalidos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (SistemaReservasException ex)
            {
                MessageBox.Show(ex.Message, "No se pudo guardar",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inesperado:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Baja ───────────────────────────────────────────────────────────

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(idEnEdicion))
            {
                MessageBox.Show("Seleccione un laboratorio de la lista.", "Atencion",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Regla de integridad: no se elimina un laboratorio con reservas vigentes.
                int reservasVigentes = gestor.Reservas.Listar()
                    .Count(r => r.IdLaboratorio == idEnEdicion && r.EstaVigente);

                if (reservasVigentes > 0)
                {
                    MessageBox.Show(
                        $"El laboratorio {idEnEdicion} tiene {reservasVigentes} reserva(s) vigente(s).\n\n" +
                        "No se puede eliminar. Si ya no se usa, desmarque 'Laboratorio activo' " +
                        "y guarde: asi deja de aparecer para reservar pero se conserva el historial.",
                        "No se puede eliminar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult respuesta = MessageBox.Show(
                    $"Confirma la eliminacion del laboratorio {idEnEdicion}?\n\n" +
                    "Esta accion no se puede deshacer.",
                    "Eliminar laboratorio", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (respuesta != DialogResult.Yes) return;

                gestor.Laboratorios.Eliminar(idEnEdicion);

                MessageBox.Show("Laboratorio eliminado.", "Listo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                CargarGrilla();
                PrepararAlta();
            }
            catch (EntidadNoEncontradaException ex)
            {
                MessageBox.Show(ex.Message, "No encontrado",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
