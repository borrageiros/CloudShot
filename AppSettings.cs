using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using CloudShot.Core;
using CloudShot.Overlay;

namespace CloudShot
{
	public class AppSettings
	{
		// Configuration file path
		private static readonly string SettingsFilePath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"CloudShot",
				"settings.xml");

		// Keyboard shortcuts
		public Keys UndoShortcut { get; set; }
		public Keys SaveShortcut { get; set; }
		public Keys CopyShortcut { get; set; }
		public Keys CancelShortcut { get; set; }
		public Keys OcrShortcut { get; set; }
		public Keys ScpShortcut { get; set; }
		public Keys ColorPickerShortcut { get; set; }

		public Keys PenToolShortcut { get; set; }
		public Keys RectangleToolShortcut { get; set; }
		public Keys FilledRectangleToolShortcut { get; set; }
		public Keys PixelateToolShortcut { get; set; }
		public Keys ArrowToolShortcut { get; set; }
		public Keys HighlighterToolShortcut { get; set; }
		public Keys LineToolShortcut { get; set; }
		public Keys StepsToolShortcut { get; set; }
		public Keys TextToolShortcut { get; set; }
		public Keys EraserToolShortcut { get; set; }
		public Keys MoveToolShortcut { get; set; }

		// Windows startup
		public bool StartWithWindows { get; set; }

		// SCP configuration
		public string ScpHost { get; set; }
		public int ScpPort { get; set; }
		public string ScpRemotePath { get; set; }
		public string ScpKeyPath { get; set; }
		public string ScpKeyPassphrase { get; set; }
		public string ScpClipboardText { get; set; }

		// Color picker configuration
		public string ColorFormat { get; set; }

		// Default drawing color (hex format, e.g. #FF0000)
		public string DefaultDrawingColor { get; set; }

		// Default drawing tool preselected when a capture starts
		public DrawingToolMode DefaultTool { get; set; }

		// Default position where the capture toolbar appears first (falls back if it does not fit)
		public ToolbarPosition ToolbarDefaultPosition { get; set; }

		public int SettingsVersion { get; set; }

		public bool ToolPenEnabled { get; set; }
		public bool ToolRectangleEnabled { get; set; }
		public bool ToolFilledRectangleEnabled { get; set; }
		public bool ToolPixelateEnabled { get; set; }
		public bool ToolArrowEnabled { get; set; }
		public bool ToolHighlighterEnabled { get; set; }
		public bool ToolLineEnabled { get; set; }
		public bool ToolStepsEnabled { get; set; }
		public bool ToolTextEnabled { get; set; }
		public bool ToolEraserEnabled { get; set; }
		public bool ToolMoveEnabled { get; set; }
		public bool ToolColorPickerEnabled { get; set; }
		public bool ToolUndoEnabled { get; set; }
		public bool ToolCopyEnabled { get; set; }
		public bool ToolSaveEnabled { get; set; }
		public bool ToolOcrEnabled { get; set; }
		public bool ToolScpEnabled { get; set; }
		public bool ToolCloseEnabled { get; set; }

		public bool ReSelectAreaOnOutsideClick { get; set; }

		public int MaxHistory { get; set; }

		// Notifications
		public bool NotificationsEnabled { get; set; }
		public bool NotifyOnCopy { get; set; }
		public bool NotifyOnSave { get; set; }
		public bool NotifyOnUpdate { get; set; }
		public bool NotifyOnOcr { get; set; }
		public bool NotifyOnScp { get; set; }
		public bool NotifyOnColorPicker { get; set; }

		// Constructor
		public AppSettings()
		{
			// Set default values
			ResetToDefaults();
		}

		// Restore default values
		public void ResetToDefaults()
		{
			UndoShortcut = Keys.Control | Keys.Z;
			SaveShortcut = Keys.Control | Keys.S;
			CopyShortcut = Keys.Control | Keys.C;
			CancelShortcut = Keys.Escape;
			OcrShortcut = Keys.Control | Keys.R;
			ScpShortcut = Keys.Control | Keys.X;
			ColorPickerShortcut = Keys.Control | Keys.V;
			ResetToolShortcutsToDefaults();
			StartWithWindows = true;
			ScpHost = "";
			ScpPort = 22;
			ScpRemotePath = "";
			ScpKeyPath = "";
			ScpKeyPassphrase = "";
			ScpClipboardText = "";
			ColorFormat = "RGB";
			DefaultDrawingColor = "#FF0000";
			DefaultTool = DrawingToolMode.Pen;
			ToolbarDefaultPosition = ToolbarPosition.Top;
			ResetToolbarToolsToDefaults();
			ReSelectAreaOnOutsideClick = true;
			MaxHistory = 100;
			NotificationsEnabled = true;
			NotifyOnCopy = true;
			NotifyOnSave = true;
			NotifyOnUpdate = true;
			NotifyOnOcr = true;
			NotifyOnScp = true;
			NotifyOnColorPicker = true;
			SettingsVersion = 10;
		}

		public bool ShouldNotify(NotificationCategory category)
		{
			if (!NotificationsEnabled)
			{
				return false;
			}

			switch (category)
			{
				case NotificationCategory.Copy: return NotifyOnCopy;
				case NotificationCategory.Save: return NotifyOnSave;
				case NotificationCategory.Update: return NotifyOnUpdate;
				case NotificationCategory.Ocr: return NotifyOnOcr;
				case NotificationCategory.Scp: return NotifyOnScp;
				case NotificationCategory.ColorPicker: return NotifyOnColorPicker;
				default: return true;
			}
		}

		public void ResetToolShortcutsToDefaults()
		{
			CaptureToolRegistry.ResetDrawingToolShortcutsToDefaults(this);
		}

		public void ResetToolbarToolsToDefaults()
		{
			CaptureToolRegistry.ResetToolEnabledToDefaults(this);
		}

		private void ApplyVersionMigrations()
		{
			if (SettingsVersion < 2)
			{
				ResetToolbarToolsToDefaults();
				SettingsVersion = 2;
			}

			if (SettingsVersion < 3)
			{
				ToolTextEnabled = true;
				SettingsVersion = 3;
			}

			if (SettingsVersion < 4)
			{
				if (PenToolShortcut == Keys.None)
					ResetToolShortcutsToDefaults();
				SettingsVersion = 4;
			}

			if (SettingsVersion < 5)
			{
				ToolEraserEnabled = true;
				if (EraserToolShortcut == Keys.None)
					EraserToolShortcut = Keys.E;
				SettingsVersion = 5;
			}

			if (SettingsVersion < 6)
			{
				ReSelectAreaOnOutsideClick = true;
				SettingsVersion = 6;
			}

			if (SettingsVersion < 7)
			{
				if (MaxHistory <= 0)
					MaxHistory = 100;
				SettingsVersion = 7;
			}

			if (SettingsVersion < 8)
			{
				DefaultTool = DrawingToolMode.Pen;
				SettingsVersion = 8;
			}

			if (SettingsVersion < 9)
			{
				ToolbarDefaultPosition = ToolbarPosition.Top;
				SettingsVersion = 9;
			}

			if (SettingsVersion < 10)
			{
				NotificationsEnabled = true;
				NotifyOnCopy = true;
				NotifyOnSave = true;
				NotifyOnUpdate = true;
				NotifyOnOcr = true;
				NotifyOnScp = true;
				NotifyOnColorPicker = true;
				SettingsVersion = 10;
			}
		}

		// Load configuration from file
		public static AppSettings Load()
		{
			try
			{
				// Create the directory if it does not exist
				string directory = Path.GetDirectoryName(SettingsFilePath);
				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				// If the file exists, load the configuration
				if (File.Exists(SettingsFilePath))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
					using (FileStream fs = new FileStream(SettingsFilePath, FileMode.Open))
					{
						var loaded = (AppSettings)serializer.Deserialize(fs);
						loaded.ApplyVersionMigrations();
						return loaded;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading configuration: {ex.Message}");
			}

			// If an error occurred or the file does not exist, return default settings
			return new AppSettings();
		}

		// Save configuration to file
		public void Save()
		{
			try
			{
				// Create the directory if it does not exist
				string directory = Path.GetDirectoryName(SettingsFilePath);
				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				// Serialize and save the configuration
				XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
				using (FileStream fs = new FileStream(SettingsFilePath, FileMode.Create))
				{
					serializer.Serialize(fs, this);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error saving configuration: {ex.Message}");
			}
		}
	}

	public enum NotificationCategory
	{
		Copy,
		Save,
		Update,
		Ocr,
		Scp,
		ColorPicker
	}
}