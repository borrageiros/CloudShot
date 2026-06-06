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

			DrawingToolMode[] drawingModes =
			{
				DrawingToolMode.Pen,
				DrawingToolMode.Eraser,
				DrawingToolMode.Rectangle,
				DrawingToolMode.FilledRectangle,
				DrawingToolMode.Pixelate,
				DrawingToolMode.Arrow,
				DrawingToolMode.Highlighter,
				DrawingToolMode.Line,
				DrawingToolMode.Steps,
				DrawingToolMode.Text
			};

			foreach (DrawingToolMode mode in drawingModes)
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
			switch (mode)
			{
				case DrawingToolMode.Pen:
					return settings.ToolPenEnabled;
				case DrawingToolMode.Rectangle:
					return settings.ToolRectangleEnabled;
				case DrawingToolMode.FilledRectangle:
					return settings.ToolFilledRectangleEnabled;
				case DrawingToolMode.Pixelate:
					return settings.ToolPixelateEnabled;
				case DrawingToolMode.Arrow:
					return settings.ToolArrowEnabled;
				case DrawingToolMode.Highlighter:
					return settings.ToolHighlighterEnabled;
				case DrawingToolMode.Line:
					return settings.ToolLineEnabled;
				case DrawingToolMode.Steps:
					return settings.ToolStepsEnabled;
				case DrawingToolMode.Text:
					return settings.ToolTextEnabled;
				case DrawingToolMode.Eraser:
					return settings.ToolEraserEnabled;
				default:
					return false;
			}
		}

		private void CaptureToolbar_ActionRequested(object sender, CaptureToolbarAction action)
		{
			switch (action)
			{
				case CaptureToolbarAction.PenMode:
					SetDrawingMode(DrawingToolMode.Pen);
					break;
				case CaptureToolbarAction.RectangleMode:
					SetDrawingMode(DrawingToolMode.Rectangle);
					break;
				case CaptureToolbarAction.FilledRectangleMode:
					SetDrawingMode(DrawingToolMode.FilledRectangle);
					break;
				case CaptureToolbarAction.PixelateMode:
					SetDrawingMode(DrawingToolMode.Pixelate);
					break;
				case CaptureToolbarAction.ArrowMode:
					SetDrawingMode(DrawingToolMode.Arrow);
					break;
				case CaptureToolbarAction.HighlighterMode:
					SetDrawingMode(DrawingToolMode.Highlighter);
					break;
				case CaptureToolbarAction.LineMode:
					SetDrawingMode(DrawingToolMode.Line);
					break;
				case CaptureToolbarAction.StepsMode:
					SetDrawingMode(DrawingToolMode.Steps);
					break;
				case CaptureToolbarAction.TextMode:
					SetDrawingMode(DrawingToolMode.Text);
					break;
				case CaptureToolbarAction.EraserMode:
					SetDrawingMode(DrawingToolMode.Eraser);
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

		private void SetDrawingMode(DrawingToolMode mode)
		{
			CancelTextEditing();
			if (currentDrawingMode == DrawingToolMode.Eraser && mode != DrawingToolMode.Eraser)
			{
				InvalidateEraserPreview(lastMousePosition);
			}

			isMoveMode = false;
			currentDrawingMode = mode;
			captureToolbar.SetDrawingMode(mode);
			UpdateCursorForCurrentMode();
			InvalidateSelectionArea();
		}

		private void SetMoveMode()
		{
			CancelTextEditing();
			if (currentDrawingMode == DrawingToolMode.Eraser)
			{
				InvalidateEraserPreview(lastMousePosition);
			}

			isMoveMode = true;
			currentDrawingMode = DrawingToolMode.Pen;
			captureToolbar.SetMoveMode(true);
			UpdateCursorForCurrentMode();
			InvalidateSelectionArea();
		}

		private void UpdateCursorForCurrentMode()
		{
			if (isMoveMode)
			{
				Cursor = lastMousePosition != Point.Empty && IsPointInsideSelectionRectangle(lastMousePosition)
					? Cursors.SizeAll
					: Cursors.Cross;
				return;
			}

			switch (currentDrawingMode)
			{
				case DrawingToolMode.Pen:
					Cursor = penCursor;
					break;
				case DrawingToolMode.Text:
					Cursor = Cursors.IBeam;
					break;
				case DrawingToolMode.Eraser:
					Cursor = Cursors.Default;
					break;
				default:
					Cursor = Cursors.Cross;
					break;
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

		private static Color ParseColorOrDefault(string value, Color fallback)
		{
			if (string.IsNullOrWhiteSpace(value))
				return fallback;

			try
			{
				return ColorTranslator.FromHtml(value);
			}
			catch
			{
				return fallback;
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
				    activeTextEditor == null,
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
				case CaptureShortcutAction.PenTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Pen))
						SetDrawingMode(DrawingToolMode.Pen);
					break;
				case CaptureShortcutAction.RectangleTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Rectangle))
						SetDrawingMode(DrawingToolMode.Rectangle);
					break;
				case CaptureShortcutAction.FilledRectangleTool:
					if (IsDrawingToolEnabled(DrawingToolMode.FilledRectangle))
						SetDrawingMode(DrawingToolMode.FilledRectangle);
					break;
				case CaptureShortcutAction.PixelateTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Pixelate))
						SetDrawingMode(DrawingToolMode.Pixelate);
					break;
				case CaptureShortcutAction.ArrowTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Arrow))
						SetDrawingMode(DrawingToolMode.Arrow);
					break;
				case CaptureShortcutAction.HighlighterTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Highlighter))
						SetDrawingMode(DrawingToolMode.Highlighter);
					break;
				case CaptureShortcutAction.LineTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Line))
						SetDrawingMode(DrawingToolMode.Line);
					break;
				case CaptureShortcutAction.StepsTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Steps))
						SetDrawingMode(DrawingToolMode.Steps);
					break;
				case CaptureShortcutAction.TextTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Text))
						SetDrawingMode(DrawingToolMode.Text);
					break;
				case CaptureShortcutAction.EraserTool:
					if (IsDrawingToolEnabled(DrawingToolMode.Eraser))
						SetDrawingMode(DrawingToolMode.Eraser);
					break;
				case CaptureShortcutAction.MoveTool:
					if (settings.ToolMoveEnabled)
						SetMoveMode();
					break;
			}

			return true;
		}

		private void ScreenshotOverlay_KeyDown(object sender, KeyEventArgs e)
		{
			if (activeTextEditor != null && e.KeyCode == Keys.Escape)
			{
				CancelTextEditing();
				e.Handled = true;
				return;
			}

			if (HandleShortcut(e.KeyCode | e.Modifiers))
			{
				e.Handled = true;
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (activeTextEditor != null && (keyData & Keys.KeyCode) == Keys.Escape)
			{
				CancelTextEditing();
				return true;
			}

			if (HandleShortcut(keyData))
			{
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void BeginTextEditing(Point clientPoint, Point imagePoint)
		{
			activeTextEditorImagePoint = imagePoint;
			activeTextEditor = new TextBox
			{
				BorderStyle = BorderStyle.FixedSingle,
				BackColor = Color.White,
				ForeColor = currentDrawingColor,
				Font = ImageExporter.GetTextFont(),
				Location = clientPoint,
				Width = 200,
				Height = 28
			};
			activeTextEditor.KeyDown += ActiveTextEditor_KeyDown;
			activeTextEditor.LostFocus += ActiveTextEditor_LostFocus;
			Controls.Add(activeTextEditor);
			activeTextEditor.BringToFront();
			captureToolbar.BringToFront();
			activeTextEditor.Focus();
		}

		private void ActiveTextEditor_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				e.SuppressKeyPress = true;
				CommitTextEditing();
			}
			else if (e.KeyCode == Keys.Escape)
			{
				e.SuppressKeyPress = true;
				CancelTextEditing();
			}
		}

		private void ActiveTextEditor_LostFocus(object sender, EventArgs e)
		{
			BeginInvoke(new Action(CommitTextEditing));
		}

		private void CommitTextEditing()
		{
			if (activeTextEditor == null)
			{
				return;
			}

			string text = activeTextEditor.Text?.Trim();
			Point imagePoint = activeTextEditorImagePoint;
			RemoveActiveTextEditor();

			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			DrawingElement textElement = new DrawingElement(
				new List<Point> { imagePoint },
				DrawingToolMode.Text,
				currentDrawingColor)
			{
				Text = text
			};
			drawingElements.Add(textElement);
			DrawElementOnAnnotationLayer(textElement);
			Point clientPoint = coordinateMapper.ToClientPoint(imagePoint);
			Invalidate(OverlayRenderer.GetTextInvalidationRect(clientPoint, text));
		}

		private void CancelTextEditing()
		{
			if (activeTextEditor == null)
			{
				return;
			}

			RemoveActiveTextEditor();
		}

		private void RemoveActiveTextEditor()
		{
			if (activeTextEditor == null)
			{
				return;
			}

			Font editorFont = activeTextEditor.Font;
			activeTextEditor.KeyDown -= ActiveTextEditor_KeyDown;
			activeTextEditor.LostFocus -= ActiveTextEditor_LostFocus;
			Controls.Remove(activeTextEditor);
			activeTextEditor.Dispose();
			activeTextEditor = null;

			if (editorFont != null && editorFont != Font)
			{
				editorFont.Dispose();
			}
		}

		private void InvalidateEraserPreview(Point center)
		{
			if (center.IsEmpty)
			{
				return;
			}

			Invalidate(OverlayRenderer.GetEraserInvalidationRect(center, AnnotationEraser.EraserRadius));
		}

		private void UpdateEraserPreviewPosition(Point newLocation)
		{
			Point previous = lastMousePosition;
			if (previous == newLocation)
			{
				return;
			}

			lastMousePosition = newLocation;
			InvalidateEraserPreview(previous);
			InvalidateEraserPreview(newLocation);
		}

		private void ApplyEraserAt(Point imagePoint)
		{
			if (!AnnotationEraser.Apply(drawingElements, imagePoint))
			{
				return;
			}

			UpdateNextStepNumber();
			RebuildAnnotationLayer();
			InvalidateSelectionArea();
		}

		private void UndoLastDrawingLine()
		{
			CancelTextEditing();

			if (drawingElements.Count == 0)
			{
				return;
			}

			drawingElements.RemoveAt(drawingElements.Count - 1);
			UpdateNextStepNumber();
			RebuildAnnotationLayer();
			InvalidateSelectionArea();
		}

		private void UpdateNextStepNumber()
		{
			nextStepNumber = 1;
			foreach (DrawingElement element in drawingElements)
			{
				if (element.Mode == DrawingToolMode.Steps && element.StepNumber >= nextStepNumber)
				{
					nextStepNumber = element.StepNumber + 1;
				}
			}
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

			bool hasSelection = selectionRectangle.Width > 0 && selectionRectangle.Height > 0;

			if (isMoveMode)
			{
				if (hasSelection && IsPointInsideSelectionRectangle(e.Location))
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
				}

				return;
			}

			int handleIndex = GetHandleIndexAt(e.Location);
			if (handleIndex >= 0 && hasSelection)
			{
				isResizing = true;
				isSelecting = false;
				isDrawing = false;
				currentHandleIndex = handleIndex;
				SetResizeCursor(handleIndex);
				captureToolbar.HideImmediate();
				return;
			}

			if (hasSelection && !isSelecting)
			{
				Point imagePoint = coordinateMapper.ToImagePoint(e.Location);

				if (currentDrawingMode == DrawingToolMode.Steps)
				{
					DrawingElement stepElement = new DrawingElement(
						new List<Point> { imagePoint },
						DrawingToolMode.Steps,
						currentDrawingColor)
					{
						StepNumber = nextStepNumber++
					};
					drawingElements.Add(stepElement);
					DrawElementOnAnnotationLayer(stepElement);
					Invalidate(OverlayRenderer.GetStepInvalidationRect(e.Location));
					return;
				}

				if (currentDrawingMode == DrawingToolMode.Text)
				{
					if (activeTextEditor != null)
					{
						CommitTextEditing();
					}

					BeginTextEditing(e.Location, imagePoint);
					return;
				}

				if (currentDrawingMode == DrawingToolMode.Eraser)
				{
					isErasing = true;
					lastEraserImagePoint = imagePoint;
					ApplyEraserAt(imagePoint);
					return;
				}

				isDrawing = true;
				currentLine = currentDrawingMode == DrawingToolMode.Pen
					? new List<Point> { imagePoint }
					: new List<Point> { imagePoint, imagePoint };
				currentDrawingElement = new DrawingElement(currentLine, currentDrawingMode, currentDrawingColor);
				drawingElements.Add(currentDrawingElement);
				rectanglePreviewInvalidationBounds = Rectangle.Empty;
				Cursor = currentDrawingMode == DrawingToolMode.Pen ? penCursor : Cursors.Cross;
				return;
			}

			CancelTextEditing();
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
			nextStepNumber = 1;
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

			if (isErasing)
			{
				Point imagePoint = coordinateMapper.ToImagePoint(e.Location);
				if (lastEraserImagePoint.IsEmpty ||
				    Math.Abs(imagePoint.X - lastEraserImagePoint.X) > 2 ||
				    Math.Abs(imagePoint.Y - lastEraserImagePoint.Y) > 2)
				{
					ApplyEraserAt(imagePoint);
					lastEraserImagePoint = imagePoint;
				}

				UpdateEraserPreviewPosition(e.Location);
				return;
			}

			if (isDrawing && currentLine != null && currentDrawingElement != null)
			{
				if (currentDrawingElement.IsPenMode)
				{
					Point imagePoint = coordinateMapper.ToImagePoint(e.Location);
					if (currentLine.Count == 0 ||
					    Math.Abs(imagePoint.X - currentLine[currentLine.Count - 1].X) > 2 ||
					    Math.Abs(imagePoint.Y - currentLine[currentLine.Count - 1].Y) > 2)
					{
						Point previousImagePoint = currentLine[currentLine.Count - 1];
						currentLine.Add(imagePoint);
						DrawSegmentOnAnnotationLayer(previousImagePoint, imagePoint, currentDrawingColor);
						Point previousClientPoint = coordinateMapper.ToClientPoint(previousImagePoint);
						Rectangle dirty = OverlayRenderer.GetDrawingInvalidationRect(e.Location, ImageExporter.DrawingPenSize);
						dirty = Rectangle.Union(dirty, OverlayRenderer.GetDrawingInvalidationRect(previousClientPoint, ImageExporter.DrawingPenSize));
						Invalidate(dirty);
					}
				}
				else if (currentDrawingElement.IsTwoPointDragMode && currentLine.Count >= 2)
				{
					currentLine[1] = coordinateMapper.ToImagePoint(e.Location);
					Point clientStart = coordinateMapper.ToClientPoint(currentLine[0]);
					Rectangle newBounds = ImageExporter.GetTwoPointDragInvalidationRect(
						clientStart,
						e.Location,
						currentDrawingMode);
					rectanglePreviewInvalidationBounds = rectanglePreviewInvalidationBounds.IsEmpty
						? newBounds
						: Rectangle.Union(rectanglePreviewInvalidationBounds, newBounds);
					Invalidate(rectanglePreviewInvalidationBounds);
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
					else if (currentDrawingMode == DrawingToolMode.Pen)
					{
						Cursor = penCursor;
					}
					else if (currentDrawingMode == DrawingToolMode.Text)
					{
						Cursor = Cursors.IBeam;
					}
					else if (currentDrawingMode == DrawingToolMode.Eraser)
					{
						Cursor = Cursors.Default;
					}
					else
					{
						Cursor = Cursors.Cross;
					}
				}
			}

			if (currentDrawingMode == DrawingToolMode.Eraser &&
			    !isMoveMode &&
			    selectionRectangle.Width > 0 &&
			    selectionRectangle.Height > 0)
			{
				UpdateEraserPreviewPosition(e.Location);
				return;
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
				captureToolbar.SetDrawingMode(currentDrawingMode);
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
				UpdateToolbarPosition();
				captureToolbar.ShowAnimated();
				Cursor = Cursors.Cross;
				return;
			}

			if (isErasing)
			{
				isErasing = false;
				lastEraserImagePoint = Point.Empty;
				return;
			}

			if (isDrawing)
			{
				DrawingElement completedElement = currentDrawingElement;
				isDrawing = false;
				currentLine = null;
				currentDrawingElement = null;

				if (completedElement != null && completedElement.IsTwoPointDragMode)
				{
					Point clientStart = coordinateMapper.ToClientPoint(completedElement.Points[0]);
					Point clientEnd = coordinateMapper.ToClientPoint(completedElement.Points[1]);
					Rectangle finalBounds = ImageExporter.GetTwoPointDragInvalidationRect(
						clientStart,
						clientEnd,
						completedElement.Mode);
					DrawElementOnAnnotationLayer(completedElement);
					Rectangle dirty = rectanglePreviewInvalidationBounds.IsEmpty
						? finalBounds
						: Rectangle.Union(rectanglePreviewInvalidationBounds, finalBounds);
					Invalidate(dirty);
				}

				rectanglePreviewInvalidationBounds = Rectangle.Empty;
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

			if (screenshotWidth <= 0 || screenshotHeight <= 0)
			{
				return;
			}

			annotationLayer = new Bitmap(screenshotWidth, screenshotHeight, PixelFormat.Format32bppArgb);

			using (Graphics g = Graphics.FromImage(annotationLayer))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				ImageExporter.DrawElementsInImageSpace(g, drawingElements, screenshot);
			}
		}

		private void DrawElementOnAnnotationLayer(DrawingElement element)
		{
			EnsureAnnotationLayer();

			if (annotationLayer == null)
			{
				return;
			}

			using (Graphics g = Graphics.FromImage(annotationLayer))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				ImageExporter.DrawElementInImageSpace(g, element, screenshot);
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
				g.SmoothingMode = SmoothingMode.HighSpeed;
				g.CompositingQuality = CompositingQuality.HighSpeed;
				using (Pen pen = new Pen(color, ImageExporter.DrawingPenSize))
				{
					g.DrawLine(pen, from, to);
				}
			}
		}

		private void EnsureAnnotationLayer()
		{
			if (annotationLayer != null &&
			    annotationLayer.Width == screenshotWidth &&
			    annotationLayer.Height == screenshotHeight)
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
			UpdateClientSelectionRect();
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

			DrawingElement inProgressDrawing = isDrawing && currentDrawingElement != null && currentDrawingElement.IsTwoPointDragMode
				? currentDrawingElement
				: null;

			overlayRenderer.Paint(
				e.Graphics,
				coordinateMapper,
				isScreenshotValid,
				isColorPickerMode,
				currentDrawingMode,
				isMoveMode,
				isSelecting,
				selectionRectangle,
				drawingElements,
				resizeHandles,
				annotationLayer,
				clientSelectionRect,
				inProgressDrawing,
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

			string tempFile = null;
			bool uploadScheduled = false;

			try
			{
				string fileName = $"cloudshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
				tempFile = Path.Combine(Path.GetTempPath(), fileName);

				using (Bitmap selectedArea = RenderCurrentSelection(true))
				{
					if (selectedArea == null)
					{
						return;
					}

					selectedArea.Save(tempFile, ImageFormat.Png);
				}

				string host = settings.ScpHost;
				int port = settings.ScpPort;
				string remotePath = settings.ScpRemotePath;
				string keyPath = settings.ScpKeyPath;
				string keyPassphrase = settings.ScpKeyPassphrase;
				string clipboardText = settings.ScpClipboardText;
				string fileToUpload = tempFile;

				Close();
				uploadScheduled = true;

				Task.Run(() =>
				{
					ScpUploadResult result = new ScpUploadService().Upload(
						fileToUpload, host, port, remotePath, keyPath, keyPassphrase);

					foreach (Form form in Application.OpenForms)
					{
						if (form is MainForm mainForm)
						{
							mainForm.BeginInvoke(new Action(() =>
							{
								try
								{
									if (result.Success)
									{
										if (!string.IsNullOrWhiteSpace(clipboardText) &&
										    clipboardText.Contains("<image>"))
										{
											Clipboard.SetText(clipboardText.Replace("<image>", fileName));
										}

										NotifyScpCompleted(fileName, clipboardText);
									}
									else
									{
										NotifyScpFailed(result.ErrorMessage);
									}
								}
								finally
								{
									DeleteTempFile(fileToUpload);
								}
							}));
							return;
						}
					}

					DeleteTempFile(fileToUpload);
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error performing SCP: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				if (!uploadScheduled)
				{
					DeleteTempFile(tempFile);
				}
			}
		}

		private void ActivateColorPicker()
		{
			if (!isScreenshotValid)
			{
				return;
			}

			CancelTextEditing();
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

		private static void NotifyScpCompleted(string fileName, string scpClipboardText)
		{
			foreach (Form form in Application.OpenForms)
			{
				if (form is MainForm mainForm)
				{
					string clipboardInfo = string.IsNullOrWhiteSpace(scpClipboardText)
						? string.Empty
						: "\nThe link has been copied to the clipboard.";

					mainForm.ShowNotification("SCP Upload Complete", $"Image {fileName} uploaded successfully.{clipboardInfo}");
					return;
				}
			}
		}

		private static void NotifyScpFailed(string errorMessage)
		{
			foreach (Form form in Application.OpenForms)
			{
				if (form is MainForm mainForm)
				{
					MessageBox.Show(mainForm, $"Error uploading via SCP:\n{errorMessage}", "SCP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}
		}

		private static void DeleteTempFile(string path)
		{
			if (path == null)
			{
				return;
			}

			try
			{
				File.Delete(path);
			}
			catch
			{
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
