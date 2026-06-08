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
	public partial class ConfigForm
	{
		private TabPage BuildAboutTab()
		{
			var page = new TabPage("About")
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
				Margin = new Padding(0, 8, 0, 0)
			};
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

			var logoPanel = new Panel
			{
				Height = 96,
				Dock = DockStyle.Top,
				Margin = new Padding(0, 0, 0, 12)
			};

			var logo = new PictureBox
			{
				Size = new Size(96, 96),
				SizeMode = PictureBoxSizeMode.Zoom,
				BackColor = Color.Transparent,
				Location = new Point(182, 0)
			};

			try
			{
				string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
				if (File.Exists(iconPath))
				{
					using (Icon appIcon = new Icon(iconPath))
					{
						logo.Image = appIcon.ToBitmap();
					}
				}
				else
				{
					logo.Image = Icon.ExtractAssociatedIcon(Application.ExecutablePath)?.ToBitmap();
				}
			}
			catch
			{
				logo.Image = SystemIcons.Application.ToBitmap();
			}

			logoPanel.Controls.Add(logo);
			logoPanel.Resize += (s, e) => logo.Left = Math.Max(0, (logoPanel.Width - logo.Width) / 2);

			var appName = new Label
			{
				Text = "CloudShot",
				Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
				ForeColor = Color.FromArgb(40, 40, 50),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleCenter,
				AutoSize = false,
				Height = 32,
				Margin = new Padding(0, 0, 0, 4)
			};

			var version = new Label
			{
				Text = $"Version {GetAppVersion()}",
				Font = BodyFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleCenter,
				AutoSize = false,
				Height = 22,
				Margin = new Padding(0, 0, 0, 12)
			};

			var tagline = new Label
			{
				Text = "Lightweight screenshot tool for Windows with annotations, OCR, and SCP upload.",
				Font = HintFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleCenter,
				AutoSize = false,
				Height = 40,
				Margin = new Padding(0, 0, 0, 20)
			};

			var linksPanel = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.TopDown,
				AutoSize = true,
				WrapContents = false,
				Margin = new Padding(0, 0, 0, 20),
				Padding = new Padding(0)
			};

			linksPanel.Controls.Add(CreateAboutLinkRow("GitHub repository", GitHubUrl));
			linksPanel.Controls.Add(CreateAboutLinkRow("Creator website", PortfolioUrl));
			linksPanel.Controls.Add(CreateAboutLinkRow("Download page", DownloadUrl));

			var btnCheckUpdates = CreateAccentButton("Check for updates", 160);
			btnCheckUpdates.Margin = new Padding(0, 0, 0, 24);
			btnCheckUpdates.Click += async (s, e) => await CheckForUpdatesAsync(btnCheckUpdates);

			var copyright = new Label
			{
				Text = $"\u00a9 {DateTime.Now.Year} borrageiros",
				Font = HintFont,
				ForeColor = Color.FromArgb(130, 130, 140),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleCenter,
				AutoSize = false,
				Height = 20,
				Margin = new Padding(0, 0, 0, 4)
			};

			var license = new Label
			{
				Text = "Open source \u00b7 MIT License",
				Font = HintFont,
				ForeColor = Color.FromArgb(130, 130, 140),
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleCenter,
				AutoSize = false,
				Height = 20,
				Margin = new Padding(0)
			};

			var buttonPanel = new Panel
			{
				AutoSize = true,
				Margin = new Padding(0, 0, 0, 24)
			};
			btnCheckUpdates.Location = new Point(0, 0);
			buttonPanel.Controls.Add(btnCheckUpdates);
			buttonPanel.Resize += (s, e) => btnCheckUpdates.Left = Math.Max(0, (buttonPanel.Width - btnCheckUpdates.Width) / 2);
			buttonPanel.Size = new Size(460, btnCheckUpdates.Height);

			var centeredLinks = new Panel
			{
				AutoSize = true,
				Margin = new Padding(0, 0, 0, 20)
			};
			centeredLinks.Controls.Add(linksPanel);
			centeredLinks.Resize += (s, e) => linksPanel.Left = Math.Max(0, (centeredLinks.Width - linksPanel.Width) / 2);

			int row = 0;
			layout.Controls.Add(logoPanel, 0, row++);
			layout.Controls.Add(appName, 0, row++);
			layout.Controls.Add(version, 0, row++);
			layout.Controls.Add(tagline, 0, row++);
			layout.Controls.Add(centeredLinks, 0, row++);
			layout.Controls.Add(buttonPanel, 0, row++);
			layout.Controls.Add(copyright, 0, row++);
			layout.Controls.Add(license, 0, row++);

			page.Controls.Add(layout);
			return page;
		}

		private Control CreateAboutLinkRow(string label, string url)
		{
			var row = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.LeftToRight,
				AutoSize = true,
				WrapContents = false,
				Margin = new Padding(0, 0, 0, 6)
			};

			row.Controls.Add(new Label
			{
				Text = label + ":",
				Font = BodyFont,
				AutoSize = true,
				Margin = new Padding(0, 3, 6, 0)
			});
			row.Controls.Add(CreateLinkLabel(url, url));

			return row;
		}

		private LinkLabel CreateLinkLabel(string text, string url)
		{
			var link = new LinkLabel
			{
				Text = text,
				AutoSize = true,
				LinkColor = AccentColor,
				ActiveLinkColor = AccentColor,
				VisitedLinkColor = AccentColor,
				Font = BodyFont,
				Margin = new Padding(0, 3, 0, 0)
			};
			link.LinkClicked += (s, e) => OpenUrl(url);
			return link;
		}

		private static void OpenUrl(string url)
		{
			try
			{
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private static string GetAppVersion()
		{
			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			if (version == null)
			{
				return "Unknown";
			}

			if (version.Build <= 0 && version.Revision <= 0)
			{
				return version.ToString(2);
			}

			return version.ToString();
		}

		private async Task CheckForUpdatesAsync(Button button)
		{
			button.Enabled = false;
			button.Text = "Checking...";

			try
			{
				UpdateCheckResult result = await UpdateService.CheckForUpdatesAsync();

				if (result == null)
				{
					MessageBox.Show(
						"Could not check for updates. Please try again later.",
						"Update check",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					return;
				}

				if (result.UpdateAvailable)
				{
					DialogResult choice = MessageBox.Show(
						$"A new version ({result.LatestVersion}) is available.\nYou are running version {result.CurrentVersion}.\n\nOpen the download page?",
						"Update available",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Information);

					if (choice == DialogResult.Yes)
					{
						OpenUrl(result.ReleaseUrl ?? DownloadUrl);
					}
				}
				else
				{
					MessageBox.Show(
						$"CloudShot is up to date.\nCurrent version: {result.CurrentVersion}",
						"Update check",
						MessageBoxButtons.OK,
						MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Could not check for updates: {ex.Message}",
					"Update check",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
			finally
			{
				button.Enabled = true;
				button.Text = "Check for updates";
			}
		}

	}
}
