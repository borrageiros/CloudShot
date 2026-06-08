using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using CloudShot.Core;
using CloudShot.Export;
using CloudShot.Overlay;

namespace CloudShot
{
	public partial class ScreenshotOverlay
	{
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
			RegisterDrawingElementAdded();
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

			if (undoableHistoryCount > drawingElements.Count)
			{
				undoableHistoryCount = drawingElements.Count;
			}

			UpdateNextStepNumber();
			RebuildAnnotationLayer();
			InvalidateAnnotationArea();
		}

		private void RegisterDrawingElementAdded()
		{
			int maxHistory = settings.MaxHistory > 0 ? settings.MaxHistory : 1;
			if (undoableHistoryCount < maxHistory)
			{
				undoableHistoryCount++;
			}
		}

		private void UndoLastDrawingLine()
		{
			CancelTextEditing();

			if (drawingElements.Count == 0 || undoableHistoryCount == 0)
			{
				return;
			}

			drawingElements.RemoveAt(drawingElements.Count - 1);
			undoableHistoryCount--;
			UpdateNextStepNumber();
			RebuildAnnotationLayer();
			InvalidateAnnotationArea();
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

		private bool CanUseDrawingToolAt(Point point)
		{
			if (!settings.ReSelectAreaOnOutsideClick)
			{
				return true;
			}

			return IsPointInsideSelectionRectangle(point);
		}

		private bool ShouldShowReSelectCursorAt(Point point)
		{
			return settings.ReSelectAreaOnOutsideClick && !IsPointInsideSelectionRectangle(point);
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
	}
}
