using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CloudShot.Overlay
{
	public partial class CaptureToolbar
	{
		private void DrawIcon(Graphics g, CaptureToolbarAction action, Rectangle rect, bool active)
		{
			Color iconColor = active ? Color.White : Color.FromArgb(210, 210, 210);
			using (Pen pen = new Pen(iconColor, 2f))
			{
				pen.StartCap = LineCap.Round;
				pen.EndCap = LineCap.Round;

				int cx = rect.X + rect.Width / 2;
				int cy = rect.Y + rect.Height / 2;

				switch (action)
				{
					case CaptureToolbarAction.PenMode:
					{
						PointF capStart = new PointF(cx + 4, cy - 10);
						PointF capEnd = new PointF(cx + 10, cy - 4);
						PointF woodLeft = new PointF(cx - 6, cy);
						PointF woodRight = new PointF(cx, cy + 6);
						PointF tip = new PointF(cx - 8, cy + 8);
						g.DrawLine(pen, capStart, capEnd);
						g.DrawLine(pen, capStart, woodLeft);
						g.DrawLine(pen, capEnd, woodRight);
						g.DrawLine(pen, woodLeft, woodRight);
						g.DrawLine(pen, woodLeft, tip);
						g.DrawLine(pen, woodRight, tip);
						break;
					}
					case CaptureToolbarAction.RectangleMode:
						g.DrawRectangle(pen, cx - 8, cy - 6, 16, 12);
						break;
					case CaptureToolbarAction.FilledRectangleMode:
						using (SolidBrush fillBrush = new SolidBrush(iconColor))
						{
							g.FillRectangle(fillBrush, cx - 8, cy - 6, 16, 12);
						}
						g.DrawRectangle(pen, cx - 8, cy - 6, 16, 12);
						break;
					case CaptureToolbarAction.PixelateMode:
						for (int row = 0; row < 3; row++)
						{
							for (int col = 0; col < 3; col++)
							{
								int shade = (row + col) % 2 == 0 ? 210 : 140;
								using (SolidBrush cellBrush = new SolidBrush(Color.FromArgb(shade, shade, shade)))
								{
									g.FillRectangle(cellBrush, cx - 9 + col * 6, cy - 9 + row * 6, 6, 6);
								}
							}
						}
						g.DrawRectangle(pen, cx - 9, cy - 9, 18, 18);
						break;
					case CaptureToolbarAction.ArrowMode:
						DrawToolbarArrowIcon(g, iconColor, cx, cy);
						break;
					case CaptureToolbarAction.HighlighterMode:
						using (Pen highlighterPen = new Pen(Color.FromArgb(160, iconColor), 9f))
						{
							highlighterPen.StartCap = LineCap.Flat;
							highlighterPen.EndCap = LineCap.Flat;
							g.DrawLine(highlighterPen, cx - 9, cy + 6, cx + 9, cy - 6);
						}
						break;
					case CaptureToolbarAction.LineMode:
						g.DrawLine(pen, cx - 8, cy + 4, cx + 8, cy - 4);
						break;
					case CaptureToolbarAction.StepsMode:
					{
						Rectangle stepCircle = new Rectangle(cx - 8, cy - 8, 16, 16);
						using (SolidBrush stepBrush = new SolidBrush(iconColor))
						{
							g.FillEllipse(stepBrush, stepCircle);
						}
						using (Font stepFont = new Font("Segoe UI", 8f, FontStyle.Bold))
						using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(33, 33, 33)))
						{
							DrawCenteredText(g, "1", stepFont, textBrush, stepCircle);
						}
						break;
					}
					case CaptureToolbarAction.TextMode:
						using (Font textFont = new Font("Segoe UI", 10f, FontStyle.Bold))
						{
							g.DrawString("T", textFont, new SolidBrush(iconColor), cx - 5, cy - 9);
						}
						break;
					case CaptureToolbarAction.EraserMode:
						DrawToolbarEraserIcon(g, iconColor, cx, cy, active);
						break;
					case CaptureToolbarAction.Move:
						g.DrawLine(pen, cx, cy - 9, cx, cy + 9);
						g.DrawLine(pen, cx - 9, cy, cx + 9, cy);
						DrawArrowHead(g, pen, cx, cy - 9, 0, -1, 4f);
						DrawArrowHead(g, pen, cx, cy + 9, 0, 1, 4f);
						DrawArrowHead(g, pen, cx - 9, cy, -1, 0, 4f);
						DrawArrowHead(g, pen, cx + 9, cy, 1, 0, 4f);
						break;
					case CaptureToolbarAction.ColorPicker:
						using (SolidBrush colorBrush = new SolidBrush(drawingColor))
						{
							g.FillEllipse(colorBrush, cx - 7, cy - 7, 14, 14);
						}
						g.DrawEllipse(pen, cx - 7, cy - 7, 14, 14);
						break;
					case CaptureToolbarAction.Undo:
					{
						const float radius = 8f;
						const float startAngle = 200f;
						const float sweepAngle = -290f;
						g.DrawArc(pen, cx - radius, cy - radius, radius * 2, radius * 2, startAngle, sweepAngle);
						double endRad = (startAngle + sweepAngle) * Math.PI / 180.0;
						float headX = cx + radius * (float)Math.Cos(endRad);
						float headY = cy + radius * (float)Math.Sin(endRad);
						DrawArrowHead(g, pen, headX, headY, Math.Sin(endRad), -Math.Cos(endRad), 5f);
						break;
					}
					case CaptureToolbarAction.Copy:
						g.DrawRectangle(pen, cx - 2, cy - 6, 10, 12);
						g.DrawRectangle(pen, cx - 8, cy - 2, 10, 12);
						break;
					case CaptureToolbarAction.Save:
						g.DrawRectangle(pen, cx - 8, cy - 7, 16, 14);
						g.DrawLine(pen, cx - 4, cy - 7, cx - 4, cy - 3);
						g.DrawLine(pen, cx + 4, cy - 7, cx + 4, cy - 3);
						g.DrawRectangle(pen, cx - 4, cy + 1, 8, 6);
						break;
					case CaptureToolbarAction.Ocr:
						using (Font font = new Font("Segoe UI", 8f, FontStyle.Bold))
						{
							g.DrawString("Aa", font, new SolidBrush(iconColor), cx - 9, cy - 8);
						}
						break;
					case CaptureToolbarAction.Scp:
						using (GraphicsPath cloud = new GraphicsPath())
						{
							cloud.AddArc(cx - 10, cy - 3, 8, 8, 90, 180);
							cloud.AddArc(cx - 6, cy - 9, 12, 12, 180, 180);
							cloud.AddArc(cx + 2, cy - 3, 8, 8, 270, 180);
							cloud.CloseFigure();
							g.DrawPath(pen, cloud);
						}
						break;
					case CaptureToolbarAction.Close:
						g.DrawLine(pen, cx - 6, cy - 6, cx + 6, cy + 6);
						g.DrawLine(pen, cx + 6, cy - 6, cx - 6, cy + 6);
						break;
				}
			}
		}

		private static void DrawToolbarArrowIcon(Graphics g, Color color, int cx, int cy)
		{
			PointF tail = new PointF(cx - 7, cy + 7);
			PointF tip = new PointF(cx + 7, cy - 7);
			float dx = tip.X - tail.X;
			float dy = tip.Y - tail.Y;
			float length = (float)Math.Sqrt(dx * dx + dy * dy);
			if (length < 1)
			{
				return;
			}

			dx /= length;
			dy /= length;
			const float headLength = 6f;
			const float headHalfWidth = 4f;
			PointF baseCenter = new PointF(tip.X - dx * headLength, tip.Y - dy * headLength);
			float perpX = -dy;
			float perpY = dx;
			PointF left = new PointF(
				baseCenter.X + perpX * headHalfWidth,
				baseCenter.Y + perpY * headHalfWidth);
			PointF right = new PointF(
				baseCenter.X - perpX * headHalfWidth,
				baseCenter.Y - perpY * headHalfWidth);

			using (Pen shaftPen = new Pen(color, 2f))
			{
				shaftPen.StartCap = LineCap.Round;
				shaftPen.EndCap = LineCap.Flat;
				g.DrawLine(shaftPen, tail, baseCenter);
			}

			using (SolidBrush fillBrush = new SolidBrush(color))
			{
				g.FillPolygon(fillBrush, new[] { tip, left, right });
			}
		}

		private static void DrawToolbarEraserIcon(Graphics g, Color iconColor, int cx, int cy, bool active)
		{
			GraphicsState state = g.Save();
			g.TranslateTransform(cx, cy);
			g.RotateTransform(-32f);

			Color sleeveColor = active ? Color.FromArgb(155, 155, 155) : Color.FromArgb(120, 120, 120);
			Color outlineColor = active ? Color.FromArgb(235, 235, 235) : Color.FromArgb(190, 190, 190);
			RectangleF sleeve = new RectangleF(-6.5f, -10f, 13f, 6.5f);
			RectangleF rubber = new RectangleF(-7f, -3.5f, 14f, 10f);

			using (SolidBrush rubberBrush = new SolidBrush(iconColor))
			{
				g.FillRectangle(rubberBrush, rubber);
			}

			using (SolidBrush sleeveBrush = new SolidBrush(sleeveColor))
			{
				g.FillRectangle(sleeveBrush, sleeve);
			}

			using (Pen outlinePen = new Pen(outlineColor, 1.5f))
			{
				g.DrawRectangle(outlinePen, sleeve.X, sleeve.Y, sleeve.Width, sleeve.Height);
				g.DrawRectangle(outlinePen, rubber.X, rubber.Y, rubber.Width, rubber.Height);
			}

			g.Restore(state);
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

		private static void DrawArrowHead(Graphics g, Pen pen, float x, float y, double dirX, double dirY, float size)
		{
			double length = Math.Sqrt(dirX * dirX + dirY * dirY);
			if (length == 0)
			{
				return;
			}

			dirX /= length;
			dirY /= length;
			double angle = Math.Atan2(dirY, dirX);
			double spread = 35.0 * Math.PI / 180.0;
			double leftAngle = angle + Math.PI - spread;
			double rightAngle = angle + Math.PI + spread;
			g.DrawLine(pen, x, y, x + (float)(size * Math.Cos(leftAngle)), y + (float)(size * Math.Sin(leftAngle)));
			g.DrawLine(pen, x, y, x + (float)(size * Math.Cos(rightAngle)), y + (float)(size * Math.Sin(rightAngle)));
		}

	}
}
