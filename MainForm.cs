using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using CloudShot.Core;

namespace CloudShot
{
  public partial class MainForm : Form
  {
    private NotifyIcon trayIcon;
    private KeyboardHook keyboardHook;
    private ScreenshotOverlay overlay;
    private AppSettings settings;
    private string pendingUpdateUrl;

    public MainForm()
    {
      // Load configuration
      settings = AppSettings.Load();

      InitializeComponents();
      SetupKeyboardHook();
    }

    private void InitializeComponents()
    {
      this.ShowInTaskbar = false;
      this.WindowState = FormWindowState.Minimized;
      this.FormBorderStyle = FormBorderStyle.None;
      this.Opacity = 0;

      // Try to load the application icon
      Icon appIcon = null;
      try
      {
        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
        {
          appIcon = new Icon(iconPath);
          this.Icon = appIcon;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading icon: {ex.Message}");
      }

      // Configure the system tray icon
      trayIcon = new NotifyIcon
      {
        Icon = appIcon ?? Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
        Text = "CloudShot",
        Visible = true
      };

      // Context menu for the system tray icon
      ContextMenuStrip menu = new ContextMenuStrip();
      menu.Items.Add("Capture Screen", null, OnCaptureScreen);
      menu.Items.Add("Settings", null, OnOpenConfig);
      menu.Items.Add("-");
      menu.Items.Add("Exit", null, OnExit);
      trayIcon.ContextMenuStrip = menu;

      // Handle double-click on the icon
      trayIcon.DoubleClick += OnCaptureScreen;

      // Hide form when starting and check for updates once
      this.Load += (s, e) =>
      {
        this.Hide();
        CheckForUpdatesOnStartup();
      };

      // Apply the startup configuration with Windows
      ApplyStartupSetting(settings.StartWithWindows);
    }

    public static void ApplyStartupSetting(bool startWithWindows)
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
        Console.WriteLine($"Error configuring automatic startup: {ex.Message}");
      }
    }

    private void OnOpenConfig(object sender, EventArgs e)
    {
      // Show configuration form
      using (ConfigForm configForm = new ConfigForm(settings))
      {
        if (configForm.ShowDialog() == DialogResult.OK)
        {
          // The configuration is already saved in the form,
          // only apply possible changes
          ApplyStartupSetting(settings.StartWithWindows);
        }
      }
    }

    private void SetupKeyboardHook()
    {
      try
      {
        keyboardHook = new KeyboardHook();
        keyboardHook.KeyPressed += OnPrintScreenPressed;
        keyboardHook.RegisterHotKey(0, Keys.PrintScreen);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error configuring the keyboard shortcut: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void OnPrintScreenPressed(object sender, KeyPressedEventArgs e)
    {
      CaptureScreen();
    }

    private void OnCaptureScreen(object sender, EventArgs e)
    {
      CaptureScreen();
    }

    private void CaptureScreen()
    {
      Bitmap screenShot = null;

      try
      {
        if (overlay != null)
        {
          try
          {
            overlay.Dispose();
            overlay = null;
          }
          catch { }
        }

        screenShot = ScreenCaptureService.CaptureAllScreens();

        this.Invoke(new Action(() =>
        {
          try
          {
            overlay = new ScreenshotOverlay(screenShot);
            overlay.ScreenshotCaptured += OnScreenshotCaptured;
            overlay.Show();
            screenShot = null;
          }
          catch (Exception ex)
          {
            MessageBox.Show($"Error showing the overlay: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            screenShot?.Dispose();
            screenShot = null;
          }
        }));
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error capturing screen: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        screenShot?.Dispose();
      }
    }

    private void OnScreenshotCaptured(object sender, ScreenshotEventArgs e)
    {
      try
      {
        if (e.Image != null)
        {
          // Copy the image to the clipboard
          Clipboard.SetImage(e.Image);

          // Show a notification
          trayIcon.ShowBalloonTip(
              3000,
              "CloudShot",
              "Screenshot copied to clipboard",
              ToolTipIcon.Info
          );
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error processing the capture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private async void CheckForUpdatesOnStartup()
    {
      try
      {
        UpdateCheckResult result = await UpdateService.CheckForUpdatesAsync();

        if (result != null && result.UpdateAvailable)
        {
          pendingUpdateUrl = result.ReleaseUrl;

          trayIcon.BalloonTipClicked -= OnUpdateBalloonClicked;
          trayIcon.BalloonTipClicked += OnUpdateBalloonClicked;

          trayIcon.ShowBalloonTip(
              5000,
              "CloudShot",
              $"A new version ({result.LatestVersion}) is available. Click to download.",
              ToolTipIcon.Info
          );
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error checking for updates: {ex.Message}");
      }
    }

    private void OnUpdateBalloonClicked(object sender, EventArgs e)
    {
      if (string.IsNullOrEmpty(pendingUpdateUrl))
      {
        return;
      }

      try
      {
        Process.Start(new ProcessStartInfo(pendingUpdateUrl) { UseShellExecute = true });
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error opening update URL: {ex.Message}");
      }
    }

    private void OnExit(object sender, EventArgs e)
    {
      trayIcon.Visible = false;
      Application.Exit();
    }

    /// <summary>
    /// Shows a notification using the system tray icon
    /// </summary>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message</param>
    public void ShowNotification(string title, string message)
    {
      if (trayIcon != null)
      {
        trayIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
      }
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (keyboardHook != null)
        {
          keyboardHook.Dispose();
          keyboardHook = null;
        }
        if (trayIcon != null)
        {
          trayIcon.Dispose();
          trayIcon = null;
        }
        if (overlay != null)
        {
          overlay.Dispose();
          overlay = null;
        }
      }
      base.Dispose(disposing);
    }
  }

  // Class to manage the key pressed event
  public class KeyPressedEventArgs : EventArgs
  {
    public Keys Key { get; private set; }

    public KeyPressedEventArgs(Keys key)
    {
      Key = key;
    }
  }

  // Class to manage the screenshot event
  public class ScreenshotEventArgs : EventArgs
  {
    public Image Image { get; private set; }

    public ScreenshotEventArgs(Image image)
    {
      Image = image;
    }
  }
}