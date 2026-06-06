using System;
using System.Collections.Generic;
using System.Drawing;
using CloudShot.Export;

namespace CloudShot.Core
{
	public static class AnnotationEraser
	{
		public const int EraserRadius = 12;

		public static bool Apply(List<DrawingElement> elements, Point eraserCenter)
		{
			if (elements == null || elements.Count == 0)
			{
				return false;
			}

			bool changed = false;

			for (int i = elements.Count - 1; i >= 0; i--)
			{
				DrawingElement element = elements[i];

				if (element.Mode == DrawingToolMode.Pen)
				{
					List<List<Point>> segments = SplitPenStrokeAfterErase(element, eraserCenter, EraserRadius);
					if (segments == null)
					{
						continue;
					}

					elements.RemoveAt(i);
					foreach (List<Point> segment in segments)
					{
						if (segment.Count >= 2)
						{
							elements.Insert(i, new DrawingElement(segment, DrawingToolMode.Pen, element.DrawingColor));
							i++;
						}
					}

					changed = true;
					continue;
				}

				if (IntersectsEraser(element, eraserCenter, EraserRadius))
				{
					elements.RemoveAt(i);
					changed = true;
				}
			}

			return changed;
		}

		private static List<List<Point>> SplitPenStrokeAfterErase(
			DrawingElement element,
			Point eraserCenter,
			int radius)
		{
			if (element.Points == null || element.Points.Count == 0)
			{
				return new List<List<Point>>();
			}

			if (element.Points.Count == 1)
			{
				return Distance(element.Points[0], eraserCenter) <= radius
					? new List<List<Point>>()
					: null;
			}

			int penPadding = ImageExporter.DrawingPenSize / 2 + 1;
			var segments = new List<List<Point>>();
			var current = new List<Point>();

			for (int i = 0; i < element.Points.Count; i++)
			{
				Point point = element.Points[i];
				bool pointHit = Distance(point, eraserCenter) <= radius;

				if (pointHit)
				{
					if (current.Count >= 2)
					{
						segments.Add(new List<Point>(current));
					}

					current.Clear();
					continue;
				}

				if (current.Count == 0)
				{
					current.Add(point);
					continue;
				}

				Point previous = current[current.Count - 1];
				bool segmentHit = SegmentIntersectsEraser(previous, point, eraserCenter, radius + penPadding);

				if (segmentHit)
				{
					if (current.Count >= 2)
					{
						segments.Add(new List<Point>(current));
					}

					current.Clear();
					current.Add(point);
				}
				else
				{
					current.Add(point);
				}
			}

			if (current.Count >= 2)
			{
				segments.Add(current);
			}

			if (segments.Count == 1 &&
			    segments[0].Count == element.Points.Count &&
			    PointsMatch(segments[0], element.Points))
			{
				return null;
			}

			return segments;
		}

		private static bool IntersectsEraser(DrawingElement element, Point eraserCenter, int radius)
		{
			if (element.Points == null || element.Points.Count == 0)
			{
				return false;
			}

			switch (element.Mode)
			{
				case DrawingToolMode.Steps:
					return Distance(element.Points[0], eraserCenter) <= ImageExporter.StepCircleRadius + radius;

				case DrawingToolMode.Text:
					Rectangle textBounds = ImageExporter.GetTextInvalidationRect(element.Points[0], element.Text);
					return RectangleIntersectsCircle(textBounds, eraserCenter, radius);

				case DrawingToolMode.Arrow:
					return SegmentIntersectsEraser(
						element.Points[0],
						element.Points[1],
						eraserCenter,
						radius + ImageExporter.DrawingPenSize);

				case DrawingToolMode.Highlighter:
					return SegmentIntersectsEraser(
						element.Points[0],
						element.Points[1],
						eraserCenter,
						radius + ImageExporter.HighlighterPenSize / 2);

				case DrawingToolMode.Line:
					return SegmentIntersectsEraser(
						element.Points[0],
						element.Points[1],
						eraserCenter,
						radius + ImageExporter.DrawingPenSize);

				case DrawingToolMode.Rectangle:
				case DrawingToolMode.FilledRectangle:
				case DrawingToolMode.Pixelate:
					return RectangleIntersectsCircle(GetElementRectangle(element), eraserCenter, radius);

				default:
					return false;
			}
		}

		private static Rectangle GetElementRectangle(DrawingElement element)
		{
			Point p1 = element.Points[0];
			Point p2 = element.Points[1];
			return new Rectangle(
				Math.Min(p1.X, p2.X),
				Math.Min(p1.Y, p2.Y),
				Math.Abs(p1.X - p2.X),
				Math.Abs(p1.Y - p2.Y));
		}

		private static bool RectangleIntersectsCircle(Rectangle rect, Point center, int radius)
		{
			int closestX = Math.Max(rect.Left, Math.Min(center.X, rect.Right));
			int closestY = Math.Max(rect.Top, Math.Min(center.Y, rect.Bottom));
			return Distance(new Point(closestX, closestY), center) <= radius;
		}

		private static bool SegmentIntersectsEraser(Point a, Point b, Point eraserCenter, int radius)
		{
			return DistanceToSegment(eraserCenter, a, b) <= radius;
		}

		private static double Distance(Point a, Point b)
		{
			double dx = a.X - b.X;
			double dy = a.Y - b.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}

		private static double DistanceToSegment(Point point, Point a, Point b)
		{
			double dx = b.X - a.X;
			double dy = b.Y - a.Y;
			double lengthSquared = dx * dx + dy * dy;

			if (lengthSquared < 1)
			{
				return Distance(point, a);
			}

			double t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / lengthSquared;
			t = Math.Max(0, Math.Min(1, t));
			double closestX = a.X + t * dx;
			double closestY = a.Y + t * dy;
			double distX = point.X - closestX;
			double distY = point.Y - closestY;
			return Math.Sqrt(distX * distX + distY * distY);
		}

		private static bool PointsMatch(List<Point> left, List<Point> right)
		{
			if (left.Count != right.Count)
			{
				return false;
			}

			for (int i = 0; i < left.Count; i++)
			{
				if (left[i] != right[i])
				{
					return false;
				}
			}

			return true;
		}
	}
}
