using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CloudShot
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

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

			AppSettings settings = AppSettings.Load();

			if (args != null && args.Any(arg => string.Equals(arg, "--settings", StringComparison.OrdinalIgnoreCase)))
			{
				using (ConfigForm configForm = new ConfigForm(settings))
				{
					if (configForm.ShowDialog() == DialogResult.OK)
					{
						MainForm.ApplyStartupSetting(settings.StartWithWindows);
					}
				}

				return;
			}

			Application.Run(new MainForm());
		}
	}
}