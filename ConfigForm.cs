using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CloudShot.Overlay;
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

		private const string ScpHostPlaceholder = "root@server.com";
		private const string ScpRemotePathPlaceholder = "/var/www/screenshots/";
		private const string ScpKeyPlaceholder = "C:\\Users\\you\\.ssh\\id_ed25519";
		private const string ScpClipboardPlaceholder = "https://my-server.com/screenshots/<image>";

		private readonly AppSettings settings;

		private HotkeyControl txtUndo;
		private HotkeyControl txtSave;
		private HotkeyControl txtCopy;
		private HotkeyControl txtCancel;
		private HotkeyControl txtOcr;
		private HotkeyControl txtScp;
		private HotkeyControl txtColorPicker;

		private HotkeyControl txtPenTool;
		private HotkeyControl txtRectangleTool;
		private HotkeyControl txtFilledRectangleTool;
		private HotkeyControl txtPixelateTool;
		private HotkeyControl txtArrowTool;
		private HotkeyControl txtHighlighterTool;
		private HotkeyControl txtLineTool;
		private HotkeyControl txtStepsTool;
		private HotkeyControl txtTextTool;
		private HotkeyControl txtMoveTool;

		private TextBox txtScpHost;
		private NumericUpDown numScpPort;
		private TextBox txtScpRemotePath;
		private TextBox txtScpKeyPath;
		private TextBox txtScpKeyPassphrase;
		private TextBox txtScpClipboardText;

		private ComboBox cmbColorFormat;

		private Panel defaultColorPreview;
		private Color defaultDrawingColor = Color.Red;

		private CheckBox chkStartWithWindows;

		private readonly Dictionary<CaptureToolbarAction, CheckBox> toolCheckBoxes =
			new Dictionary<CaptureToolbarAction, CheckBox>();

		private readonly List<(string Label, HotkeyControl Control)> shortcutBindings =
			new List<(string Label, HotkeyControl Control)>();

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
			ClientSize = new Size(520, 680);
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
				AutoScroll = true,
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

			var actionsTable = BuildShortcutTable(8);
			AddTableHeaderCell(actionsTable, 0, 0, "Action");
			AddTableHeaderCell(actionsTable, 0, 1, "Shortcut");

			txtUndo = AddTableShortcutRow(actionsTable, 1, "Undo last edit");
			txtSave = AddTableShortcutRow(actionsTable, 2, "Save to computer");
			txtCopy = AddTableShortcutRow(actionsTable, 3, "Copy to clipboard");
			txtOcr = AddTableShortcutRow(actionsTable, 4, "Extract text (OCR)");
			txtScp = AddTableShortcutRow(actionsTable, 5, "Upload via SCP");
			txtColorPicker = AddTableShortcutRow(actionsTable, 6, "Color picker");
			txtCancel = AddTableShortcutRow(actionsTable, 7, "Cancel capture");

			var toolsSection = new Label
			{
				Text = "Drawing tools",
				Font = TitleFont,
				ForeColor = Color.FromArgb(50, 50, 60),
				Dock = DockStyle.Top,
				AutoSize = true,
				Padding = new Padding(0, 16, 0, 8)
			};

			var toolsTable = BuildShortcutTable(11);
			AddTableHeaderCell(toolsTable, 0, 0, "Tool");
			AddTableHeaderCell(toolsTable, 0, 1, "Shortcut");

			txtPenTool = AddTableShortcutRow(toolsTable, 1, "Pen");
			txtRectangleTool = AddTableShortcutRow(toolsTable, 2, "Rectangle");
			txtFilledRectangleTool = AddTableShortcutRow(toolsTable, 3, "Filled rectangle");
			txtPixelateTool = AddTableShortcutRow(toolsTable, 4, "Pixelate");
			txtArrowTool = AddTableShortcutRow(toolsTable, 5, "Arrow");
			txtHighlighterTool = AddTableShortcutRow(toolsTable, 6, "Highlighter");
			txtLineTool = AddTableShortcutRow(toolsTable, 7, "Line");
			txtStepsTool = AddTableShortcutRow(toolsTable, 8, "Steps");
			txtTextTool = AddTableShortcutRow(toolsTable, 9, "Text");
			txtMoveTool = AddTableShortcutRow(toolsTable, 10, "Move selection");

			container.Controls.Add(toolsTable);
			container.Controls.Add(toolsSection);
			container.Controls.Add(actionsTable);
			container.Controls.Add(hint);
			page.Controls.Add(container);
			return page;
		}

		private TableLayoutPanel BuildShortcutTable(int rowCount)
		{
			var table = new TableLayoutPanel
			{
				ColumnCount = 2,
				RowCount = rowCount,
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

			for (int i = 1; i < rowCount; i++)
				table.RowStyles.Add(new RowStyle(SizeType.Absolute, TableRowHeight));

			return table;
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
			shortcutBindings.Add((action, hotkey));
			hotkey.DuplicateValidator = key => GetShortcutConflict(hotkey, key);

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

		private string GetShortcutConflict(HotkeyControl source, Keys key)
		{
			if (key == Keys.None)
				return null;

			foreach (var binding in shortcutBindings)
			{
				if (binding.Control != source && binding.Control.Hotkey == key)
					return binding.Label;
			}

			return null;
		}

		private bool ValidateAllShortcuts()
		{
			var assigned = new Dictionary<Keys, string>();

			foreach (var binding in shortcutBindings)
			{
				Keys key = binding.Control.Hotkey;
				if (key == Keys.None)
					continue;

				if (assigned.TryGetValue(key, out string existingLabel))
				{
					MessageBox.Show(
						$"The shortcut \"{HotkeyControl.FormatHotkey(key)}\" is assigned to both \"{existingLabel}\" and \"{binding.Label}\".",
						"Duplicate shortcut",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					binding.Control.Focus();
					return false;
				}

				assigned[key] = binding.Label;
			}

			return true;
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
				Text = "Upload captures using the built-in scp command (OpenSSH). Host accepts user@server or an alias from ~/.ssh/config.",
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

			var keyHint = new Label
			{
				Text = "Path to your SSH private key (id_ed25519, id_rsa, ...).",
				Font = HintFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				AutoSize = true,
				MaximumSize = new Size(460, 0),
				Margin = new Padding(0, 0, 0, 4)
			};

			txtScpKeyPassphrase = CreateInputField();
			txtScpKeyPassphrase.UseSystemPasswordChar = true;

			var passphraseHint = new Label
			{
				Text = "Required if your private key is protected with a passphrase.",
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
			layout.Controls.Add(CreateFieldGroup("SSH private key", keyHint, WrapWithBrowse(txtScpKeyPath)), 0, row++);
			layout.Controls.Add(CreateFieldGroup("Key passphrase (optional)", passphraseHint, WrapWithPasswordToggle(txtScpKeyPassphrase)), 0, row++);
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
				Filter = "All files (*.*)|*.*",
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
				Padding = new Padding(16),
				AutoScroll = true
			};

			var layout = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				ColumnCount = 1,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				RowCount = 14,
				Margin = new Padding(0, 0, 0, 8)
			};
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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

			var drawingTitle = new Label
			{
				Text = "Drawing",
				Font = TitleFont,
				AutoSize = true,
				Margin = new Padding(0, 0, 0, 4)
			};

			var drawingDescription = new Label
			{
				Text = "Default color used for pen, rectangle and filled rectangle annotations.",
				Font = HintFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				AutoSize = true,
				MaximumSize = new Size(460, 0),
				Margin = new Padding(0, 0, 0, 8)
			};

			layout.Controls.Add(startupTitle, 0, 0);
			layout.Controls.Add(chkStartWithWindows, 0, 1);
			layout.Controls.Add(startupHint, 0, 2);
			layout.Controls.Add(colorTitle, 0, 4);
			layout.Controls.Add(colorDescription, 0, 5);
			layout.Controls.Add(formatLabel, 0, 6);
			layout.Controls.Add(cmbColorFormat, 0, 7);
			layout.Controls.Add(drawingTitle, 0, 9);
			layout.Controls.Add(drawingDescription, 0, 10);
			layout.Controls.Add(BuildDefaultColorPicker(), 0, 11);
			layout.Controls.Add(BuildToolbarToolsSection(), 0, 13);

			page.Controls.Add(layout);
			return page;
		}

		private Control BuildToolbarToolsSection()
		{
			var container = new TableLayoutPanel
			{
				ColumnCount = 1,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Top,
				Margin = new Padding(0)
			};
			container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			container.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			var title = new Label
			{
				Text = "Toolbar tools",
				Font = TitleFont,
				AutoSize = true,
				Margin = new Padding(0, 0, 0, 4)
			};

			var description = new Label
			{
				Text = "Choose which tools appear in the capture toolbar.",
				Font = HintFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				AutoSize = true,
				MaximumSize = new Size(460, 0),
				Margin = new Padding(0, 0, 0, 8)
			};

			var grid = new TableLayoutPanel
			{
				ColumnCount = 2,
				RowCount = 8,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = new Padding(0, 0, 0, 4),
				Dock = DockStyle.Top
			};
			grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			for (int row = 0; row < 9; row++)
			{
				grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			}

			var toolEntries = new[]
			{
				(CaptureToolbarAction.PenMode, "Pen"),
				(CaptureToolbarAction.RectangleMode, "Rectangle"),
				(CaptureToolbarAction.FilledRectangleMode, "Filled rectangle"),
				(CaptureToolbarAction.PixelateMode, "Pixelate"),
				(CaptureToolbarAction.ArrowMode, "Arrow"),
				(CaptureToolbarAction.HighlighterMode, "Highlighter"),
				(CaptureToolbarAction.LineMode, "Line"),
				(CaptureToolbarAction.StepsMode, "Steps"),
				(CaptureToolbarAction.TextMode, "Text"),
				(CaptureToolbarAction.Move, "Move"),
				(CaptureToolbarAction.ColorPicker, "Color"),
				(CaptureToolbarAction.Undo, "Undo"),
				(CaptureToolbarAction.Copy, "Copy"),
				(CaptureToolbarAction.Save, "Save"),
				(CaptureToolbarAction.Ocr, "OCR"),
				(CaptureToolbarAction.Scp, "Upload"),
				(CaptureToolbarAction.Close, "Cancel")
			};

			toolCheckBoxes.Clear();
			for (int i = 0; i < toolEntries.Length; i++)
			{
				var entry = toolEntries[i];
				var checkBox = new CheckBox
				{
					Text = entry.Item2,
					Font = BodyFont,
					AutoSize = true,
					Margin = new Padding(0, 0, 0, 8)
				};
				toolCheckBoxes[entry.Item1] = checkBox;
				grid.Controls.Add(checkBox, i % 2, i / 2);
			}

			container.Controls.Add(title, 0, 0);
			container.Controls.Add(description, 0, 1);
			container.Controls.Add(grid, 0, 2);

			return container;
		}

		private Control BuildDefaultColorPicker()
		{
			var panel = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.LeftToRight,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				WrapContents = false,
				Margin = new Padding(0)
			};

			defaultColorPreview = new Panel
			{
				Size = new Size(28, 28),
				BackColor = defaultDrawingColor,
				BorderStyle = BorderStyle.FixedSingle,
				Margin = new Padding(0, 0, 8, 0),
				Cursor = Cursors.Hand
			};
			defaultColorPreview.Click += (s, e) => ChooseDefaultColor();

			var chooseButton = new Button
			{
				Text = "Change...",
				Size = new Size(92, 28),
				FlatStyle = FlatStyle.System,
				Font = BodyFont,
				Margin = new Padding(0)
			};
			chooseButton.Click += (s, e) => ChooseDefaultColor();

			panel.Controls.Add(defaultColorPreview);
			panel.Controls.Add(chooseButton);
			return panel;
		}

		private void ChooseDefaultColor()
		{
			using (var dialog = new ColorDialog
			{
				Color = defaultDrawingColor,
				FullOpen = true,
				AnyColor = true
			})
			{
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					defaultDrawingColor = dialog.Color;
					defaultColorPreview.BackColor = defaultDrawingColor;
				}
			}
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

		private static Color ParseColorOrDefault(string value, Color fallback)
		{
			if (string.IsNullOrWhiteSpace(value))
				return fallback;

			try
			{
				return ColorTranslator.FromHtml(value);
			}
			catch
			{
				return fallback;
			}
		}

		private static string ToHex(Color color)
		{
			return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
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

			txtPenTool.Hotkey = settings.PenToolShortcut;
			txtRectangleTool.Hotkey = settings.RectangleToolShortcut;
			txtFilledRectangleTool.Hotkey = settings.FilledRectangleToolShortcut;
			txtPixelateTool.Hotkey = settings.PixelateToolShortcut;
			txtArrowTool.Hotkey = settings.ArrowToolShortcut;
			txtHighlighterTool.Hotkey = settings.HighlighterToolShortcut;
			txtLineTool.Hotkey = settings.LineToolShortcut;
			txtStepsTool.Hotkey = settings.StepsToolShortcut;
			txtTextTool.Hotkey = settings.TextToolShortcut;
			txtMoveTool.Hotkey = settings.MoveToolShortcut;

			ApplyPlaceholder(txtScpHost, settings.ScpHost, ScpHostPlaceholder);
			numScpPort.Value = settings.ScpPort >= numScpPort.Minimum && settings.ScpPort <= numScpPort.Maximum
				? settings.ScpPort
				: 22;
			ApplyPlaceholder(txtScpRemotePath, settings.ScpRemotePath, ScpRemotePathPlaceholder);
			ApplyPlaceholder(txtScpKeyPath, settings.ScpKeyPath, ScpKeyPlaceholder);
			txtScpKeyPassphrase.Text = settings.ScpKeyPassphrase ?? "";
			ApplyPlaceholder(txtScpClipboardText, settings.ScpClipboardText, ScpClipboardPlaceholder);

			cmbColorFormat.SelectedItem = settings.ColorFormat;
			if (cmbColorFormat.SelectedIndex < 0)
				cmbColorFormat.SelectedIndex = 0;

			defaultDrawingColor = ParseColorOrDefault(settings.DefaultDrawingColor, Color.Red);
			if (defaultColorPreview != null)
				defaultColorPreview.BackColor = defaultDrawingColor;

			chkStartWithWindows.Checked = settings.StartWithWindows;
			LoadToolbarToolSettings();
		}

		private void LoadToolbarToolSettings()
		{
			SetToolCheckBox(CaptureToolbarAction.PenMode, settings.ToolPenEnabled);
			SetToolCheckBox(CaptureToolbarAction.RectangleMode, settings.ToolRectangleEnabled);
			SetToolCheckBox(CaptureToolbarAction.FilledRectangleMode, settings.ToolFilledRectangleEnabled);
			SetToolCheckBox(CaptureToolbarAction.PixelateMode, settings.ToolPixelateEnabled);
			SetToolCheckBox(CaptureToolbarAction.ArrowMode, settings.ToolArrowEnabled);
			SetToolCheckBox(CaptureToolbarAction.HighlighterMode, settings.ToolHighlighterEnabled);
			SetToolCheckBox(CaptureToolbarAction.LineMode, settings.ToolLineEnabled);
			SetToolCheckBox(CaptureToolbarAction.StepsMode, settings.ToolStepsEnabled);
			SetToolCheckBox(CaptureToolbarAction.TextMode, settings.ToolTextEnabled);
			SetToolCheckBox(CaptureToolbarAction.Move, settings.ToolMoveEnabled);
			SetToolCheckBox(CaptureToolbarAction.ColorPicker, settings.ToolColorPickerEnabled);
			SetToolCheckBox(CaptureToolbarAction.Undo, settings.ToolUndoEnabled);
			SetToolCheckBox(CaptureToolbarAction.Copy, settings.ToolCopyEnabled);
			SetToolCheckBox(CaptureToolbarAction.Save, settings.ToolSaveEnabled);
			SetToolCheckBox(CaptureToolbarAction.Ocr, settings.ToolOcrEnabled);
			SetToolCheckBox(CaptureToolbarAction.Scp, settings.ToolScpEnabled);
			SetToolCheckBox(CaptureToolbarAction.Close, settings.ToolCloseEnabled);
		}

		private void SetToolCheckBox(CaptureToolbarAction action, bool enabled)
		{
			if (toolCheckBoxes.TryGetValue(action, out CheckBox checkBox))
			{
				checkBox.Checked = enabled;
			}
		}

		private bool GetToolCheckBox(CaptureToolbarAction action)
		{
			return toolCheckBoxes.TryGetValue(action, out CheckBox checkBox) && checkBox.Checked;
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

			settings.PenToolShortcut = txtPenTool.Hotkey;
			settings.RectangleToolShortcut = txtRectangleTool.Hotkey;
			settings.FilledRectangleToolShortcut = txtFilledRectangleTool.Hotkey;
			settings.PixelateToolShortcut = txtPixelateTool.Hotkey;
			settings.ArrowToolShortcut = txtArrowTool.Hotkey;
			settings.HighlighterToolShortcut = txtHighlighterTool.Hotkey;
			settings.LineToolShortcut = txtLineTool.Hotkey;
			settings.StepsToolShortcut = txtStepsTool.Hotkey;
			settings.TextToolShortcut = txtTextTool.Hotkey;
			settings.MoveToolShortcut = txtMoveTool.Hotkey;

			settings.ScpHost = GetTextBoxValue(txtScpHost);
			settings.ScpPort = (int)numScpPort.Value;
			settings.ScpRemotePath = GetTextBoxValue(txtScpRemotePath);
			settings.ScpKeyPath = GetTextBoxValue(txtScpKeyPath);
			settings.ScpKeyPassphrase = txtScpKeyPassphrase.Text;
			settings.ScpClipboardText = GetTextBoxValue(txtScpClipboardText);

			settings.ColorFormat = cmbColorFormat.SelectedItem.ToString();
			settings.DefaultDrawingColor = ToHex(defaultDrawingColor);
			settings.StartWithWindows = chkStartWithWindows.Checked;
			settings.ToolPenEnabled = GetToolCheckBox(CaptureToolbarAction.PenMode);
			settings.ToolRectangleEnabled = GetToolCheckBox(CaptureToolbarAction.RectangleMode);
			settings.ToolFilledRectangleEnabled = GetToolCheckBox(CaptureToolbarAction.FilledRectangleMode);
			settings.ToolPixelateEnabled = GetToolCheckBox(CaptureToolbarAction.PixelateMode);
			settings.ToolArrowEnabled = GetToolCheckBox(CaptureToolbarAction.ArrowMode);
			settings.ToolHighlighterEnabled = GetToolCheckBox(CaptureToolbarAction.HighlighterMode);
			settings.ToolLineEnabled = GetToolCheckBox(CaptureToolbarAction.LineMode);
			settings.ToolStepsEnabled = GetToolCheckBox(CaptureToolbarAction.StepsMode);
			settings.ToolTextEnabled = GetToolCheckBox(CaptureToolbarAction.TextMode);
			settings.ToolMoveEnabled = GetToolCheckBox(CaptureToolbarAction.Move);
			settings.ToolColorPickerEnabled = GetToolCheckBox(CaptureToolbarAction.ColorPicker);
			settings.ToolUndoEnabled = GetToolCheckBox(CaptureToolbarAction.Undo);
			settings.ToolCopyEnabled = GetToolCheckBox(CaptureToolbarAction.Copy);
			settings.ToolSaveEnabled = GetToolCheckBox(CaptureToolbarAction.Save);
			settings.ToolOcrEnabled = GetToolCheckBox(CaptureToolbarAction.Ocr);
			settings.ToolScpEnabled = GetToolCheckBox(CaptureToolbarAction.Scp);
			settings.ToolCloseEnabled = GetToolCheckBox(CaptureToolbarAction.Close);
			settings.SettingsVersion = 4;

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
			if (!ValidateAllShortcuts())
				return;

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

		public Func<Keys, string> DuplicateValidator { get; set; }

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
				Text = FormatHotkey(_hotkey);
			}
		}

		public static string FormatHotkey(Keys hotkey)
		{
			if (hotkey == Keys.None)
				return string.Empty;

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

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if ((e.Modifiers & Keys.Alt) == Keys.Alt)
				return;

			Keys keyCode = e.KeyCode;
			if (keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey || keyCode == Keys.Menu)
				return;

			Keys proposed = keyCode | e.Modifiers;
			string conflict = DuplicateValidator?.Invoke(proposed);
			if (!string.IsNullOrEmpty(conflict))
			{
				MessageBox.Show(
					$"This shortcut is already assigned to \"{conflict}\".",
					"Duplicate shortcut",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			_hotkey = proposed;
			Text = FormatHotkey(_hotkey);

			e.SuppressKeyPress = true;
			e.Handled = true;
		}
	}
}
