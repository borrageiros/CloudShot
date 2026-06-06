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
		ArrowMode,
		HighlighterMode,
		LineMode,
		StepsMode,
		TextMode,
		EraserMode,
		Move,
		ColorPicker,
		Undo,
		Copy,
		Save,
		Ocr,
		Scp,
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

		private static readonly CaptureToolbarAction[] AllActions =
		{
			CaptureToolbarAction.PenMode,
			CaptureToolbarAction.EraserMode,
			CaptureToolbarAction.RectangleMode,
			CaptureToolbarAction.FilledRectangleMode,
			CaptureToolbarAction.PixelateMode,
			CaptureToolbarAction.ArrowMode,
			CaptureToolbarAction.HighlighterMode,
			CaptureToolbarAction.LineMode,
			CaptureToolbarAction.StepsMode,
			CaptureToolbarAction.TextMode,
			CaptureToolbarAction.Move,
			CaptureToolbarAction.ColorPicker,
			CaptureToolbarAction.Undo,
			CaptureToolbarAction.Copy,
			CaptureToolbarAction.Save,
			CaptureToolbarAction.Ocr,
			CaptureToolbarAction.Scp,
			CaptureToolbarAction.Close
		};

		private readonly List<CaptureToolbarAction> visibleActions = new List<CaptureToolbarAction>();

		public void ConfigureVisibleTools(AppSettings settings)
		{
			visibleActions.Clear();
			foreach (CaptureToolbarAction action in AllActions)
			{
				if (IsToolEnabled(settings, action))
				{
					visibleActions.Add(action);
				}
			}

			Size = CalculateSize();
			UpdateRegion();
			Invalidate();
		}

		private static bool IsToolEnabled(AppSettings settings, CaptureToolbarAction action)
		{
			switch (action)
			{
				case CaptureToolbarAction.PenMode:
					return settings.ToolPenEnabled;
				case CaptureToolbarAction.RectangleMode:
					return settings.ToolRectangleEnabled;
				case CaptureToolbarAction.FilledRectangleMode:
					return settings.ToolFilledRectangleEnabled;
				case CaptureToolbarAction.PixelateMode:
					return settings.ToolPixelateEnabled;
				case CaptureToolbarAction.ArrowMode:
					return settings.ToolArrowEnabled;
				case CaptureToolbarAction.HighlighterMode:
					return settings.ToolHighlighterEnabled;
				case CaptureToolbarAction.LineMode:
					return settings.ToolLineEnabled;
				case CaptureToolbarAction.StepsMode:
					return settings.ToolStepsEnabled;
				case CaptureToolbarAction.TextMode:
					return settings.ToolTextEnabled;
				case CaptureToolbarAction.EraserMode:
					return settings.ToolEraserEnabled;
				case CaptureToolbarAction.Move:
					return settings.ToolMoveEnabled;
				case CaptureToolbarAction.ColorPicker:
					return settings.ToolColorPickerEnabled;
				case CaptureToolbarAction.Undo:
					return settings.ToolUndoEnabled;
				case CaptureToolbarAction.Copy:
					return settings.ToolCopyEnabled;
				case CaptureToolbarAction.Save:
					return settings.ToolSaveEnabled;
				case CaptureToolbarAction.Ocr:
					return settings.ToolOcrEnabled;
				case CaptureToolbarAction.Scp:
					return settings.ToolScpEnabled && !string.IsNullOrWhiteSpace(settings.ScpHost);
				case CaptureToolbarAction.Close:
					return settings.ToolCloseEnabled;
				default:
					return true;
			}
		}

		public void ConfigureShortcuts(AppSettings settings)
		{
			toolTip.SetToolTip(this, string.Empty);
			shortcutLabels[(int)CaptureToolbarAction.PenMode] = FormatShortcut(settings.PenToolShortcut);
			shortcutLabels[(int)CaptureToolbarAction.RectangleMode] = FormatShortcut(settings.RectangleToolShortcut);
			shortcutLabels[(int)CaptureToolbarAction.FilledRectangleMode] = FormatShortcut(settings.FilledRectangleToolShortcut);
			shortcutLabels[(int)CaptureToolbarAction.PixelateMode] = FormatShortcut(settings.PixelateToolShortcut);
			shortcutLabels[(int)CaptureToolbarAction.ArrowMode] = FormatShortcut(settings.ArrowToolShortcut);
			shortcutLabels[(int)CaptureToolbarAction.HighlighterMode] = FormatShortcut(settings.HighlighterToolShortcut);
			shortcutLabels[(int)CaptureToolbarAction.LineMode] = FormatShortcut(settings.LineToolShortcut);
			shortcutLabels[(int)CaptureToolbarAction.StepsMode] = FormatShortcut(settings.StepsToolShortcut);
			shortcutLabels[(int)CaptureToolbarAction.TextMode] = FormatShortcut(settings.TextToolShortcut);
			shortcutLabels[(int)CaptureToolbarAction.EraserMode] = FormatShortcut(settings.EraserToolShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Move] = FormatShortcut(settings.MoveToolShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Undo] = FormatShortcut(settings.UndoShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Copy] = FormatShortcut(settings.CopyShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Save] = FormatShortcut(settings.SaveShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Ocr] = FormatShortcut(settings.OcrShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Scp] = FormatShortcut(settings.ScpShortcut);
			shortcutLabels[(int)CaptureToolbarAction.Close] = FormatShortcut(settings.CancelShortcut);
		}

		private readonly ToolTip toolTip = new ToolTip();
		private readonly string[] shortcutLabels = new string[18];

		private DrawingToolMode currentDrawingMode = DrawingToolMode.Pen;
		private bool isMoveMode;
		private ToolbarOrientation orientation = ToolbarOrientation.Horizontal;
		private Color drawingColor = Color.Red;
		private int hoveredIndex = -1;

		public event EventHandler<CaptureToolbarAction> ActionRequested;

		public CaptureToolbar()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint |
			         ControlStyles.OptimizedDoubleBuffer |
			         ControlStyles.ResizeRedraw |
			         ControlStyles.UserPaint |
			         ControlStyles.SupportsTransparentBackColor, true);

			BackColor = Color.Transparent;
			visibleActions.AddRange(AllActions);
			Size = CalculateSize();
			Visible = false;

			ConfigureTooltips();
			UpdateRegion();
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
			Visible = true;
			Invalidate();
		}

		public void HideImmediate()
		{
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

			if (index >= 0 && index < visibleActions.Count)
			{
				string shortcut = shortcutLabels[(int)visibleActions[index]];
				string label = GetActionLabel(visibleActions[index]);
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
			if (index >= 0 && index < visibleActions.Count)
			{
				ActionRequested?.Invoke(this, visibleActions[index]);
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			UpdateRegion();
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			if (Parent == null)
			{
				return;
			}

			int offsetX = Left;
			int offsetY = Top;
			Rectangle parentClip = new Rectangle(offsetX, offsetY, Width, Height);

			pevent.Graphics.TranslateTransform(-offsetX, -offsetY);
			try
			{
				using (PaintEventArgs parentArgs = new PaintEventArgs(pevent.Graphics, parentClip))
				{
					InvokePaintBackground(Parent, parentArgs);
					InvokePaint(Parent, parentArgs);
				}
			}
			finally
			{
				pevent.Graphics.TranslateTransform(offsetX, offsetY);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			Color background = Color.FromArgb(33, 33, 33);
			using (GraphicsPath path = CreateRoundedRectPath(ClientRectangle, CornerRadius))
			using (SolidBrush brush = new SolidBrush(background))
			{
				g.FillPath(brush, path);
			}

			for (int i = 0; i < visibleActions.Count; i++)
			{
				Rectangle buttonRect = GetButtonRect(i);
				bool active = IsActionActive(visibleActions[i]);
				bool hovered = i == hoveredIndex;

				Color buttonColor = active
					? Color.FromArgb(46, 125, 50)
					: hovered
						? Color.FromArgb(66, 66, 66)
						: Color.FromArgb(48, 48, 48);

				using (SolidBrush brush = new SolidBrush(buttonColor))
				{
					g.FillEllipse(brush, buttonRect);
				}

				DrawIcon(g, visibleActions[i], buttonRect, active);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
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

		private Size CalculateSize()
		{
			return GetSizeFor(orientation);
		}

		private Size GetSizeFor(ToolbarOrientation value)
		{
			int groups = CountVisibleGroups();
			int buttons = visibleActions.Count;
			if (buttons == 0)
			{
				return new Size(PaddingHorizontal * 2, PaddingVertical * 2 + ButtonSize);
			}

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

		private static int GetActionGroup(CaptureToolbarAction action)
		{
			switch (action)
			{
				case CaptureToolbarAction.PenMode:
				case CaptureToolbarAction.RectangleMode:
				case CaptureToolbarAction.FilledRectangleMode:
				case CaptureToolbarAction.PixelateMode:
				case CaptureToolbarAction.ArrowMode:
				case CaptureToolbarAction.HighlighterMode:
				case CaptureToolbarAction.LineMode:
				case CaptureToolbarAction.StepsMode:
				case CaptureToolbarAction.TextMode:
				case CaptureToolbarAction.EraserMode:
					return 0;
				case CaptureToolbarAction.Move:
				case CaptureToolbarAction.ColorPicker:
				case CaptureToolbarAction.Undo:
					return 1;
				default:
					return 2;
			}
		}

		private int CountVisibleGroups()
		{
			if (visibleActions.Count == 0)
			{
				return 0;
			}

			int groups = 1;
			for (int i = 1; i < visibleActions.Count; i++)
			{
				if (GetActionGroup(visibleActions[i]) != GetActionGroup(visibleActions[i - 1]))
				{
					groups++;
				}
			}

			return groups;
		}

		private Rectangle GetButtonRect(int index)
		{
			int offset = 0;
			for (int i = 0; i < index; i++)
			{
				offset += ButtonSize + ButtonSpacing;
				if (GetActionGroup(visibleActions[i]) != GetActionGroup(visibleActions[i + 1]))
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
			for (int i = 0; i < visibleActions.Count; i++)
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
				case CaptureToolbarAction.ArrowMode:
					return currentDrawingMode == DrawingToolMode.Arrow && !isMoveMode;
				case CaptureToolbarAction.HighlighterMode:
					return currentDrawingMode == DrawingToolMode.Highlighter && !isMoveMode;
				case CaptureToolbarAction.LineMode:
					return currentDrawingMode == DrawingToolMode.Line && !isMoveMode;
				case CaptureToolbarAction.StepsMode:
					return currentDrawingMode == DrawingToolMode.Steps && !isMoveMode;
				case CaptureToolbarAction.TextMode:
					return currentDrawingMode == DrawingToolMode.Text && !isMoveMode;
				case CaptureToolbarAction.EraserMode:
					return currentDrawingMode == DrawingToolMode.Eraser && !isMoveMode;
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
				case CaptureToolbarAction.ArrowMode:
					return "Arrow";
				case CaptureToolbarAction.HighlighterMode:
					return "Highlighter";
				case CaptureToolbarAction.LineMode:
					return "Line";
				case CaptureToolbarAction.StepsMode:
					return "Steps";
				case CaptureToolbarAction.TextMode:
					return "Text";
				case CaptureToolbarAction.EraserMode:
					return "Eraser";
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
				case CaptureToolbarAction.Scp:
					return "Upload";
				case CaptureToolbarAction.Close:
					return "Cancel";
				default:
					return action.ToString();
			}
		}

		private void UpdateRegion()
		{
			using (GraphicsPath path = CreateRoundedRectPath(ClientRectangle, CornerRadius))
			{
				Region = new Region(path);
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
