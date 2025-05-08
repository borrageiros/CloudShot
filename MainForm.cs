using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing.Drawing2D;
using Microsoft.Win32;

namespace CloudShot
{
  public partial class MainForm : Form
  {
    private NotifyIcon trayIcon;
    private KeyboardHook keyboardHook;
    private ScreenshotOverlay overlay;
    private AppSettings settings;

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

      // Hide form when starting
      this.Load += (s, e) => this.Hide();

      // Apply the startup configuration with Windows
      ApplyStartupSetting(settings.StartWithWindows);
    }

    private void ApplyStartupSetting(bool startWithWindows)
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
      Bitmap overlay_screenshot = null;

      try
      {
        // Give time to the system to release resources
        GC.Collect();
        System.Threading.Thread.Sleep(100);

        // Release the previous overlay if it exists
        if (overlay != null)
        {
          try
          {
            overlay.Dispose();
            overlay = null;
          }
          catch { }
        }

        // Capture all screens
        Rectangle totalBounds = GetTotalScreenBounds();

        if (totalBounds.Width <= 0 || totalBounds.Height <= 0)
        {
          MessageBox.Show("Unable to determine screen dimensions", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
          return;
        }

        screenShot = new Bitmap(totalBounds.Width, totalBounds.Height, PixelFormat.Format32bppArgb);

        using (Graphics g = Graphics.FromImage(screenShot))
        {
          // Set a high quality for the capture
          g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
          g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
          g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
          g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

          // Fill with black color as base
          g.FillRectangle(Brushes.Black, 0, 0, totalBounds.Width, totalBounds.Height);

          // Capture all screens
          foreach (Screen screen in Screen.AllScreens)
          {
            // Calculate the relative position of each screen
            Rectangle bounds = screen.Bounds;
            int relX = bounds.X - totalBounds.X;
            int relY = bounds.Y - totalBounds.Y;

            try
            {
              // Capture the current screen and place it in the correct position
              g.CopyFromScreen(
                  bounds.X, bounds.Y,
                  relX, relY,
                  bounds.Size,
                  CopyPixelOperation.SourceCopy
              );
            }
            catch (Exception ex)
            {
              Console.WriteLine($"Error capturing screen {screen.DeviceName}: {ex.Message}");
            }
          }
        }

        // Create a copy of the capture for the overlay
        overlay_screenshot = new Bitmap(screenShot);

        // We don't need the original capture anymore
        screenShot.Dispose();
        screenShot = null;

        // Show the overlay to select a portion
        this.Invoke(new Action(() =>
        {
          try
          {
            overlay = new ScreenshotOverlay(overlay_screenshot);
            overlay.ScreenshotCaptured += OnScreenshotCaptured;
            overlay.Show();

            // The overlay will be responsible for releasing overlay_screenshot
            overlay_screenshot = null;
          }
          catch (Exception ex)
          {
            MessageBox.Show($"Error showing the overlay: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (overlay_screenshot != null)
            {
              overlay_screenshot.Dispose();
              overlay_screenshot = null;
            }
          }
        }));
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error capturing screen: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        // Ensure resources are released
        if (screenShot != null)
        {
          screenShot.Dispose();
        }
        if (overlay_screenshot != null)
        {
          overlay_screenshot.Dispose();
        }
      }
    }

    private Rectangle GetTotalScreenBounds()
    {
      // Calculate a rectangle that contains all screens
      int left = int.MaxValue;
      int top = int.MaxValue;
      int right = int.MinValue;
      int bottom = int.MinValue;

      foreach (Screen screen in Screen.AllScreens)
      {
        Rectangle bounds = screen.Bounds;

        left = Math.Min(left, bounds.Left);
        top = Math.Min(top, bounds.Top);
        right = Math.Max(right, bounds.Right);
        bottom = Math.Max(bottom, bounds.Bottom);
      }

      return new Rectangle(left, top, right - left, bottom - top);
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