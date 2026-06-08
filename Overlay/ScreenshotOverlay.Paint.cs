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
	}
}
