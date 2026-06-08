using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CloudShot.Core;
using CloudShot.Overlay;

namespace CloudShot
{
	public partial class ScreenshotOverlay
	{
		private void ActivateColorPicker()
		{
			if (!isScreenshotValid)
			{
				return;
			}

			CancelTextEditing();
			isColorPickerMode = true;
			isSelecting = false;
			isResizing = false;
			isDrawing = false;
			isColorSelected = false;
			captureToolbar.HideImmediate();

			if (colorPickerPreview == null)
			{
				colorPickerPreview = new Bitmap(ColorPickerPreviewSize, ColorPickerPreviewSize);
			}

			Cursor = Cursors.Cross;
			Invalidate();
		}

		private void ProcessColorPick(Point location)
		{
			if (!isColorPickerMode || !isScreenshotValid)
			{
				return;
			}

			Point imagePoint = coordinateMapper.ToImagePoint(location);

			if (imagePoint.X < 0 || imagePoint.X >= screenshotWidth || imagePoint.Y < 0 || imagePoint.Y >= screenshotHeight)
			{
				return;
			}

			selectedColor = BitmapPixelReader.GetPixel(screenshot, imagePoint.X, imagePoint.Y);
			colorPickerPoint = location;
			UpdateColorPickerPreview(location);
			lastMousePosition = location;
			Invalidate();
		}

		private void UpdateColorPickerPreview(Point location)
		{
			if (colorPickerPreview == null || !isScreenshotValid)
			{
				return;
			}

			Point screenshotPoint = coordinateMapper.ToImagePoint(location);
			int previewSourceSize = ColorPickerPreviewSize / ColorPickerZoomFactor;
			int halfSourceSize = previewSourceSize / 2;

			int previewX = Math.Max(0, screenshotPoint.X - halfSourceSize);
			int previewY = Math.Max(0, screenshotPoint.Y - halfSourceSize);
			previewX = Math.Min(previewX, screenshotWidth - previewSourceSize);
			previewY = Math.Min(previewY, screenshotHeight - previewSourceSize);

			Rectangle sourceRect = new Rectangle(
				previewX,
				previewY,
				Math.Min(previewSourceSize, screenshotWidth - previewX),
				Math.Min(previewSourceSize, screenshotHeight - previewY));

			using (Graphics g = Graphics.FromImage(colorPickerPreview))
			{
				g.FillRectangle(Brushes.White, 0, 0, ColorPickerPreviewSize, ColorPickerPreviewSize);
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.PixelOffsetMode = PixelOffsetMode.Half;
				g.DrawImage(
					screenshot,
					new Rectangle(0, 0, ColorPickerPreviewSize, ColorPickerPreviewSize),
					sourceRect,
					GraphicsUnit.Pixel);
			}
		}

		private void FinishColorPick()
		{
			if (!isColorPickerMode || !isScreenshotValid || selectedColor == Color.Empty)
			{
				return;
			}

			try
			{
				string colorString = ColorFormatter.GetColorString(selectedColor, settings.ColorFormat);
				Clipboard.SetText(colorString);
				isColorSelected = true;

				BeginInvoke(new Action(() =>
				{
					Close();
					NotifyColorPicked(selectedColor, colorString, settings.ColorFormat);
				}));
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error copying color: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void NotifyColorPicked(Color color, string colorString, string format)
		{
			if (!settings.ShouldNotify(NotificationCategory.ColorPicker))
			{
				return;
			}

			foreach (Form form in Application.OpenForms)
			{
				if (form is MainForm mainForm)
				{
					mainForm.ShowNotification("Color Picked", $"Color {format}: {colorString}\nCopied to clipboard.");
					return;
				}
			}
		}
	}
}
