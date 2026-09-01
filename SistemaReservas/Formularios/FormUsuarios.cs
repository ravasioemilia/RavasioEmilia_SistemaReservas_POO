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
    /// Alta y listado de usuarios, con distincion de rol.
    ///
    /// Al elegir el rol en el ComboBox se instancia la CLASE CONCRETA correspondiente
    /// (Administrador, Docente o Alumno). Los limites que se muestran en pantalla no
    /// estan escritos en el formulario: se los pide al objeto recien creado.
    /// </summary>
    public class FormUsuarios : Form
    {
        private readonly GestorReservas gestor;
        private readonly Usuario usuarioActual;

        private Label lblTitulo;
        private Label lblFiltro;
        private ComboBox cmbFiltroRol;
        private DataGridView dgvUsuarios;

        private GroupBox grpAlta;
        private Label lblLegajo;
        private TextBox txtLegajo;
        private Label lblRol;
        private ComboBox cmbRol;
        private Label lblApellido;
        private TextBox txtApellido;
        private Label lblNombre;
        private TextBox txtNombre;
        private Label lblEmail;
        private TextBox txtEmail;
        private CheckBox chkActivo;
        private Label lblLimites;

        private Button btnAgregar;
        private Button btnLimpiar;
        private Button btnCerrar;

        public FormUsuarios(GestorReservas gestor, Usuario usuarioActual)
        {
            if (gestor == null) throw new ArgumentNullException(nameof(gestor));
            if (usuarioActual == null) throw new ArgumentNullException(nameof(usuarioActual));

            if (!usuarioActual.PuedeGestionarLaboratorios())
                throw new PermisoDenegadoException(usuarioActual.Legajo, "gestionar usuarios");

            this.gestor = gestor;
            this.usuarioActual = usuarioActual;

            InicializarComponentes();
            CargarGrilla();
            MostrarLimitesDelRol();
        }

        private void InicializarComponentes()
        {
            this.Text = "Gestion de usuarios";
            this.Size = new Size(950, 660);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(850, 620);

            lblTitulo = new Label();
            lblTitulo.Text = "Usuarios del sistema";
            lblTitulo.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTitulo.Location = new Point(20, 15);
            lblTitulo.Size = new Size(400, 25);

            lblFiltro = new Label();
            lblFiltro.Text = "Ver rol:";
            lblFiltro.Location = new Point(600, 20);
            lblFiltro.Size = new Size(55, 20);
            lblFiltro.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            cmbFiltroRol = new ComboBox();
            cmbFiltroRol.Location = new Point(660, 17);
            cmbFiltroRol.Size = new Size(250, 24);
            cmbFiltroRol.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFiltroRol.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbFiltroRol.Items.AddRange(new object[] { "(Todos)", "Administrador", "Docente", "Alumno" });
            cmbFiltroRol.SelectedIndex = 0;
            cmbFiltroRol.SelectedIndexChanged += (s, e) => CargarGrilla();

            dgvUsuarios = new DataGridView();
            dgvUsuarios.Location = new Point(20, 50);
            dgvUsuarios.Size = new Size(890, 240);
            dgvUsuarios.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvUsuarios.ReadOnly = true;
            dgvUsuarios.AllowUserToAddRows = false;
            dgvUsuarios.RowHeadersVisible = false;
            dgvUsuarios.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvUsuarios.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // ── Panel de alta ──
            grpAlta = new GroupBox();
            grpAlta.Text = "Alta de usuario";
            grpAlta.Location = new Point(20, 300);
            grpAlta.Size = new Size(890, 235);
            grpAlta.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            lblLegajo = new Label();
            lblLegajo.Text = "Legajo:";
            lblLegajo.Location = new Point(20, 35);
            lblLegajo.Size = new Size(80, 20);

            txtLegajo = new TextBox();
            txtLegajo.Location = new Point(105, 32);
            txtLegajo.Size = new Size(180, 24);
            txtLegajo.MaxLength = 20;
            txtLegajo.CharacterCasing = CharacterCasing.Upper;

            lblRol = new Label();
            lblRol.Text = "Rol:";
            lblRol.Location = new Point(320, 35);
            lblRol.Size = new Size(40, 20);

            cmbRol = new ComboBox();
            cmbRol.Location = new Point(365, 32);
            cmbRol.Size = new Size(180, 24);
            cmbRol.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRol.Items.AddRange(new object[] { "Alumno", "Docente", "Administrador" });
            cmbRol.SelectedIndex = 0;
            cmbRol.SelectedIndexChanged += (s, e) => MostrarLimitesDelRol();

            lblApellido = new Label();
            lblApellido.Text = "Apellido:";
            lblApellido.Location = new Point(20, 75);
            lblApellido.Size = new Size(80, 20);

            txtApellido = new TextBox();
            txtApellido.Location = new Point(105, 72);
            txtApellido.Size = new Size(180, 24);
            txtApellido.MaxLength = 40;

            lblNombre = new Label();
            lblNombre.Text = "Nombre:";
            lblNombre.Location = new Point(320, 75);
            lblNombre.Size = new Size(60, 20);

            txtNombre = new TextBox();
            txtNombre.Location = new Point(365, 72);
            txtNombre.Size = new Size(180, 24);
            txtNombre.MaxLength = 40;

            lblEmail = new Label();
            lblEmail.Text = "Email:";
            lblEmail.Location = new Point(20, 115);
            lblEmail.Size = new Size(80, 20);

            txtEmail = new TextBox();
            txtEmail.Location = new Point(105, 112);
            txtEmail.Size = new Size(440, 24);
            txtEmail.MaxLength = 80;

            chkActivo = new CheckBox();
            chkActivo.Text = "Usuario activo";
            chkActivo.Location = new Point(105, 148);
            chkActivo.Size = new Size(150, 24);
            chkActivo.Checked = true;

            lblLimites = new Label();
            lblLimites.Location = new Point(575, 32);
            lblLimites.Size = new Size(295, 140);
            lblLimites.BorderStyle = BorderStyle.FixedSingle;
            lblLimites.Padding = new Padding(10);
            lblLimites.TextAlign = ContentAlignment.MiddleLeft;

            grpAlta.Controls.Add(lblLegajo);
            grpAlta.Controls.Add(txtLegajo);
            grpAlta.Controls.Add(lblRol);
            grpAlta.Controls.Add(cmbRol);
            grpAlta.Controls.Add(lblApellido);
            grpAlta.Controls.Add(txtApellido);
            grpAlta.Controls.Add(lblNombre);
            grpAlta.Controls.Add(txtNombre);
            grpAlta.Controls.Add(lblEmail);
            grpAlta.Controls.Add(txtEmail);
            grpAlta.Controls.Add(chkActivo);
            grpAlta.Controls.Add(lblLimites);

            // ── Botones ──
            btnAgregar = new Button();
            btnAgregar.Text = "Agregar usuario";
            btnAgregar.Location = new Point(20, 550);
            btnAgregar.Size = new Size(150, 38);
            btnAgregar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnAgregar.Click += BtnAgregar_Click;

            btnLimpiar = new Button();
            btnLimpiar.Text = "Limpiar";
            btnLimpiar.Location = new Point(180, 550);
            btnLimpiar.Size = new Size(120, 38);
            btnLimpiar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnLimpiar.Click += (s, e) => LimpiarFormulario();

            btnCerrar = new Button();
            btnCerrar.Text = "Cerrar";
            btnCerrar.Location = new Point(790, 550);
            btnCerrar.Size = new Size(120, 38);
            btnCerrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCerrar.Click += (s, e) => this.Close();

            this.CancelButton = btnCerrar;

            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblFiltro);
            this.Controls.Add(cmbFiltroRol);
            this.Controls.Add(dgvUsuarios);
            this.Controls.Add(grpAlta);
            this.Controls.Add(btnAgregar);
            this.Controls.Add(btnLimpiar);
            this.Controls.Add(btnCerrar);
        }

        // ─── Listado ────────────────────────────────────────────────────────

        private void CargarGrilla()
        {
            try
            {
                IEnumerable<Usuario> consulta = gestor.Usuarios.Listar();

                string rolElegido = cmbFiltroRol.SelectedItem as string;

                // Filtro por rol usando la propiedad polimorfica Rol.
                if (!string.IsNullOrEmpty(rolElegido) && rolElegido != "(Todos)")
                    consulta = consulta.Where(u => u.Rol == rolElegido);

                // Se arma una fila por usuario mostrando los limites que devuelve cada objeto.
                var filas = consulta
                    .OrderBy(u => u.Rol)
                    .ThenBy(u => u.Apellido)
                    .Select(u => new
                    {
                        Legajo = u.Legajo,
                        Rol = u.Rol,
                        Nombre = u.NombreCompleto,
                        Email = u.Email,
                        Activo = u.Activo ? "Si" : "No",
                        LimiteSemanal = u.ObtenerLimiteReservasSemanales() == int.MaxValue
                            ? "Sin limite"
                            : u.ObtenerLimiteReservasSemanales().ToString(),
                        AnticipacionDias = u.ObtenerAnticipacionMaximaDias()
                    })
                    .ToList();

                dgvUsuarios.DataSource = filas;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los usuarios:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Creacion del objeto segun el rol elegido ───────────────────────

        /// <summary>
        /// Crea la instancia concreta que corresponde al rol seleccionado.
        /// Devuelve el tipo BASE Usuario: de ahi en adelante el codigo trabaja
        /// con la abstraccion y no necesita saber cual de las tres es.
        /// </summary>
        private Usuario CrearUsuarioSegunRolElegido(string legajo, string nombre,
                                                    string apellido, string email, bool activo)
        {
            string rol = cmbRol.SelectedItem as string;

            switch (rol)
            {
                case "Administrador": return new Administrador(legajo, nombre, apellido, email, activo);
                case "Docente": return new Docente(legajo, nombre, apellido, email, activo);
                case "Alumno": return new Alumno(legajo, nombre, apellido, email, activo);
                default:
                    throw new SistemaReservasException("Debe seleccionar un rol valido.");
            }
        }

        /// <summary>
        /// Muestra los limites del rol elegido creando un objeto de prueba y
        /// preguntandole. Los numeros no estan escritos en el formulario:
        /// viven en cada clase derivada.
        /// </summary>
        private void MostrarLimitesDelRol()
        {
            try
            {
                Usuario ejemplo = CrearUsuarioSegunRolElegido(
                    "EJEMPLO", "Ejemplo", "Ejemplo", "ejemplo@instituto.edu.ar", true);

                string limite = ejemplo.ObtenerLimiteReservasSemanales() == int.MaxValue
                    ? "sin limite"
                    : ejemplo.ObtenerLimiteReservasSemanales() + " por semana";

                lblLimites.Text =
                    $"Reglas del rol {ejemplo.Rol}\n\n" +
                    $"Reservas: {limite}\n" +
                    $"Anticipacion maxima: {ejemplo.ObtenerAnticipacionMaximaDias()} dias\n" +
                    $"Gestiona laboratorios: {(ejemplo.PuedeGestionarLaboratorios() ? "Si" : "No")}\n\n" +
                    ejemplo.DescribirPermisos();
            }
            catch (Exception)
            {
                lblLimites.Text = "Seleccione un rol.";
            }
        }

        // ─── Alta ───────────────────────────────────────────────────────────

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            try
            {
                Usuario nuevo = CrearUsuarioSegunRolElegido(
                    txtLegajo.Text,
                    txtNombre.Text,
                    txtApellido.Text,
                    txtEmail.Text,
                    chkActivo.Checked);

                List<string> errores = nuevo.Validar();

                if (errores.Count > 0)
                {
                    MessageBox.Show("Corrija los siguientes datos:\n\n - " +
                        string.Join("\n - ", errores), "Datos incompletos",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                gestor.Usuarios.Agregar(nuevo);

                MessageBox.Show(
                    $"Usuario dado de alta:\n\n{nuevo.NombreCompleto}\n" +
                    $"Rol: {nuevo.Rol}\nLegajo: {nuevo.Legajo}",
                    "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                CargarGrilla();
                LimpiarFormulario();
            }
            catch (EntidadDuplicadaException ex)
            {
                MessageBox.Show(ex.Message, "Legajo repetido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Datos invalidos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (SistemaReservasException ex)
            {
                MessageBox.Show(ex.Message, "No se pudo dar de alta",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inesperado:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarFormulario()
        {
            txtLegajo.Clear();
            txtApellido.Clear();
            txtNombre.Clear();
            txtEmail.Clear();
            chkActivo.Checked = true;
            txtLegajo.Focus();
        }
    }
}
