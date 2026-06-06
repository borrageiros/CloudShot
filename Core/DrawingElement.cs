using System.Collections.Generic;
using System.Drawing;

namespace CloudShot.Core
{
	public enum DrawingToolMode
	{
		Pen,
		Rectangle,
		FilledRectangle,
		Pixelate,
		Arrow,
		Highlighter,
		Line,
		Steps,
		Text
	}

	public class DrawingElement
	{
		public List<Point> Points { get; set; }
		public DrawingToolMode Mode { get; set; }
		public Color DrawingColor { get; set; }
		public int StepNumber { get; set; }
		public string Text { get; set; }

		public DrawingElement(List<Point> points, DrawingToolMode mode, Color color)
		{
			Points = points;
			Mode = mode;
			DrawingColor = color;
		}

		public bool IsPenMode => Mode == DrawingToolMode.Pen;

		public bool IsTwoPointDragMode =>
			Mode == DrawingToolMode.Rectangle ||
			Mode == DrawingToolMode.FilledRectangle ||
			Mode == DrawingToolMode.Pixelate ||
			Mode == DrawingToolMode.Arrow ||
			Mode == DrawingToolMode.Highlighter ||
			Mode == DrawingToolMode.Line;

		public bool IsRectangleToolMode =>
			Mode == DrawingToolMode.Rectangle ||
			Mode == DrawingToolMode.FilledRectangle ||
			Mode == DrawingToolMode.Pixelate;
	}
}
