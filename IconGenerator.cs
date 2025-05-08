using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace CloudShot
{
	public static class IconGenerator
	{
		public static void CreateAppIcon(string outputPath = "app.ico")
		{
			// Sizes of the icons that we will include
			int[] sizes = new int[] { 16, 32, 48, 64, 128, 256 };

			// Use Bitmap to create the different versions of the icon
			using (FileStream fs = new FileStream(outputPath, FileMode.Create))
			{
				// ICO file structure
				// Header: 6 bytes
				BinaryWriter bw = new BinaryWriter(fs);
				bw.Write((short)0);     // Reserved, must be 0
				bw.Write((short)1);     // Resource type: 1 for icons
				bw.Write((short)sizes.Length);  // Number of images

				// Create the images for each size
				long directoryOffset = 6;
				long dataOffset = 6 + 16 * sizes.Length;

				for (int i = 0; i < sizes.Length; i++)
				{
					int size = sizes[i];
					using (Bitmap bmp = CreateIconImage(size))
					{
						// Current position to go back after
						long position = fs.Position;

						// Go to the directory position for this icon
						fs.Seek(directoryOffset, SeekOrigin.Begin);
						bw.Write((byte)size);      // Width in pixels
						bw.Write((byte)size);      // Height in pixels
						bw.Write((byte)0);         // Number of colors (0 for >=8bpp)
						bw.Write((byte)0);         // Reserved, must be 0
						bw.Write((short)1);        // Color planes (always 1)
						bw.Write((short)32);       // Bits per pixel

						// Convert to PNG and save in memory
						using (MemoryStream ms = new MemoryStream())
						{
							bmp.Save(ms, ImageFormat.Png);
							byte[] imageData = ms.ToArray();

							// Size in bytes of the image
							int imageSize = imageData.Length;
							bw.Write(imageSize);    // Image size
							bw.Write((int)dataOffset); // Position from the start of the file

							// Go back to the data writing position
							fs.Seek(dataOffset, SeekOrigin.Begin);

							// Write the image data
							bw.Write(imageData);

							// Update the offsets
							directoryOffset += 16;  // Each directory entry is 16 bytes
							dataOffset += imageSize;
						}

						// Go back to the saved position
						fs.Seek(position, SeekOrigin.Begin);
					}
				}
			}

			Console.WriteLine($"Icon created in: {Path.GetFullPath(outputPath)}");
		}

		private static Bitmap CreateIconImage(int size)
		{
			Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);

			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.Clear(Color.Transparent);

				// Draw a rounded background
				using (GraphicsPath path = new GraphicsPath())
				{
					float roundRadius = size * 0.2f;
					path.AddArc(0, 0, roundRadius * 2, roundRadius * 2, 180, 90);
					path.AddArc(size - roundRadius * 2, 0, roundRadius * 2, roundRadius * 2, 270, 90);
					path.AddArc(size - roundRadius * 2, size - roundRadius * 2, roundRadius * 2, roundRadius * 2, 0, 90);
					path.AddArc(0, size - roundRadius * 2, roundRadius * 2, roundRadius * 2, 90, 90);
					path.CloseFigure();

					// Fill with a gradient
					using (LinearGradientBrush brush = new LinearGradientBrush(
							new Point(0, 0),
							new Point(size, size),
							Color.FromArgb(225, 30, 160, 70),
							Color.FromArgb(225, 20, 120, 50)))
					{
						g.FillPath(brush, path);
					}

					// Darker border
					using (Pen pen = new Pen(Color.FromArgb(225, 20, 100, 40), size * 0.05f))
					{
						g.DrawPath(pen, path);
					}

					// Draw a stylized "CS" (CloudShot)
					using (Font font = new Font("Arial", size * 0.5f, FontStyle.Bold))
					{
						using (StringFormat sf = new StringFormat())
						{
							sf.Alignment = StringAlignment.Center;
							sf.LineAlignment = StringAlignment.Center;

							using (GraphicsPath textPath = new GraphicsPath())
							{
								textPath.AddString("C",
										font.FontFamily,
										(int)font.Style,
										g.DpiY * font.Size / 72,
										new Point(size / 2 - size / 6, size / 2),
										sf);

								textPath.AddString("S",
										font.FontFamily,
										(int)font.Style,
										g.DpiY * font.Size / 72,
										new Point(size / 2 + size / 6, size / 2),
										sf);

								// Fill text
								g.FillPath(Brushes.White, textPath);

								// Text border
								using (Pen textPen = new Pen(Color.FromArgb(150, 255, 255, 255), size * 0.02f))
								{
									g.DrawPath(textPen, textPath);
								}
							}
						}
					}
				}
			}

			return bmp;
		}
	}
}