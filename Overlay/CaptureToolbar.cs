using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CloudShot.Core;

namespace CloudShot.Overlay
{
	public enum CaptureToolbarAction
	{
		PenMode,
		RectangleMode,
		FilledRectangleMode,
		PixelateMode,
		Move,
		ColorPicker,
		Undo,
		Copy,
		Save,
		Ocr,
		Upload,
		Close
	}

	public enum ToolbarOrientation
	{
		Horizontal,
		Vertical
	}

	public class CaptureToolbar : Control
	{
		private const int ButtonSize = 36;
		private const int ButtonSpacing = 4;
		private const int GroupSpacing = 10;
		private const int PaddingHorizontal = 10;
		private const int PaddingVertical = 8;
		private const int CornerRadius = 10;
		private const int FadeDurationMs = 150;

		private readonly CaptureToolbarAction[] actions =
		{
			CaptureToolbarAction.PenMode,
			CaptureToolbarAction.RectangleMode,
			CaptureToolbarAction.FilledRectangleMode,
			CaptureToolbarAction.PixelateMode,
			CaptureToolbarAction.Move,
			CaptureToolbarAction.ColorPicker,
			CaptureToolbarAction.Undo,
			CaptureToolbarAction.Copy,
			CaptureToolbarAction.Save,
			CaptureToolbarAction.Ocr,
			CaptureToolbarAction.Upload,
			CaptureToolbarAction.Close
		};

		public void ConfigureShortcuts(AppSettings settings)
		{
			toolTip.SetToolTip(this, string.Empty);
			shortcutLabels[(int)CaptureToolbarAction.Undo] = FormatShortcut(settings.UndoShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Copy] = FormatShortcut(settings.CopyShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Save] = FormatShortcut(settings.SaveShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Ocr] = FormatShortcut(settings.OcrShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Upload] = FormatShortcut(settings.ScpShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Close] = FormatShortcut(settings.CancelShortcut);
		}

		private readonly ToolTip toolTip = new ToolTip();
		private readonly string[] shortcutLabels = new string[12];
		private readonly Timer fadeTimer = new Timer();

		private DrawingToolMode currentDrawingMode = DrawingToolMode.Pen;
		private bool isMoveMode;
		private ToolbarOrientation orientation = ToolbarOrientation.Horizontal;
		private Color drawingColor = Color.Red;
		private int hoveredIndex = -1;
		private int fadeAlpha;
		private bool fadingIn;

		public event EventHandler<CaptureToolbarAction> ActionRequested;

		public CaptureToolbar()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint |
			         ControlStyles.OptimizedDoubleBuffer |
			         ControlStyles.ResizeRedraw |
			         ControlStyles.UserPaint |
			         ControlStyles.SupportsTransparentBackColor, true);

			BackColor = Color.Transparent;
			Size = CalculateSize();
			Visible = false;

			fadeTimer.Interval = 16;
			fadeTimer.Tick += FadeTimerTick;

			ConfigureTooltips();
		}

		public void SetDrawingMode(DrawingToolMode mode)
		{
			currentDrawingMode = mode;
			isMoveMode = false;
			Invalidate();
		}

		public void SetPenMode(bool penMode)
		{
			SetDrawingMode(penMode ? DrawingToolMode.Pen : DrawingToolMode.Rectangle);
		}

		public void SetMoveMode(bool moveMode)
		{
			isMoveMode = moveMode;
			Invalidate();
		}

		public void SetDrawingColor(Color color)
		{
			drawingColor = color;
			Invalidate();
		}

		public void ShowAnimated()
		{
			fadeAlpha = 0;
			fadingIn = true;
			Visible = true;
			fadeTimer.Start();
			Invalidate();
		}

		public void HideImmediate()
		{
			fadeTimer.Stop();
			fadingIn = false;
			fadeAlpha = 0;
			Visible = false;
		}

		public void Reposition(Rectangle clientSelectionRect, Size overlayClientSize, int offsetX, int offsetY)
		{
			if (clientSelectionRect.IsEmpty)
			{
				HideImmediate();
				return;
			}

			Rectangle selectionOnClient = new Rectangle(
				clientSelectionRect.X + offsetX,
				clientSelectionRect.Y + offsetY,
				clientSelectionRect.Width,
				clientSelectionRect.Height);

			Point location = CalculateBestLocation(selectionOnClient, overlayClientSize);
			Location = location;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			int index = HitTest(e.Location);
			if (index != hoveredIndex)
			{
				hoveredIndex = index;
				Invalidate();
			}

			if (index >= 0)
			{
				string shortcut = shortcutLabels[index];
				string label = GetActionLabel(actions[index]);
				toolTip.SetToolTip(this, string.IsNullOrEmpty(shortcut) ? label : $"{label} ({shortcut})");
			}
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			hoveredIndex = -1;
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button != MouseButtons.Left)
			{
				return;
			}

			int index = HitTest(e.Location);
			if (index >= 0)
			{
				ActionRequested?.Invoke(this, actions[index]);
			}
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			Color background = Color.FromArgb(Math.Min(255, fadeAlpha), 33, 33, 33);
			using (GraphicsPath path = CreateRoundedRectPath(ClientRectangle, CornerRadius))
			using (SolidBrush brush = new SolidBrush(background))
			{
				g.FillPath(brush, path);
			}

			for (int i = 0; i < actions.Length; i++)
			{
				Rectangle buttonRect = GetButtonRect(i);
				bool active = IsActionActive(actions[i]);
				bool hovered = i == hoveredIndex;

				Color buttonColor = active
					? Color.FromArgb(Math.Min(255, fadeAlpha), 46, 125, 50)
					: hovered
						? Color.FromArgb(Math.Min(255, fadeAlpha), 66, 66, 66)
						: Color.FromArgb(Math.Min(255, fadeAlpha), 48, 48, 48);

				using (SolidBrush brush = new SolidBrush(buttonColor))
				{
					g.FillEllipse(brush, buttonRect);
				}

				DrawIcon(g, actions[i], buttonRect, active);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				fadeTimer.Stop();
				fadeTimer.Dispose();
				toolTip.Dispose();
			}

			base.Dispose(disposing);
		}

		private void ConfigureTooltips()
		{
			toolTip.SetToolTip(this, string.Empty);
			toolTip.AutoPopDelay = 4000;
			toolTip.InitialDelay = 300;
		}

		private void FadeTimerTick(object sender, EventArgs e)
		{
			if (fadingIn)
			{
				fadeAlpha += 17;
				if (fadeAlpha >= 255)
				{
					fadeAlpha = 255;
					fadeTimer.Stop();
				}
			}
			else
			{
				fadeAlpha -= 17;
				if (fadeAlpha <= 0)
				{
					fadeAlpha = 0;
					fadeTimer.Stop();
					Visible = false;
				}
			}

			Invalidate();
		}

		private Size CalculateSize()
		{
			return GetSizeFor(orientation);
		}

		private Size GetSizeFor(ToolbarOrientation value)
		{
			int groups = 3;
			int buttons = actions.Length;
			int mainLength = buttons * ButtonSize +
			                 (buttons - groups) * ButtonSpacing +
			                 (groups - 1) * GroupSpacing;

			if (value == ToolbarOrientation.Horizontal)
			{
				return new Size(PaddingHorizontal * 2 + mainLength, PaddingVertical * 2 + ButtonSize);
			}

			return new Size(PaddingVertical * 2 + ButtonSize, PaddingHorizontal * 2 + mainLength);
		}

		private void SetOrientation(ToolbarOrientation value)
		{
			if (orientation == value)
			{
				return;
			}

			orientation = value;
			Size = CalculateSize();
			Invalidate();
		}

		private Point CalculateBestLocation(Rectangle selection, Size overlaySize)
		{
			const int margin = 8;
			Size horizontalSize = GetSizeFor(ToolbarOrientation.Horizontal);
			Size verticalSize = GetSizeFor(ToolbarOrientation.Vertical);

			ToolbarOrientation[] orientations =
			{
				ToolbarOrientation.Horizontal,
				ToolbarOrientation.Horizontal,
				ToolbarOrientation.Vertical,
				ToolbarOrientation.Vertical
			};

			Point[] candidates =
			{
				new Point(selection.Left + (selection.Width - horizontalSize.Width) / 2, selection.Top - horizontalSize.Height - margin),
				new Point(selection.Left + (selection.Width - horizontalSize.Width) / 2, selection.Bottom + margin),
				new Point(selection.Left - verticalSize.Width - margin, selection.Top + (selection.Height - verticalSize.Height) / 2),
				new Point(selection.Right + margin, selection.Top + (selection.Height - verticalSize.Height) / 2)
			};

			for (int i = 0; i < candidates.Length; i++)
			{
				Size candidateSize = orientations[i] == ToolbarOrientation.Horizontal ? horizontalSize : verticalSize;
				Rectangle bounds = new Rectangle(candidates[i], candidateSize);
				if (FitsInOverlay(bounds, overlaySize))
				{
					SetOrientation(orientations[i]);
					return ClampToOverlay(candidates[i], candidateSize, overlaySize);
				}
			}

			SetOrientation(ToolbarOrientation.Horizontal);
			return ClampToOverlay(candidates[1], horizontalSize, overlaySize);
		}

		private static bool FitsInOverlay(Rectangle bounds, Size overlaySize)
		{
			return bounds.Left >= 0 &&
			       bounds.Top >= 0 &&
			       bounds.Right <= overlaySize.Width &&
			       bounds.Bottom <= overlaySize.Height;
		}

		private static Point ClampToOverlay(Point location, Size toolbarSize, Size overlaySize)
		{
			int x = Math.Max(8, Math.Min(location.X, overlaySize.Width - toolbarSize.Width - 8));
			int y = Math.Max(8, Math.Min(location.Y, overlaySize.Height - toolbarSize.Height - 8));
			return new Point(x, y);
		}

		private Rectangle GetButtonRect(int index)
		{
			int offset = 0;
			for (int i = 0; i < index; i++)
			{
				offset += ButtonSize + ButtonSpacing;
				if (i == 5 || i == 8)
				{
					offset += GroupSpacing - ButtonSpacing;
				}
			}

			if (orientation == ToolbarOrientation.Horizontal)
			{
				return new Rectangle(PaddingHorizontal + offset, PaddingVertical, ButtonSize, ButtonSize);
			}

			return new Rectangle(PaddingVertical, PaddingHorizontal + offset, ButtonSize, ButtonSize);
		}

		private int HitTest(Point location)
		{
			for (int i = 0; i < actions.Length; i++)
			{
				Rectangle rect = GetButtonRect(i);
				if (rect.Contains(location))
				{
					return i;
				}
			}

			return -1;
		}

		private bool IsActionActive(CaptureToolbarAction action)
		{
			switch (action)
			{
				case CaptureToolbarAction.PenMode:
					return currentDrawingMode == DrawingToolMode.Pen && !isMoveMode;
				case CaptureToolbarAction.RectangleMode:
					return currentDrawingMode == DrawingToolMode.Rectangle && !isMoveMode;
				case CaptureToolbarAction.FilledRectangleMode:
					return currentDrawingMode == DrawingToolMode.FilledRectangle && !isMoveMode;
				case CaptureToolbarAction.PixelateMode:
					return currentDrawingMode == DrawingToolMode.Pixelate && !isMoveMode;
				case CaptureToolbarAction.Move:
					return isMoveMode;
				default:
					return false;
			}
		}

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
					case CaptureToolbarAction.Upload:
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

		private static string FormatShortcut(Keys shortcut)
		{
			if (shortcut == Keys.None)
			{
				return string.Empty;
			}

			List<string> parts = new List<string>();
			if ((shortcut & Keys.Control) == Keys.Control)
			{
				parts.Add("Ctrl");
			}

			if ((shortcut & Keys.Shift) == Keys.Shift)
			{
				parts.Add("Shift");
			}

			if ((shortcut & Keys.Alt) == Keys.Alt)
			{
				parts.Add("Alt");
			}

			Keys key = shortcut & Keys.KeyCode;
			if (key != Keys.ControlKey && key != Keys.ShiftKey && key != Keys.Menu)
			{
				parts.Add(key.ToString());
			}

			return string.Join("+", parts);
		}

		private static string GetActionLabel(CaptureToolbarAction action)
		{
			switch (action)
			{
				case CaptureToolbarAction.PenMode:
					return "Pen";
				case CaptureToolbarAction.RectangleMode:
					return "Rectangle";
				case CaptureToolbarAction.FilledRectangleMode:
					return "Filled rectangle";
				case CaptureToolbarAction.PixelateMode:
					return "Pixelate";
				case CaptureToolbarAction.Move:
					return "Move";
				case CaptureToolbarAction.ColorPicker:
					return "Color";
				case CaptureToolbarAction.Undo:
					return "Undo";
				case CaptureToolbarAction.Copy:
					return "Copy";
				case CaptureToolbarAction.Save:
					return "Save";
				case CaptureToolbarAction.Ocr:
					return "OCR";
				case CaptureToolbarAction.Upload:
					return "Upload";
				case CaptureToolbarAction.Close:
					return "Cancel";
				default:
					return action.ToString();
			}
		}

		private static GraphicsPath CreateRoundedRectPath(Rectangle bounds, int radius)
		{
			GraphicsPath path = new GraphicsPath();
			int diameter = radius * 2;
			path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
			path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
			path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
			path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
			path.CloseFigure();
			return path;
		}
	}
}
