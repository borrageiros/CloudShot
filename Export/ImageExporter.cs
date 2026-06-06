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
		public const int HighlighterPenSize = 20;
		public const int HighlighterAlpha = 96;
		public const int ArrowHeadSize = 18;
		public const float ArrowHeadWidth = 16f;
		public const int StepCircleRadius = 14;
		public const float TextFontSize = 16f;
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
			if (element.Points == null || element.Points.Count == 0)
			{
				return;
			}

			if (element.Mode == DrawingToolMode.Steps)
			{
				DrawStepMarker(g, element.Points[0], element.StepNumber, element.DrawingColor);
				return;
			}

			if (element.Mode == DrawingToolMode.Text)
			{
				DrawTextAnnotation(g, element.Points[0], element.Text, element.DrawingColor);
				return;
			}

			if (element.IsPenMode)
			{
				if (element.Points.Count <= 1)
				{
					return;
				}

				using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
				{
					for (int i = 0; i < element.Points.Count - 1; i++)
					{
						g.DrawLine(elementPen, element.Points[i], element.Points[i + 1]);
					}
				}

				return;
			}

			if (element.IsTwoPointDragMode && element.Points.Count >= 2)
			{
				Point p1 = element.Points[0];
				Point p2 = element.Points[1];
				switch (element.Mode)
				{
					case DrawingToolMode.Arrow:
						DrawArrow(g, p1, p2, element.DrawingColor);
						break;
					case DrawingToolMode.Highlighter:
						DrawHighlighterLine(g, p1, p2, element.DrawingColor);
						break;
					case DrawingToolMode.Line:
						DrawStraightLine(g, p1, p2, element.DrawingColor);
						break;
				}

				if (!element.IsRectangleToolMode)
				{
					return;
				}
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
			if (element == null || element.Points == null || element.Points.Count == 0 || element.IsPenMode)
			{
				return;
			}

			if (element.Mode == DrawingToolMode.Steps)
			{
				return;
			}

			if (!element.IsRectangleToolMode && element.Points.Count >= 2)
			{
				Point clientStart = LayerToClient(element.Points[0], clientSelectionRect);
				Point clientEnd = LayerToClient(element.Points[1], clientSelectionRect);
				switch (element.Mode)
				{
					case DrawingToolMode.Arrow:
						DrawArrow(g, clientStart, clientEnd, element.DrawingColor);
						break;
					case DrawingToolMode.Highlighter:
						DrawHighlighterLine(g, clientStart, clientEnd, element.DrawingColor);
						break;
					case DrawingToolMode.Line:
						DrawStraightLine(g, clientStart, clientEnd, element.DrawingColor);
						break;
				}

				return;
			}

			if (element.Points.Count <= 1)
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
			if (element.Points == null || element.Points.Count == 0)
			{
				return;
			}

			if (element.Mode == DrawingToolMode.Steps)
			{
				Point exportPoint = LayerToExportPoint(element.Points[0], selectionRectangle, validRect);
				DrawStepMarker(g, exportPoint, element.StepNumber, element.DrawingColor);
				return;
			}

			if (element.Mode == DrawingToolMode.Text)
			{
				Point exportPoint = LayerToExportPoint(element.Points[0], selectionRectangle, validRect);
				DrawTextAnnotation(g, exportPoint, element.Text, element.DrawingColor);
				return;
			}

			if (element.IsPenMode)
			{
				if (element.Points.Count <= 1)
				{
					return;
				}

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

			if (element.IsTwoPointDragMode && element.Points.Count >= 2)
			{
				Point p1 = LayerToExportPoint(element.Points[0], selectionRectangle, validRect);
				Point p2 = LayerToExportPoint(element.Points[1], selectionRectangle, validRect);
				switch (element.Mode)
				{
					case DrawingToolMode.Arrow:
						DrawArrow(g, p1, p2, element.DrawingColor);
						break;
					case DrawingToolMode.Highlighter:
						DrawHighlighterLine(g, p1, p2, element.DrawingColor);
						break;
					case DrawingToolMode.Line:
						DrawStraightLine(g, p1, p2, element.DrawingColor);
						break;
				}

				if (!element.IsRectangleToolMode)
				{
					return;
				}
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

		public static Rectangle GetTwoPointDragInvalidationRect(Point start, Point end, DrawingToolMode mode)
		{
			switch (mode)
			{
				case DrawingToolMode.Arrow:
					return GetArrowInvalidationRect(start, end);
				case DrawingToolMode.Highlighter:
					return GetSegmentInvalidationRect(start, end, HighlighterPenSize / 2 + 2);
				case DrawingToolMode.Line:
					return GetSegmentInvalidationRect(start, end, DrawingPenSize * 2);
				default:
					return GetSegmentInvalidationRect(start, end, DrawingPenSize);
			}
		}

		private static Rectangle GetSegmentInvalidationRect(Point start, Point end, int padding)
		{
			int x = Math.Min(start.X, end.X) - padding;
			int y = Math.Min(start.Y, end.Y) - padding;
			int width = Math.Abs(start.X - end.X) + padding * 2;
			int height = Math.Abs(start.Y - end.Y) + padding * 2;
			return new Rectangle(x, y, Math.Max(width, padding * 2), Math.Max(height, padding * 2));
		}

		private static Rectangle GetArrowInvalidationRect(Point start, Point end)
		{
			if (!TryGetArrowGeometry(start, end, out PointF tip, out PointF baseCenter, out PointF left, out PointF right))
			{
				return GetSegmentInvalidationRect(start, end, DrawingPenSize * 2);
			}

			int padding = DrawingPenSize + 2;
			float minX = Math.Min(start.X, Math.Min(tip.X, Math.Min(baseCenter.X, Math.Min(left.X, right.X)))) - padding;
			float minY = Math.Min(start.Y, Math.Min(tip.Y, Math.Min(baseCenter.Y, Math.Min(left.Y, right.Y)))) - padding;
			float maxX = Math.Max(start.X, Math.Max(tip.X, Math.Max(baseCenter.X, Math.Max(left.X, right.X)))) + padding;
			float maxY = Math.Max(start.Y, Math.Max(tip.Y, Math.Max(baseCenter.Y, Math.Max(left.Y, right.Y)))) + padding;
			return Rectangle.FromLTRB((int)Math.Floor(minX), (int)Math.Floor(minY), (int)Math.Ceiling(maxX), (int)Math.Ceiling(maxY));
		}

		private static bool TryGetArrowGeometry(
			Point p1,
			Point p2,
			out PointF tip,
			out PointF baseCenter,
			out PointF left,
			out PointF right)
		{
			double dx = p2.X - p1.X;
			double dy = p2.Y - p1.Y;
			double length = Math.Sqrt(dx * dx + dy * dy);
			if (length < 1)
			{
				tip = PointF.Empty;
				baseCenter = PointF.Empty;
				left = PointF.Empty;
				right = PointF.Empty;
				return false;
			}

			dx /= length;
			dy /= length;
			tip = new PointF(p2.X, p2.Y);
			baseCenter = new PointF(
				p2.X - (float)(dx * ArrowHeadSize),
				p2.Y - (float)(dy * ArrowHeadSize));
			double perpX = -dy;
			double perpY = dx;
			left = new PointF(
				baseCenter.X + (float)(perpX * ArrowHeadWidth * 0.5),
				baseCenter.Y + (float)(perpY * ArrowHeadWidth * 0.5));
			right = new PointF(
				baseCenter.X - (float)(perpX * ArrowHeadWidth * 0.5),
				baseCenter.Y - (float)(perpY * ArrowHeadWidth * 0.5));
			return true;
		}

		private static void DrawStraightLine(Graphics g, Point p1, Point p2, Color color)
		{
			using (Pen pen = new Pen(color, DrawingPenSize))
			{
				pen.StartCap = LineCap.Round;
				pen.EndCap = LineCap.Round;
				g.DrawLine(pen, p1, p2);
			}
		}

		private static void DrawHighlighterLine(Graphics g, Point p1, Point p2, Color color)
		{
			Color highlightColor = Color.FromArgb(HighlighterAlpha, color.R, color.G, color.B);
			using (Pen pen = new Pen(highlightColor, HighlighterPenSize))
			{
				pen.StartCap = LineCap.Flat;
				pen.EndCap = LineCap.Flat;
				g.DrawLine(pen, p1, p2);
			}
		}

		private static void DrawArrow(Graphics g, Point p1, Point p2, Color color)
		{
			if (!TryGetArrowGeometry(p1, p2, out PointF tip, out PointF baseCenter, out PointF left, out PointF right))
			{
				return;
			}

			using (Pen pen = new Pen(color, DrawingPenSize))
			{
				pen.StartCap = LineCap.Round;
				pen.EndCap = LineCap.Flat;
				g.DrawLine(pen, p1, baseCenter);
			}

			using (SolidBrush brush = new SolidBrush(color))
			{
				g.FillPolygon(brush, new[] { tip, left, right });
			}
		}

		public static Rectangle GetTextInvalidationRect(Point topLeft, string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return new Rectangle(topLeft.X - 4, topLeft.Y - 4, 8, 8);
			}

			Size size = TextRenderer.MeasureText(text, GetTextFont());
			return new Rectangle(topLeft.X - 4, topLeft.Y - 4, size.Width + 8, size.Height + 8);
		}

		public static Font GetTextFont()
		{
			return new Font("Segoe UI", TextFontSize, FontStyle.Bold);
		}

		private static void DrawTextAnnotation(Graphics g, Point topLeft, string text, Color color)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			using (Font font = GetTextFont())
			using (SolidBrush brush = new SolidBrush(color))
			{
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
				g.DrawString(text, font, brush, topLeft);
			}
		}

		private static void DrawStepMarker(Graphics g, Point center, int stepNumber, Color color)
		{
			Rectangle circle = new Rectangle(
				center.X - StepCircleRadius,
				center.Y - StepCircleRadius,
				StepCircleRadius * 2,
				StepCircleRadius * 2);

			using (SolidBrush brush = new SolidBrush(color))
			{
				g.FillEllipse(brush, circle);
			}

			using (Pen borderPen = new Pen(GetStepLabelColor(color), 1f))
			{
				g.DrawEllipse(borderPen, circle);
			}

			string text = stepNumber.ToString();
			using (Font font = new Font("Segoe UI", 10f, FontStyle.Bold))
			using (SolidBrush textBrush = new SolidBrush(GetStepLabelColor(color)))
			{
				DrawCenteredText(g, text, font, textBrush, circle);
			}
		}

		private static void DrawCenteredText(Graphics g, string text, Font font, Brush brush, Rectangle bounds)
		{
			using (StringFormat format = new StringFormat())
			{
				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;
				format.FormatFlags = StringFormatFlags.NoWrap;
				g.DrawString(text, font, brush, bounds, format);
			}
		}

		private static Color GetStepLabelColor(Color background)
		{
			double luminance = 0.299 * background.R + 0.587 * background.G + 0.114 * background.B;
			return luminance > 140 ? Color.Black : Color.White;
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

