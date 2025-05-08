using System;
using System.IO;
using System.Windows.Forms;

namespace CloudShot
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Check if the application icon exists, if not, generate it
			string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
			if (!File.Exists(iconPath))
			{
				try
				{
					Console.WriteLine("Generating application icon...");
					IconGenerator.CreateAppIcon(iconPath);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error generating icon: {ex.Message}");
				}
			}

			// Load or create the application configuration
			AppSettings settings = AppSettings.Load();

			Application.Run(new MainForm());
		}
	}
}