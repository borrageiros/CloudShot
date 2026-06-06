using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

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
			ResetToolbarToolsToDefaults();
			SettingsVersion = 5;
		}

		public void ResetToolShortcutsToDefaults()
		{
			PenToolShortcut = Keys.P;
			RectangleToolShortcut = Keys.R;
			FilledRectangleToolShortcut = Keys.F;
			PixelateToolShortcut = Keys.X;
			ArrowToolShortcut = Keys.A;
			HighlighterToolShortcut = Keys.H;
			LineToolShortcut = Keys.L;
			StepsToolShortcut = Keys.N;
			TextToolShortcut = Keys.T;
			EraserToolShortcut = Keys.E;
			MoveToolShortcut = Keys.M;
		}

		public void ResetToolbarToolsToDefaults()
		{
			ToolPenEnabled = true;
			ToolRectangleEnabled = true;
			ToolFilledRectangleEnabled = true;
			ToolPixelateEnabled = true;
			ToolArrowEnabled = true;
			ToolHighlighterEnabled = true;
			ToolLineEnabled = true;
			ToolStepsEnabled = true;
			ToolTextEnabled = true;
			ToolEraserEnabled = true;
			ToolMoveEnabled = true;
			ToolColorPickerEnabled = true;
			ToolUndoEnabled = true;
			ToolCopyEnabled = true;
			ToolSaveEnabled = true;
			ToolOcrEnabled = true;
			ToolScpEnabled = true;
			ToolCloseEnabled = true;
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
}