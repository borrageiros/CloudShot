using System.Drawing;
using System.Windows.Forms;

namespace CloudShot.Core
{
	public class CoordinateMapper
	{
		private readonly Rectangle totalScreenBounds;
		private readonly Form form;

		public CoordinateMapper(Form form, Rectangle totalScreenBounds)
		{
			this.form = form;
			this.totalScreenBounds = totalScreenBounds;
		}

		public int OffsetX => form.Bounds.X - totalScreenBounds.X;
		public int OffsetY => form.Bounds.Y - totalScreenBounds.Y;

		public Rectangle ToClientRect(Rectangle imageRect)
		{
			return new Rectangle(
				imageRect.X + OffsetX,
				imageRect.Y + OffsetY,
				imageRect.Width,
				imageRect.Height);
		}

		public Point ToImagePoint(Point clientPoint)
		{
			return new Point(clientPoint.X - OffsetX, clientPoint.Y - OffsetY);
		}

		public Rectangle CalculateSelectionRectangle(Point startPoint, Point endPoint)
		{
			int x = System.Math.Min(startPoint.X, endPoint.X);
			int y = System.Math.Min(startPoint.Y, endPoint.Y);
			int width = System.Math.Abs(startPoint.X - endPoint.X);
			int height = System.Math.Abs(startPoint.Y - endPoint.Y);

			x += OffsetX;
			y += OffsetY;

			return new Rectangle(x, y, width, height);
		}

		public Rectangle ClampToImage(Rectangle rect, int imageWidth, int imageHeight)
		{
			int x = System.Math.Max(0, rect.X);
			int y = System.Math.Max(0, rect.Y);
			int width = System.Math.Min(imageWidth - x, rect.Width);
			int height = System.Math.Min(imageHeight - y, rect.Height);
			return new Rectangle(x, y, width, height);
		}
	}
}
