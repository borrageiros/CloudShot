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

		// Windows startup
		public bool StartWithWindows { get; set; }

		// Upload provider ("Scp" or "Imgur")
		public string UploadProvider { get; set; }

		// SCP configuration
		public string ScpHost { get; set; }
		public int ScpPort { get; set; }
		public string ScpRemotePath { get; set; }
		public string ScpKeyPath { get; set; }
		public string ScpKeyPassphrase { get; set; }
		public string ScpClipboardText { get; set; }

		// Imgur configuration (optional Client-ID override; falls back to the embedded one)
		public string ImgurClientId { get; set; }

		// Color picker configuration
		public string ColorFormat { get; set; }

		// Default drawing color (hex format, e.g. #FF0000)
		public string DefaultDrawingColor { get; set; }

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
			StartWithWindows = true;
			UploadProvider = "Scp";
			ScpHost = "";
			ScpPort = 22;
			ScpRemotePath = "";
			ScpKeyPath = "";
			ScpKeyPassphrase = "";
			ScpClipboardText = "";
			ImgurClientId = "";
			ColorFormat = "RGB";
			DefaultDrawingColor = "#FF0000";
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
						return (AppSettings)serializer.Deserialize(fs);
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