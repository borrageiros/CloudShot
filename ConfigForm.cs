using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CloudShot
{
	public partial class ConfigForm : Form
	{
		private static readonly Color TableHeaderColor = Color.FromArgb(240, 240, 243);
		private static readonly Color TableBorderColor = Color.FromArgb(210, 210, 215);
		private const int TableRowHeight = 36;
		private const int TableHeaderHeight = 32;

		private static readonly Color AccentColor = Color.FromArgb(0, 120, 215);
		private static readonly Color SurfaceColor = Color.FromArgb(250, 250, 252);
		private static readonly Color FooterColor = Color.FromArgb(245, 245, 248);
		private static readonly Font TitleFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
		private static readonly Font BodyFont = new Font("Segoe UI", 9F);
		private static readonly Font HintFont = new Font("Segoe UI", 8.5F);

		private const string ScpHostPlaceholder = "user@server.com";
		private const string ScpRemotePathPlaceholder = "/var/www/screenshots/";
		private const string ScpKeyPlaceholder = "C:\\path\\to\\key.pem";
		private const string ScpClipboardPlaceholder = "https://my-server.com/screenshots/<image>";

		private readonly AppSettings settings;

		private HotkeyControl txtUndo;
		private HotkeyControl txtSave;
		private HotkeyControl txtCopy;
		private HotkeyControl txtCancel;
		private HotkeyControl txtOcr;
		private HotkeyControl txtScp;
		private HotkeyControl txtColorPicker;

		private TextBox txtScpHost;
		private NumericUpDown numScpPort;
		private TextBox txtScpRemotePath;
		private TextBox txtScpKeyPath;
		private TextBox txtScpPassword;
		private TextBox txtScpClipboardText;

		private ComboBox cmbColorFormat;

		private CheckBox chkStartWithWindows;

		private Button btnSave;
		private Button btnCancel;
		private Button btnReset;

		public ConfigForm(AppSettings settings)
		{
			this.settings = settings;
			InitializeComponents();
			LoadSettings();
		}

		private void InitializeComponents()
		{
			Text = "CloudShot Settings";
			ClientSize = new Size(520, 540);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = SurfaceColor;
			Font = BodyFont;
			Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

			var tabControl = new TabControl
			{
				Dock = DockStyle.Fill,
				Font = BodyFont,
				Padding = new Point(12, 6)
			};

			tabControl.TabPages.Add(BuildGeneralTab());
			tabControl.TabPages.Add(BuildShortcutsTab());
			tabControl.TabPages.Add(BuildScpTab());

			Controls.Add(tabControl);
			Controls.Add(BuildFooter());
		}

		private TabPage BuildShortcutsTab()
		{
			var page = new TabPage("Shortcuts")
			{
				BackColor = Color.White,
				Padding = new Padding(0)
			};

			var container = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(16, 12, 16, 16),
				BackColor = Color.White
			};

			var hint = new Label
			{
				Text = "Click a shortcut cell and press the key combination you want to assign.",
				Font = HintFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				Dock = DockStyle.Top,
				AutoSize = true,
				MaximumSize = new Size(460, 0),
				Padding = new Padding(0, 0, 0, 12)
			};

			var table = new TableLayoutPanel
			{
				ColumnCount = 2,
				RowCount = 8,
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
				BackColor = TableBorderColor,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
			table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
			table.RowStyles.Add(new RowStyle(SizeType.Absolute, TableHeaderHeight));

			for (int i = 0; i < 7; i++)
				table.RowStyles.Add(new RowStyle(SizeType.Absolute, TableRowHeight));

			AddTableHeaderCell(table, 0, 0, "Action");
			AddTableHeaderCell(table, 0, 1, "Shortcut");

			txtUndo = AddTableShortcutRow(table, 1, "Undo last edit");
			txtSave = AddTableShortcutRow(table, 2, "Save to computer");
			txtCopy = AddTableShortcutRow(table, 3, "Copy to clipboard");
			txtOcr = AddTableShortcutRow(table, 4, "Extract text (OCR)");
			txtScp = AddTableShortcutRow(table, 5, "Upload via SCP");
			txtColorPicker = AddTableShortcutRow(table, 6, "Color picker");
			txtCancel = AddTableShortcutRow(table, 7, "Cancel capture");

			container.Controls.Add(table);
			container.Controls.Add(hint);
			page.Controls.Add(container);
			return page;
		}

		private static void AddTableHeaderCell(TableLayoutPanel table, int row, int column, string text)
		{
			var label = new Label
			{
				Text = text,
				Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
				TextAlign = ContentAlignment.MiddleLeft,
				Dock = DockStyle.Fill,
				BackColor = TableHeaderColor,
				Padding = new Padding(10, 0, 10, 0),
				Margin = Padding.Empty
			};
			table.Controls.Add(label, column, row);
		}

		private HotkeyControl AddTableShortcutRow(TableLayoutPanel table, int row, string action)
		{
			var actionLabel = new Label
			{
				Text = action,
				Font = BodyFont,
				TextAlign = ContentAlignment.MiddleLeft,
				Dock = DockStyle.Fill,
				BackColor = Color.White,
				Padding = new Padding(10, 0, 10, 0),
				Margin = Padding.Empty
			};
			table.Controls.Add(actionLabel, 0, row);

			var hotkey = new HotkeyControl();

			var hotkeyCell = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = Color.White,
				Padding = new Padding(6, 5, 6, 5),
				Margin = Padding.Empty
			};
			hotkey.Dock = DockStyle.Fill;
			hotkeyCell.Controls.Add(hotkey);
			table.Controls.Add(hotkeyCell, 1, row);

			return hotkey;
		}

		private TabPage BuildScpTab()
		{
			var page = new TabPage("SCP")
			{
				BackColor = Color.White,
				Padding = new Padding(16),
				AutoScroll = true
			};

			var layout = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				ColumnCount = 1,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink
			};
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

			var title = new Label
			{
				Text = "Remote upload (SCP)",
				Font = TitleFont,
				AutoSize = true,
				Margin = new Padding(0, 0, 0, 4)
			};

			var description = new Label
			{
				Text = "Upload captures to a remote server over SSH. Provide the destination and authenticate with an SSH key or a password.",
				Font = HintFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				AutoSize = true,
				MaximumSize = new Size(460, 0),
				Margin = new Padding(0, 0, 0, 12)
			};

			txtScpHost = CreateInputField();
			SetupPlaceholder(txtScpHost, ScpHostPlaceholder);

			numScpPort = new NumericUpDown
			{
				Minimum = 1,
				Maximum = 65535,
				Value = 22,
				Width = 90,
				Font = BodyFont,
				Anchor = AnchorStyles.Left
			};

			txtScpRemotePath = CreateInputField();
			SetupPlaceholder(txtScpRemotePath, ScpRemotePathPlaceholder);

			txtScpKeyPath = CreateInputField();
			SetupPlaceholder(txtScpKeyPath, ScpKeyPlaceholder);

			txtScpPassword = CreateInputField();
			txtScpPassword.UseSystemPasswordChar = true;

			var passwordHint = new Label
			{
				Text = "Leave empty when using an SSH key. Password authentication requires PuTTY's pscp in PATH.",
				Font = HintFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				AutoSize = true,
				MaximumSize = new Size(460, 0),
				Margin = new Padding(0, 0, 0, 4)
			};

			var clipboardHint = new Label
			{
				Text = "Text copied after a successful upload. <image> is replaced with the remote filename.",
				Font = HintFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				AutoSize = true,
				MaximumSize = new Size(460, 0),
				Margin = new Padding(0, 0, 0, 4)
			};

			txtScpClipboardText = CreateInputField();
			SetupPlaceholder(txtScpClipboardText, ScpClipboardPlaceholder);

			int row = 0;
			layout.Controls.Add(title, 0, row++);
			layout.Controls.Add(description, 0, row++);
			layout.Controls.Add(CreateFieldGroup("Host", null, txtScpHost), 0, row++);
			layout.Controls.Add(CreateFieldGroup("Port", null, WrapFixedWidth(numScpPort)), 0, row++);
			layout.Controls.Add(CreateFieldGroup("Remote path", null, txtScpRemotePath), 0, row++);
			layout.Controls.Add(CreateFieldGroup("SSH key file (optional)", null, WrapWithBrowse(txtScpKeyPath)), 0, row++);
			layout.Controls.Add(CreateFieldGroup("Password (optional)", passwordHint, WrapWithPasswordToggle(txtScpPassword)), 0, row++);
			layout.Controls.Add(CreateFieldGroup("Clipboard text (optional)", clipboardHint, txtScpClipboardText), 0, row++);

			page.Controls.Add(layout);
			return page;
		}

		private Control CreateFieldGroup(string labelText, Label hint, Control input)
		{
			var group = new TableLayoutPanel
			{
				ColumnCount = 1,
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = new Padding(0, 0, 0, 10)
			};
			group.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			group.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			if (labelText != null)
			{
				group.Controls.Add(new Label
				{
					Text = labelText,
					Font = BodyFont,
					AutoSize = true,
					Margin = new Padding(0, 0, 0, 4)
				}, 0, 0);
			}

			int inputRow = labelText != null ? 1 : 0;
			if (hint != null)
			{
				group.RowStyles.Add(new RowStyle(SizeType.AutoSize));
				group.Controls.Add(hint, 0, inputRow);
				inputRow++;
			}

			group.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
			group.Controls.Add(input, 0, inputRow);
			return group;
		}

		private static Control WrapFixedWidth(Control input)
		{
			var panel = new Panel
			{
				Dock = DockStyle.Fill,
				Height = 30,
				Margin = new Padding(0)
			};
			input.Location = new Point(0, 1);
			panel.Controls.Add(input);
			return panel;
		}

		private Control WrapWithBrowse(TextBox input)
		{
			var panel = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 1,
				Height = 30,
				Margin = new Padding(0)
			};
			panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));

			input.Dock = DockStyle.Fill;
			input.Margin = new Padding(0, 0, 6, 0);

			var browse = new Button
			{
				Text = "Browse...",
				Dock = DockStyle.Fill,
				FlatStyle = FlatStyle.System,
				Font = BodyFont,
				Margin = new Padding(0)
			};
			browse.Click += (s, e) => BrowseForKeyFile(input);

			panel.Controls.Add(input, 0, 0);
			panel.Controls.Add(browse, 1, 0);
			return panel;
		}

		private Control WrapWithPasswordToggle(TextBox input)
		{
			var panel = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 1,
				Height = 30,
				Margin = new Padding(0)
			};
			panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));

			input.Dock = DockStyle.Fill;
			input.Margin = new Padding(0, 0, 6, 0);

			var toggle = new CheckBox
			{
				Text = "Show",
				Dock = DockStyle.Fill,
				Font = HintFont,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(0)
			};
			toggle.CheckedChanged += (s, e) => input.UseSystemPasswordChar = !toggle.Checked;

			panel.Controls.Add(input, 0, 0);
			panel.Controls.Add(toggle, 1, 0);
			return panel;
		}

		private void BrowseForKeyFile(TextBox target)
		{
			using (var dialog = new OpenFileDialog
			{
				Title = "Select SSH private key",
				Filter = "Key files (*.pem;*.ppk;*.key)|*.pem;*.ppk;*.key|All files (*.*)|*.*",
				CheckFileExists = true
			})
			{
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					target.Text = dialog.FileName;
					target.ForeColor = SystemColors.WindowText;
				}
			}
		}

		private TabPage BuildGeneralTab()
		{
			var page = new TabPage("General")
			{
				BackColor = Color.White,
				Padding = new Padding(16)
			};

			var layout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 8
			};
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

			var startupTitle = new Label
			{
				Text = "Startup",
				Font = TitleFont,
				AutoSize = true,
				Margin = new Padding(0, 0, 0, 4)
			};

			chkStartWithWindows = new CheckBox
			{
				Text = "Start CloudShot automatically with Windows",
				Font = BodyFont,
				AutoSize = true,
				Margin = new Padding(0, 0, 0, 4)
			};

			var startupHint = new Label
			{
				Text = "CloudShot runs in the system tray. Use Print Screen or the tray icon to capture.",
				Font = HintFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				AutoSize = true,
				MaximumSize = new Size(460, 0),
				Margin = new Padding(0, 0, 0, 0)
			};

			var colorTitle = new Label
			{
				Text = "Color picker",
				Font = TitleFont,
				AutoSize = true,
				Margin = new Padding(0, 0, 0, 4)
			};

			var colorDescription = new Label
			{
				Text = "Choose how picked colors are formatted when copied to the clipboard.",
				Font = HintFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				AutoSize = true,
				MaximumSize = new Size(460, 0),
				Margin = new Padding(0, 0, 0, 8)
			};

			var formatLabel = new Label
			{
				Text = "Format",
				Font = BodyFont,
				AutoSize = true,
				Margin = new Padding(0, 0, 0, 4)
			};

			cmbColorFormat = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Size = new Size(200, 28),
				Font = BodyFont,
				Margin = new Padding(0, 0, 0, 0)
			};
			cmbColorFormat.Items.AddRange(new object[] { "RGB", "HEX", "HSL" });

			layout.Controls.Add(startupTitle, 0, 0);
			layout.Controls.Add(chkStartWithWindows, 0, 1);
			layout.Controls.Add(startupHint, 0, 2);
			layout.Controls.Add(colorTitle, 0, 4);
			layout.Controls.Add(colorDescription, 0, 5);
			layout.Controls.Add(formatLabel, 0, 6);
			layout.Controls.Add(cmbColorFormat, 0, 7);

			page.Controls.Add(layout);
			return page;
		}

		private Panel BuildFooter()
		{
			var footer = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 52,
				BackColor = FooterColor,
				Padding = new Padding(16, 10, 16, 10)
			};

			var separator = new Panel
			{
				Dock = DockStyle.Top,
				Height = 1,
				BackColor = Color.FromArgb(220, 220, 225)
			};
			footer.Controls.Add(separator);

			btnReset = CreateFooterButton("Reset defaults", 120);
			btnReset.Location = new Point(16, 14);
			btnReset.Click += BtnReset_Click;

			btnCancel = CreateFooterButton("Cancel", 88);
			btnSave = CreateAccentButton("Save", 88);

			btnCancel.Location = new Point(footer.Width - 200, 14);
			btnSave.Location = new Point(footer.Width - 104, 14);
			btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;

			btnCancel.Click += BtnCancel_Click;
			btnSave.Click += BtnSave_Click;

			footer.Controls.Add(btnReset);
			footer.Controls.Add(btnCancel);
			footer.Controls.Add(btnSave);
			footer.Resize += (s, e) =>
			{
				btnCancel.Location = new Point(footer.Width - 200, 14);
				btnSave.Location = new Point(footer.Width - 104, 14);
			};

			return footer;
		}

		private static Button CreateFooterButton(string text, int width)
		{
			return new Button
			{
				Text = text,
				Size = new Size(width, 30),
				FlatStyle = FlatStyle.System,
				Font = BodyFont
			};
		}

		private Button CreateAccentButton(string text, int width)
		{
			var button = new Button
			{
				Text = text,
				Size = new Size(width, 30),
				FlatStyle = FlatStyle.Flat,
				BackColor = AccentColor,
				ForeColor = Color.White,
				Font = BodyFont
			};
			button.FlatAppearance.BorderSize = 0;
			return button;
		}

		private static TextBox CreateInputField()
		{
			return new TextBox
			{
				Dock = DockStyle.Fill,
				Font = BodyFont,
				BorderStyle = BorderStyle.FixedSingle
			};
		}

		private static void SetupPlaceholder(TextBox textBox, string placeholder)
		{
			textBox.Tag = placeholder;
			textBox.GotFocus += (s, e) =>
			{
				if ((string)textBox.Tag == textBox.Text)
				{
					textBox.Text = "";
					textBox.ForeColor = SystemColors.WindowText;
				}
			};
			textBox.LostFocus += (s, e) =>
			{
				if (string.IsNullOrWhiteSpace(textBox.Text))
				{
					textBox.Text = (string)textBox.Tag;
					textBox.ForeColor = Color.Gray;
				}
			};
		}

		private static void ApplyPlaceholder(TextBox textBox, string value, string placeholder)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				textBox.Text = placeholder;
				textBox.ForeColor = Color.Gray;
			}
			else
			{
				textBox.Text = value;
				textBox.ForeColor = SystemColors.WindowText;
			}
		}

		private static string GetTextBoxValue(TextBox textBox)
		{
			string placeholder = textBox.Tag as string;
			if (placeholder != null && textBox.Text == placeholder)
				return "";
			return textBox.Text.Trim();
		}

		private void LoadSettings()
		{
			txtUndo.Hotkey = settings.UndoShortcut;
			txtSave.Hotkey = settings.SaveShortcut;
			txtCopy.Hotkey = settings.CopyShortcut;
			txtCancel.Hotkey = settings.CancelShortcut;
			txtOcr.Hotkey = settings.OcrShortcut;
			txtScp.Hotkey = settings.ScpShortcut;
			txtColorPicker.Hotkey = settings.ColorPickerShortcut;

			ApplyPlaceholder(txtScpHost, settings.ScpHost, ScpHostPlaceholder);
			numScpPort.Value = settings.ScpPort >= numScpPort.Minimum && settings.ScpPort <= numScpPort.Maximum
				? settings.ScpPort
				: 22;
			ApplyPlaceholder(txtScpRemotePath, settings.ScpRemotePath, ScpRemotePathPlaceholder);
			ApplyPlaceholder(txtScpKeyPath, settings.ScpKeyPath, ScpKeyPlaceholder);
			txtScpPassword.Text = settings.ScpPassword ?? "";
			ApplyPlaceholder(txtScpClipboardText, settings.ScpClipboardText, ScpClipboardPlaceholder);

			cmbColorFormat.SelectedItem = settings.ColorFormat;
			if (cmbColorFormat.SelectedIndex < 0)
				cmbColorFormat.SelectedIndex = 0;

			chkStartWithWindows.Checked = settings.StartWithWindows;
		}

		private void SaveSettings()
		{
			settings.UndoShortcut = txtUndo.Hotkey;
			settings.SaveShortcut = txtSave.Hotkey;
			settings.CopyShortcut = txtCopy.Hotkey;
			settings.CancelShortcut = txtCancel.Hotkey;
			settings.OcrShortcut = txtOcr.Hotkey;
			settings.ScpShortcut = txtScp.Hotkey;
			settings.ColorPickerShortcut = txtColorPicker.Hotkey;

			settings.ScpHost = GetTextBoxValue(txtScpHost);
			settings.ScpPort = (int)numScpPort.Value;
			settings.ScpRemotePath = GetTextBoxValue(txtScpRemotePath);
			settings.ScpKeyPath = GetTextBoxValue(txtScpKeyPath);
			settings.ScpPassword = txtScpPassword.Text;
			settings.ScpClipboardText = GetTextBoxValue(txtScpClipboardText);

			settings.ColorFormat = cmbColorFormat.SelectedItem.ToString();
			settings.StartWithWindows = chkStartWithWindows.Checked;

			ApplyStartupSetting(settings.StartWithWindows);
			settings.Save();
		}

		private void ApplyStartupSetting(bool startWithWindows)
		{
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
				{
					if (startWithWindows)
						key.SetValue("CloudShot", Application.ExecutablePath);
					else if (key.GetValue("CloudShot") != null)
						key.DeleteValue("CloudShot", false);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error configuring automatic startup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnSave_Click(object sender, EventArgs e)
		{
			SaveSettings();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
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

	public class HotkeyControl : TextBox
	{
		private Keys _hotkey;

		public HotkeyControl()
		{
			ReadOnly = true;
			BackColor = SystemColors.Window;
			Font = new Font("Segoe UI", 9F);
			BorderStyle = BorderStyle.FixedSingle;
		}

		public Keys Hotkey
		{
			get { return _hotkey; }
			set
			{
				_hotkey = value;
				Text = GetHotkeyDisplayText(_hotkey);
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if ((e.Modifiers & Keys.Alt) == Keys.Alt)
				return;

			Keys keyCode = e.KeyCode;
			if (keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey || keyCode == Keys.Menu)
				return;

			_hotkey = keyCode | e.Modifiers;
			Text = GetHotkeyDisplayText(_hotkey);

			e.SuppressKeyPress = true;
			e.Handled = true;
		}

		private static string GetHotkeyDisplayText(Keys hotkey)
		{
			string text = "";

			if ((hotkey & Keys.Control) == Keys.Control)
				text += "Ctrl + ";
			if ((hotkey & Keys.Shift) == Keys.Shift)
				text += "Shift + ";
			if ((hotkey & Keys.Alt) == Keys.Alt)
				text += "Alt + ";

			text += (hotkey & Keys.KeyCode).ToString();
			return text;
		}
	}
}
