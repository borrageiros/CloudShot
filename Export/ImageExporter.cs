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

		public static Bitmap RenderSelection(
			Bitmap screenshot,
			Rectangle selectionRectangle,
			IReadOnlyList<DrawingElement> drawingElements,
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
					foreach (DrawingElement element in drawingElements)
					{
						DrawElementCore(g, element, screenshot, validRect.Location);
					}
				}
			}

			return output;
		}

		public static void DrawElementsInImageSpace(
			Graphics g,
			IReadOnlyList<DrawingElement> drawingElements,
			Bitmap screenshot)
		{
			if (drawingElements == null)
			{
				return;
			}

			foreach (DrawingElement element in drawingElements)
			{
				DrawElementCore(g, element, screenshot, Point.Empty);
			}
		}

		public static void DrawElementInImageSpace(
			Graphics g,
			DrawingElement element,
			Bitmap screenshot)
		{
			DrawElementCore(g, element, screenshot, Point.Empty);
		}

		public static void DrawElementPreview(
			Graphics g,
			DrawingElement element,
			Bitmap screenshot,
			int offsetX,
			int offsetY)
		{
			if (element == null || element.Points == null || element.Points.Count == 0 || element.IsPenMode)
			{
				return;
			}

			if (element.Mode == DrawingToolMode.Steps || element.Mode == DrawingToolMode.Text)
			{
				return;
			}

			DrawElementCore(g, element, screenshot, new Point(-offsetX, -offsetY));
		}

		private static void DrawElementCore(
			Graphics g,
			DrawingElement element,
			Bitmap screenshot,
			Point origin)
		{
			if (element.Points == null || element.Points.Count == 0)
			{
				return;
			}

			if (element.Mode == DrawingToolMode.Steps)
			{
				DrawStepMarker(g, Map(element.Points[0], origin), element.StepNumber, element.DrawingColor);
				return;
			}

			if (element.Mode == DrawingToolMode.Text)
			{
				DrawTextAnnotation(g, Map(element.Points[0], origin), element.Text, element.DrawingColor);
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
						g.DrawLine(elementPen, Map(element.Points[i], origin), Map(element.Points[i + 1], origin));
					}
				}

				return;
			}

			if (element.IsTwoPointDragMode && element.Points.Count >= 2)
			{
				Point p1 = Map(element.Points[0], origin);
				Point p2 = Map(element.Points[1], origin);
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

			Rectangle imageRect = GetImageRectangle(element);
			if (imageRect.Width <= 0 || imageRect.Height <= 0)
			{
				return;
			}

			Rectangle targetRect = new Rectangle(
				imageRect.X - origin.X,
				imageRect.Y - origin.Y,
				imageRect.Width,
				imageRect.Height);

			switch (element.Mode)
			{
				case DrawingToolMode.Rectangle:
					using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
					{
						g.DrawRectangle(elementPen, targetRect);
					}
					break;
				case DrawingToolMode.FilledRectangle:
					using (SolidBrush brush = new SolidBrush(element.DrawingColor))
					{
						g.FillRectangle(brush, targetRect);
					}
					break;
				case DrawingToolMode.Pixelate:
					DrawPixelatedRegion(g, screenshot, imageRect, origin);
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

		private static Point Map(Point point, Point origin)
		{
			return new Point(point.X - origin.X, point.Y - origin.Y);
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

		private static Rectangle GetImageRectangle(DrawingElement element)
		{
			Point p1 = element.Points[0];
			Point p2 = element.Points[1];
			return new Rectangle(
				Math.Min(p1.X, p2.X),
				Math.Min(p1.Y, p2.Y),
				Math.Abs(p1.X - p2.X),
				Math.Abs(p1.Y - p2.Y));
		}

		private static void DrawPixelatedRegion(
			Graphics g,
			Bitmap screenshot,
			Rectangle imageRect,
			Point origin)
		{
			if (screenshot == null || imageRect.Width <= 0 || imageRect.Height <= 0)
			{
				return;
			}

			Rectangle clipped = Rectangle.Intersect(imageRect, new Rectangle(0, 0, screenshot.Width, screenshot.Height));
			if (clipped.Width <= 0 || clipped.Height <= 0)
			{
				return;
			}

			using (Bitmap pixelated = CreatePixelatedRegion(screenshot, clipped, PixelateBlockSize))
			{
				if (pixelated == null)
				{
					return;
				}

				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.PixelOffsetMode = PixelOffsetMode.Half;
				Point drawLocation = new Point(clipped.X - origin.X, clipped.Y - origin.Y);
				g.DrawImage(pixelated, new Rectangle(drawLocation, clipped.Size));
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
