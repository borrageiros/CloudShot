using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CloudShot.Core;
using CloudShot.Export;
using CloudShot.Overlay;

namespace CloudShot
{
	public partial class ScreenshotOverlay : Form
	{
		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		private const int HandleSize = 8;
		private const int ColorPickerPreviewSize = 150;
		private const int ColorPickerZoomFactor = 3;

		public event EventHandler<ScreenshotEventArgs> ScreenshotCaptured;

		private Bitmap screenshot;
		private Bitmap annotationLayer;
		private Bitmap colorPickerPreview;
		private OverlayRenderer overlayRenderer;
		private CoordinateMapper coordinateMapper;
		private CaptureToolbar captureToolbar;

		private Point startPoint;
		private Point endPoint;
		private Point moveDragOffset;
		private Point lastMousePosition = Point.Empty;
		private Rectangle selectionRectangle = Rectangle.Empty;
		private Rectangle previousSelectionRectangle = Rectangle.Empty;
		private Rectangle clientSelectionRect = Rectangle.Empty;

		private bool isSelecting;
		private bool isResizing;
		private bool isMoving;
		private bool isDrawing;
		private bool isErasing;
		private Point lastEraserImagePoint = Point.Empty;
		private bool isScreenshotValid;
		private DrawingToolMode currentDrawingMode = DrawingToolMode.Pen;
		private bool isMoveMode;
		private bool isColorPickerMode;
		private bool isColorSelected;

		private int screenshotWidth;
		private int screenshotHeight;
		private int currentHandleIndex = -1;

		private List<Rectangle> resizeHandles = new List<Rectangle>();
		private List<DrawingElement> drawingElements = new List<DrawingElement>();
		private int undoableHistoryCount;
		private List<Point> currentLine;
		private DrawingElement currentDrawingElement;
		private Rectangle rectanglePreviewInvalidationBounds = Rectangle.Empty;
		private int nextStepNumber = 1;
		private TextBox activeTextEditor;
		private Point activeTextEditorImagePoint;

		private Color selectedColor = Color.Empty;
		private Point colorPickerPoint = Point.Empty;
		private Color currentDrawingColor = Color.Red;

		private Cursor penCursor = Cursors.Cross;
		private AppSettings settings;
		private Rectangle totalScreenBounds;

		public ScreenshotOverlay(Bitmap screenshot)
		{
			settings = AppSettings.Load();
			currentDrawingColor = ParseColorOrDefault(settings.DefaultDrawingColor, Color.Red);
			currentDrawingMode = settings.DefaultTool;

			if (screenshot != null && screenshot.Width > 0 && screenshot.Height > 0)
			{
				this.screenshot = screenshot;
				screenshotWidth = screenshot.Width;
				screenshotHeight = screenshot.Height;
				isScreenshotValid = true;
				totalScreenBounds = ScreenCaptureService.GetTotalScreenBounds();
			}
			else
			{
				isScreenshotValid = false;
				MessageBox.Show("Could not obtain a valid screenshot.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			overlayRenderer = new OverlayRenderer();
			InitializeComponents();
		}

		private void InitializeComponents()
		{
			FormBorderStyle = FormBorderStyle.None;
			StartPosition = FormStartPosition.Manual;
			TopMost = true;
			Cursor = Cursors.Cross;
			BackColor = Color.Black;
			Opacity = 1.0;
			ShowInTaskbar = false;
			DoubleBuffered = true;
			KeyPreview = true;

			if (isScreenshotValid)
			{
				Bounds = totalScreenBounds;
				coordinateMapper = new CoordinateMapper(this, totalScreenBounds);
				overlayRenderer.Initialize(screenshot, ClientSize);
			}
			else
			{
				WindowState = FormWindowState.Maximized;
			}

			captureToolbar = new CaptureToolbar();
			captureToolbar.ConfigureShortcuts(settings);
			captureToolbar.ConfigureVisibleTools(settings);
			captureToolbar.ActionRequested += CaptureToolbar_ActionRequested;
			captureToolbar.Visible = false;
			Controls.Add(captureToolbar);
			captureToolbar.BringToFront();
			EnsureInitialDrawingModeEnabled();

			KeyDown += ScreenshotOverlay_KeyDown;
			MouseDown += ScreenshotOverlay_MouseDown;
			MouseMove += ScreenshotOverlay_MouseMove;
			MouseUp += ScreenshotOverlay_MouseUp;
			Paint += ScreenshotOverlay_Paint;
		}

		private void EnsureInitialDrawingModeEnabled()
		{
			if (IsDrawingToolEnabled(currentDrawingMode))
			{
				captureToolbar.SetDrawingMode(currentDrawingMode);
				return;
			}

			foreach (DrawingToolMode mode in CaptureToolRegistry.DrawingModeFallbackOrder)
			{
				if (IsDrawingToolEnabled(mode))
				{
					SetDrawingMode(mode);
					return;
				}
			}
		}

		private bool IsDrawingToolEnabled(DrawingToolMode mode)
		{
			return CaptureToolRegistry.IsDrawingToolEnabled(settings, mode);
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			Activate();
			Focus();
			SetForegroundWindow(Handle);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			UpdateToolbarPosition();
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x00000008;
				return cp;
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			screenshot?.Dispose();
			screenshot = null;
			DisposeAnnotationLayer();
			colorPickerPreview?.Dispose();
			colorPickerPreview = null;
			overlayRenderer?.Dispose();
		}
	}
}
