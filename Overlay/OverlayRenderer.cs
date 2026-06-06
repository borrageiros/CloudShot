using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CloudShot.Core;
using CloudShot.Export;

namespace CloudShot.Overlay
{
	public sealed class OverlayRenderer : IDisposable
	{
		private const int DimAlpha = 128;
		private const int HandleSize = 8;

		private readonly SolidBrush dimBrush = new SolidBrush(Color.FromArgb(DimAlpha, 0, 0, 0));
		private readonly Pen selectionBorderPen = new Pen(Color.Red, 1);
		private readonly Pen rectangleSelectionBorderPen = new Pen(Color.Red, 2);
		private readonly Pen blackPen = new Pen(Color.Black, 1);
		private readonly Pen whiteDashedPen;
		private readonly Font titleFont = new Font("Segoe UI", 9f, FontStyle.Bold);
		private readonly Font valueFont = new Font("Segoe UI", 9f);
		private readonly Font instructionFont = new Font("Segoe UI", 11f, FontStyle.Bold);

		private Bitmap screenshot;

		public OverlayRenderer()
		{
			whiteDashedPen = new Pen(Color.White, 1) { DashStyle = DashStyle.Dot };
		}

		public void Initialize(Bitmap sourceScreenshot, Size clientSize)
		{
			screenshot = sourceScreenshot;
		}

		public void Paint(
			Graphics g,
			CoordinateMapper mapper,
			bool isScreenshotValid,
			bool isColorPickerMode,
			DrawingToolMode currentDrawingMode,
			bool isMoveMode,
			bool isSelecting,
			Rectangle selectionRectangle,
			IReadOnlyList<DrawingElement> drawingElements,
			IReadOnlyList<Rectangle> resizeHandles,
			Bitmap annotationLayer,
			Rectangle clientSelectionRect,
			DrawingElement inProgressDrawing,
			ColorPickerPaintState colorPickerState,
			AppSettings settings,
			Point lastMousePosition)
		{
			g.CompositingQuality = CompositingQuality.HighSpeed;
			g.InterpolationMode = InterpolationMode.Low;
			g.SmoothingMode = SmoothingMode.HighSpeed;
			g.PixelOffsetMode = PixelOffsetMode.HighSpeed;

			if (isColorPickerMode && isScreenshotValid)
			{
				PaintColorPickerMode(g, mapper, colorPickerState, settings, lastMousePosition);
				return;
			}

			if (!isScreenshotValid || screenshot == null)
			{
				return;
			}

			Rectangle screenshotClientRect = mapper.ToClientRect(new Rectangle(0, 0, screenshot.Width, screenshot.Height));
			g.DrawImage(
				screenshot,
				screenshotClientRect,
				0,
				0,
				screenshot.Width,
				screenshot.Height,
				GraphicsUnit.Pixel);

			DrawDimOverlay(g, clientSelectionRect);

			if (selectionRectangle.IsEmpty || selectionRectangle.Width <= 0 || selectionRectangle.Height <= 0)
			{
				return;
			}

			Rectangle validRect = mapper.ClampToImage(selectionRectangle, screenshot.Width, screenshot.Height);
			if (validRect.Width <= 0 || validRect.Height <= 0)
			{
				return;
			}

			Rectangle screenRect = mapper.ToClientRect(validRect);

			if (isMoveMode)
			{
				g.DrawRectangle(whiteDashedPen, screenRect);
				g.DrawRectangle(selectionBorderPen, screenRect);
			}
			else
			{
				Pen borderPen = currentDrawingMode == DrawingToolMode.Pen ? selectionBorderPen : rectangleSelectionBorderPen;
				g.DrawRectangle(borderPen, screenRect);
			}

			if (annotationLayer != null && !annotationLayer.Size.IsEmpty)
			{
				g.DrawImage(annotationLayer, screenRect);
			}
			else if (drawingElements != null && drawingElements.Count > 0)
			{
				g.TranslateTransform(screenRect.X, screenRect.Y);
				ImageExporter.DrawAnnotationsOnLayer(g, drawingElements, screenshot, selectionRectangle);
				g.ResetTransform();
			}

			if (inProgressDrawing != null)
			{
				ImageExporter.DrawElementPreview(g, inProgressDrawing, clientSelectionRect, screenshot, selectionRectangle);
			}

			if (resizeHandles != null && !isSelecting && !isMoveMode)
			{
				foreach (Rectangle handle in resizeHandles)
				{
					g.FillRectangle(Brushes.White, handle);
					g.DrawRectangle(Pens.Black, handle);
				}
			}
		}

		public static Rectangle GetSelectionInvalidationRect(Rectangle previous, Rectangle current, int padding)
		{
			if (previous.IsEmpty)
			{
				return InflateRect(current, padding);
			}

			Rectangle union = Rectangle.Union(previous, current);
			return InflateRect(union, padding);
		}

		public static Rectangle GetDrawingInvalidationRect(Point point, int penSize)
		{
			int padding = penSize * 3;
			return new Rectangle(
				point.X - padding,
				point.Y - padding,
				padding * 2,
				padding * 2);
		}

		public static Rectangle GetRectangleDrawingInvalidationRect(Point start, Point end, int penSize)
		{
			int x = Math.Min(start.X, end.X) - penSize;
			int y = Math.Min(start.Y, end.Y) - penSize;
			int width = Math.Abs(start.X - end.X) + penSize * 2;
			int height = Math.Abs(start.Y - end.Y) + penSize * 2;
			return new Rectangle(x, y, width, height);
		}

		public static Rectangle GetStepInvalidationRect(Point center)
		{
			int padding = ImageExporter.StepCircleRadius + 4;
			return new Rectangle(
				center.X - padding,
				center.Y - padding,
				padding * 2,
				padding * 2);
		}

		public static Rectangle GetTextInvalidationRect(Point topLeft, string text)
		{
			return ImageExporter.GetTextInvalidationRect(topLeft, text);
		}

		private void DrawDimOverlay(Graphics g, Rectangle excludeRect)
		{
			Rectangle bounds = Rectangle.Round(g.VisibleClipBounds);

			if (excludeRect.IsEmpty || excludeRect.Width <= 0 || excludeRect.Height <= 0)
			{
				g.FillRectangle(dimBrush, bounds);
				return;
			}

			Rectangle exclude = Rectangle.Intersect(excludeRect, bounds);
			if (exclude.IsEmpty)
			{
				g.FillRectangle(dimBrush, bounds);
				return;
			}

			if (exclude.Top > bounds.Top)
			{
				g.FillRectangle(dimBrush, bounds.Left, bounds.Top, bounds.Width, exclude.Top - bounds.Top);
			}

			if (exclude.Bottom < bounds.Bottom)
			{
				g.FillRectangle(dimBrush, bounds.Left, exclude.Bottom, bounds.Width, bounds.Bottom - exclude.Bottom);
			}

			if (exclude.Left > bounds.Left)
			{
				g.FillRectangle(dimBrush, bounds.Left, exclude.Top, exclude.Left - bounds.Left, exclude.Height);
			}

			if (exclude.Right < bounds.Right)
			{
				g.FillRectangle(dimBrush, exclude.Right, exclude.Top, bounds.Right - exclude.Right, exclude.Height);
			}
		}

		private void PaintColorPickerMode(
			Graphics g,
			CoordinateMapper mapper,
			ColorPickerPaintState state,
			AppSettings settings,
			Point lastMousePosition)
		{
			g.DrawImage(screenshot, new Rectangle(0, 0, screenshot.Width, screenshot.Height), 0, 0, screenshot.Width, screenshot.Height, GraphicsUnit.Pixel);

			if (state.SelectedColor == Color.Empty)
			{
				DrawCrosshair(g, lastMousePosition);
				return;
			}

			int previewX = state.PreviewPoint.X + 20;
			int previewY = state.PreviewPoint.Y + 20;

			if (previewX + state.PreviewSize + 160 > g.VisibleClipBounds.Width)
			{
				previewX = state.PreviewPoint.X - state.PreviewSize - 160;
			}

			if (previewY + state.PreviewSize + 60 > g.VisibleClipBounds.Height)
			{
				previewY = state.PreviewPoint.Y - state.PreviewSize - 10;
			}

			if (state.PreviewBitmap != null)
			{
				g.FillRectangle(Brushes.White, previewX - 2, previewY - 2, state.PreviewSize + 4, state.PreviewSize + 4);
				g.DrawImage(state.PreviewBitmap, new Rectangle(previewX, previewY, state.PreviewSize, state.PreviewSize));
				g.DrawRectangle(Pens.Black, previewX, previewY, state.PreviewSize, state.PreviewSize);
			}

			int infoBoxX = previewX + state.PreviewSize + 10;
			int infoBoxY = previewY;
			int infoBoxWidth = 140;
			int infoBoxHeight = 110;

			g.FillRectangle(new SolidBrush(Color.FromArgb(240, 230, 230, 230)), infoBoxX, infoBoxY, infoBoxWidth, infoBoxHeight);
			g.DrawRectangle(Pens.Black, infoBoxX, infoBoxY, infoBoxWidth, infoBoxHeight);

			int colorSampleX = infoBoxX + 10;
			int colorSampleY = infoBoxY + 10;
			int colorSampleWidth = infoBoxWidth - 20;
			int colorSampleHeight = 40;

			g.FillRectangle(new SolidBrush(state.SelectedColor), colorSampleX, colorSampleY, colorSampleWidth, colorSampleHeight);
			g.DrawRectangle(Pens.Black, colorSampleX, colorSampleY, colorSampleWidth, colorSampleHeight);

			string colorInfo = ColorFormatter.GetColorString(state.SelectedColor, settings.ColorFormat);
			string rgbInfo = $"R: {state.SelectedColor.R}, G: {state.SelectedColor.G}, B: {state.SelectedColor.B}";
			if (state.SelectedColor.A < 255)
			{
				rgbInfo = $"A: {state.SelectedColor.A}, " + rgbInfo;
			}

			g.DrawString(settings.ColorFormat + ":", titleFont, Brushes.Black, infoBoxX + 10, infoBoxY + 60);
			g.DrawString(colorInfo, valueFont, Brushes.Blue, infoBoxX + 50, infoBoxY + 60);
			g.DrawString(rgbInfo, valueFont, Brushes.Black, infoBoxX + 10, infoBoxY + 85);

			string instructions = "Click to copy to clipboard";
			SizeF textSize = g.MeasureString(instructions, instructionFont);
			Rectangle instructBg = new Rectangle(
				(int)(g.VisibleClipBounds.Width / 2 - textSize.Width / 2 - 10),
				(int)g.VisibleClipBounds.Height - 40,
				(int)textSize.Width + 20,
				(int)textSize.Height + 10);

			g.FillRectangle(new SolidBrush(Color.FromArgb(200, 0, 0, 0)), instructBg);
			g.DrawRectangle(Pens.White, instructBg);
			g.DrawString(instructions, instructionFont, Brushes.White, instructBg.X + 10, instructBg.Y + 5);

			DrawCrosshair(g, lastMousePosition);
		}

		private void DrawCrosshair(Graphics g, Point position)
		{
			if (position.IsEmpty)
			{
				return;
			}

			int x = position.X;
			int y = position.Y;

			g.DrawLine(blackPen, x - 10, y, x + 10, y);
			g.DrawLine(blackPen, x, y - 10, x, y + 10);
			g.DrawLine(whiteDashedPen, x - 10, y, x + 10, y);
			g.DrawLine(whiteDashedPen, x, y - 10, x, y + 10);
		}

		private static Rectangle InflateRect(Rectangle rect, int padding)
		{
			return new Rectangle(
				rect.X - padding,
				rect.Y - padding,
				rect.Width + padding * 2,
				rect.Height + padding * 2);
		}

		public void Dispose()
		{
			screenshot = null;
			dimBrush?.Dispose();
			selectionBorderPen?.Dispose();
			rectangleSelectionBorderPen?.Dispose();
			blackPen?.Dispose();
			whiteDashedPen?.Dispose();
			titleFont?.Dispose();
			valueFont?.Dispose();
			instructionFont?.Dispose();
		}
	}

	public struct ColorPickerPaintState
	{
		public Color SelectedColor;
		public Point PreviewPoint;
		public Bitmap PreviewBitmap;
		public int PreviewSize;
	}
}
