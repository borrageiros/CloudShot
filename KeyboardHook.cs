using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CloudShot
{
	public enum ModifierKeys
	{
		None = 0,
		Alt = 1,
		Control = 2,
		Shift = 4,
		Win = 8
	}

	public class KeyboardHook : IDisposable
	{
		// Windows API imports
		[DllImport("user32.dll")]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

		[DllImport("user32.dll")]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		// Modifier key constants
		private const int WM_HOTKEY = 0x0312;

		private readonly Window window = new Window();
		private int currentId = 0;

		public KeyboardHook()
		{
			window.KeyPressed += delegate (object sender, KeyPressedEventArgs args)
			{
				KeyPressed?.Invoke(this, args);
			};
		}

		public event EventHandler<KeyPressedEventArgs> KeyPressed;

		public void RegisterHotKey(int modifier, Keys key)
		{
			// Increment the id to avoid duplicates
			currentId = currentId + 1;

			// Register the hotkey
			if (!RegisterHotKey(window.Handle, currentId, modifier, (int)key))
			{
				throw new InvalidOperationException("Could not register the key.");
			}
		}

		// Class to intercept keyboard events
		private class Window : NativeWindow, IDisposable
		{
			public event EventHandler<KeyPressedEventArgs> KeyPressed;

			public Window()
			{
				// Create an invisible window
				CreateHandle(new CreateParams());
			}

			protected override void WndProc(ref Message m)
			{
				base.WndProc(ref m);

				// Verify if it is a key press
				if (m.Msg == WM_HOTKEY)
				{
					// Get the pressed key
					Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
					KeyPressed?.Invoke(this, new KeyPressedEventArgs(key));
				}
			}

			public void Dispose()
			{
				DestroyHandle();
			}
		}

		public void Dispose()
		{
			// Unregister all keys
			for (int i = currentId; i > 0; i--)
			{
				UnregisterHotKey(window.Handle, i);
			}

			// Release the window
			window.Dispose();
		}
	}
}