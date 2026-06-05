using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CloudShot.Core
{
	public static class BitmapPixelReader
	{
		public static Color GetPixel(Bitmap bitmap, int x, int y)
		{
			if (bitmap == null || x < 0 || y < 0 || x >= bitmap.Width || y >= bitmap.Height)
			{
				return Color.Empty;
			}

			Rectangle rect = new Rectangle(x, y, 1, 1);
			BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			try
			{
				int argb = Marshal.ReadInt32(data.Scan0);
				return Color.FromArgb(argb);
			}
			finally
			{
				bitmap.UnlockBits(data);
			}
		}
	}
}
