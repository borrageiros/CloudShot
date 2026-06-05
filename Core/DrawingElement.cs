using System.Collections.Generic;
using System.Drawing;

namespace CloudShot.Core
{
	public class DrawingElement
	{
		public List<Point> Points { get; set; }
		public bool IsPenMode { get; set; }
		public Color DrawingColor { get; set; }

		public DrawingElement(List<Point> points, bool isPenMode, Color color)
		{
			Points = points;
			IsPenMode = isPenMode;
			DrawingColor = color;
		}
	}
}
