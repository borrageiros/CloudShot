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
					DrawAnnotations(g, drawingElements, validRect, offsetX, offsetY, width, height);
				}
			}

			return output;
		}

		public static void DrawAnnotations(
			Graphics g,
			IReadOnlyList<DrawingElement> drawingElements,
			Rectangle validRect,
			int offsetX,
			int offsetY,
			int width,
			int height)
		{
			foreach (DrawingElement element in drawingElements)
			{
				if (element.Points == null || element.Points.Count <= 1)
				{
					continue;
				}

				using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
				{
					if (element.IsPenMode)
					{
						for (int i = 0; i < element.Points.Count - 1; i++)
						{
							Point p1 = ToLocalPoint(element.Points[i], validRect, offsetX, offsetY);
							Point p2 = ToLocalPoint(element.Points[i + 1], validRect, offsetX, offsetY);
							g.DrawLine(elementPen, p1, p2);
						}
					}
					else
					{
						Point p1 = ToLocalPoint(element.Points[0], validRect, offsetX, offsetY);
						Point p2 = ToLocalPoint(element.Points[1], validRect, offsetX, offsetY);

						int rectX = Math.Min(p1.X, p2.X);
						int rectY = Math.Min(p1.Y, p2.Y);
						int rectWidth = Math.Abs(p1.X - p2.X);
						int rectHeight = Math.Abs(p1.Y - p2.Y);

						g.DrawRectangle(elementPen, rectX, rectY, rectWidth, rectHeight);
					}
				}
			}
		}

		public static void DrawAnnotationsOnLayer(
			Graphics g,
			IReadOnlyList<DrawingElement> drawingElements,
			Rectangle clientSelectionRect)
		{
			foreach (DrawingElement element in drawingElements)
			{
				if (element.Points == null || element.Points.Count <= 1)
				{
					continue;
				}

				using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
				{
					if (element.IsPenMode)
					{
						for (int i = 0; i < element.Points.Count - 1; i++)
						{
							Point p1 = ToLayerPoint(element.Points[i], clientSelectionRect);
							Point p2 = ToLayerPoint(element.Points[i + 1], clientSelectionRect);
							g.DrawLine(elementPen, p1, p2);
						}
					}
					else
					{
						Point p1 = ToLayerPoint(element.Points[0], clientSelectionRect);
						Point p2 = ToLayerPoint(element.Points[1], clientSelectionRect);

						int rectX = Math.Min(p1.X, p2.X);
						int rectY = Math.Min(p1.Y, p2.Y);
						int rectWidth = Math.Abs(p1.X - p2.X);
						int rectHeight = Math.Abs(p1.Y - p2.Y);

						g.DrawRectangle(elementPen, rectX, rectY, rectWidth, rectHeight);
					}
				}
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

		private static Point ToLocalPoint(Point clientPoint, Rectangle validRect, int offsetX, int offsetY)
		{
			return new Point(
				clientPoint.X - validRect.X - offsetX,
				clientPoint.Y - validRect.Y - offsetY);
		}

		private static Point ToLayerPoint(Point clientPoint, Rectangle clientSelectionRect)
		{
			return new Point(
				clientPoint.X - clientSelectionRect.X,
				clientPoint.Y - clientSelectionRect.Y);
		}
	}
}
