using System;
using System.Drawing;
using System.Windows.Forms;

namespace CloudShot
{
	public class HotkeyControl : TextBox
	{
		private Keys _hotkey;

		public Func<Keys, string> DuplicateValidator { get; set; }

		public HotkeyControl()
		{
			ReadOnly = true;
			BackColor = SystemColors.Window;
			Font = new Font("Segoe UI", 9F);
			BorderStyle = BorderStyle.FixedSingle;
		}

		public Keys Hotkey
		{
			get { return _hotkey; }
			set
			{
				_hotkey = value;
				Text = FormatHotkey(_hotkey);
			}
		}

		public static string FormatHotkey(Keys hotkey)
		{
			if (hotkey == Keys.None)
				return string.Empty;

			string text = "";

			if ((hotkey & Keys.Control) == Keys.Control)
				text += "Ctrl + ";
			if ((hotkey & Keys.Shift) == Keys.Shift)
				text += "Shift + ";
			if ((hotkey & Keys.Alt) == Keys.Alt)
				text += "Alt + ";

			text += (hotkey & Keys.KeyCode).ToString();
			return text;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if ((e.Modifiers & Keys.Alt) == Keys.Alt)
				return;

			Keys keyCode = e.KeyCode;
			if (keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey || keyCode == Keys.Menu)
				return;

			Keys proposed = keyCode | e.Modifiers;
			string conflict = DuplicateValidator?.Invoke(proposed);
			if (!string.IsNullOrEmpty(conflict))
			{
				MessageBox.Show(
					$"This shortcut is already assigned to \"{conflict}\".",
					"Duplicate shortcut",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			_hotkey = proposed;
			Text = FormatHotkey(_hotkey);

			e.SuppressKeyPress = true;
			e.Handled = true;
		}
	}
}
