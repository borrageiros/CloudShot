using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CloudShot.Core;
using CloudShot.Export;
using CloudShot.Overlay;

namespace CloudShot
{
	public partial class ScreenshotOverlay
	{
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

			if (hasSelection && !isSelecting && CanUseDrawingToolAt(e.Location))
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
					RegisterDrawingElementAdded();
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
				RegisterDrawingElementAdded();
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
					else if (ShouldShowReSelectCursorAt(e.Location))
					{
						Cursor = Cursors.Cross;
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
				InvalidateAnnotationArea();
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
	}
}
