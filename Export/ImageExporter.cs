using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using CloudShot.Core;

namespace CloudShot.Export
{
	public static class ImageExporter
	{
		public const int DrawingPenSize = 3;
		public const int PixelateBlockSize = 10;

		public static Point ClientToLayer(Point clientPoint, Rectangle clientSelectionRect)
		{
			return new Point(
				clientPoint.X - clientSelectionRect.X,
				clientPoint.Y - clientSelectionRect.Y);
		}

		public static Point LayerToClient(Point layerPoint, Rectangle clientSelectionRect)
		{
			return new Point(
				layerPoint.X + clientSelectionRect.X,
				layerPoint.Y + clientSelectionRect.Y);
		}

		public static Bitmap RenderSelection(
			Bitmap screenshot,
			Rectangle selectionRectangle,
			IReadOnlyList<DrawingElement> drawingElements,
			int offsetX,
			int offsetY,
			bool includeAnnotations = true)
		{
			int x = Math.Max(0, selectionRectangle.X);
			int y = Math.Max(0, selectionRectangle.Y);
			int width = Math.Min(screenshot.Width - x, selectionRectangle.Width);
			int height = Math.Min(screenshot.Height - y, selectionRectangle.Height);

			if (width <= 0 || height <= 0)
			{
				return null;
			}

			Rectangle validRect = new Rectangle(x, y, width, height);
			Bitmap output = new Bitmap(width, height, PixelFormat.Format32bppArgb);

			using (Graphics g = Graphics.FromImage(output))
			{
				g.CompositingQuality = CompositingQuality.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.SmoothingMode = SmoothingMode.AntiAlias;

				g.DrawImage(
					screenshot,
					new Rectangle(0, 0, width, height),
					validRect,
					GraphicsUnit.Pixel);

				if (includeAnnotations && drawingElements != null)
				{
					DrawAnnotations(g, drawingElements, screenshot, selectionRectangle, validRect);
				}
			}

			return output;
		}

		public static void DrawAnnotations(
			Graphics g,
			IReadOnlyList<DrawingElement> drawingElements,
			Bitmap screenshot,
			Rectangle selectionRectangle,
			Rectangle validRect)
		{
			foreach (DrawingElement element in drawingElements)
			{
				DrawElement(g, element, screenshot, selectionRectangle, validRect);
			}
		}

		public static void DrawAnnotationsOnLayer(
			Graphics g,
			IReadOnlyList<DrawingElement> drawingElements,
			Bitmap screenshot,
			Rectangle selectionRectangle)
		{
			foreach (DrawingElement element in drawingElements)
			{
				DrawElementOnLayer(g, element, screenshot, selectionRectangle);
			}
		}

		public static void DrawElementOnLayer(
			Graphics g,
			DrawingElement element,
			Bitmap screenshot,
			Rectangle selectionRectangle)
		{
			if (element.Points == null || element.Points.Count <= 1)
			{
				return;
			}

			if (element.IsPenMode)
			{
				using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
				{
					for (int i = 0; i < element.Points.Count - 1; i++)
					{
						g.DrawLine(elementPen, element.Points[i], element.Points[i + 1]);
					}
				}

				return;
			}

			Rectangle layerRect = GetLayerRectangle(element);
			if (layerRect.Width <= 0 || layerRect.Height <= 0)
			{
				return;
			}

			switch (element.Mode)
			{
				case DrawingToolMode.Rectangle:
					using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
					{
						g.DrawRectangle(elementPen, layerRect);
					}
					break;
				case DrawingToolMode.FilledRectangle:
					using (SolidBrush brush = new SolidBrush(element.DrawingColor))
					{
						g.FillRectangle(brush, layerRect);
					}
					break;
				case DrawingToolMode.Pixelate:
					DrawPixelatedRegion(g, screenshot, selectionRectangle, layerRect, layerRect.Location);
					break;
			}
		}

		public static void DrawElementPreview(
			Graphics g,
			DrawingElement element,
			Rectangle clientSelectionRect,
			Bitmap screenshot,
			Rectangle selectionRectangle)
		{
			if (element == null || element.Points == null || element.Points.Count <= 1 || element.IsPenMode)
			{
				return;
			}

			Rectangle layerRect = GetLayerRectangle(element);
			if (layerRect.Width <= 0 || layerRect.Height <= 0)
			{
				return;
			}

			Point clientOrigin = LayerToClient(layerRect.Location, clientSelectionRect);
			Rectangle clientRect = new Rectangle(clientOrigin, layerRect.Size);

			switch (element.Mode)
			{
				case DrawingToolMode.Rectangle:
					using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
					{
						g.DrawRectangle(elementPen, clientRect);
					}
					break;
				case DrawingToolMode.FilledRectangle:
					using (SolidBrush brush = new SolidBrush(element.DrawingColor))
					{
						g.FillRectangle(brush, clientRect);
					}
					break;
				case DrawingToolMode.Pixelate:
					DrawPixelatedRegion(g, screenshot, selectionRectangle, layerRect, clientRect.Location);
					break;
			}
		}

		public static bool SaveToFile(Bitmap image, AppSettings settings)
		{
			using (SaveFileDialog saveDialog = new SaveFileDialog())
			{
				saveDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|All files (*.*)|*.*";
				saveDialog.DefaultExt = ".png";
				saveDialog.FileName = $"CloudShot_{DateTime.Now:yyyyMMdd_HHmmss}.png";

				if (saveDialog.ShowDialog() != DialogResult.OK)
				{
					return false;
				}

				string extension = Path.GetExtension(saveDialog.FileName).ToLowerInvariant();
				ImageFormat format = extension == ".jpg" || extension == ".jpeg"
					? ImageFormat.Jpeg
					: ImageFormat.Png;

				image.Save(saveDialog.FileName, format);
				return true;
			}
		}

		private static void DrawElement(
			Graphics g,
			DrawingElement element,
			Bitmap screenshot,
			Rectangle selectionRectangle,
			Rectangle validRect)
		{
			if (element.Points == null || element.Points.Count <= 1)
			{
				return;
			}

			if (element.IsPenMode)
			{
				using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
				{
					for (int i = 0; i < element.Points.Count - 1; i++)
					{
						Point p1 = LayerToExportPoint(element.Points[i], selectionRectangle, validRect);
						Point p2 = LayerToExportPoint(element.Points[i + 1], selectionRectangle, validRect);
						g.DrawLine(elementPen, p1, p2);
					}
				}

				return;
			}

			Rectangle layerRect = GetLayerRectangle(element);
			if (layerRect.Width <= 0 || layerRect.Height <= 0)
			{
				return;
			}

			Rectangle exportRect = LayerRectToExportRect(layerRect, selectionRectangle, validRect);

			switch (element.Mode)
			{
				case DrawingToolMode.Rectangle:
					using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
					{
						g.DrawRectangle(elementPen, exportRect);
					}
					break;
				case DrawingToolMode.FilledRectangle:
					using (SolidBrush brush = new SolidBrush(element.DrawingColor))
					{
						g.FillRectangle(brush, exportRect);
					}
					break;
				case DrawingToolMode.Pixelate:
					DrawPixelatedRegion(g, screenshot, selectionRectangle, layerRect, exportRect.Location, validRect);
					break;
			}
		}

		private static Rectangle GetLayerRectangle(DrawingElement element)
		{
			Point p1 = element.Points[0];
			Point p2 = element.Points[1];
			return new Rectangle(
				Math.Min(p1.X, p2.X),
				Math.Min(p1.Y, p2.Y),
				Math.Abs(p1.X - p2.X),
				Math.Abs(p1.Y - p2.Y));
		}

		private static Rectangle LayerRectToImageRect(Rectangle layerRect, Rectangle selectionRectangle)
		{
			return new Rectangle(
				layerRect.X + selectionRectangle.X,
				layerRect.Y + selectionRectangle.Y,
				layerRect.Width,
				layerRect.Height);
		}

		private static Rectangle LayerRectToExportRect(Rectangle layerRect, Rectangle selectionRectangle, Rectangle validRect)
		{
			return new Rectangle(
				layerRect.X + selectionRectangle.X - validRect.X,
				layerRect.Y + selectionRectangle.Y - validRect.Y,
				layerRect.Width,
				layerRect.Height);
		}

		private static Point LayerToExportPoint(Point layerPoint, Rectangle selectionRectangle, Rectangle validRect)
		{
			return new Point(
				layerPoint.X + selectionRectangle.X - validRect.X,
				layerPoint.Y + selectionRectangle.Y - validRect.Y);
		}

		private static void DrawPixelatedRegion(
			Graphics g,
			Bitmap screenshot,
			Rectangle selectionRectangle,
			Rectangle layerRect,
			Point drawLocation)
		{
			DrawPixelatedRegion(g, screenshot, selectionRectangle, layerRect, drawLocation, Rectangle.Empty);
		}

		private static void DrawPixelatedRegion(
			Graphics g,
			Bitmap screenshot,
			Rectangle selectionRectangle,
			Rectangle layerRect,
			Point drawLocation,
			Rectangle validRect)
		{
			if (screenshot == null || layerRect.Width <= 0 || layerRect.Height <= 0)
			{
				return;
			}

			Rectangle imageRect = LayerRectToImageRect(layerRect, selectionRectangle);
			if (!validRect.IsEmpty)
			{
				imageRect = Rectangle.Intersect(imageRect, validRect);
				if (imageRect.Width <= 0 || imageRect.Height <= 0)
				{
					return;
				}

				int offsetX = imageRect.X - (layerRect.X + selectionRectangle.X);
				int offsetY = imageRect.Y - (layerRect.Y + selectionRectangle.Y);
				drawLocation = new Point(drawLocation.X + offsetX, drawLocation.Y + offsetY);
				layerRect = new Rectangle(layerRect.X + offsetX, layerRect.Y + offsetY, imageRect.Width, imageRect.Height);
			}

			using (Bitmap pixelated = CreatePixelatedRegion(screenshot, imageRect, PixelateBlockSize))
			{
				if (pixelated == null)
				{
					return;
				}

				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.PixelOffsetMode = PixelOffsetMode.Half;
				g.DrawImage(pixelated, new Rectangle(drawLocation, layerRect.Size));
			}
		}

		private static Bitmap CreatePixelatedRegion(Bitmap source, Rectangle sourceRect, int blockSize)
		{
			sourceRect = Rectangle.Intersect(sourceRect, new Rectangle(0, 0, source.Width, source.Height));
			if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
			{
				return null;
			}

			int width = sourceRect.Width;
			int height = sourceRect.Height;
			int smallWidth = Math.Max(1, (width + blockSize - 1) / blockSize);
			int smallHeight = Math.Max(1, (height + blockSize - 1) / blockSize);

			Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			using (Bitmap small = new Bitmap(smallWidth, smallHeight, PixelFormat.Format32bppArgb))
			{
				using (Graphics smallGraphics = Graphics.FromImage(small))
				{
					smallGraphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
					smallGraphics.DrawImage(source, new Rectangle(0, 0, smallWidth, smallHeight), sourceRect, GraphicsUnit.Pixel);
				}

				using (Graphics resultGraphics = Graphics.FromImage(result))
				{
					resultGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
					resultGraphics.PixelOffsetMode = PixelOffsetMode.Half;
					resultGraphics.DrawImage(small, new Rectangle(0, 0, width, height));
				}
			}

			return result;
		}
	}
}
