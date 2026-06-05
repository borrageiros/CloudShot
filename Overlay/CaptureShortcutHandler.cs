using System.Windows.Forms;

namespace CloudShot.Overlay
{
	public static class CaptureShortcutHandler
	{
		public static bool TryHandle(
			Keys keyData,
			AppSettings settings,
			bool isScreenshotValid,
			bool isColorPickerMode,
			bool hasSelection,
			out CaptureShortcutAction action)
		{
			action = CaptureShortcutAction.None;

			if (keyData == settings.ColorPickerShortcut ||
			    keyData == (Keys.Control | Keys.V))
			{
				if (isScreenshotValid)
				{
					action = CaptureShortcutAction.ActivateColorPicker;
					return true;
				}
			}

			if (isColorPickerMode && keyData == Keys.Escape)
			{
				action = CaptureShortcutAction.Close;
				return true;
			}

			if (keyData == settings.CancelShortcut)
			{
				action = CaptureShortcutAction.Close;
				return true;
			}

			if (keyData == settings.CopyShortcut && hasSelection && isScreenshotValid)
			{
				action = CaptureShortcutAction.Copy;
				return true;
			}

			if (keyData == settings.SaveShortcut && hasSelection && isScreenshotValid)
			{
				action = CaptureShortcutAction.Save;
				return true;
			}

			if (keyData == settings.UndoShortcut && isScreenshotValid)
			{
				action = CaptureShortcutAction.Undo;
				return true;
			}

			if ((keyData == settings.OcrShortcut || (keyData == (Keys.Control | Keys.R))) &&
			    hasSelection && isScreenshotValid)
			{
				action = CaptureShortcutAction.Ocr;
				return true;
			}

			if ((keyData == settings.ScpShortcut || (keyData == (Keys.Control | Keys.X))) &&
			    hasSelection && isScreenshotValid)
			{
				action = CaptureShortcutAction.Scp;
				return true;
			}

			return false;
		}
	}

	public enum CaptureShortcutAction
	{
		None,
		Close,
		Copy,
		Save,
		Undo,
		Ocr,
		Scp,
		ActivateColorPicker
	}
}
