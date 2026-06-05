using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CloudShot.Core;
using CloudShot.Export;
using CloudShot.Overlay;

namespace CloudShot
{
	public class ScreenshotOverlay : Form
	{
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
		private bool isScreenshotValid;
		private bool isPenMode = true;
		private bool isMoveMode;
		private bool isColorPickerMode;
		private bool isColorSelected;

		private int screenshotWidth;
		private int screenshotHeight;
		private int currentHandleIndex = -1;

		private List<Rectangle> resizeHandles = new List<Rectangle>();
		private List<DrawingElement> drawingElements = new List<DrawingElement>();
		private List<Point> currentLine;
		private DrawingElement currentDrawingElement;

		private Color selectedColor = Color.Empty;
		private Point colorPickerPoint = Point.Empty;
		private Color currentDrawingColor = Color.Red;

		private Cursor penCursor = Cursors.Cross;
		private AppSettings settings;
		private Rectangle totalScreenBounds;

		public ScreenshotOverlay(Bitmap screenshot)
		{
			settings = AppSettings.Load();

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
			captureToolbar.ActionRequested += CaptureToolbar_ActionRequested;
			captureToolbar.Visible = false;
			Controls.Add(captureToolbar);
			captureToolbar.BringToFront();

			KeyDown += ScreenshotOverlay_KeyDown;
			MouseDown += ScreenshotOverlay_MouseDown;
			MouseMove += ScreenshotOverlay_MouseMove;
			MouseUp += ScreenshotOverlay_MouseUp;
			Paint += ScreenshotOverlay_Paint;
		}

		private void CaptureToolbar_ActionRequested(object sender, CaptureToolbarAction action)
		{
			switch (action)
			{
				case CaptureToolbarAction.PenMode:
					SetMode(true);
					break;
				case CaptureToolbarAction.RectangleMode:
					SetMode(false);
					break;
				case CaptureToolbarAction.Move:
					SetMoveMode();
					break;
				case CaptureToolbarAction.ColorPicker:
					ShowDrawingColorDialog();
					break;
				case CaptureToolbarAction.Undo:
					UndoLastDrawingLine();
					break;
				case CaptureToolbarAction.Copy:
					CaptureSelectedArea();
					Close();
					break;
				case CaptureToolbarAction.Save:
					SaveSelectedArea();
					Close();
					break;
				case CaptureToolbarAction.Ocr:
					_ = PerformOcr();
					break;
				case CaptureToolbarAction.Scp:
					PerformScp();
					break;
				case CaptureToolbarAction.Close:
					Close();
					break;
			}
		}

		private void SetMode(bool penMode)
		{
			isMoveMode = false;
			isPenMode = penMode;
			captureToolbar.SetPenMode(penMode);
			UpdateCursorForCurrentMode();
			InvalidateSelectionArea();
		}

		private void SetMoveMode()
		{
			isMoveMode = true;
			isPenMode = false;
			captureToolbar.SetMoveMode(true);
			UpdateCursorForCurrentMode();
			InvalidateSelectionArea();
		}

		private void UpdateCursorForCurrentMode()
		{
			if (lastMousePosition != Point.Empty && IsPointInsideSelectionRectangle(lastMousePosition))
			{
				if (isMoveMode)
				{
					Cursor = Cursors.SizeAll;
				}
				else if (isPenMode)
				{
					Cursor = penCursor;
				}
				else
				{
					Cursor = Cursors.Cross;
				}
			}
			else
			{
				Cursor = Cursors.Cross;
			}
		}

		private void ShowDrawingColorDialog()
		{
			using (ColorDialog colorDialog = new ColorDialog())
			{
				colorDialog.Color = currentDrawingColor;
				colorDialog.FullOpen = true;
				colorDialog.AnyColor = true;

				if (colorDialog.ShowDialog() == DialogResult.OK)
				{
					currentDrawingColor = colorDialog.Color;
					captureToolbar.SetDrawingColor(currentDrawingColor);
				}
			}
		}

		private bool HandleShortcut(Keys keyData)
		{
			bool hasSelection = !selectionRectangle.IsEmpty && selectionRectangle.Width > 0 && selectionRectangle.Height > 0;

			if (!CaptureShortcutHandler.TryHandle(
				    keyData,
				    settings,
				    isScreenshotValid,
				    isColorPickerMode,
				    hasSelection,
				    out CaptureShortcutAction action))
			{
				return false;
			}

			switch (action)
			{
				case CaptureShortcutAction.Close:
					Close();
					break;
				case CaptureShortcutAction.Copy:
					CaptureSelectedArea();
					Close();
					break;
				case CaptureShortcutAction.Save:
					SaveSelectedArea();
					Close();
					break;
				case CaptureShortcutAction.Undo:
					UndoLastDrawingLine();
					break;
				case CaptureShortcutAction.Ocr:
					_ = PerformOcr();
					break;
				case CaptureShortcutAction.Scp:
					PerformScp();
					break;
				case CaptureShortcutAction.ActivateColorPicker:
					ActivateColorPicker();
					break;
			}

			return true;
		}

		private void ScreenshotOverlay_KeyDown(object sender, KeyEventArgs e)
		{
			if (HandleShortcut(e.KeyCode | e.Modifiers))
			{
				e.Handled = true;
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (HandleShortcut(keyData))
			{
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void UndoLastDrawingLine()
		{
			if (drawingElements.Count == 0)
			{
				return;
			}

			drawingElements.RemoveAt(drawingElements.Count - 1);
			RebuildAnnotationLayer();
			InvalidateSelectionArea();
		}

		private bool IsPointInsideSelectionRectangle(Point point)
		{
			if (point.IsEmpty || clientSelectionRect.IsEmpty)
			{
				return false;
			}

			return clientSelectionRect.Contains(point);
		}

		private void ScreenshotOverlay_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || !isScreenshotValid)
			{
				return;
			}

			lastMousePosition = e.Location;

			if (isColorPickerMode)
			{
				FinishColorPick();
				return;
			}

			if (isMoveMode &&
			    IsPointInsideSelectionRectangle(e.Location) &&
			    selectionRectangle.Width > 0 &&
			    selectionRectangle.Height > 0)
			{
				isMoving = true;
				isSelecting = false;
				isResizing = false;
				isDrawing = false;
				moveDragOffset = new Point(
					e.Location.X - clientSelectionRect.X,
					e.Location.Y - clientSelectionRect.Y);
				Cursor = Cursors.SizeAll;
				captureToolbar.HideImmediate();
				return;
			}

			int handleIndex = GetHandleIndexAt(e.Location);
			if (!isMoveMode &&
			    handleIndex >= 0 &&
			    selectionRectangle.Width > 0 &&
			    selectionRectangle.Height > 0)
			{
				isResizing = true;
				isSelecting = false;
				isDrawing = false;
				currentHandleIndex = handleIndex;
				SetResizeCursor(handleIndex);
				captureToolbar.HideImmediate();
				return;
			}

			if (!isMoveMode &&
			    IsPointInsideSelectionRectangle(e.Location) &&
			    !isSelecting &&
			    selectionRectangle.Width > 0)
			{
				isDrawing = true;
				currentLine = isPenMode
					? new List<Point> { e.Location }
					: new List<Point> { e.Location, e.Location };
				currentDrawingElement = new DrawingElement(currentLine, isPenMode, currentDrawingColor);
				drawingElements.Add(currentDrawingElement);
				Cursor = isPenMode ? penCursor : Cursors.Cross;
				return;
			}

			if (isMoveMode)
			{
				return;
			}

			isSelecting = true;
			isResizing = false;
			isDrawing = false;
			startPoint = e.Location;
			endPoint = e.Location;
			selectionRectangle = Rectangle.Empty;
			previousSelectionRectangle = Rectangle.Empty;
			clientSelectionRect = Rectangle.Empty;
			resizeHandles.Clear();
			drawingElements.Clear();
			DisposeAnnotationLayer();
			captureToolbar.HideImmediate();
			Invalidate();
		}

		private void ScreenshotOverlay_MouseMove(object sender, MouseEventArgs e)
		{
			if (!isScreenshotValid)
			{
				return;
			}

			if (isColorPickerMode)
			{
				ProcessColorPick(e.Location);
				return;
			}

			if (isMoving)
			{
				Rectangle previous = selectionRectangle;
				MoveSelection(e.Location);
				InvalidateSelectionArea(previous);
				lastMousePosition = e.Location;
				return;
			}

			if (isResizing)
			{
				Rectangle previous = selectionRectangle;
				ResizeSelectionFromHandle(e.Location);
				UpdateClientSelectionRect();
				InvalidateSelectionArea(previous);
				lastMousePosition = e.Location;
				return;
			}

			if (isSelecting)
			{
				endPoint = e.Location;
				Rectangle newSelection = coordinateMapper.CalculateSelectionRectangle(startPoint, endPoint);
				Rectangle invalidationRect = OverlayRenderer.GetSelectionInvalidationRect(
					GetClientSelectionRect(previousSelectionRectangle),
					GetClientSelectionRect(newSelection),
					4);
				previousSelectionRectangle = selectionRectangle;
				selectionRectangle = newSelection;
				UpdateClientSelectionRect();
				Invalidate(invalidationRect);
				return;
			}

			if (isDrawing && currentLine != null && currentDrawingElement != null)
			{
				if (currentDrawingElement.IsPenMode)
				{
					if (currentLine.Count == 0 ||
					    Math.Abs(e.Location.X - currentLine[currentLine.Count - 1].X) > 2 ||
					    Math.Abs(e.Location.Y - currentLine[currentLine.Count - 1].Y) > 2)
					{
						Point previousPoint = currentLine[currentLine.Count - 1];
						currentLine.Add(e.Location);
						DrawSegmentOnAnnotationLayer(previousPoint, e.Location, currentDrawingColor);
						Rectangle dirty = OverlayRenderer.GetDrawingInvalidationRect(e.Location, ImageExporter.DrawingPenSize);
						dirty = Rectangle.Union(dirty, OverlayRenderer.GetDrawingInvalidationRect(previousPoint, ImageExporter.DrawingPenSize));
						Invalidate(dirty);
					}
				}
				else if (currentLine.Count >= 2)
				{
					currentLine[1] = e.Location;
					RebuildAnnotationLayer();
					Rectangle dirty = OverlayRenderer.GetRectangleDrawingInvalidationRect(
						currentLine[0],
						currentLine[1],
						ImageExporter.DrawingPenSize);
					Invalidate(dirty);
				}

				return;
			}

			if (selectionRectangle.Width > 0 && selectionRectangle.Height > 0)
			{
				if (isMoveMode)
				{
					Cursor = IsPointInsideSelectionRectangle(e.Location)
						? Cursors.SizeAll
						: Cursors.Cross;
				}
				else
				{
					int handleIndex = GetHandleIndexAt(e.Location);
					if (handleIndex >= 0)
					{
						SetResizeCursor(handleIndex);
					}
					else if (IsPointInsideSelectionRectangle(e.Location) && isPenMode)
					{
						Cursor = penCursor;
					}
					else
					{
						Cursor = Cursors.Cross;
					}
				}
			}

			lastMousePosition = e.Location;
		}

		private void ScreenshotOverlay_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || !isScreenshotValid)
			{
				return;
			}

			if (isSelecting)
			{
				isSelecting = false;
				endPoint = e.Location;
				selectionRectangle = coordinateMapper.CalculateSelectionRectangle(startPoint, endPoint);

				if (selectionRectangle.Width < 10 || selectionRectangle.Height < 10)
				{
					selectionRectangle = new Rectangle(
						selectionRectangle.X,
						selectionRectangle.Y,
						Math.Max(10, selectionRectangle.Width),
						Math.Max(10, selectionRectangle.Height));
				}

				UpdateClientSelectionRect();
				UpdateResizeHandles();
				RebuildAnnotationLayer();
				UpdateToolbarPosition();
				captureToolbar.SetPenMode(isPenMode);
				captureToolbar.SetDrawingColor(currentDrawingColor);
				captureToolbar.ShowAnimated();
				InvalidateSelectionArea();
				return;
			}

			if (isMoving)
			{
				isMoving = false;
				UpdateResizeHandles();
				UpdateToolbarPosition();
				captureToolbar.ShowAnimated();
				UpdateCursorForCurrentMode();
				return;
			}

			if (isResizing)
			{
				isResizing = false;
				currentHandleIndex = -1;
				UpdateResizeHandles();
				UpdateClientSelectionRect();
				RebuildAnnotationLayer();
				UpdateToolbarPosition();
				captureToolbar.ShowAnimated();
				Cursor = Cursors.Cross;
				return;
			}

			if (isDrawing)
			{
				isDrawing = false;
				currentLine = null;
				currentDrawingElement = null;
			}
		}

		private void UpdateClientSelectionRect()
		{
			if (selectionRectangle.IsEmpty)
			{
				clientSelectionRect = Rectangle.Empty;
				return;
			}

			clientSelectionRect = coordinateMapper.ToClientRect(
				coordinateMapper.ClampToImage(selectionRectangle, screenshotWidth, screenshotHeight));
		}

		private Rectangle GetClientSelectionRect(Rectangle imageSelectionRect)
		{
			if (imageSelectionRect.IsEmpty)
			{
				return Rectangle.Empty;
			}

			return coordinateMapper.ToClientRect(imageSelectionRect);
		}

		private void InvalidateSelectionArea()
		{
			if (clientSelectionRect.IsEmpty)
			{
				Invalidate();
				return;
			}

			Rectangle dirty = OverlayRenderer.GetSelectionInvalidationRect(Rectangle.Empty, clientSelectionRect, 8);
			Invalidate(dirty);
		}

		private void InvalidateSelectionArea(Rectangle previousImageSelection)
		{
			Rectangle previousClient = GetClientSelectionRect(previousImageSelection);
			Rectangle dirty = OverlayRenderer.GetSelectionInvalidationRect(previousClient, clientSelectionRect, 8);
			Invalidate(dirty);
		}

		private void UpdateToolbarPosition()
		{
			if (captureToolbar == null || selectionRectangle.IsEmpty)
			{
				return;
			}

			captureToolbar.Reposition(selectionRectangle, ClientSize, coordinateMapper.OffsetX, coordinateMapper.OffsetY);
		}

		private void RebuildAnnotationLayer()
		{
			DisposeAnnotationLayer();

			if (clientSelectionRect.IsEmpty || clientSelectionRect.Width <= 0 || clientSelectionRect.Height <= 0)
			{
				return;
			}

			annotationLayer = new Bitmap(clientSelectionRect.Width, clientSelectionRect.Height, PixelFormat.Format32bppArgb);

			using (Graphics g = Graphics.FromImage(annotationLayer))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				ImageExporter.DrawAnnotationsOnLayer(g, drawingElements, clientSelectionRect);
			}
		}

		private void DrawSegmentOnAnnotationLayer(Point from, Point to, Color color)
		{
			EnsureAnnotationLayer();

			if (annotationLayer == null)
			{
				return;
			}

			using (Graphics g = Graphics.FromImage(annotationLayer))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				using (Pen pen = new Pen(color, ImageExporter.DrawingPenSize))
				{
					g.DrawLine(
						pen,
						from.X - clientSelectionRect.X,
						from.Y - clientSelectionRect.Y,
						to.X - clientSelectionRect.X,
						to.Y - clientSelectionRect.Y);
				}
			}
		}

		private void EnsureAnnotationLayer()
		{
			if (annotationLayer != null &&
			    annotationLayer.Width == clientSelectionRect.Width &&
			    annotationLayer.Height == clientSelectionRect.Height)
			{
				return;
			}

			RebuildAnnotationLayer();
		}

		private void DisposeAnnotationLayer()
		{
			annotationLayer?.Dispose();
			annotationLayer = null;
		}

		private void UpdateResizeHandles()
		{
			resizeHandles.Clear();

			if (clientSelectionRect.Width <= 0 || clientSelectionRect.Height <= 0)
			{
				return;
			}

			Rectangle adjustedRect = clientSelectionRect;
			adjustedRect.X = Math.Max(HandleSize / 2, Math.Min(adjustedRect.X, Width - HandleSize / 2));
			adjustedRect.Y = Math.Max(HandleSize / 2, Math.Min(adjustedRect.Y, Height - HandleSize / 2));
			adjustedRect.Width = Math.Min(adjustedRect.Width, Width - adjustedRect.X - HandleSize / 2);
			adjustedRect.Height = Math.Min(adjustedRect.Height, Height - adjustedRect.Y - HandleSize / 2);

			if (adjustedRect.Width < 10 || adjustedRect.Height < 10)
			{
				return;
			}

			resizeHandles.Add(new Rectangle(adjustedRect.Left - HandleSize / 2, adjustedRect.Top - HandleSize / 2, HandleSize, HandleSize));
			resizeHandles.Add(new Rectangle(adjustedRect.Left + adjustedRect.Width / 2 - HandleSize / 2, adjustedRect.Top - HandleSize / 2, HandleSize, HandleSize));
			resizeHandles.Add(new Rectangle(adjustedRect.Right - HandleSize / 2, adjustedRect.Top - HandleSize / 2, HandleSize, HandleSize));
			resizeHandles.Add(new Rectangle(adjustedRect.Right - HandleSize / 2, adjustedRect.Top + adjustedRect.Height / 2 - HandleSize / 2, HandleSize, HandleSize));
			resizeHandles.Add(new Rectangle(adjustedRect.Right - HandleSize / 2, adjustedRect.Bottom - HandleSize / 2, HandleSize, HandleSize));
			resizeHandles.Add(new Rectangle(adjustedRect.Left + adjustedRect.Width / 2 - HandleSize / 2, adjustedRect.Bottom - HandleSize / 2, HandleSize, HandleSize));
			resizeHandles.Add(new Rectangle(adjustedRect.Left - HandleSize / 2, adjustedRect.Bottom - HandleSize / 2, HandleSize, HandleSize));
			resizeHandles.Add(new Rectangle(adjustedRect.Left - HandleSize / 2, adjustedRect.Top + adjustedRect.Height / 2 - HandleSize / 2, HandleSize, HandleSize));
		}

		private int GetHandleIndexAt(Point location)
		{
			for (int i = 0; i < resizeHandles.Count; i++)
			{
				if (resizeHandles[i].Contains(location))
				{
					return i;
				}
			}

			return -1;
		}

		private void SetResizeCursor(int handleIndex)
		{
			switch (handleIndex)
			{
				case 0:
					Cursor = Cursors.SizeNWSE;
					break;
				case 1:
					Cursor = Cursors.SizeNS;
					break;
				case 2:
					Cursor = Cursors.SizeNESW;
					break;
				case 3:
					Cursor = Cursors.SizeWE;
					break;
				case 4:
					Cursor = Cursors.SizeNWSE;
					break;
				case 5:
					Cursor = Cursors.SizeNS;
					break;
				case 6:
					Cursor = Cursors.SizeNESW;
					break;
				case 7:
					Cursor = Cursors.SizeWE;
					break;
				default:
					Cursor = Cursors.Cross;
					break;
			}
		}

		private void MoveSelection(Point currentPosition)
		{
			int newClientX = currentPosition.X - moveDragOffset.X;
			int newClientY = currentPosition.Y - moveDragOffset.Y;
			Point imageTopLeft = coordinateMapper.ToImagePoint(new Point(newClientX, newClientY));

			int newX = Math.Max(0, Math.Min(imageTopLeft.X, screenshotWidth - selectionRectangle.Width));
			int newY = Math.Max(0, Math.Min(imageTopLeft.Y, screenshotHeight - selectionRectangle.Height));
			int dx = newX - selectionRectangle.X;
			int dy = newY - selectionRectangle.Y;

			if (dx == 0 && dy == 0)
			{
				return;
			}

			selectionRectangle = new Rectangle(newX, newY, selectionRectangle.Width, selectionRectangle.Height);
			TranslateDrawingElements(dx, dy);
			UpdateClientSelectionRect();
			RebuildAnnotationLayer();
		}

		private void TranslateDrawingElements(int dx, int dy)
		{
			foreach (DrawingElement element in drawingElements)
			{
				for (int i = 0; i < element.Points.Count; i++)
				{
					Point point = element.Points[i];
					element.Points[i] = new Point(point.X + dx, point.Y + dy);
				}
			}
		}

		private void ResizeSelectionFromHandle(Point currentPosition)
		{
			int dx = currentPosition.X - lastMousePosition.X;
			int dy = currentPosition.Y - lastMousePosition.Y;

			Rectangle newRect = new Rectangle(
				selectionRectangle.X,
				selectionRectangle.Y,
				selectionRectangle.Width,
				selectionRectangle.Height);

			switch (currentHandleIndex)
			{
				case 0:
					newRect.X += dx;
					newRect.Y += dy;
					newRect.Width -= dx;
					newRect.Height -= dy;
					break;
				case 1:
					newRect.Y += dy;
					newRect.Height -= dy;
					break;
				case 2:
					newRect.Y += dy;
					newRect.Width += dx;
					newRect.Height -= dy;
					break;
				case 3:
					newRect.Width += dx;
					break;
				case 4:
					newRect.Width += dx;
					newRect.Height += dy;
					break;
				case 5:
					newRect.Height += dy;
					break;
				case 6:
					newRect.X += dx;
					newRect.Width -= dx;
					newRect.Height += dy;
					break;
				case 7:
					newRect.X += dx;
					newRect.Width -= dx;
					break;
			}

			if (newRect.Width < 10)
			{
				if (currentHandleIndex == 0 || currentHandleIndex == 6 || currentHandleIndex == 7)
				{
					newRect.X = selectionRectangle.Right - 10;
				}

				newRect.Width = 10;
			}

			if (newRect.Height < 10)
			{
				if (currentHandleIndex == 0 || currentHandleIndex == 1 || currentHandleIndex == 2)
				{
					newRect.Y = selectionRectangle.Bottom - 10;
				}

				newRect.Height = 10;
			}

			newRect.X = Math.Max(0, Math.Min(newRect.X, screenshotWidth - 10));
			newRect.Y = Math.Max(0, Math.Min(newRect.Y, screenshotHeight - 10));
			newRect.Width = Math.Min(newRect.Width, screenshotWidth - newRect.X);
			newRect.Height = Math.Min(newRect.Height, screenshotHeight - newRect.Y);

			selectionRectangle = newRect;
			UpdateClientSelectionRect();
			UpdateResizeHandles();
		}

		private void ScreenshotOverlay_Paint(object sender, PaintEventArgs e)
		{
			ColorPickerPaintState colorPickerState = new ColorPickerPaintState
			{
				SelectedColor = selectedColor,
				PreviewPoint = colorPickerPoint,
				PreviewBitmap = colorPickerPreview,
				PreviewSize = ColorPickerPreviewSize
			};

			overlayRenderer.Paint(
				e.Graphics,
				coordinateMapper,
				isScreenshotValid,
				isColorPickerMode,
				isPenMode,
				isMoveMode,
				isSelecting,
				selectionRectangle,
				drawingElements,
				resizeHandles,
				annotationLayer,
				clientSelectionRect,
				colorPickerState,
				settings,
				lastMousePosition);
		}

		private Bitmap RenderCurrentSelection(bool includeAnnotations)
		{
			return ImageExporter.RenderSelection(
				screenshot,
				selectionRectangle,
				drawingElements,
				coordinateMapper.OffsetX,
				coordinateMapper.OffsetY,
				includeAnnotations);
		}

		private void CaptureSelectedArea()
		{
			if (!isScreenshotValid || selectionRectangle.IsEmpty)
			{
				return;
			}

			try
			{
				using (Bitmap selectedArea = RenderCurrentSelection(true))
				{
					if (selectedArea == null)
					{
						return;
					}

					ScreenshotCaptured?.Invoke(this, new ScreenshotEventArgs(new Bitmap(selectedArea)));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error capturing area: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void SaveSelectedArea()
		{
			if (!isScreenshotValid || selectionRectangle.IsEmpty)
			{
				return;
			}

			try
			{
				using (Bitmap selectedArea = RenderCurrentSelection(true))
				{
					if (selectedArea == null)
					{
						return;
					}

					ImageExporter.SaveToFile(selectedArea, settings);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error saving area: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private async Task PerformOcr()
		{
			if (!isScreenshotValid || selectionRectangle.IsEmpty)
			{
				MessageBox.Show("Please select a valid area of the image to perform OCR.", "OCR", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			try
			{
				Cursor = Cursors.WaitCursor;

				using (Bitmap selectedArea = RenderCurrentSelection(false))
				{
					if (selectedArea == null)
					{
						return;
					}

					string tempFile = Path.Combine(Path.GetTempPath(), "cloudshot_ocr_temp.png");
					await Task.Run(() => selectedArea.Save(tempFile, ImageFormat.Png));

					try
					{
						string extractedText = await ExtractTextFromImageAsync(tempFile);

						if (!string.IsNullOrWhiteSpace(extractedText))
						{
							Clipboard.SetText(extractedText);
							NotifyTextExtracted(extractedText);
							Close();
						}
						else
						{
							MessageBox.Show(
								"Could not extract text from the selected image.",
								"OCR - No text found",
								MessageBoxButtons.OK,
								MessageBoxIcon.Information);
						}
					}
					finally
					{
						await Task.Run(() =>
						{
							try { File.Delete(tempFile); } catch { }
						});
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error performing OCR: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private async Task<string> ExtractTextFromImageAsync(string imagePath)
		{
			var file = await global::Windows.Storage.StorageFile.GetFileFromPathAsync(imagePath);

			using (var stream = await file.OpenAsync(global::Windows.Storage.FileAccessMode.Read))
			{
				var decoder = await global::Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
				var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

				var ocrEngine = global::Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(
					new global::Windows.Globalization.Language("es-ES"));

				if (ocrEngine == null)
				{
					ocrEngine = global::Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(
						new global::Windows.Globalization.Language("en-US"));
				}

				if (ocrEngine == null)
				{
					ocrEngine = global::Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();
				}

				if (ocrEngine == null)
				{
					throw new Exception("Could not initialize OCR engine. Verify that Windows OCR is installed.");
				}

				var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
				return ocrResult.Text;
			}
		}

		private void PerformScp()
		{
			if (!isScreenshotValid || selectionRectangle.IsEmpty)
			{
				MessageBox.Show("Please select a valid area of the image to upload via SCP.", "SCP", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			if (string.IsNullOrWhiteSpace(settings.ScpHost))
			{
				MessageBox.Show(
					"SCP is not configured.\nOpen Settings and set at least the destination host.",
					"SCP Configuration Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return;
			}

			bool usePassword = !string.IsNullOrWhiteSpace(settings.ScpPassword);
			string keyPath = ExpandUserPath(settings.ScpKeyPath);

			if (!usePassword && !string.IsNullOrWhiteSpace(keyPath) && !File.Exists(keyPath))
			{
				if (MessageBox.Show(
					    $"The specified key file does not exist:\n{keyPath}\n\nDo you want to continue anyway?",
					    "SCP Configuration Warning",
					    MessageBoxButtons.YesNo,
					    MessageBoxIcon.Warning) == DialogResult.No)
				{
					return;
				}
			}

			try
			{
				Cursor = Cursors.WaitCursor;

				string fileName = $"cloudshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
				string tempFile = Path.Combine(Path.GetTempPath(), fileName);

				using (Bitmap selectedArea = RenderCurrentSelection(true))
				{
					if (selectedArea == null)
					{
						return;
					}

					selectedArea.Save(tempFile, ImageFormat.Png);

					int port = settings.ScpPort > 0 ? settings.ScpPort : 22;
					string remotePath = settings.ScpRemotePath ?? "";
					string target = $"{settings.ScpHost}:{remotePath}";

					using (System.Diagnostics.Process process = new System.Diagnostics.Process())
					{
						if (usePassword)
						{
							process.StartInfo.FileName = "pscp";
							process.StartInfo.Arguments =
								$"-pw \"{settings.ScpPassword}\" -P {port} \"{tempFile}\" \"{target}\"";
							process.StartInfo.RedirectStandardInput = true;
						}
						else
						{
							string keyArg = string.IsNullOrWhiteSpace(keyPath) ? "" : $"-i \"{keyPath}\" ";
							process.StartInfo.FileName = "scp";
							process.StartInfo.Arguments =
								$"{keyArg}-P {port} -o StrictHostKeyChecking=no -o BatchMode=yes \"{tempFile}\" \"{target}\"";
						}

						process.StartInfo.UseShellExecute = false;
						process.StartInfo.CreateNoWindow = true;
						process.StartInfo.RedirectStandardOutput = true;
						process.StartInfo.RedirectStandardError = true;
						process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
						process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

						StringBuilder error = new StringBuilder();
						process.ErrorDataReceived += (s, e) =>
						{
							if (!string.IsNullOrEmpty(e.Data))
							{
								error.AppendLine(e.Data);
							}
						};

						try
						{
							process.Start();
						}
						catch (System.ComponentModel.Win32Exception)
						{
							string tool = usePassword ? "pscp (PuTTY)" : "scp (OpenSSH client)";
							MessageBox.Show(
								$"Could not start {tool}.\nMake sure it is installed and available in the system PATH.",
								"SCP Error",
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);
							return;
						}

						process.BeginErrorReadLine();

						if (usePassword)
						{
							process.StandardInput.WriteLine("y");
							process.StandardInput.Close();
						}

						process.WaitForExit();

						if (process.ExitCode == 0)
						{
							if (!string.IsNullOrWhiteSpace(settings.ScpClipboardText) &&
							    settings.ScpClipboardText.Contains("<image>"))
							{
								Clipboard.SetText(settings.ScpClipboardText.Replace("<image>", Path.GetFileName(tempFile)));
							}

							BeginInvoke(new Action(() =>
							{
								Close();
								NotifyScpCompleted(Path.GetFileName(tempFile));
							}));
						}
						else
						{
							string errorMsg = error.ToString().Trim();
							if (string.IsNullOrEmpty(errorMsg))
							{
								errorMsg = "No specific error message received. Please verify the SCP configuration.";
							}

							MessageBox.Show($"Error executing SCP:\n{errorMsg}", "Error SCP", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error performing SCP: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private static string ExpandUserPath(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return path;
			}

			if (path.StartsWith("~"))
			{
				return path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
			}

			return path;
		}

		private void ActivateColorPicker()
		{
			if (!isScreenshotValid)
			{
				return;
			}

			isColorPickerMode = true;
			isSelecting = false;
			isResizing = false;
			isDrawing = false;
			isColorSelected = false;
			captureToolbar.HideImmediate();

			if (colorPickerPreview == null)
			{
				colorPickerPreview = new Bitmap(ColorPickerPreviewSize, ColorPickerPreviewSize);
			}

			Cursor = Cursors.Cross;
			Invalidate();
		}

		private void ProcessColorPick(Point location)
		{
			if (!isColorPickerMode || !isScreenshotValid)
			{
				return;
			}

			Point imagePoint = coordinateMapper.ToImagePoint(location);

			if (imagePoint.X < 0 || imagePoint.X >= screenshotWidth || imagePoint.Y < 0 || imagePoint.Y >= screenshotHeight)
			{
				return;
			}

			selectedColor = BitmapPixelReader.GetPixel(screenshot, imagePoint.X, imagePoint.Y);
			colorPickerPoint = location;
			UpdateColorPickerPreview(location);
			lastMousePosition = location;
			Invalidate();
		}

		private void UpdateColorPickerPreview(Point location)
		{
			if (colorPickerPreview == null || !isScreenshotValid)
			{
				return;
			}

			Point screenshotPoint = coordinateMapper.ToImagePoint(location);
			int previewSourceSize = ColorPickerPreviewSize / ColorPickerZoomFactor;
			int halfSourceSize = previewSourceSize / 2;

			int previewX = Math.Max(0, screenshotPoint.X - halfSourceSize);
			int previewY = Math.Max(0, screenshotPoint.Y - halfSourceSize);
			previewX = Math.Min(previewX, screenshotWidth - previewSourceSize);
			previewY = Math.Min(previewY, screenshotHeight - previewSourceSize);

			Rectangle sourceRect = new Rectangle(
				previewX,
				previewY,
				Math.Min(previewSourceSize, screenshotWidth - previewX),
				Math.Min(previewSourceSize, screenshotHeight - previewY));

			using (Graphics g = Graphics.FromImage(colorPickerPreview))
			{
				g.FillRectangle(Brushes.White, 0, 0, ColorPickerPreviewSize, ColorPickerPreviewSize);
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.PixelOffsetMode = PixelOffsetMode.Half;
				g.DrawImage(
					screenshot,
					new Rectangle(0, 0, ColorPickerPreviewSize, ColorPickerPreviewSize),
					sourceRect,
					GraphicsUnit.Pixel);
			}
		}

		private void FinishColorPick()
		{
			if (!isColorPickerMode || !isScreenshotValid || selectedColor == Color.Empty)
			{
				return;
			}

			try
			{
				string colorString = ColorFormatter.GetColorString(selectedColor, settings.ColorFormat);
				Clipboard.SetText(colorString);
				isColorSelected = true;

				BeginInvoke(new Action(() =>
				{
					Close();
					NotifyColorPicked(selectedColor, colorString, settings.ColorFormat);
				}));
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error copying color: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void NotifyTextExtracted(string text)
		{
			string previewText = text.Length > 50 ? text.Substring(0, 47) + "..." : text;

			foreach (Form form in Application.OpenForms)
			{
				if (form is MainForm mainForm)
				{
					mainForm.ShowNotification("Text extracted", $"The text has been copied to the clipboard:\n{previewText}");
					return;
				}
			}
		}

		private void NotifyScpCompleted(string fileName)
		{
			foreach (Form form in Application.OpenForms)
			{
				if (form is MainForm mainForm)
				{
					string clipboardInfo = string.IsNullOrWhiteSpace(settings.ScpClipboardText)
						? string.Empty
						: "\nThe link has been copied to the clipboard.";

					mainForm.ShowNotification("SCP Upload Complete", $"Image {fileName} uploaded successfully.{clipboardInfo}");
					return;
				}
			}
		}

		private void NotifyColorPicked(Color color, string colorString, string format)
		{
			foreach (Form form in Application.OpenForms)
			{
				if (form is MainForm mainForm)
				{
					mainForm.ShowNotification("Color Picked", $"Color {format}: {colorString}\nCopied to clipboard.");
					return;
				}
			}
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
