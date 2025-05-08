using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;

namespace CloudShot
{
	public partial class ConfigForm : Form
	{
		private bool isLoading = true;
		private AppSettings settings;

		// Controls for keyboard shortcuts
		private Label lblKeyboardShortcuts;
		private Label lblUndo;
		private Label lblSave;
		private Label lblCopy;
		private Label lblCancel;
		private Label lblOcr;
		private Label lblScp;
		private Label lblColorPicker;
		private HotkeyControl txtUndo;
		private HotkeyControl txtSave;
		private HotkeyControl txtCopy;
		private HotkeyControl txtCancel;
		private HotkeyControl txtOcr;
		private HotkeyControl txtScp;
		private HotkeyControl txtColorPicker;

		// Controls for SCP configuration
		private Label lblScpConfig;
		private Label lblScpCommand;
		private Label lblScpClipboardText;
		private TextBox txtScpCommand;
		private TextBox txtScpClipboardText;
        
        // Controls for Color Picker configuration
        private Label lblColorPickerConfig;
        private Label lblColorFormat;
        private ComboBox cmbColorFormat;

		// Control for Windows startup
		private CheckBox chkStartWithWindows;

		// Buttons
		private Button btnSave;
		private Button btnCancel;
		private Button btnReset;

		public ConfigForm(AppSettings settings)
		{
			this.settings = settings;
			InitializeComponents();
			LoadSettings();
			isLoading = false;
		}

		private void InitializeComponents()
		{
			this.Text = "CloudShot Settings";
			this.Size = new Size(500, 750); // Increased height for new controls
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

			// Calculate panel width and positions for proper centering
			int formWidth = this.ClientSize.Width;
			int panelWidth = 460;
			int panelX = (formWidth - panelWidth) / 2;

			// Panel de atajos de teclado
			Panel pnlKeyboardShortcuts = new Panel();
			pnlKeyboardShortcuts.Location = new Point(panelX, 20);
			pnlKeyboardShortcuts.Size = new Size(panelWidth, 320); // Increased height for new shortcut
			pnlKeyboardShortcuts.BorderStyle = BorderStyle.FixedSingle;
			this.Controls.Add(pnlKeyboardShortcuts);

			// Título del panel
			lblKeyboardShortcuts = new Label();
			lblKeyboardShortcuts.Text = "Keyboard Shortcuts";
			lblKeyboardShortcuts.Font = new Font(lblKeyboardShortcuts.Font, FontStyle.Bold);
			lblKeyboardShortcuts.Location = new Point(10, 10);
			lblKeyboardShortcuts.AutoSize = true;
			pnlKeyboardShortcuts.Controls.Add(lblKeyboardShortcuts);

			// Labels para atajos
			lblUndo = new Label();
			lblUndo.Text = "Undo last edit:";
			lblUndo.Location = new Point(10, 50);
			lblUndo.Size = new Size(150, 20);
			pnlKeyboardShortcuts.Controls.Add(lblUndo);

			lblSave = new Label();
			lblSave.Text = "Save to computer:";
			lblSave.Location = new Point(10, 90);
			lblSave.Size = new Size(150, 20);
			pnlKeyboardShortcuts.Controls.Add(lblSave);

			lblCopy = new Label();
			lblCopy.Text = "Copy to clipboard:";
			lblCopy.Location = new Point(10, 130);
			lblCopy.Size = new Size(150, 20);
			pnlKeyboardShortcuts.Controls.Add(lblCopy);

			lblOcr = new Label();
			lblOcr.Text = "Extract text (OCR):";
			lblOcr.Location = new Point(10, 170);
			lblOcr.Size = new Size(150, 20);
			pnlKeyboardShortcuts.Controls.Add(lblOcr);

			lblScp = new Label();
			lblScp.Text = "Upload via SCP:";
			lblScp.Location = new Point(10, 210);
			lblScp.Size = new Size(150, 20);
			pnlKeyboardShortcuts.Controls.Add(lblScp);
            
            lblColorPicker = new Label();
			lblColorPicker.Text = "Color picker:";
			lblColorPicker.Location = new Point(10, 250);
			lblColorPicker.Size = new Size(150, 20);
			pnlKeyboardShortcuts.Controls.Add(lblColorPicker);

			lblCancel = new Label();
			lblCancel.Text = "Cancel capture:";
			lblCancel.Location = new Point(10, 290);
			lblCancel.Size = new Size(150, 20);
			pnlKeyboardShortcuts.Controls.Add(lblCancel);

			// Controles para atajos
			txtUndo = new HotkeyControl();
			txtUndo.Location = new Point(170, 45);
			txtUndo.Size = new Size(260, 25);
			pnlKeyboardShortcuts.Controls.Add(txtUndo);

			txtSave = new HotkeyControl();
			txtSave.Location = new Point(170, 85);
			txtSave.Size = new Size(260, 25);
			pnlKeyboardShortcuts.Controls.Add(txtSave);

			txtCopy = new HotkeyControl();
			txtCopy.Location = new Point(170, 125);
			txtCopy.Size = new Size(260, 25);
			pnlKeyboardShortcuts.Controls.Add(txtCopy);

			txtOcr = new HotkeyControl();
			txtOcr.Location = new Point(170, 165);
			txtOcr.Size = new Size(260, 25);
			pnlKeyboardShortcuts.Controls.Add(txtOcr);

			txtScp = new HotkeyControl();
			txtScp.Location = new Point(170, 205);
			txtScp.Size = new Size(260, 25);
			pnlKeyboardShortcuts.Controls.Add(txtScp);
            
            txtColorPicker = new HotkeyControl();
			txtColorPicker.Location = new Point(170, 245);
			txtColorPicker.Size = new Size(260, 25);
			pnlKeyboardShortcuts.Controls.Add(txtColorPicker);

			txtCancel = new HotkeyControl();
			txtCancel.Location = new Point(170, 285);
			txtCancel.Size = new Size(260, 25);
			pnlKeyboardShortcuts.Controls.Add(txtCancel);

			// SCP configuration panel and its content
			InitializeScpConfigPanel(panelX, panelWidth);
            
            // Color Picker configuration panel
            InitializeColorPickerConfigPanel(panelX, panelWidth);

			// Checkbox to start with Windows
			chkStartWithWindows = new CheckBox();
			chkStartWithWindows.Text = "Start CloudShot automatically with Windows";
			chkStartWithWindows.Location = new Point(panelX, 600); // Centered horizontally
			chkStartWithWindows.Size = new Size(panelWidth, 20);
			chkStartWithWindows.CheckedChanged += ChkStartWithWindows_CheckedChanged;
			this.Controls.Add(chkStartWithWindows);

			// Center the buttons
			int btnWidth = 80;
			int btnSpacing = 10;
			int btnsTotalWidth = (btnWidth * 3) + (btnSpacing * 2);
			int btnsStartX = (formWidth - btnsTotalWidth) / 2;

			// Buttons
			btnSave = new Button();
			btnSave.Text = "Save";
			btnSave.Location = new Point(btnsStartX, 650);
			btnSave.Size = new Size(btnWidth, 30);
			btnSave.Click += BtnSave_Click;
			this.Controls.Add(btnSave);

			btnCancel = new Button();
			btnCancel.Text = "Cancel";
			btnCancel.Location = new Point(btnsStartX + btnWidth + btnSpacing, 650);
			btnCancel.Size = new Size(btnWidth, 30);
			btnCancel.Click += BtnCancel_Click;
			this.Controls.Add(btnCancel);

			btnReset = new Button();
			btnReset.Text = "Reset";
			btnReset.Location = new Point(btnsStartX + (btnWidth + btnSpacing) * 2, 650);
			btnReset.Size = new Size(btnWidth, 30);
			btnReset.Click += BtnReset_Click;
			this.Controls.Add(btnReset);
		}

		private void InitializeScpConfigPanel(int panelX, int panelWidth)
		{
			// Panel para la configuración de SCP
			Panel pnlScpConfig = new Panel();
			pnlScpConfig.Location = new Point(panelX, 350); // Moved down
			pnlScpConfig.Size = new Size(panelWidth, 150);
			pnlScpConfig.BorderStyle = BorderStyle.FixedSingle;
			this.Controls.Add(pnlScpConfig);

			// Título del panel
			lblScpConfig = new Label();
			lblScpConfig.Text = "SCP Configuration";
			lblScpConfig.Font = new Font(lblScpConfig.Font, FontStyle.Bold);
			lblScpConfig.Location = new Point(10, 10);
			lblScpConfig.AutoSize = true;
			pnlScpConfig.Controls.Add(lblScpConfig);

			// Label para el comando SCP
			lblScpCommand = new Label();
			lblScpCommand.Text = "SCP Command (use <image> as reference to the file):";
			lblScpCommand.Location = new Point(10, 40);
			lblScpCommand.Size = new Size(150, 20);
			pnlScpConfig.Controls.Add(lblScpCommand);

			// TextBox para el comando SCP
			txtScpCommand = new TextBox();
			txtScpCommand.Location = new Point(10, 65);
			txtScpCommand.Size = new Size(panelWidth - 30, 25);
			txtScpCommand.Text = "Example: scp -i path/to/key.pem <image> user@host:/path/";
			txtScpCommand.ForeColor = Color.Gray;
			pnlScpConfig.Controls.Add(txtScpCommand);

			// Add placeholder behavior
			txtScpCommand.GotFocus += (s, e) =>
			{
				if (txtScpCommand.Text == "Example: scp -i path/to/key.pem <image> user@host:/path/")
				{
					txtScpCommand.Text = "";
					txtScpCommand.ForeColor = Color.Black;
				}
			};
			txtScpCommand.LostFocus += (s, e) =>
			{
				if (string.IsNullOrWhiteSpace(txtScpCommand.Text))
				{
					txtScpCommand.Text = "Example: scp -i path/to/key.pem <image> user@host:/path/";
					txtScpCommand.ForeColor = Color.Gray;
				}
			};

			// Label para el texto del portapapeles
			lblScpClipboardText = new Label();
			lblScpClipboardText.Text = "Text to copy (optional, use <image> as reference):";
			lblScpClipboardText.Location = new Point(10, 100);
			lblScpClipboardText.Size = new Size(150, 20);
			pnlScpConfig.Controls.Add(lblScpClipboardText);

			// TextBox para el texto del portapapeles
			txtScpClipboardText = new TextBox();
			txtScpClipboardText.Location = new Point(10, 120);
			txtScpClipboardText.Size = new Size(panelWidth - 30, 25);
			txtScpClipboardText.Text = "Example: https://my-server.com/screenshots/<image>";
			txtScpClipboardText.ForeColor = Color.Gray;
			pnlScpConfig.Controls.Add(txtScpClipboardText);

			// Add placeholder behavior
			txtScpClipboardText.GotFocus += (s, e) =>
			{
				if (txtScpClipboardText.Text == "Example: https://my-server.com/screenshots/<image>")
				{
					txtScpClipboardText.Text = "";
					txtScpClipboardText.ForeColor = Color.Black;
				}
			};
			txtScpClipboardText.LostFocus += (s, e) =>
			{
				if (string.IsNullOrWhiteSpace(txtScpClipboardText.Text))
				{
					txtScpClipboardText.Text = "Example: https://my-server.com/screenshots/<image>";
					txtScpClipboardText.ForeColor = Color.Gray;
				}
			};

			// Ensure that the controls are visible
			txtScpCommand.BringToFront();
			txtScpClipboardText.BringToFront();

			// Ensure that all controls are visible
			pnlScpConfig.Refresh();
		}
        
        private void InitializeColorPickerConfigPanel(int panelX, int panelWidth)
        {
            // Panel para la configuración del color picker
            Panel pnlColorPickerConfig = new Panel();
            pnlColorPickerConfig.Location = new Point(panelX, 510); // Below SCP panel
            pnlColorPickerConfig.Size = new Size(panelWidth, 80);
            pnlColorPickerConfig.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(pnlColorPickerConfig);

            // Título del panel
            lblColorPickerConfig = new Label();
            lblColorPickerConfig.Text = "Color Picker Configuration";
            lblColorPickerConfig.Font = new Font(lblColorPickerConfig.Font, FontStyle.Bold);
            lblColorPickerConfig.Location = new Point(10, 10);
            lblColorPickerConfig.AutoSize = true;
            pnlColorPickerConfig.Controls.Add(lblColorPickerConfig);

            // Label para el formato de color
            lblColorFormat = new Label();
            lblColorFormat.Text = "Color format:";
            lblColorFormat.Location = new Point(10, 40);
            lblColorFormat.Size = new Size(150, 20);
            pnlColorPickerConfig.Controls.Add(lblColorFormat);

            // ComboBox para el formato de color
            cmbColorFormat = new ComboBox();
            cmbColorFormat.Location = new Point(170, 40);
            cmbColorFormat.Size = new Size(260, 25);
            cmbColorFormat.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbColorFormat.Items.AddRange(new object[] { "RGB", "HEX", "HSL" });
            pnlColorPickerConfig.Controls.Add(cmbColorFormat);
        }

		private void LoadSettings()
		{
			// Load keyboard shortcuts
			txtUndo.Hotkey = settings.UndoShortcut;
			txtSave.Hotkey = settings.SaveShortcut;
			txtCopy.Hotkey = settings.CopyShortcut;
			txtCancel.Hotkey = settings.CancelShortcut;
			txtOcr.Hotkey = settings.OcrShortcut;
			txtScp.Hotkey = settings.ScpShortcut;
            txtColorPicker.Hotkey = settings.ColorPickerShortcut;

			// Load SCP configuration
			txtScpCommand.Text = settings.ScpCommand;
			txtScpClipboardText.Text = settings.ScpClipboardText;
            
            // Load Color Picker configuration
            cmbColorFormat.SelectedItem = settings.ColorFormat;

			// Load Windows startup configuration
			chkStartWithWindows.Checked = settings.StartWithWindows;
		}

		private void SaveSettings()
		{
			// Save keyboard shortcuts
			settings.UndoShortcut = txtUndo.Hotkey;
			settings.SaveShortcut = txtSave.Hotkey;
			settings.CopyShortcut = txtCopy.Hotkey;
			settings.CancelShortcut = txtCancel.Hotkey;
			settings.OcrShortcut = txtOcr.Hotkey;
			settings.ScpShortcut = txtScp.Hotkey;
            settings.ColorPickerShortcut = txtColorPicker.Hotkey;

			// Save SCP configuration
			settings.ScpCommand = txtScpCommand.Text.Trim();
			settings.ScpClipboardText = txtScpClipboardText.Text.Trim();
            
            // Save Color Picker configuration
            settings.ColorFormat = cmbColorFormat.SelectedItem.ToString();

			// Save Windows startup configuration
			settings.StartWithWindows = chkStartWithWindows.Checked;

			// Apply Windows startup configuration
			ApplyStartupSetting(settings.StartWithWindows);

			// Save configuration to file
			settings.Save();
		}

		private void ApplyStartupSetting(bool startWithWindows)
		{
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
				{
					if (startWithWindows)
					{
						key.SetValue("CloudShot", Application.ExecutablePath);
					}
					else
					{
						if (key.GetValue("CloudShot") != null)
						{
							key.DeleteValue("CloudShot", false);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error configuring automatic startup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ChkStartWithWindows_CheckedChanged(object sender, EventArgs e)
		{
			if (!isLoading)
			{
				// No need to do anything here, it will be applied when saving
			}
		}

		private void BtnSave_Click(object sender, EventArgs e)
		{
			SaveSettings();
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void BtnReset_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show(
					"Are you sure you want to restore default settings?",
					"Reset Settings",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question) == DialogResult.Yes)
			{
				settings.ResetToDefaults();
				LoadSettings();
			}
		}
	}

	// Control to capture keyboard shortcuts
	public class HotkeyControl : TextBox
	{
		private Keys _hotkey;

		public HotkeyControl()
		{
			this.ReadOnly = true;
			this.BackColor = SystemColors.Window;
		}

		public Keys Hotkey
		{
			get { return _hotkey; }
			set
			{
				_hotkey = value;
				this.Text = GetHotkeyDisplayText(_hotkey);
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			// Ignore combinations with Alt because they open menus
			if ((e.Modifiers & Keys.Alt) == Keys.Alt)
			{
				return;
			}

			// Capture the key
			Keys keyCode = e.KeyCode;
			Keys modifiers = e.Modifiers;

			// Ignore if only modifiers are pressed
			if (keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey || keyCode == Keys.Menu)
			{
				return;
			}

			// Set the shortcut
			_hotkey = keyCode | modifiers;
			this.Text = GetHotkeyDisplayText(_hotkey);

			e.SuppressKeyPress = true;
			e.Handled = true;
		}

		private string GetHotkeyDisplayText(Keys hotkey)
		{
			string text = "";

			// Add modifiers
			if ((hotkey & Keys.Control) == Keys.Control)
				text += "Ctrl + ";
			if ((hotkey & Keys.Shift) == Keys.Shift)
				text += "Shift + ";
			if ((hotkey & Keys.Alt) == Keys.Alt)
				text += "Alt + ";

			// Add the main key
			Keys keyCode = hotkey & Keys.KeyCode;
			text += keyCode.ToString();

			return text;
		}
	}
}