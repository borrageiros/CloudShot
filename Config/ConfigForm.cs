using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using CloudShot.Core;
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

		private static readonly Color CardColor = Color.FromArgb(252, 252, 253);
		private static readonly Color CardBorderColor = Color.FromArgb(228, 228, 234);
		private static readonly Color CardTitleColor = Color.FromArgb(45, 45, 55);
		private static readonly Color CardDescriptionColor = Color.FromArgb(140, 140, 152);
		private static readonly Color SecondaryButtonBorderColor = Color.FromArgb(205, 205, 212);
		private static readonly Color SecondaryButtonForeColor = Color.FromArgb(60, 60, 70);
		private static readonly Font CardTitleFont = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
		private const int CardSpacing = 14;
		private const int InputRowHeight = 30;

		private const string ScpHostPlaceholder = "root@server.com";
		private const string ScpRemotePathPlaceholder = "/var/www/screenshots/";
		private const string ScpKeyPlaceholder = "C:\\Users\\you\\.ssh\\id_ed25519";
		private const string ScpClipboardPlaceholder = "https://my-server.com/screenshots/<image>";
		private const string GitHubUrl = "https://github.com/borrageiros/CloudShot";
		private const string PortfolioUrl = "https://borrageiros.com";
		private const string DownloadUrl = "https://borrageiros.github.io/CloudShot/";

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
		private HotkeyControl txtEraserTool;
		private HotkeyControl txtMoveTool;

		private TextBox txtScpHost;
		private NumericUpDown numScpPort;
		private TextBox txtScpRemotePath;
		private TextBox txtScpKeyPath;
		private TextBox txtScpKeyPassphrase;
		private TextBox txtScpClipboardText;

		private ComboBox cmbColorFormat;
		private ComboBox cmbDefaultTool;
		private ComboBox cmbToolbarPosition;

		private static readonly (DrawingToolMode Mode, string Label)[] DefaultToolEntries =
		{
			(DrawingToolMode.Pen, "Pen"),
			(DrawingToolMode.Eraser, "Eraser"),
			(DrawingToolMode.Rectangle, "Rectangle"),
			(DrawingToolMode.FilledRectangle, "Filled rectangle"),
			(DrawingToolMode.Pixelate, "Pixelate"),
			(DrawingToolMode.Arrow, "Arrow"),
			(DrawingToolMode.Highlighter, "Highlighter"),
			(DrawingToolMode.Line, "Line"),
			(DrawingToolMode.Steps, "Steps"),
			(DrawingToolMode.Text, "Text")
		};

		private static readonly (ToolbarPosition Position, string Label)[] ToolbarPositionEntries =
		{
			(ToolbarPosition.Top, "Top"),
			(ToolbarPosition.Bottom, "Bottom"),
			(ToolbarPosition.Left, "Left"),
			(ToolbarPosition.Right, "Right")
		};

		private Panel defaultColorPreview;
		private Label defaultColorHexLabel;
		private Color defaultDrawingColor = Color.Red;

		private CheckBox chkStartWithWindows;
		private CheckBox chkReSelectAreaOnOutsideClick;
		private NumericUpDown numMaxHistory;

		private CheckBox chkNotificationsEnabled;
		private CheckBox chkNotifyOnCopy;
		private CheckBox chkNotifyOnSave;
		private CheckBox chkNotifyOnUpdate;
		private CheckBox chkNotifyOnOcr;
		private CheckBox chkNotifyOnScp;
		private CheckBox chkNotifyOnColorPicker;

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
			tabControl.TabPages.Add(BuildNotificationsTab());
			tabControl.TabPages.Add(BuildAboutTab());

			Controls.Add(tabControl);
			Controls.Add(BuildFooter());
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
			txtEraserTool.Hotkey = settings.EraserToolShortcut;
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

			cmbDefaultTool.SelectedIndex = Array.FindIndex(DefaultToolEntries, e => e.Mode == settings.DefaultTool);
			if (cmbDefaultTool.SelectedIndex < 0)
				cmbDefaultTool.SelectedIndex = 0;

			cmbToolbarPosition.SelectedIndex = Array.FindIndex(ToolbarPositionEntries, e => e.Position == settings.ToolbarDefaultPosition);
			if (cmbToolbarPosition.SelectedIndex < 0)
				cmbToolbarPosition.SelectedIndex = Array.FindIndex(ToolbarPositionEntries, e => e.Position == ToolbarPosition.Top);

			defaultDrawingColor = ParseColorOrDefault(settings.DefaultDrawingColor, Color.Red);
			if (defaultColorPreview != null)
				defaultColorPreview.BackColor = defaultDrawingColor;
			if (defaultColorHexLabel != null)
				defaultColorHexLabel.Text = ToHex(defaultDrawingColor);

			chkStartWithWindows.Checked = settings.StartWithWindows;
			chkReSelectAreaOnOutsideClick.Checked = settings.ReSelectAreaOnOutsideClick;
			numMaxHistory.Value = Math.Min(
				numMaxHistory.Maximum,
				Math.Max(numMaxHistory.Minimum, settings.MaxHistory > 0 ? settings.MaxHistory : 100));

			chkNotificationsEnabled.Checked = settings.NotificationsEnabled;
			chkNotifyOnCopy.Checked = settings.NotifyOnCopy;
			chkNotifyOnSave.Checked = settings.NotifyOnSave;
			chkNotifyOnUpdate.Checked = settings.NotifyOnUpdate;
			chkNotifyOnOcr.Checked = settings.NotifyOnOcr;
			chkNotifyOnScp.Checked = settings.NotifyOnScp;
			chkNotifyOnColorPicker.Checked = settings.NotifyOnColorPicker;
			UpdateNotificationCheckboxesState();

			LoadToolbarToolSettings();
		}

		private void LoadToolbarToolSettings()
		{
			foreach (CaptureToolDefinition definition in CaptureToolRegistry.Definitions)
				SetToolCheckBox(definition.ToolbarAction, definition.GetEnabled(settings));
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
			settings.EraserToolShortcut = txtEraserTool.Hotkey;
			settings.MoveToolShortcut = txtMoveTool.Hotkey;

			settings.ScpHost = GetTextBoxValue(txtScpHost);
			settings.ScpPort = (int)numScpPort.Value;
			settings.ScpRemotePath = GetTextBoxValue(txtScpRemotePath);
			settings.ScpKeyPath = GetTextBoxValue(txtScpKeyPath);
			settings.ScpKeyPassphrase = txtScpKeyPassphrase.Text;
			settings.ScpClipboardText = GetTextBoxValue(txtScpClipboardText);

			settings.ColorFormat = cmbColorFormat.SelectedItem.ToString();
			settings.DefaultDrawingColor = ToHex(defaultDrawingColor);
			if (cmbDefaultTool.SelectedIndex >= 0)
				settings.DefaultTool = DefaultToolEntries[cmbDefaultTool.SelectedIndex].Mode;
			if (cmbToolbarPosition.SelectedIndex >= 0)
				settings.ToolbarDefaultPosition = ToolbarPositionEntries[cmbToolbarPosition.SelectedIndex].Position;
			settings.StartWithWindows = chkStartWithWindows.Checked;
			settings.ReSelectAreaOnOutsideClick = chkReSelectAreaOnOutsideClick.Checked;
			settings.MaxHistory = (int)numMaxHistory.Value;

			settings.NotificationsEnabled = chkNotificationsEnabled.Checked;
			settings.NotifyOnCopy = chkNotifyOnCopy.Checked;
			settings.NotifyOnSave = chkNotifyOnSave.Checked;
			settings.NotifyOnUpdate = chkNotifyOnUpdate.Checked;
			settings.NotifyOnOcr = chkNotifyOnOcr.Checked;
			settings.NotifyOnScp = chkNotifyOnScp.Checked;
			settings.NotifyOnColorPicker = chkNotifyOnColorPicker.Checked;

			foreach (CaptureToolDefinition definition in CaptureToolRegistry.Definitions)
				definition.SetEnabled(settings, GetToolCheckBox(definition.ToolbarAction));
			settings.SettingsVersion = 10;

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
}
