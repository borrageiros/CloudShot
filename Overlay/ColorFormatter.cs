using System;
using System.Drawing;

namespace CloudShot.Overlay
{
	public static class ColorFormatter
	{
		public static string GetColorString(Color color, string format)
		{
			switch (format?.ToUpperInvariant())
			{
				case "HEX":
					return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
				case "HSL":
					ColorToHsl(color, out float h, out float s, out float l);
					return $"hsl({Math.Round(h)}, {Math.Round(s * 100)}%, {Math.Round(l * 100)}%)";
				case "RGB":
				default:
					return color.A < 255
						? $"rgba({color.R}, {color.G}, {color.B}, {color.A / 255f:0.##})"
						: $"rgb({color.R}, {color.G}, {color.B})";
			}
		}

		private static void ColorToHsl(Color color, out float h, out float s, out float l)
		{
			float r = color.R / 255f;
			float g = color.G / 255f;
			float b = color.B / 255f;

			float max = Math.Max(r, Math.Max(g, b));
			float min = Math.Min(r, Math.Min(g, b));
			l = (max + min) / 2f;

			if (Math.Abs(max - min) < 0.0001f)
			{
				h = s = 0f;
				return;
			}

			float delta = max - min;
			s = l > 0.5f ? delta / (2f - max - min) : delta / (max + min);

			if (Math.Abs(max - r) < 0.0001f)
			{
				h = (g - b) / delta + (g < b ? 6f : 0f);
			}
			else if (Math.Abs(max - g) < 0.0001f)
			{
				h = (b - r) / delta + 2f;
			}
			else
			{
				h = (r - g) / delta + 4f;
			}

			h *= 60f;
		}
	}
}
