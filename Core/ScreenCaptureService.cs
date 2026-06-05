using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace CloudShot.Core
{
	public static class ScreenCaptureService
	{
		public static Rectangle GetTotalScreenBounds()
		{
			int left = int.MaxValue;
			int top = int.MaxValue;
			int right = int.MinValue;
			int bottom = int.MinValue;

			foreach (Screen screen in Screen.AllScreens)
			{
				Rectangle bounds = screen.Bounds;
				left = Math.Min(left, bounds.Left);
				top = Math.Min(top, bounds.Top);
				right = Math.Max(right, bounds.Right);
				bottom = Math.Max(bottom, bounds.Bottom);
			}

			return new Rectangle(left, top, right - left, bottom - top);
		}

		public static Bitmap CaptureAllScreens()
		{
			Rectangle totalBounds = GetTotalScreenBounds();

			if (totalBounds.Width <= 0 || totalBounds.Height <= 0)
			{
				throw new InvalidOperationException("Unable to determine screen dimensions");
			}

			Bitmap screenShot = new Bitmap(totalBounds.Width, totalBounds.Height, PixelFormat.Format32bppArgb);

			using (Graphics g = Graphics.FromImage(screenShot))
			{
				g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
				g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

				g.FillRectangle(Brushes.Black, 0, 0, totalBounds.Width, totalBounds.Height);

				foreach (Screen screen in Screen.AllScreens)
				{
					Rectangle bounds = screen.Bounds;
					int relX = bounds.X - totalBounds.X;
					int relY = bounds.Y - totalBounds.Y;

					try
					{
						g.CopyFromScreen(
							bounds.X, bounds.Y,
							relX, relY,
							bounds.Size,
							CopyPixelOperation.SourceCopy);
					}
					catch
					{
					}
				}
			}

			return screenShot;
		}
	}
}
