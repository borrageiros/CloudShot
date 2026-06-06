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
			bool allowToolShortcuts,
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

			if (allowToolShortcuts && isScreenshotValid && !isColorPickerMode)
			{
				if (MatchesShortcut(keyData, settings.PenToolShortcut))
				{
					action = CaptureShortcutAction.PenTool;
					return true;
				}

				if (MatchesShortcut(keyData, settings.RectangleToolShortcut))
				{
					action = CaptureShortcutAction.RectangleTool;
					return true;
				}

				if (MatchesShortcut(keyData, settings.FilledRectangleToolShortcut))
				{
					action = CaptureShortcutAction.FilledRectangleTool;
					return true;
				}

				if (MatchesShortcut(keyData, settings.PixelateToolShortcut))
				{
					action = CaptureShortcutAction.PixelateTool;
					return true;
				}

				if (MatchesShortcut(keyData, settings.ArrowToolShortcut))
				{
					action = CaptureShortcutAction.ArrowTool;
					return true;
				}

				if (MatchesShortcut(keyData, settings.HighlighterToolShortcut))
				{
					action = CaptureShortcutAction.HighlighterTool;
					return true;
				}

				if (MatchesShortcut(keyData, settings.LineToolShortcut))
				{
					action = CaptureShortcutAction.LineTool;
					return true;
				}

				if (MatchesShortcut(keyData, settings.StepsToolShortcut))
				{
					action = CaptureShortcutAction.StepsTool;
					return true;
				}

				if (MatchesShortcut(keyData, settings.TextToolShortcut))
				{
					action = CaptureShortcutAction.TextTool;
					return true;
				}

				if (MatchesShortcut(keyData, settings.MoveToolShortcut))
				{
					action = CaptureShortcutAction.MoveTool;
					return true;
				}
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

		private static bool MatchesShortcut(Keys keyData, Keys configured)
		{
			return configured != Keys.None && keyData == configured;
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
		ActivateColorPicker,
		PenTool,
		RectangleTool,
		FilledRectangleTool,
		PixelateTool,
		ArrowTool,
		HighlighterTool,
		LineTool,
		StepsTool,
		TextTool,
		MoveTool
	}
}
