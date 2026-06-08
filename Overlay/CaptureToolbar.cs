using System;
using System.Collections.Generic;
using System.Linq;
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

	public enum ToolbarPosition
	{
		Top,
		Bottom,
		Left,
		Right
	}
	public partial class CaptureToolbar : Control
	{
		private const int ButtonSize = 36;
		private const int ButtonSpacing = 4;
		private const int GroupSpacing = 10;
		private const int PaddingHorizontal = 10;
		private const int PaddingVertical = 8;
		private const int CornerRadius = 10;

		private static readonly CaptureToolbarAction[] AllActions =
			CaptureToolRegistry.ToolbarDisplayOrder.ToArray();

		private readonly List<CaptureToolbarAction> visibleActions = new List<CaptureToolbarAction>();

		public void ConfigureVisibleTools(AppSettings settings)
		{
			preferredPosition = settings.ToolbarDefaultPosition;
			visibleActions.Clear();
			foreach (CaptureToolbarAction action in AllActions)
			{
				if (CaptureToolRegistry.IsToolbarActionVisible(settings, action))
				{
					visibleActions.Add(action);
				}
			}

			Size = CalculateSize();
			UpdateRegion();
			Invalidate();
		}

		public void ConfigureShortcuts(AppSettings settings)
		{
			toolTip.SetToolTip(this, string.Empty);
			foreach (CaptureToolDefinition definition in CaptureToolRegistry.Definitions)
			{
				shortcutLabels[(int)definition.ToolbarAction] =
					FormatShortcut(definition.GetToolbarShortcut(settings));
			}
		}

		private readonly ToolTip toolTip = new ToolTip();
		private readonly string[] shortcutLabels = new string[18];

		private DrawingToolMode currentDrawingMode = DrawingToolMode.Pen;
		private bool isMoveMode;
		private ToolbarOrientation orientation = ToolbarOrientation.Horizontal;
		private ToolbarPosition preferredPosition = ToolbarPosition.Top;
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

			foreach (ToolbarPosition position in GetPositionOrder())
			{
				ToolbarOrientation candidateOrientation = GetOrientationFor(position);
				Size candidateSize = candidateOrientation == ToolbarOrientation.Horizontal ? horizontalSize : verticalSize;
				Point candidate = GetPositionLocation(position, selection, candidateSize, margin);
				Rectangle bounds = new Rectangle(candidate, candidateSize);
				if (FitsInOverlay(bounds, overlaySize))
				{
					SetOrientation(candidateOrientation);
					return ClampToOverlay(candidate, candidateSize, overlaySize);
				}
			}

			Point fallback = GetPositionLocation(ToolbarPosition.Bottom, selection, horizontalSize, margin);
			SetOrientation(ToolbarOrientation.Horizontal);
			return ClampToOverlay(fallback, horizontalSize, overlaySize);
		}

		private IEnumerable<ToolbarPosition> GetPositionOrder()
		{
			ToolbarPosition[] fallbackOrder =
			{
				ToolbarPosition.Top,
				ToolbarPosition.Bottom,
				ToolbarPosition.Left,
				ToolbarPosition.Right
			};

			yield return preferredPosition;
			foreach (ToolbarPosition position in fallbackOrder)
			{
				if (position != preferredPosition)
				{
					yield return position;
				}
			}
		}

		private static ToolbarOrientation GetOrientationFor(ToolbarPosition position)
		{
			return position == ToolbarPosition.Left || position == ToolbarPosition.Right
				? ToolbarOrientation.Vertical
				: ToolbarOrientation.Horizontal;
		}

		private static Point GetPositionLocation(ToolbarPosition position, Rectangle selection, Size size, int margin)
		{
			switch (position)
			{
				case ToolbarPosition.Top:
					return new Point(selection.Left + (selection.Width - size.Width) / 2, selection.Top - size.Height - margin);
				case ToolbarPosition.Left:
					return new Point(selection.Left - size.Width - margin, selection.Top + (selection.Height - size.Height) / 2);
				case ToolbarPosition.Right:
					return new Point(selection.Right + margin, selection.Top + (selection.Height - size.Height) / 2);
				case ToolbarPosition.Bottom:
				default:
					return new Point(selection.Left + (selection.Width - size.Width) / 2, selection.Bottom + margin);
			}
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
			return CaptureToolRegistry.GetToolbarGroup(action);
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
			return CaptureToolRegistry.GetDisplayLabel(action);
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
