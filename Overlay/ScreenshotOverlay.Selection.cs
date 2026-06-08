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

		private void InvalidateAnnotationArea()
		{
			if (!isScreenshotValid || screenshotWidth <= 0 || screenshotHeight <= 0)
			{
				Invalidate();
				return;
			}

			Invalidate(coordinateMapper.ToClientRect(new Rectangle(0, 0, screenshotWidth, screenshotHeight)));
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
	}
}
