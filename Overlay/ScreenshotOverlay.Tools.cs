using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CloudShot.Core;
using CloudShot.Export;
using CloudShot.Overlay;

namespace CloudShot
{
	public partial class ScreenshotOverlay
	{
		private void CaptureToolbar_ActionRequested(object sender, CaptureToolbarAction action)
		{
			switch (action)
			{
				case CaptureToolbarAction.PenMode:
					SetDrawingMode(DrawingToolMode.Pen);
					break;
				case CaptureToolbarAction.RectangleMode:
					SetDrawingMode(DrawingToolMode.Rectangle);
					break;
				case CaptureToolbarAction.FilledRectangleMode:
					SetDrawingMode(DrawingToolMode.FilledRectangle);
					break;
				case CaptureToolbarAction.PixelateMode:
					SetDrawingMode(DrawingToolMode.Pixelate);
					break;
				case CaptureToolbarAction.ArrowMode:
					SetDrawingMode(DrawingToolMode.Arrow);
					break;
				case CaptureToolbarAction.HighlighterMode:
					SetDrawingMode(DrawingToolMode.Highlighter);
					break;
				case CaptureToolbarAction.LineMode:
					SetDrawingMode(DrawingToolMode.Line);
					break;
				case CaptureToolbarAction.StepsMode:
					SetDrawingMode(DrawingToolMode.Steps);
					break;
				case CaptureToolbarAction.TextMode:
					SetDrawingMode(DrawingToolMode.Text);
					break;
				case CaptureToolbarAction.EraserMode:
					SetDrawingMode(DrawingToolMode.Eraser);
					break;
				case CaptureToolbarAction.Move:
					SetMoveMode();
					break;
				case CaptureToolbarAction.ColorPicker:
					ShowDrawingColorDialog();
					break;
				case CaptureToolbarAction.Undo:
					UndoLastDrawingLine();
					break;
				case CaptureToolbarAction.Copy:
					CaptureSelectedArea();
					Close();
					break;
				case CaptureToolbarAction.Save:
					SaveSelectedArea();
					Close();
					break;
				case CaptureToolbarAction.Ocr:
					_ = PerformOcr();
					break;
				case CaptureToolbarAction.Scp:
					PerformScp();
					break;
				case CaptureToolbarAction.Close:
					Close();
					break;
			}
		}

		private void SetDrawingMode(DrawingToolMode mode)
		{
			CancelTextEditing();
			if (currentDrawingMode == DrawingToolMode.Eraser && mode != DrawingToolMode.Eraser)
			{
				InvalidateEraserPreview(lastMousePosition);
			}

			isMoveMode = false;
			currentDrawingMode = mode;
			captureToolbar.SetDrawingMode(mode);
			UpdateCursorForCurrentMode();
			InvalidateSelectionArea();
		}

		private void SetMoveMode()
		{
			CancelTextEditing();
			if (currentDrawingMode == DrawingToolMode.Eraser)
			{
				InvalidateEraserPreview(lastMousePosition);
			}

			isMoveMode = true;
			currentDrawingMode = DrawingToolMode.Pen;
			captureToolbar.SetMoveMode(true);
			UpdateCursorForCurrentMode();
			InvalidateSelectionArea();
		}

		private void UpdateCursorForCurrentMode()
		{
			if (isMoveMode)
			{
				Cursor = lastMousePosition != Point.Empty && IsPointInsideSelectionRectangle(lastMousePosition)
					? Cursors.SizeAll
					: Cursors.Cross;
				return;
			}

			if (ShouldShowReSelectCursorAt(lastMousePosition))
			{
				Cursor = Cursors.Cross;
				return;
			}

			switch (currentDrawingMode)
			{
				case DrawingToolMode.Pen:
					Cursor = penCursor;
					break;
				case DrawingToolMode.Text:
					Cursor = Cursors.IBeam;
					break;
				case DrawingToolMode.Eraser:
					Cursor = Cursors.Default;
					break;
				default:
					Cursor = Cursors.Cross;
					break;
			}
		}

		private void ShowDrawingColorDialog()
		{
			using (ColorDialog colorDialog = new ColorDialog())
			{
				colorDialog.Color = currentDrawingColor;
				colorDialog.FullOpen = true;
				colorDialog.AnyColor = true;

				if (colorDialog.ShowDialog() == DialogResult.OK)
				{
					currentDrawingColor = colorDialog.Color;
					captureToolbar.SetDrawingColor(currentDrawingColor);
				}
			}
		}

		private static Color ParseColorOrDefault(string value, Color fallback)
		{
			if (string.IsNullOrWhiteSpace(value))
				return fallback;

			try
			{
				return ColorTranslator.FromHtml(value);
			}
			catch
			{
				return fallback;
			}
		}

		private bool HandleShortcut(Keys keyData)
		{
			bool hasSelection = !selectionRectangle.IsEmpty && selectionRectangle.Width > 0 && selectionRectangle.Height > 0;

			if (!CaptureShortcutHandler.TryHandle(
				    keyData,
				    settings,
				    isScreenshotValid,
				    isColorPickerMode,
				    hasSelection,
				    activeTextEditor == null,
				    out CaptureShortcutAction action))
			{
				return false;
			}

			switch (action)
			{
				case CaptureShortcutAction.Close:
					Close();
					break;
				case CaptureShortcutAction.Copy:
					CaptureSelectedArea();
					Close();
					break;
				case CaptureShortcutAction.Save:
					SaveSelectedArea();
					Close();
					break;
				case CaptureShortcutAction.Undo:
					UndoLastDrawingLine();
					break;
				case CaptureShortcutAction.Ocr:
					_ = PerformOcr();
					break;
				case CaptureShortcutAction.Scp:
					PerformScp();
					break;
				case CaptureShortcutAction.ActivateColorPicker:
					ActivateColorPicker();
					break;
				case CaptureShortcutAction.PenTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Pen))
						SetDrawingMode(DrawingToolMode.Pen);
					break;
				case CaptureShortcutAction.RectangleTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Rectangle))
						SetDrawingMode(DrawingToolMode.Rectangle);
					break;
				case CaptureShortcutAction.FilledRectangleTool:
					if (IsDrawingToolEnabled(DrawingToolMode.FilledRectangle))
						SetDrawingMode(DrawingToolMode.FilledRectangle);
					break;
				case CaptureShortcutAction.PixelateTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Pixelate))
						SetDrawingMode(DrawingToolMode.Pixelate);
					break;
				case CaptureShortcutAction.ArrowTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Arrow))
						SetDrawingMode(DrawingToolMode.Arrow);
					break;
				case CaptureShortcutAction.HighlighterTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Highlighter))
						SetDrawingMode(DrawingToolMode.Highlighter);
					break;
				case CaptureShortcutAction.LineTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Line))
						SetDrawingMode(DrawingToolMode.Line);
					break;
				case CaptureShortcutAction.StepsTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Steps))
						SetDrawingMode(DrawingToolMode.Steps);
					break;
				case CaptureShortcutAction.TextTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Text))
						SetDrawingMode(DrawingToolMode.Text);
					break;
				case CaptureShortcutAction.EraserTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Eraser))
						SetDrawingMode(DrawingToolMode.Eraser);
					break;
				case CaptureShortcutAction.MoveTool:
					if (settings.ToolMoveEnabled)
						SetMoveMode();
					break;
			}

			return true;
		}

		private void ScreenshotOverlay_KeyDown(object sender, KeyEventArgs e)
		{
			if (activeTextEditor != null && e.KeyCode == Keys.Escape)
			{
				CancelTextEditing();
				e.Handled = true;
				return;
			}

			if (HandleShortcut(e.KeyCode | e.Modifiers))
			{
				e.Handled = true;
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (activeTextEditor != null && (keyData & Keys.KeyCode) == Keys.Escape)
			{
				CancelTextEditing();
				return true;
			}

			if (HandleShortcut(keyData))
			{
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}
	}
}
