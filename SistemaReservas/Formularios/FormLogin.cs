using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SistemaReservas.Modelos;
using SistemaReservas.Servicios;

namespace SistemaReservas.Formularios
{
    /// <summary>
    /// Pantalla de inicio de sesion. El usuario se elige de una lista.
    ///
    /// Al seleccionarlo se muestra la descripcion de sus permisos llamando a
    /// usuario.DescribirPermisos(): un mismo llamado devuelve un texto distinto
    /// para cada rol. Es el POLIMORFISMO visible en pantalla.
    /// </summary>
    public class FormLogin : Form
    {
        // ─── Datos que necesita el formulario ───────────────────────────────
        private readonly GestorReservas gestor;

        /// <summary>Usuario elegido. Lo lee Program.cs despues de cerrar la ventana.</summary>
        public Usuario UsuarioSeleccionado { get; private set; }

        // ─── Controles ──────────────────────────────────────────────────────
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Label lblUsuario;
        private ComboBox cmbUsuarios;
        private Label lblPermisos;
        private Button btnIngresar;
        private Button btnSalir;

        // ─── Constructor ────────────────────────────────────────────────────

        public FormLogin(GestorReservas gestor)
        {
            if (gestor == null) throw new ArgumentNullException(nameof(gestor));

            this.gestor = gestor;

            InicializarComponentes();
            CargarUsuarios();
        }

        /// <summary>Crea y ubica todos los controles de la ventana.</summary>
        private void InicializarComponentes()
        {
            // ── Ventana ──
            this.Text = "Sistema de Reservas de Laboratorios - Ingreso";
            this.Size = new Size(520, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // ── Titulo ──
            lblTitulo = new Label();
            lblTitulo.Text = "Sistema de Reservas de Laboratorios";
            lblTitulo.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitulo.Location = new Point(30, 25);
            lblTitulo.Size = new Size(450, 30);

            lblSubtitulo = new Label();
            lblSubtitulo.Text = "Seleccione su usuario para ingresar al sistema.";
            lblSubtitulo.Location = new Point(30, 58);
            lblSubtitulo.Size = new Size(450, 20);

            // ── Seleccion de usuario ──
            lblUsuario = new Label();
            lblUsuario.Text = "Usuario:";
            lblUsuario.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblUsuario.Location = new Point(30, 100);
            lblUsuario.Size = new Size(80, 20);

            cmbUsuarios = new ComboBox();
            cmbUsuarios.Location = new Point(30, 122);
            cmbUsuarios.Size = new Size(440, 24);
            // DropDownList = solo se puede elegir de la lista, no escribir texto libre.
            cmbUsuarios.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbUsuarios.SelectedIndexChanged += CmbUsuarios_SelectedIndexChanged;

            // ── Cuadro de permisos ──
            lblPermisos = new Label();
            lblPermisos.Location = new Point(30, 165);
            lblPermisos.Size = new Size(440, 90);
            lblPermisos.BorderStyle = BorderStyle.FixedSingle;
            lblPermisos.TextAlign = ContentAlignment.MiddleLeft;
            lblPermisos.Padding = new Padding(10);
            lblPermisos.Text = "Seleccione un usuario para ver sus permisos.";

            // ── Botones ──
            btnIngresar = new Button();
            btnIngresar.Text = "Ingresar";
            btnIngresar.Location = new Point(270, 275);
            btnIngresar.Size = new Size(100, 35);
            btnIngresar.Click += BtnIngresar_Click;

            btnSalir = new Button();
            btnSalir.Text = "Salir";
            btnSalir.Location = new Point(380, 275);
            btnSalir.Size = new Size(90, 35);
            btnSalir.Click += BtnSalir_Click;

            // Enter = Ingresar, Escape = Salir
            this.AcceptButton = btnIngresar;
            this.CancelButton = btnSalir;

            // ── Se agregan a la ventana ──
            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblSubtitulo);
            this.Controls.Add(lblUsuario);
            this.Controls.Add(cmbUsuarios);
            this.Controls.Add(lblPermisos);
            this.Controls.Add(btnIngresar);
            this.Controls.Add(btnSalir);
        }

        // ─── Carga de datos ─────────────────────────────────────────────────

        private void CargarUsuarios()
        {
            try
            {
                List<Usuario> usuarios = gestor.ObtenerUsuariosActivos();

                if (usuarios.Count == 0)
                {
                    MessageBox.Show(
                        "No hay usuarios cargados en el sistema.\n\n" +
                        "Verifique el archivo Datos\\usuarios.txt.",
                        "Sin usuarios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Se enlaza la lista de objetos directamente al ComboBox.
                // El texto que se ve es el que devuelve Usuario.ToString().
                cmbUsuarios.DataSource = usuarios;
                cmbUsuarios.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudieron cargar los usuarios:\n\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Manejadores de eventos ─────────────────────────────────────────

        /// <summary>
        /// Al cambiar la seleccion se pide al usuario que describa sus propios permisos.
        /// El formulario no sabe ni le importa de que rol se trata.
        /// </summary>
        private void CmbUsuarios_SelectedIndexChanged(object sender, EventArgs e)
        {
            Usuario seleccionado = cmbUsuarios.SelectedItem as Usuario;

            if (seleccionado == null) return;

            lblPermisos.Text =
                $"Legajo: {seleccionado.Legajo}\n" +
                $"Nombre: {seleccionado.NombreCompleto}\n" +
                $"Email:  {seleccionado.Email}\n\n" +
                seleccionado.DescribirPermisos();
        }

        private void BtnIngresar_Click(object sender, EventArgs e)
        {
            Usuario seleccionado = cmbUsuarios.SelectedItem as Usuario;

            if (seleccionado == null)
            {
                MessageBox.Show("Debe seleccionar un usuario.", "Atencion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UsuarioSeleccionado = seleccionado;
            this.DialogResult = DialogResult.OK;   // avisa a Program.cs que se ingreso
            this.Close();
        }

        private void BtnSalir_Click(object sender, EventArgs e)
        {
            UsuarioSeleccionado = null;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
