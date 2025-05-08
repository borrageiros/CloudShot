using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CloudShot
{
	public class ScreenshotOverlay : Form
	{
		public event EventHandler<ScreenshotEventArgs> ScreenshotCaptured;

		private Bitmap screenshot;
		private Point startPoint;
		private Point endPoint;
		private bool isSelecting = false;
		private Rectangle selectionRectangle = Rectangle.Empty;
		private bool isScreenshotValid = false;
		private int screenshotWidth = 0;
		private int screenshotHeight = 0;
		private Rectangle totalScreenBounds;

		// To handle the resizing controllers
		private const int HandleSize = 8;
		private List<Rectangle> resizeHandles = new List<Rectangle>();
		private bool isResizing = false;
		private int currentHandleIndex = -1;
		private Point lastMousePosition = Point.Empty;
		private Rectangle originalSelectionRect;

		// To draw within the selected area
		private bool isDrawing = false;
		private List<DrawingElement> drawingElements = new List<DrawingElement>();
		private List<Point> currentLine = null;
		private const int DrawingPenSize = 3;

		// Color Picker related fields
		private bool isColorPickerMode = false;
		private Bitmap colorPickerPreview = null;
		private const int ColorPickerPreviewSize = 150;
		private const int ColorPickerZoomFactor = 3; // Zoom factor for pixel preview
		private Color selectedColor = Color.Empty;
		private Point colorPickerPoint = Point.Empty;
		private bool isColorSelected = false;

		// Class to store drawing elements with their type
		private class DrawingElement
		{
			public List<Point> Points { get; set; }
			public bool IsPenMode { get; set; }
			public Color DrawingColor { get; set; }

			public DrawingElement(List<Point> points, bool isPenMode, Color color)
			{
				Points = points;
				IsPenMode = isPenMode;
				DrawingColor = color;
			}
		}

		// Custom cursors
		private Cursor penCursor = null;

		// Variables for selection modes
		private bool isPenMode = true;
		private PictureBox penModeButton;
		private PictureBox rectangleModeButton;
		private PictureBox colorPickerButton;
		private Panel buttonsPanel;
		private const int ButtonSize = 40;
		private const int ButtonSpacing = 10;
		private const int ButtonMargin = 10;

		// Current color for drawing
		private Color currentDrawingColor = Color.Red;

		private AppSettings settings;

		// Add at the beginning of the class
		private DateTime _lastButtonUpdate = DateTime.Now;

		public ScreenshotOverlay(Bitmap screenshot)
		{
			try
			{
				// Load configuration
				settings = AppSettings.Load();

				// Ensure the OCR shortcut is correctly configured
				EnsureOcrShortcutIsSet();

				// Print diagnostic
				PrintSettingsDiagnostic();

				// Initialize lists and variables to avoid null references
				resizeHandles = new List<Rectangle>();
				drawingElements = new List<DrawingElement>();
				lastMousePosition = Point.Empty;
				selectionRectangle = Rectangle.Empty;

				// Save a copy of the image
				if (screenshot != null && screenshot.Width > 0 && screenshot.Height > 0)
				{
					this.screenshot = new Bitmap(screenshot);
					this.screenshotWidth = screenshot.Width;
					this.screenshotHeight = screenshot.Height;
					this.isScreenshotValid = true;

					// Get the total area of all screens
					this.totalScreenBounds = GetTotalScreenBounds();
				}
				else
				{
					this.isScreenshotValid = false;
					MessageBox.Show("Could not obtain a valid screenshot.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}

				// Create custom cursors
				CreateCustomCursors();

				// Initialize the interface components
				InitializeComponents();

				// Set the initial state when the components have been initialized
				SetInitialState();
			}
			catch (Exception ex)
			{
				this.isScreenshotValid = false;
				MessageBox.Show($"Error creating screenshot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void EnsureOcrShortcutIsSet()
		{
			// Verify if the OCR shortcut is configured correctly
			if (settings.OcrShortcut != (Keys.Control | Keys.R))
			{
				Console.WriteLine($"Correcting OCR shortcut value. Current value: {settings.OcrShortcut}");
				settings.OcrShortcut = Keys.Control | Keys.R;

				try
				{
					// Save the corrected configuration
					settings.Save();
					Console.WriteLine("Configuration updated and saved");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error saving corrected settings: {ex.Message}");
				}
			}
		}

		private void CreateCustomCursors()
		{
			try
			{
				// Use a cross cursor as a pen (because Cursors.Pen does not exist)
				penCursor = Cursors.Hand;
			}
			catch
			{
				// If there is any error, use a cross cursor
				penCursor = Cursors.Cross;
			}
		}

		private Rectangle GetTotalScreenBounds()
		{
			// Calculate a rectangle that contains all screens
			int left = int.MaxValue;
			int top = int.MaxValue;
			int right = int.MinValue;
			int bottom = int.MinValue;

			foreach (Screen screen in Screen.AllScreens)
			{
				Rectangle bounds = screen.Bounds;

				left = Math.Min(left, bounds.Left);
				top = Math.Min(top, bounds.Top);
				right = Math.Max(right, bounds.Right);
				bottom = Math.Max(bottom, bounds.Bottom);
			}

			return new Rectangle(left, top, right - left, bottom - top);
		}

		private void InitializeComponents()
		{
			try
			{
				// Configure form properties
				this.FormBorderStyle = FormBorderStyle.None;
				this.StartPosition = FormStartPosition.Manual;
				this.TopMost = true;
				this.Cursor = Cursors.Cross;
				this.BackColor = Color.Black;
				this.Opacity = 0.5;
				this.ShowInTaskbar = false;
				this.DoubleBuffered = true;
				this.KeyPreview = true;

				// Configure to show on all screens
				if (isScreenshotValid)
				{
					this.Bounds = totalScreenBounds;
				}
				else
				{
					// Fallback to main screen if an error occurred
					this.WindowState = FormWindowState.Maximized;
				}

				try
				{
					// Create the mode buttons using an auxiliary method
					CreateModeButtons();

					// Add the buttons panel to the form
					if (buttonsPanel != null)
					{
						this.Controls.Add(buttonsPanel);
						buttonsPanel.BringToFront();
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error creating buttons: {ex.Message}");
				}

				// Configure events
				this.KeyDown += ScreenshotOverlay_KeyDown;
				this.MouseDown += ScreenshotOverlay_MouseDown;
				this.MouseMove += ScreenshotOverlay_MouseMove;
				this.MouseUp += ScreenshotOverlay_MouseUp;
				this.Paint += ScreenshotOverlay_Paint;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error initializing interface: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CreateModeButtons()
		{
			// Configure a vertical panel with solid color and rounded corners
			buttonsPanel = new Panel();
			buttonsPanel.Size = new Size(ButtonSize + 20, ButtonSize * 3 + ButtonSpacing * 2 + 20);
			buttonsPanel.Location = new Point(10, 10);
			buttonsPanel.BackColor = ColorTranslator.FromHtml("#212121"); // Solid color
			buttonsPanel.BorderStyle = BorderStyle.None;
			buttonsPanel.Padding = new Padding(5);

			// For rounded corners, we use a region
			int radius = 10; // Radius of the rounded corners
			using (GraphicsPath path = new GraphicsPath())
			{
				path.AddArc(0, 0, radius, radius, 180, 90);
				path.AddArc(buttonsPanel.Width - radius, 0, radius, radius, 270, 90);
				path.AddArc(buttonsPanel.Width - radius, buttonsPanel.Height - radius, radius, radius, 0, 90);
				path.AddArc(0, buttonsPanel.Height - radius, radius, radius, 90, 90);
				path.CloseAllFigures();
				buttonsPanel.Region = new Region(path);
			}

			// Hide the panel initially
			buttonsPanel.Visible = false;

			// Color that we will use for the icons
			Color iconColor = ColorTranslator.FromHtml("#adadad");

			// Create and configure the pen mode button (top)
			penModeButton = new PictureBox();
			penModeButton.Size = new Size(ButtonSize, ButtonSize);
			penModeButton.Location = new Point(10, 10);
			penModeButton.BorderStyle = BorderStyle.None;
			penModeButton.BackColor = Color.Transparent;
			penModeButton.Image = CreatePenModeImage(true, iconColor);
			penModeButton.SizeMode = PictureBoxSizeMode.StretchImage;
			penModeButton.Click += (s, e) => SetMode(true);
			penModeButton.Cursor = Cursors.Hand;

			// Create and configure the rectangle mode button (middle)
			rectangleModeButton = new PictureBox();
			rectangleModeButton.Size = new Size(ButtonSize, ButtonSize);
			rectangleModeButton.Location = new Point(10, 10 + ButtonSize + ButtonSpacing);
			rectangleModeButton.BorderStyle = BorderStyle.None;
			rectangleModeButton.BackColor = Color.Transparent;
			rectangleModeButton.Image = CreateRectangleModeImage(false, iconColor);
			rectangleModeButton.SizeMode = PictureBoxSizeMode.StretchImage;
			rectangleModeButton.Click += (s, e) => SetMode(false);
			rectangleModeButton.Cursor = Cursors.Hand;

			// Create and configure the color picker button (bottom)
			colorPickerButton = new PictureBox();
			colorPickerButton.Size = new Size(ButtonSize, ButtonSize);
			colorPickerButton.Location = new Point(10, 10 + (ButtonSize + ButtonSpacing) * 2);
			colorPickerButton.BorderStyle = BorderStyle.None;
			colorPickerButton.BackColor = Color.Transparent;
			colorPickerButton.Image = CreateColorPickerImage(currentDrawingColor);
			colorPickerButton.SizeMode = PictureBoxSizeMode.StretchImage;
			colorPickerButton.Click += (s, e) => ShowColorPicker();
			colorPickerButton.Cursor = Cursors.Hand;

			// Add the buttons to the panel
			buttonsPanel.Controls.Add(penModeButton);
			buttonsPanel.Controls.Add(rectangleModeButton);
			buttonsPanel.Controls.Add(colorPickerButton);
		}

		private Bitmap CreatePenModeImage(bool active, Color iconColor)
		{
			Bitmap bmp = new Bitmap(ButtonSize, ButtonSize);
			using (Graphics g = Graphics.FromImage(bmp))
			{
				// Transparent background
				g.Clear(Color.Transparent);

				// Create a circular button background
				using (SolidBrush bgBrush = new SolidBrush(active ?
						Color.FromArgb(240, 30, 160, 70) : Color.FromArgb(180, 60, 60, 60)))
				{
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.FillEllipse(bgBrush, 2, 2, ButtonSize - 4, ButtonSize - 4);
				}

				// Button border
				using (Pen borderPen = new Pen(active ?
						Color.FromArgb(240, 40, 200, 90) : Color.FromArgb(120, 150, 150, 150), 2))
				{
					g.DrawEllipse(borderPen, 2, 2, ButtonSize - 4, ButtonSize - 4);
				}

				// Pen icon drawing
				using (Pen pen = new Pen(iconColor, 2.5f))
				{
					pen.StartCap = LineCap.Round;
					pen.EndCap = LineCap.Round;

					// Draw the pen
					g.DrawLine(pen, 12, 12, 28, 28);
					g.DrawLine(pen, 14, 28, 20, 20);
					g.DrawLine(pen, 22, 22, 28, 14);
				}
			}
			return bmp;
		}

		private Bitmap CreateRectangleModeImage(bool active, Color iconColor)
		{
			Bitmap bmp = new Bitmap(ButtonSize, ButtonSize);
			using (Graphics g = Graphics.FromImage(bmp))
			{
				// Transparent background
				g.Clear(Color.Transparent);

				// Create a circular button background
				using (SolidBrush bgBrush = new SolidBrush(active ?
						Color.FromArgb(240, 30, 160, 70) : Color.FromArgb(180, 60, 60, 60)))
				{
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.FillEllipse(bgBrush, 2, 2, ButtonSize - 4, ButtonSize - 4);
				}

				// Button border
				using (Pen borderPen = new Pen(active ?
						Color.FromArgb(240, 40, 200, 90) : Color.FromArgb(120, 150, 150, 150), 2))
				{
					g.DrawEllipse(borderPen, 2, 2, ButtonSize - 4, ButtonSize - 4);
				}

				// Rectangle icon drawing
				using (Pen pen = new Pen(iconColor, 2.5f))
				{
					pen.StartCap = LineCap.Round;
					pen.EndCap = LineCap.Round;
					pen.LineJoin = LineJoin.Round;

					g.SmoothingMode = SmoothingMode.AntiAlias;

					// Draw a rectangle
					Rectangle rect = new Rectangle(10, 10, 20, 20);
					g.DrawRectangle(pen, rect);
				}
			}
			return bmp;
		}

		private void SetMode(bool penMode)
		{
			try
			{
				// Update the mode state
				isPenMode = penMode;

				// Update the appearance of the buttons according to the active mode
				if (penModeButton != null && rectangleModeButton != null)
				{
					// Release resources of the previous images
					if (penModeButton.Image != null)
					{
						Image oldPenImage = penModeButton.Image;
						penModeButton.Image = null;
						oldPenImage.Dispose();
					}

					if (rectangleModeButton.Image != null)
					{
						Image oldRectImage = rectangleModeButton.Image;
						rectangleModeButton.Image = null;
						oldRectImage.Dispose();
					}

					// Color for the icons
					Color iconColor = ColorTranslator.FromHtml("#adadad");

					// Recreate the images with the correct states
					penModeButton.Image = CreatePenModeImage(isPenMode, iconColor);
					rectangleModeButton.Image = CreateRectangleModeImage(!isPenMode, iconColor);

					// Verify changes
					Console.WriteLine($"Mode changed: Pen={isPenMode}, Rectangle={!isPenMode}");

					// Bring the buttons to the front to ensure they are visible
					penModeButton.BringToFront();
					rectangleModeButton.BringToFront();
				}

				// Update the cursor according to the mode
				if (penMode && lastMousePosition != Point.Empty)
				{
					try
					{
						this.Cursor = IsPointInsideSelectionRectangle(lastMousePosition) ? penCursor : Cursors.Cross;
					}
					catch
					{
						this.Cursor = Cursors.Cross;
					}
				}
				else
				{
					this.Cursor = Cursors.Cross;
				}

				// Update the display
				this.Invalidate();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error changing mode: {ex.Message}");
				this.Cursor = Cursors.Cross;
			}
		}

		private void ScreenshotOverlay_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				// Get the current key combination
				Keys pressedKeys = e.KeyCode | e.Modifiers;
				Console.WriteLine($"Key pressed: {pressedKeys}, OCR shortcut: {settings.OcrShortcut}");

				// If the cancel key is pressed, close the window
				if (pressedKeys == settings.CancelShortcut)
				{
					Console.WriteLine("Atajo de cancelar detectado");
					this.Close();
					e.Handled = true;
					return;
				}

				// If the copy key is pressed, copy the selection to the clipboard
				if (pressedKeys == settings.CopyShortcut && !selectionRectangle.IsEmpty && isScreenshotValid)
				{
					Console.WriteLine("Atajo de copiar detectado");
					CaptureSelectedArea();
					this.Close();
					e.Handled = true;
					return;
				}

				// If the save key is pressed, save the selection
				if (pressedKeys == settings.SaveShortcut && !selectionRectangle.IsEmpty && isScreenshotValid)
				{
					Console.WriteLine("Atajo de guardar detectado");
					SaveSelectedArea();
					this.Close();
					e.Handled = true;
					return;
				}

				// If the undo key is pressed, undo the last drawn line
				if (pressedKeys == settings.UndoShortcut && isScreenshotValid)
				{
					Console.WriteLine("Atajo de deshacer detectado");
					UndoLastDrawingLine();
					e.Handled = true;
					return;
				}

				// If the OCR key is pressed, extract text from the image
				if (pressedKeys == settings.OcrShortcut && !selectionRectangle.IsEmpty && isScreenshotValid)
				{
					Console.WriteLine("OCR shortcut detected - Executing OCR");
					_ = PerformOcr();
					e.Handled = true;
					return;
				}

				// If the SCP key is pressed, upload the image by SCP
				if (pressedKeys == settings.ScpShortcut && !selectionRectangle.IsEmpty && isScreenshotValid)
				{
					Console.WriteLine("SCP shortcut detected - Executing SCP");
					PerformScp();
					e.Handled = true;
					return;
				}
				
				// If the Color Picker key is pressed, activate color picker mode
				if (pressedKeys == settings.ColorPickerShortcut && isScreenshotValid)
				{
					Console.WriteLine("Color Picker shortcut detected - Activating Color Picker");
					ActivateColorPicker();
					e.Handled = true;
					return;
				}

				// Additional verification for Control+R directly
				if (e.Control && e.KeyCode == Keys.R && !selectionRectangle.IsEmpty && isScreenshotValid)
				{
					Console.WriteLine("Control+R detected directly - Executing OCR");
					_ = PerformOcr();
					e.Handled = true;
					return;
				}

				// Additional verification for Control+X directly
				if (e.Control && e.KeyCode == Keys.X && !selectionRectangle.IsEmpty && isScreenshotValid)
				{
					Console.WriteLine("Control+X detected directly - Executing SCP");
					PerformScp();
					e.Handled = true;
					return;
				}
				
				// Additional verification for Control+V directly
				if (e.Control && e.KeyCode == Keys.V && isScreenshotValid)
				{
					Console.WriteLine("Control+V detected directly - Activating Color Picker");
					ActivateColorPicker();
					e.Handled = true;
					return;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in KeyDown: {ex.Message}");
			}
		}

		private void UndoLastDrawingLine()
		{
			if (drawingElements.Count > 0)
			{
				// Eliminate the last line from the list
				drawingElements.RemoveAt(drawingElements.Count - 1);

				// Update the display
				this.Invalidate();

				Console.WriteLine($"Line removed. {drawingElements.Count} lines remaining.");
			}
		}

		private bool IsPointInsideSelectionRectangle(Point point)
		{
			try
			{
				if (point == Point.Empty || selectionRectangle.IsEmpty || selectionRectangle.Width <= 0 || selectionRectangle.Height <= 0)
					return false;

				// Adjust for relative position to the form window
				int offsetX = this.Bounds.X - totalScreenBounds.X;
				int offsetY = this.Bounds.Y - totalScreenBounds.Y;

				Rectangle adjustedRect = new Rectangle(
						selectionRectangle.X + offsetX,
						selectionRectangle.Y + offsetY,
						selectionRectangle.Width,
						selectionRectangle.Height
				);

				return adjustedRect.Contains(point);
			}
			catch
			{
				// If an error occurs, consider that the point is not inside
				return false;
			}
		}

		private void ScreenshotOverlay_MouseDown(object sender, MouseEventArgs e)
		{
			try
			{
				if (e.Button == MouseButtons.Left && isScreenshotValid)
				{
					lastMousePosition = e.Location;

					// If in color picker mode, process the click to select a color
					if (isColorPickerMode)
					{
						FinishColorPick();
						return;
					}

					// Check if clicking on a resizing handle
					int handleIndex = GetHandleIndexAt(e.Location);
					if (handleIndex >= 0 && selectionRectangle.Width > 0 && selectionRectangle.Height > 0)
					{
						// Start resizing
						isResizing = true;
						isSelecting = false;
						isDrawing = false;
						currentHandleIndex = handleIndex;
						originalSelectionRect = selectionRectangle;

						// Set the appropriate cursor for the handle
						SetResizeCursor(handleIndex);

						Console.WriteLine($"Starting resizing with handle {handleIndex}");
					}
					else if (IsPointInsideSelectionRectangle(e.Location))
					{
						// Start drawing within the selected area
						isDrawing = true;
						isSelecting = false;
						isResizing = false;

						// Create a new drawing line
						currentLine = new List<Point>();
						currentLine.Add(e.Location);

						if (isPenMode)
						{
							// Pen mode
							drawingElements.Add(new DrawingElement(currentLine, true, currentDrawingColor));
							this.Cursor = penCursor;
							Console.WriteLine("Starting freehand drawing");
						}
						else
						{
							// Rectangle mode
							currentLine.Add(e.Location); // Add second point (will be updated in MouseMove)
							drawingElements.Add(new DrawingElement(currentLine, false, currentDrawingColor));
							this.Cursor = Cursors.Cross;
							Console.WriteLine("Starting rectangle drawing");
						}
					}
					else
					{
						// Start a new selection for both modes
						isSelecting = true;
						isResizing = false;
						isDrawing = false;
						startPoint = e.Location;
						endPoint = e.Location;
						selectionRectangle = Rectangle.Empty;
						resizeHandles.Clear();
						drawingElements.Clear();
						this.Invalidate();
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in MouseDown: {ex.Message}");
			}
		}

		private void ScreenshotOverlay_MouseMove(object sender, MouseEventArgs e)
		{
			if (!isScreenshotValid) return;

			if (isColorPickerMode)
			{
				ProcessColorPick(e.Location);
				return;
			}

			if (isResizing)
			{
				ResizeSelectionFromHandle(e.Location);
				if (DateTime.Now.Subtract(_lastButtonUpdate).TotalMilliseconds > 100)
				{
					RepositionButtons();
					_lastButtonUpdate = DateTime.Now;
				}
			}
			else if (isSelecting)
			{
				endPoint = e.Location;
				selectionRectangle = CalculateRectangle(startPoint, endPoint);
				if (buttonsPanel != null)
				{
					buttonsPanel.Visible = false;
				}
				this.Invalidate();
			}
			else if (isDrawing && currentLine != null)
			{
				DrawingElement currentElement = drawingElements.Find(elem => elem.Points == currentLine);

				if (currentElement != null)
				{
					if (currentElement.IsPenMode)
					{
						// Optimize pen drawing by adding points only if they're far enough
						if (currentLine.Count == 0 || 
							Math.Abs(e.Location.X - currentLine[currentLine.Count - 1].X) > 2 ||
							Math.Abs(e.Location.Y - currentLine[currentLine.Count - 1].Y) > 2)
						{
							currentLine.Add(e.Location);
							this.Invalidate(new Rectangle(
								e.Location.X - DrawingPenSize * 2,
								e.Location.Y - DrawingPenSize * 2,
								DrawingPenSize * 4,
								DrawingPenSize * 4
							));
						}
					}
					else
					{
						if (currentLine.Count >= 2)
						{
							currentLine[1] = e.Location;
							this.Invalidate();
						}
					}
				}
			}
			else if (selectionRectangle.Width > 0 && selectionRectangle.Height > 0)
			{
				int handleIndex = GetHandleIndexAt(e.Location);
				if (handleIndex >= 0)
				{
					SetResizeCursor(handleIndex);
				}
				else if (IsPointInsideSelectionRectangle(e.Location) && isPenMode)
				{
					this.Cursor = penCursor;
				}
				else
				{
					this.Cursor = Cursors.Cross;
				}
			}

			lastMousePosition = e.Location;
		}

		private void ScreenshotOverlay_MouseUp(object sender, MouseEventArgs e)
		{
			try
			{
				if (e.Button == MouseButtons.Left && isScreenshotValid)
				{
					if (isSelecting)
					{
						isSelecting = false;
						endPoint = e.Location;
						selectionRectangle = CalculateRectangle(startPoint, endPoint);

						// Verify that the rectangle has a minimum size
						if (selectionRectangle.Width < 10 || selectionRectangle.Height < 10)
						{
							// If the rectangle is too small, create a default one
							selectionRectangle = new Rectangle(
									selectionRectangle.X,
									selectionRectangle.Y,
									Math.Max(10, selectionRectangle.Width),
									Math.Max(10, selectionRectangle.Height)
							);
						}

						UpdateResizeHandles();

						// Update the images of the buttons before showing them
						if (penModeButton != null && rectangleModeButton != null)
						{
							// Clear previous images
							if (penModeButton.Image != null)
							{
								Image oldPenImage = penModeButton.Image;
								penModeButton.Image = null;
								oldPenImage.Dispose();
							}

							if (rectangleModeButton.Image != null)
							{
								Image oldRectImage = rectangleModeButton.Image;
								rectangleModeButton.Image = null;
								oldRectImage.Dispose();
							}

							// Color for the icons
							Color iconColor = ColorTranslator.FromHtml("#adadad");

							// Recreate the images with the correct states
							penModeButton.Image = CreatePenModeImage(isPenMode, iconColor);
							rectangleModeButton.Image = CreateRectangleModeImage(!isPenMode, iconColor);

							Console.WriteLine($"Buttons updated after selection: Pen={isPenMode}, Rectangle={!isPenMode}");
						}

						// Now show the buttons after selection
						RepositionButtons();
						this.Invalidate();
					}
					else if (isResizing)
					{
						isResizing = false;
						currentHandleIndex = -1;

						// Update the handles after resizing
						UpdateResizeHandles();

						// Restore the normal cursor
						this.Cursor = Cursors.Cross;

						// Update the position of the buttons after resizing
						RepositionButtons();

						Console.WriteLine("Resizing finished");
					}
					else if (isDrawing)
					{
						isDrawing = false;
						currentLine = null;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in MouseUp: {ex.Message}");
			}
		}

		private void UpdateResizeHandles()
		{
			try
			{
				resizeHandles.Clear();

				if (selectionRectangle.Width <= 0 || selectionRectangle.Height <= 0)
					return;

				// Adjustments for the position of the form
				int offsetX = this.Bounds.X - totalScreenBounds.X;
				int offsetY = this.Bounds.Y - totalScreenBounds.Y;

				// Calculate the adjusted position of the selection rectangle
				Rectangle adjustedRect = new Rectangle(
						selectionRectangle.X + offsetX,
						selectionRectangle.Y + offsetY,
						selectionRectangle.Width,
						selectionRectangle.Height
				);

				// Verify that the adjusted rectangle is within the limits of the form
				adjustedRect.X = Math.Max(HandleSize / 2, Math.Min(adjustedRect.X, this.Width - HandleSize / 2));
				adjustedRect.Y = Math.Max(HandleSize / 2, Math.Min(adjustedRect.Y, this.Height - HandleSize / 2));
				adjustedRect.Width = Math.Min(adjustedRect.Width, this.Width - adjustedRect.X - HandleSize / 2);
				adjustedRect.Height = Math.Min(adjustedRect.Height, this.Height - adjustedRect.Y - HandleSize / 2);

				// Verify that the rectangle has a minimum size
				if (adjustedRect.Width < 10 || adjustedRect.Height < 10)
					return;

				// Add handles in each corner and in the middle of each side
				// Top left
				resizeHandles.Add(new Rectangle(adjustedRect.Left - HandleSize / 2, adjustedRect.Top - HandleSize / 2, HandleSize, HandleSize));
				// Top middle
				resizeHandles.Add(new Rectangle(adjustedRect.Left + adjustedRect.Width / 2 - HandleSize / 2, adjustedRect.Top - HandleSize / 2, HandleSize, HandleSize));
				// Top right
				resizeHandles.Add(new Rectangle(adjustedRect.Right - HandleSize / 2, adjustedRect.Top - HandleSize / 2, HandleSize, HandleSize));
				// Middle right
				resizeHandles.Add(new Rectangle(adjustedRect.Right - HandleSize / 2, adjustedRect.Top + adjustedRect.Height / 2 - HandleSize / 2, HandleSize, HandleSize));
				// Bottom right
				resizeHandles.Add(new Rectangle(adjustedRect.Right - HandleSize / 2, adjustedRect.Bottom - HandleSize / 2, HandleSize, HandleSize));
				// Bottom middle
				resizeHandles.Add(new Rectangle(adjustedRect.Left + adjustedRect.Width / 2 - HandleSize / 2, adjustedRect.Bottom - HandleSize / 2, HandleSize, HandleSize));
				// Bottom left
				resizeHandles.Add(new Rectangle(adjustedRect.Left - HandleSize / 2, adjustedRect.Bottom - HandleSize / 2, HandleSize, HandleSize));
				// Middle left
				resizeHandles.Add(new Rectangle(adjustedRect.Left - HandleSize / 2, adjustedRect.Top + adjustedRect.Height / 2 - HandleSize / 2, HandleSize, HandleSize));
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in UpdateResizeHandles: {ex.Message}");
			}
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
				case 0: // Top left
					this.Cursor = Cursors.SizeNWSE;
					break;
				case 1: // Top middle
					this.Cursor = Cursors.SizeNS;
					break;
				case 2: // Top right
					this.Cursor = Cursors.SizeNESW;
					break;
				case 3: // Middle right
					this.Cursor = Cursors.SizeWE;
					break;
				case 4: // Bottom right
					this.Cursor = Cursors.SizeNWSE;
					break;
				case 5: // Bottom middle
					this.Cursor = Cursors.SizeNS;
					break;
				case 6: // Bottom left
					this.Cursor = Cursors.SizeNESW;
					break;
				case 7: // Middle left
					this.Cursor = Cursors.SizeWE;
					break;
				default:
					this.Cursor = Cursors.Cross;
					break;
			}
		}

		private void ResizeSelectionFromHandle(Point currentPosition)
		{
			try
			{
				// Calculate the difference between the current and last position
				int dx = currentPosition.X - lastMousePosition.X;
				int dy = currentPosition.Y - lastMousePosition.Y;

				// Hide the buttons panel during resizing
				if (buttonsPanel != null)
				{
					buttonsPanel.Visible = false;
				}

				// Adjustments for the position of the form
				int offsetX = this.Bounds.X - totalScreenBounds.X;
				int offsetY = this.Bounds.Y - totalScreenBounds.Y;

				// Create a copy of the rectangle to modify it
				Rectangle newRect = new Rectangle(
						selectionRectangle.X,
						selectionRectangle.Y,
						selectionRectangle.Width,
						selectionRectangle.Height
				);

				// Apply the changes according to the handle that is being dragged
				switch (currentHandleIndex)
				{
					case 0: // Top left
						newRect.X += dx;
						newRect.Y += dy;
						newRect.Width -= dx;
						newRect.Height -= dy;
						break;
					case 1: // Top middle
						newRect.Y += dy;
						newRect.Height -= dy;
						break;
					case 2: // Top right
						newRect.Y += dy;
						newRect.Width += dx;
						newRect.Height -= dy;
						break;
					case 3: // Middle right
						newRect.Width += dx;
						break;
					case 4: // Bottom right
						newRect.Width += dx;
						newRect.Height += dy;
						break;
					case 5: // Bottom middle
						newRect.Height += dy;
						break;
					case 6: // Bottom left
						newRect.X += dx;
						newRect.Width -= dx;
						newRect.Height += dy;
						break;
					case 7: // Middle left
						newRect.X += dx;
						newRect.Width -= dx;
						break;
				}

				// Ensure that the rectangle has a minimum size
				if (newRect.Width < 10)
				{
					// Keep the position of the opposite edge constant
					if (currentHandleIndex == 0 || currentHandleIndex == 6 || currentHandleIndex == 7)
					{
						// If we are resizing from the left side
						newRect.X = selectionRectangle.Right - 10;
					}
					newRect.Width = 10;
				}

				if (newRect.Height < 10)
				{
					// Keep the position of the opposite edge constant
					if (currentHandleIndex == 0 || currentHandleIndex == 1 || currentHandleIndex == 2)
					{
						// If we are resizing from the top side
						newRect.Y = selectionRectangle.Bottom - 10;
					}
					newRect.Height = 10;
				}

				// Ensure that the rectangle is within the limits of the image
				newRect.X = Math.Max(0, Math.Min(newRect.X, screenshotWidth - 10));
				newRect.Y = Math.Max(0, Math.Min(newRect.Y, screenshotHeight - 10));
				newRect.Width = Math.Min(newRect.Width, screenshotWidth - newRect.X);
				newRect.Height = Math.Min(newRect.Height, screenshotHeight - newRect.Y);

				// Update the selection rectangle
				selectionRectangle = newRect;

				// Update the resize handles
				UpdateResizeHandles();

				// Update the screen to reflect changes
				this.Invalidate();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in ResizeSelectionFromHandle: {ex.Message}");
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			try
			{
				base.OnPaint(e);
				e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
				e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
				e.Graphics.InterpolationMode = InterpolationMode.Low;
				
				ScreenshotOverlay_Paint(this, e);

				if (!isSelecting && !isResizing && !selectionRectangle.IsEmpty && 
					selectionRectangle.Width > 0 && selectionRectangle.Height > 0 &&
					DateTime.Now.Subtract(_lastButtonUpdate).TotalMilliseconds > 100)
				{
					RepositionButtons();
					_lastButtonUpdate = DateTime.Now;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in OnPaint: {ex.Message}");
			}
		}

		private Rectangle CalculateRectangle(Point startPoint, Point endPoint)
		{
			int x = Math.Min(startPoint.X, endPoint.X);
			int y = Math.Min(startPoint.Y, endPoint.Y);
			int width = Math.Abs(startPoint.X - endPoint.X);
			int height = Math.Abs(startPoint.Y - endPoint.Y);

			// Adjust for the position of the form
			x += this.Left - totalScreenBounds.X;
			y += this.Top - totalScreenBounds.Y;

			return new Rectangle(x, y, width, height);
		}

		private void CaptureSelectedArea()
		{
			if (!isScreenshotValid || selectionRectangle.IsEmpty || selectionRectangle.Width <= 0 || selectionRectangle.Height <= 0)
				return;

			try
			{
				// Validate that the rectangle is within the limits of the image
				int x = Math.Max(0, selectionRectangle.X);
				int y = Math.Max(0, selectionRectangle.Y);
				int width = Math.Min(screenshotWidth - x, selectionRectangle.Width);
				int height = Math.Min(screenshotHeight - y, selectionRectangle.Height);

				if (width <= 0 || height <= 0)
					return;

				Rectangle validRect = new Rectangle(x, y, width, height);

				using (Bitmap selectedArea = new Bitmap(width, height))
				{
					using (Graphics g = Graphics.FromImage(selectedArea))
					{
						// Draw the original image
						g.DrawImage(screenshot,
								new Rectangle(0, 0, width, height),
								validRect,
								GraphicsUnit.Pixel);

						// Configure graphics for high quality drawing
						g.SmoothingMode = SmoothingMode.AntiAlias;
						g.InterpolationMode = InterpolationMode.HighQualityBicubic;

						// Draw all the lines drawn by the user
						// Adjustments for the position of the form
						int offsetX = this.Bounds.X - totalScreenBounds.X;
						int offsetY = this.Bounds.Y - totalScreenBounds.Y;

						foreach (DrawingElement element in drawingElements)
						{
							if (element.Points.Count > 1)
							{
								using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
								{
									if (element.IsPenMode)
									{
										// Draw the lines drawn by hand
										for (int i = 0; i < element.Points.Count - 1; i++)
										{
											// Convert points to the image coordinate space
											Point p1 = new Point(
															element.Points[i].X - validRect.X - offsetX,
															element.Points[i].Y - validRect.Y - offsetY
											);
											Point p2 = new Point(
															element.Points[i + 1].X - validRect.X - offsetX,
															element.Points[i + 1].Y - validRect.Y - offsetY
											);

											// Draw only if the points are within the image
											if (p1.X >= 0 && p1.X < width && p1.Y >= 0 && p1.Y < height &&
													p2.X >= 0 && p2.X < width && p2.Y >= 0 && p2.Y < height)
											{
												g.DrawLine(elementPen, p1, p2);
											}
										}
									}
									else
									{
										// Draw rectangle
										Point startPoint = element.Points[0];
										Point endPoint = element.Points[1];

										// Convert points to the image coordinate space
										Point p1 = new Point(
												startPoint.X - validRect.X - offsetX,
												startPoint.Y - validRect.Y - offsetY
										);
										Point p2 = new Point(
												endPoint.X - validRect.X - offsetX,
												endPoint.Y - validRect.Y - offsetY
										);

										// Calculate rectangle
										int rectX = Math.Min(p1.X, p2.X);
										int rectY = Math.Min(p1.Y, p2.Y);
										int rectWidth = Math.Abs(p1.X - p2.X);
										int rectHeight = Math.Abs(p1.Y - p2.Y);

										// Draw only if the rectangle is at least partially within the image
										if (rectX + rectWidth >= 0 && rectX < width &&
												rectY + rectHeight >= 0 && rectY < height)
										{
											Rectangle rect = new Rectangle(rectX, rectY, rectWidth, rectHeight);
											g.DrawRectangle(elementPen, rect);
										}
									}
								}
							}
						}
					}

					var clonedImage = new Bitmap(selectedArea);
					ScreenshotCaptured?.Invoke(this, new ScreenshotEventArgs(clonedImage));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error capturing area: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void SaveSelectedArea()
		{
			if (!isScreenshotValid || selectionRectangle.IsEmpty || selectionRectangle.Width <= 0 || selectionRectangle.Height <= 0)
				return;

			try
			{
				// Validate that the rectangle is within the limits of the image
				int x = Math.Max(0, selectionRectangle.X);
				int y = Math.Max(0, selectionRectangle.Y);
				int width = Math.Min(screenshotWidth - x, selectionRectangle.Width);
				int height = Math.Min(screenshotHeight - y, selectionRectangle.Height);

				if (width <= 0 || height <= 0)
					return;

				Rectangle validRect = new Rectangle(x, y, width, height);

				using (Bitmap selectedArea = new Bitmap(width, height))
				{
					using (Graphics g = Graphics.FromImage(selectedArea))
					{
						// Draw the original image
						g.DrawImage(screenshot,
								new Rectangle(0, 0, width, height),
								validRect,
								GraphicsUnit.Pixel);

						// Configure graphics for high quality drawing
						g.SmoothingMode = SmoothingMode.AntiAlias;
						g.InterpolationMode = InterpolationMode.HighQualityBicubic;

						// Draw all the lines drawn by the user
						// Adjustments for the position of the form
						int offsetX = this.Bounds.X - totalScreenBounds.X;
						int offsetY = this.Bounds.Y - totalScreenBounds.Y;

						foreach (DrawingElement element in drawingElements)
						{
							if (element.Points.Count > 1)
							{
								using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
								{
									if (element.IsPenMode)
									{
										// Draw the lines drawn by hand
										for (int i = 0; i < element.Points.Count - 1; i++)
										{
											// Convert points to the image coordinate space
											Point p1 = new Point(
															element.Points[i].X - validRect.X - offsetX,
															element.Points[i].Y - validRect.Y - offsetY
											);
											Point p2 = new Point(
															element.Points[i + 1].X - validRect.X - offsetX,
															element.Points[i + 1].Y - validRect.Y - offsetY
											);

											// Draw only if the points are within the image
											if (p1.X >= 0 && p1.X < width && p1.Y >= 0 && p1.Y < height &&
													p2.X >= 0 && p2.X < width && p2.Y >= 0 && p2.Y < height)
											{
												g.DrawLine(elementPen, p1, p2);
											}
										}
									}
									else
									{
										// Draw rectangle
										Point startPoint = element.Points[0];
										Point endPoint = element.Points[1];

										// Convert points to the image coordinate space
										Point p1 = new Point(
												startPoint.X - validRect.X - offsetX,
												startPoint.Y - validRect.Y - offsetY
										);
										Point p2 = new Point(
												endPoint.X - validRect.X - offsetX,
												endPoint.Y - validRect.Y - offsetY
										);

										// Calculate rectangle
										int rectX = Math.Min(p1.X, p2.X);
										int rectY = Math.Min(p1.Y, p2.Y);
										int rectWidth = Math.Abs(p1.X - p2.X);
										int rectHeight = Math.Abs(p1.Y - p2.Y);

										// Draw only if the rectangle is at least partially within the image
										if (rectX + rectWidth >= 0 && rectX < width &&
												rectY + rectHeight >= 0 && rectY < height)
										{
											Rectangle rect = new Rectangle(rectX, rectY, rectWidth, rectHeight);
											g.DrawRectangle(elementPen, rect);
										}
									}
								}
							}
						}
					}

					SaveFileDialog saveDialog = new SaveFileDialog
					{
						Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|All files (*.*)|*.*",
						DefaultExt = ".png",
						FileName = $"CloudShot_{DateTime.Now:yyyyMMdd_HHmmss}.png"
					};

					if (saveDialog.ShowDialog() == DialogResult.OK)
					{
						string extension = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();
						ImageFormat format = ImageFormat.Png;

						if (extension == ".jpg" || extension == ".jpeg")
						{
							format = ImageFormat.Jpeg;
						}

						selectedArea.Save(saveDialog.FileName, format);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error saving area: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			try
			{
				base.OnClosed(e);

				if (screenshot != null)
				{
					screenshot.Dispose();
					screenshot = null;
				}
				
				if (colorPickerPreview != null)
				{
					colorPickerPreview.Dispose();
					colorPickerPreview = null;
				}
			}
			catch { /* Ignorar errores al cerrar */ }
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			// Print for diagnostic
			Console.WriteLine($"ProcessCmdKey: {keyData}, OCR shortcut: {settings.OcrShortcut}");

			// Color Picker shortcut
			if (keyData == settings.ColorPickerShortcut && isScreenshotValid)
			{
				Console.WriteLine("Color Picker shortcut detected");
				ActivateColorPicker();
				return true;
			}
			else if (keyData == (Keys.Control | Keys.V) && isScreenshotValid)
			{
				Console.WriteLine("Control+V detected directly - Activating Color Picker");
				ActivateColorPicker();
				return true;
			}

			// If already in color picker mode, Escape cancels it
			if (isColorPickerMode && keyData == Keys.Escape)
			{
				isColorPickerMode = false;
				this.Close();
				return true;
			}

			// Intercept keyboard shortcuts
			if (keyData == settings.CancelShortcut)
			{
				this.Close();
				return true;
			}
			else if (keyData == settings.CopyShortcut && !selectionRectangle.IsEmpty && isScreenshotValid)
			{
				CaptureSelectedArea();
				this.Close();
				return true;
			}
			else if (keyData == settings.SaveShortcut && !selectionRectangle.IsEmpty && isScreenshotValid)
			{
				SaveSelectedArea();
				this.Close();
				return true;
			}
			else if (keyData == settings.UndoShortcut && isScreenshotValid)
			{
				UndoLastDrawingLine();
				return true;
			}
			else if (keyData == settings.OcrShortcut && !selectionRectangle.IsEmpty && isScreenshotValid)
			{
				Console.WriteLine("OCR shortcut detected in ProcessCmdKey");
				_ = PerformOcr(); // Asynchronous call without waiting
				return true;
			}
			else if (keyData == settings.ScpShortcut && !selectionRectangle.IsEmpty && isScreenshotValid)
			{
				Console.WriteLine("SCP shortcut detected in ProcessCmdKey");
				PerformScp();
				return true;
			}
			// Additional verification for Control+R directly
			else if (keyData == (Keys.Control | Keys.R) && !selectionRectangle.IsEmpty && isScreenshotValid)
			{
				Console.WriteLine("Control+R detected directly in ProcessCmdKey");
				_ = PerformOcr();
				return true;
			}
			// Additional verification for Control+X directly
			else if (keyData == (Keys.Control | Keys.X) && !selectionRectangle.IsEmpty && isScreenshotValid)
			{
				Console.WriteLine("Control+X detected directly in ProcessCmdKey");
				PerformScp();
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		protected override void OnShown(EventArgs e)
		{
			try
			{
				base.OnShown(e);

				// Ensure that the panel is configured correctly but not initially displayed
				if (buttonsPanel != null)
				{
					buttonsPanel.Parent = this;
					// Do not call RepositionButtons here to avoid displaying the panel initially
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in OnShown: {ex.Message}");
			}
		}

		private void RepositionButtons()
		{
			try
			{
				if (buttonsPanel != null)
				{
					// If we are resizing, hide the panel and exit
					if (isResizing)
					{
						buttonsPanel.Visible = false;
						return;
					}

					// Verify if there is a valid selection
					if (!selectionRectangle.IsEmpty && selectionRectangle.Width > 0 && selectionRectangle.Height > 0)
					{
						// Adjust for relative position to the form window
						int offsetX = this.Bounds.X - totalScreenBounds.X;
						int offsetY = this.Bounds.Y - totalScreenBounds.Y;

						// Position the panel in the top right corner of the selected area
						// with a small margin of 5 pixels
						buttonsPanel.Location = new Point(
								selectionRectangle.Right + offsetX + 5,
								selectionRectangle.Top + offsetY + 5
						);

						// Ensure that the panel does not go out of the screen
						if (buttonsPanel.Location.X + buttonsPanel.Width > this.Width)
						{
							// If it goes out to the right, place it to the left of the selected area
							buttonsPanel.Location = new Point(
									selectionRectangle.Left + offsetX - buttonsPanel.Width - 5,
									buttonsPanel.Location.Y
							);
						}

						if (buttonsPanel.Location.Y + buttonsPanel.Height > this.Height)
						{
							// If it goes out to the bottom, adjust the vertical position
							buttonsPanel.Location = new Point(
									buttonsPanel.Location.X,
									this.Height - buttonsPanel.Height - 5
							);
						}
					}
					else
					{
						// If there is no selection, hide the panel
						buttonsPanel.Visible = false;
						return;
					}

					// Ensure that it has the correct size for the vertical panel
					buttonsPanel.Size = new Size(ButtonSize + 20, ButtonSize * 3 + ButtonSpacing * 2 + 20);

					// Update the region to keep the rounded corners
					int radius = 10; // Radius of the rounded corners
					using (GraphicsPath path = new GraphicsPath())
					{
						path.AddArc(0, 0, radius, radius, 180, 90);
						path.AddArc(buttonsPanel.Width - radius, 0, radius, radius, 270, 90);
						path.AddArc(buttonsPanel.Width - radius, buttonsPanel.Height - radius, radius, radius, 0, 90);
						path.AddArc(0, buttonsPanel.Height - radius, radius, radius, 90, 90);
						path.CloseAllFigures();
						buttonsPanel.Region = new Region(path);
					}

					// Ensure that the buttons have the correct images
					if (penModeButton != null && rectangleModeButton != null)
					{
						bool needsUpdate = false;

						// Verify if the buttons have the correct images according to the mode
						if (isPenMode && penModeButton.Image != null &&
								(penModeButton.Image as Bitmap).GetPixel(ButtonSize / 2, ButtonSize / 2).G < 100)
						{
							needsUpdate = true;
						}

						if (!isPenMode && rectangleModeButton.Image != null &&
								(rectangleModeButton.Image as Bitmap).GetPixel(ButtonSize / 2, ButtonSize / 2).G < 100)
						{
							needsUpdate = true;
						}

						if (needsUpdate)
						{
							// Update images
							if (penModeButton.Image != null)
							{
								Image oldPenImage = penModeButton.Image;
								penModeButton.Image = null;
								oldPenImage.Dispose();
							}

							if (rectangleModeButton.Image != null)
							{
								Image oldRectImage = rectangleModeButton.Image;
								rectangleModeButton.Image = null;
								oldRectImage.Dispose();
							}

							// Color for the icons
							Color iconColor = ColorTranslator.FromHtml("#adadad");

							penModeButton.Image = CreatePenModeImage(isPenMode, iconColor);
							rectangleModeButton.Image = CreateRectangleModeImage(!isPenMode, iconColor);

							Console.WriteLine("Images updated in RepositionButtons");
						}
					}

					// Ensure visibility and order Z
					buttonsPanel.Visible = true;
					buttonsPanel.BringToFront();
					this.Controls.SetChildIndex(buttonsPanel, 0);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error repositioning buttons: {ex.Message}");
			}
		}

		protected override void OnResize(EventArgs e)
		{
			try
			{
				base.OnResize(e);

				// Reposition the buttons when the size changes
				RepositionButtons();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in OnResize: {ex.Message}");
			}
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				// Set the form as a foreground window (always visible)
				cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST
				return cp;
			}
		}

		private void SetInitialState()
		{
			try
			{
				// Set the initial mode as pen
				isPenMode = true;

				// Set the initial color as red
				currentDrawingColor = Color.Red;

				// Hide the buttons panel until there is a selection
				if (buttonsPanel != null)
				{
					buttonsPanel.Visible = false;
				}

				// Update the appearance of the buttons only if they already exist
				if (penModeButton != null && rectangleModeButton != null && colorPickerButton != null)
				{
					// Clean previous images if they exist
					penModeButton.Image?.Dispose();
					rectangleModeButton.Image?.Dispose();
					colorPickerButton.Image?.Dispose();

					// Color for the icons
					Color iconColor = ColorTranslator.FromHtml("#adadad");

					penModeButton.Image = CreatePenModeImage(true, iconColor);
					rectangleModeButton.Image = CreateRectangleModeImage(false, iconColor);
					colorPickerButton.Image = CreateColorPickerImage(currentDrawingColor);

					// Register initial state
					Console.WriteLine("Initial state set: Pen mode active, red color");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error setting initial state: {ex.Message}");
			}
		}

		private void ScreenshotOverlay_Paint(object sender, PaintEventArgs e)
		{
			try
			{
				// Draw the full image with low opacity (darkened)
				e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), this.ClientRectangle);

				// If in color picker mode, draw specially
				if (isColorPickerMode && isScreenshotValid)
				{
					// Draw the full screenshot without darkness
					e.Graphics.DrawImage(screenshot, this.ClientRectangle, 0, 0, 
						screenshotWidth, screenshotHeight, GraphicsUnit.Pixel);

					// If a color has been selected, draw the preview and info
					if (selectedColor != Color.Empty)
					{
						// Position of preview box - place near cursor but ensure it's visible
						int previewX = colorPickerPoint.X + 20; // Offset from cursor
						int previewY = colorPickerPoint.Y + 20;
						
						// Make sure preview stays within screen bounds
						if (previewX + ColorPickerPreviewSize + 160 > this.Width)
						{
							previewX = colorPickerPoint.X - ColorPickerPreviewSize - 160;
						}
						if (previewY + ColorPickerPreviewSize + 60 > this.Height)
						{
							previewY = colorPickerPoint.Y - ColorPickerPreviewSize - 10;
						}

						// Draw zoomed area (preview)
						if (colorPickerPreview != null)
						{
							// Draw a white border around the preview
							e.Graphics.FillRectangle(new SolidBrush(Color.White), 
								previewX - 2, previewY - 2, ColorPickerPreviewSize + 4, ColorPickerPreviewSize + 4);
							
							// Draw the zoomed preview
							e.Graphics.DrawImage(colorPickerPreview, 
								new Rectangle(previewX, previewY, ColorPickerPreviewSize, ColorPickerPreviewSize));
							
							// Draw a black border around the preview
							e.Graphics.DrawRectangle(Pens.Black, 
								previewX, previewY, ColorPickerPreviewSize, ColorPickerPreviewSize);
						}
						
						// Color info box next to the preview
						int infoBoxX = previewX + ColorPickerPreviewSize + 10;
						int infoBoxY = previewY;
						int infoBoxWidth = 140;
						int infoBoxHeight = 110;
						
						// Draw color info background (with solid white background)
						e.Graphics.FillRectangle(Brushes.White, 
							infoBoxX, infoBoxY, infoBoxWidth, infoBoxHeight);
						e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(240, 230, 230, 230)), 
							infoBoxX, infoBoxY, infoBoxWidth, infoBoxHeight);
						e.Graphics.DrawRectangle(Pens.Black, 
							infoBoxX, infoBoxY, infoBoxWidth, infoBoxHeight);
						
						// Draw color sample (with checker background for transparency)
						int colorSampleX = infoBoxX + 10;
						int colorSampleY = infoBoxY + 10;
						int colorSampleWidth = infoBoxWidth - 20;
						int colorSampleHeight = 40;
						
						// Draw checkerboard pattern (for transparency)
						int checkerSize = 8;
						for (int y = 0; y < colorSampleHeight; y += checkerSize)
						{
							for (int x = 0; x < colorSampleWidth; x += checkerSize)
							{
								bool isAlternate = ((x / checkerSize) + (y / checkerSize)) % 2 == 0;
								using (SolidBrush brush = new SolidBrush(isAlternate ? Color.LightGray : Color.White))
								{
									e.Graphics.FillRectangle(brush, 
										colorSampleX + x, 
										colorSampleY + y, 
										Math.Min(checkerSize, colorSampleWidth - x), 
										Math.Min(checkerSize, colorSampleHeight - y));
								}
							}
						}
						
						// Draw the selected color on top
						e.Graphics.FillRectangle(new SolidBrush(selectedColor), 
							colorSampleX, colorSampleY, colorSampleWidth, colorSampleHeight);
						
						// Draw border around color sample
						e.Graphics.DrawRectangle(Pens.Black, 
							colorSampleX, colorSampleY, colorSampleWidth, colorSampleHeight);
						
						// Draw color text information
						string colorFormat = settings.ColorFormat;
						string colorInfo = GetColorString(selectedColor, colorFormat);
						
						// Draw the RGB values in a separate line
						string rgbInfo = $"R: {selectedColor.R}, G: {selectedColor.G}, B: {selectedColor.B}";
						if (selectedColor.A < 255)
						{
							rgbInfo = $"A: {selectedColor.A}, " + rgbInfo;
						}
						
						using (Font titleFont = new Font("Arial", 9, FontStyle.Bold))
						{
							// Draw the color format name
							e.Graphics.DrawString(colorFormat + ":", titleFont, Brushes.Black, 
								infoBoxX + 10, infoBoxY + 60);
							
							// Draw the RGB label
							e.Graphics.DrawString("Values:", titleFont, Brushes.Black, 
								infoBoxX + 10, infoBoxY + 85);
						}
						
						using (Font valueFont = new Font("Arial", 9))
						{
							// Draw the color formatted value
							e.Graphics.DrawString(colorInfo, valueFont, Brushes.Blue, 
								infoBoxX + 50, infoBoxY + 60);
							
							// Draw the RGB values
							e.Graphics.DrawString(rgbInfo, valueFont, Brushes.Black, 
								infoBoxX + 10, infoBoxY + 85 + valueFont.Height);
						}
						
						// Draw instructions
						string instructions = "Click to copy to clipboard";
						using (Font font = new Font("Arial", 11, FontStyle.Bold))
						{
							SizeF textSize = e.Graphics.MeasureString(instructions, font);
							
							// Draw with a background for better visibility
							Rectangle instructBg = new Rectangle(
								(int)(this.Width / 2 - textSize.Width / 2 - 10),
								this.Height - 40,
								(int)textSize.Width + 20,
								(int)textSize.Height + 10
							);
							
							e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(200, 0, 0, 0)), instructBg);
							e.Graphics.DrawRectangle(Pens.White, instructBg);
							
							e.Graphics.DrawString(instructions, font, Brushes.White, 
								this.Width / 2 - textSize.Width / 2, this.Height - 35);
						}
					}
					
					// Draw crosshair cursor at current mouse position
					if (lastMousePosition != Point.Empty)
					{
						int x = lastMousePosition.X;
						int y = lastMousePosition.Y;
						
						// Draw black outer lines
						e.Graphics.DrawLine(new Pen(Color.Black, 1), x - 10, y, x + 10, y);
						e.Graphics.DrawLine(new Pen(Color.Black, 1), x, y - 10, x, y + 10);
						
						// Draw white inner lines
						using (Pen pen = new Pen(Color.White, 1))
						{
							pen.DashStyle = DashStyle.Dot;
							e.Graphics.DrawLine(pen, x - 10, y, x + 10, y);
							e.Graphics.DrawLine(pen, x, y - 10, x, y + 10);
						}
					}
					
					return;
				}

				// Regular screenshot overlay drawing for selection mode
				if (isScreenshotValid && !selectionRectangle.IsEmpty && selectionRectangle.Width > 0 && selectionRectangle.Height > 0)
				{
					// Validate that the rectangle is within the limits of the image
					int x = Math.Max(0, selectionRectangle.X);
					int y = Math.Max(0, selectionRectangle.Y);
					int width = Math.Min(screenshotWidth - x, selectionRectangle.Width);
					int height = Math.Min(screenshotHeight - y, selectionRectangle.Height);

					if (width <= 0 || height <= 0)
						return;

					Rectangle validRect = new Rectangle(x, y, width, height);

					// Adjust for relative position to the form window
					int offsetX = this.Bounds.X - totalScreenBounds.X;
					int offsetY = this.Bounds.Y - totalScreenBounds.Y;

					Rectangle screenRect = new Rectangle(
							validRect.X + offsetX,
							validRect.Y + offsetY,
							validRect.Width,
							validRect.Height
					);

					// Draw the selected area
					e.Graphics.DrawImage(screenshot, screenRect, validRect, GraphicsUnit.Pixel);

					// Draw the selection rectangle border (always for rectangle mode, thinner for pen mode)
					using (Pen borderPen = new Pen(Color.Red, !isPenMode ? 2 : 1))
					{
						e.Graphics.DrawRectangle(borderPen, screenRect);
					}

					// Draw the drawn lines
					if (drawingElements.Count > 0)
					{
						foreach (DrawingElement element in drawingElements)
						{
							if (element.Points.Count > 1)
							{
								using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
								{
									if (element.IsPenMode)
									{
										// Draw freehand lines
										for (int i = 0; i < element.Points.Count - 1; i++)
										{
											e.Graphics.DrawLine(elementPen, element.Points[i], element.Points[i + 1]);
										}
									}
									else
									{
										// Draw rectangle
										Point startPoint = element.Points[0];
										Point endPoint = element.Points[1];

										int rectX = Math.Min(startPoint.X, endPoint.X);
										int rectY = Math.Min(startPoint.Y, endPoint.Y);
										int rectWidth = Math.Abs(startPoint.X - endPoint.X);
										int rectHeight = Math.Abs(startPoint.Y - endPoint.Y);

										Rectangle rect = new Rectangle(rectX, rectY, rectWidth, rectHeight);
										e.Graphics.DrawRectangle(elementPen, rect);
									}
								}
							}
						}
					}

					// Draw the resize handles
					foreach (Rectangle handle in resizeHandles)
					{
						e.Graphics.FillRectangle(Brushes.White, handle);
						e.Graphics.DrawRectangle(Pens.Black, handle);
					}

					// Do not position the buttons during selection or resizing
					if (!isSelecting && !isResizing)
					{
						RepositionButtons();
					}
					else if (buttonsPanel != null)
					{
						// Ensure that the panel is hidden while selecting or resizing
						buttonsPanel.Visible = false;
					}
				}
			}
			catch (Exception ex)
			{
				// Only register the error, do not show a MessageBox here to avoid recursion
				Console.WriteLine($"Error in Paint: {ex.Message}");
			}
		}

		private Bitmap CreateColorPickerImage(Color color)
		{
			Bitmap bmp = new Bitmap(ButtonSize, ButtonSize);
			using (Graphics g = Graphics.FromImage(bmp))
			{
				// Transparent background
				g.Clear(Color.Transparent);

				// Create a circular button background with the same style as the other buttons
				using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(180, 60, 60, 60)))
				{
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.FillEllipse(bgBrush, 2, 2, ButtonSize - 4, ButtonSize - 4);
				}

				// Button border
				using (Pen borderPen = new Pen(Color.FromArgb(120, 150, 150, 150), 2))
				{
					g.DrawEllipse(borderPen, 2, 2, ButtonSize - 4, ButtonSize - 4);
				}

				// Draw a circle with the current color
				using (SolidBrush colorBrush = new SolidBrush(color))
				{
					g.FillEllipse(colorBrush, 10, 10, ButtonSize - 20, ButtonSize - 20);
				}

				// Color circle border
				using (Pen colorBorderPen = new Pen(ColorTranslator.FromHtml("#adadad"), 1.5f))
				{
					g.DrawEllipse(colorBorderPen, 10, 10, ButtonSize - 20, ButtonSize - 20);
				}
			}
			return bmp;
		}

		private void ShowColorPicker()
		{
			try
			{
				// Create color selection dialog
				ColorDialog colorDialog = new ColorDialog();

				// Configure the dialog
				colorDialog.Color = currentDrawingColor;
				colorDialog.FullOpen = true;
				colorDialog.AnyColor = true;

				// Show the dialog and check if the user accepted
				if (colorDialog.ShowDialog() == DialogResult.OK)
				{
					// Update the current color
					currentDrawingColor = colorDialog.Color;

					// Update the color picker button image
					if (colorPickerButton != null)
					{
						// Release resources of the previous image
						if (colorPickerButton.Image != null)
						{
							Image oldImage = colorPickerButton.Image;
							colorPickerButton.Image = null;
							oldImage.Dispose();
						}

						// Create and assign new image
						colorPickerButton.Image = CreateColorPickerImage(currentDrawingColor);

						Console.WriteLine($"Color changed to: R={currentDrawingColor.R}, G={currentDrawingColor.G}, B={currentDrawingColor.B}");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error showing color picker: {ex.Message}");
			}
		}

		private async Task PerformOcr()
		{
			Console.WriteLine("STARTING OCR EXECUTION");

			if (!isScreenshotValid || selectionRectangle.IsEmpty || selectionRectangle.Width <= 0 || selectionRectangle.Height <= 0)
			{
				Console.WriteLine("Cannot perform OCR - invalid selection or invalid image");
				MessageBox.Show("Please select a valid area of the image to perform OCR.",
						"OCR", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			try
			{
				// Show a wait cursor
				Cursor = Cursors.WaitCursor;

				// Validate that the rectangle is within the limits of the image
				int x = Math.Max(0, selectionRectangle.X);
				int y = Math.Max(0, selectionRectangle.Y);
				int width = Math.Min(screenshotWidth - x, selectionRectangle.Width);
				int height = Math.Min(screenshotHeight - y, selectionRectangle.Height);

				if (width <= 0 || height <= 0)
				{
					Console.WriteLine("Invalid dimensions for OCR");
					return;
				}

				Rectangle validRect = new Rectangle(x, y, width, height);
				Console.WriteLine($"Running OCR on area: X={x}, Y={y}, Width={width}, Height={height}");

				using (Bitmap selectedArea = new Bitmap(width, height))
				{
					using (Graphics g = Graphics.FromImage(selectedArea))
					{
						// Draw the original image without annotations
						g.DrawImage(screenshot,
								new Rectangle(0, 0, width, height),
								validRect,
								GraphicsUnit.Pixel);
					}

					// Save temporarily the image to process it with OCR
					string tempFile = Path.Combine(Path.GetTempPath(), "cloudshot_ocr_temp.png");

					// Save the image asynchronously
					await Task.Run(() =>
					{
						selectedArea.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);
					});

					Console.WriteLine($"Image saved in: {tempFile}");

					try
					{
						// Use Windows OCR Engine
						string extractedText = await ExtractTextFromImageAsync(tempFile);

						if (!string.IsNullOrWhiteSpace(extractedText))
						{
							// Copy the text to the clipboard
							Clipboard.SetText(extractedText);

							// Notify the user
							NotifyTextExtracted(extractedText);

							Console.WriteLine("OCR completed successfully");

							// Close the selection after successfully processing OCR
							this.Close();
						}
						else
						{
							MessageBox.Show(
									"Could not extract text from the selected image.\n" +
									"It's possible that there is no text visible in the selection or the text cannot be recognized.",
									"OCR - No text found",
									MessageBoxButtons.OK,
									MessageBoxIcon.Information);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Error during OCR: {ex.Message}");
						// Show message and keep temporary image for diagnosis
						MessageBox.Show($"Error processing OCR: {ex.Message}\nImage saved in: {tempFile}",
								"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					finally
					{
						// Delete the temporary file asynchronously
						await Task.Run(() =>
						{
							try { File.Delete(tempFile); } catch { }
						});
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error general in OCR: {ex.Message}");
				MessageBox.Show($"Error performing OCR: {ex.Message}",
						"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				// Restore the cursor
				Cursor = Cursors.Default;
				Console.WriteLine("FIN DE EJECUCIÓN OCR");
			}
		}

		private async Task<string> ExtractTextFromImageAsync(string imagePath)
		{
			try
			{
				// Create a StorageFile from the image path
				var file = await global::Windows.Storage.StorageFile.GetFileFromPathAsync(imagePath);

				// Open the file as a stream
				using (var stream = await file.OpenAsync(global::Windows.Storage.FileAccessMode.Read))
				{
					// Create a decoder for the image
					var decoder = await global::Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);

					// Get the software bitmap for the image
					var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

					// Create the OCR engine for the system language
					var ocrEngine = global::Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(
							new global::Windows.Globalization.Language("es-ES"));

					if (ocrEngine == null)
					{
						// Try with English as an alternative
						ocrEngine = global::Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(
								new global::Windows.Globalization.Language("en-US"));

						if (ocrEngine == null)
						{
							// If an OCR engine for English cannot be created,
							// try with the default user language
							ocrEngine = global::Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages();

							if (ocrEngine == null)
							{
								throw new Exception("Could not initialize OCR engine. Verify that Windows OCR is installed.");
							}
						}
					}

					// Perform OCR
					var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

					// Get the recognized text
					return ocrResult.Text;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in ExtractTextFromImageAsync: {ex.Message}");
				throw new Exception($"Error extracting text: {ex.Message}", ex);
			}
		}

		private void NotifyTextExtracted(string text)
		{
			try
			{
				// Limit the text length for the notification
				string previewText = text.Length > 50 ? text.Substring(0, 47) + "..." : text;

				// Search for the MainForm to show the notification
				foreach (Form form in Application.OpenForms)
				{
					if (form is MainForm mainForm)
					{
						// Use a specific method for notifications in MainForm
						mainForm.ShowNotification("Text extracted",
								$"The text has been copied to the clipboard:\n{previewText}");
						return;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error notifying: {ex.Message}");
			}
		}

		private void PerformScp()
		{
			Console.WriteLine("STARTING SCP EXECUTION");

			if (!isScreenshotValid || selectionRectangle.IsEmpty || selectionRectangle.Width <= 0 || selectionRectangle.Height <= 0)
			{
				Console.WriteLine("Cannot perform SCP - invalid selection or invalid image");
				MessageBox.Show("Please select a valid area of the image to upload via SCP.",
						"SCP", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Verify if the SCP command is configured
			if (string.IsNullOrWhiteSpace(settings.ScpCommand) || !settings.ScpCommand.Contains("<image>"))
			{
				MessageBox.Show(
						"The SCP command is not configured correctly.\n" +
						"You must configure a command that includes '<image>' as a reference to the file.",
						"SCP Configuration Error",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
				return;
			}

			// Verify if the SCP command has the -i parameter but the identity file does not exist
			if (settings.ScpCommand.Contains(" -i "))
			{
				string[] parts = settings.ScpCommand.Split(new[] { " -i " }, StringSplitOptions.None);
				if (parts.Length > 1)
				{
					string keyPath = parts[1].Split(' ')[0].Trim();
					// Expand ~ if it is a Unix path
					if (keyPath.StartsWith("~"))
					{
						keyPath = keyPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
					}

					if (!File.Exists(keyPath))
					{
						Console.WriteLine($"Key file not found: {keyPath}");
						if (MessageBox.Show(
								$"The specified key file does not exist:\n{keyPath}\n\n" +
								"This is the path to the private key file (parameter -i).\n" +
								"Do you want to continue anyway?",
								"SCP Configuration Warning",
								MessageBoxButtons.YesNo,
								MessageBoxIcon.Warning) == DialogResult.No)
						{
							return;
						}
					}
				}
			}

			try
			{
				// Show a wait cursor
				Cursor = Cursors.WaitCursor;

				// Validate that the rectangle is within the limits of the image
				int x = Math.Max(0, selectionRectangle.X);
				int y = Math.Max(0, selectionRectangle.Y);
				int width = Math.Min(screenshotWidth - x, selectionRectangle.Width);
				int height = Math.Min(screenshotHeight - y, selectionRectangle.Height);

				if (width <= 0 || height <= 0)
				{
					Console.WriteLine("Invalid dimensions for SCP");
					return;
				}

				Rectangle validRect = new Rectangle(x, y, width, height);
				Console.WriteLine($"Executing SCP in area: X={x}, Y={y}, Width={width}, Height={height}");

				// Generate a unique name for the image
				string fileName = $"cloudshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
				string tempDir = Path.GetTempPath();
				string tempFile = Path.Combine(tempDir, fileName);

				using (Bitmap selectedArea = new Bitmap(width, height))
				{
					using (Graphics g = Graphics.FromImage(selectedArea))
					{
						// Draw the original image without annotations
						g.DrawImage(screenshot,
								new Rectangle(0, 0, width, height),
								validRect,
								GraphicsUnit.Pixel);
                        
                        // Configure graphics for high quality drawing
						g.SmoothingMode = SmoothingMode.AntiAlias;
						g.InterpolationMode = InterpolationMode.HighQualityBicubic;

						// Draw all the lines drawn by the user
						// Adjustments for the position of the form
						int offsetX = this.Bounds.X - totalScreenBounds.X;
						int offsetY = this.Bounds.Y - totalScreenBounds.Y;

						foreach (DrawingElement element in drawingElements)
						{
							if (element.Points.Count > 1)
							{
								using (Pen elementPen = new Pen(element.DrawingColor, DrawingPenSize))
								{
									if (element.IsPenMode)
									{
										// Draw the lines drawn by hand
										for (int i = 0; i < element.Points.Count - 1; i++)
										{
											// Convert points to the image coordinate space
											Point p1 = new Point(
															element.Points[i].X - validRect.X - offsetX,
															element.Points[i].Y - validRect.Y - offsetY
											);
											Point p2 = new Point(
															element.Points[i + 1].X - validRect.X - offsetX,
															element.Points[i + 1].Y - validRect.Y - offsetY
											);

											// Draw only if the points are within the image
											if (p1.X >= 0 && p1.X < width && p1.Y >= 0 && p1.Y < height &&
													p2.X >= 0 && p2.X < width && p2.Y >= 0 && p2.Y < height)
											{
												g.DrawLine(elementPen, p1, p2);
											}
										}
									}
									else
									{
										// Draw rectangle
										Point startPoint = element.Points[0];
										Point endPoint = element.Points[1];

										// Convert points to the image coordinate space
										Point p1 = new Point(
												startPoint.X - validRect.X - offsetX,
												startPoint.Y - validRect.Y - offsetY
										);
										Point p2 = new Point(
												endPoint.X - validRect.X - offsetX,
												endPoint.Y - validRect.Y - offsetY
										);

										// Calculate rectangle
										int rectX = Math.Min(p1.X, p2.X);
										int rectY = Math.Min(p1.Y, p2.Y);
										int rectWidth = Math.Abs(p1.X - p2.X);
										int rectHeight = Math.Abs(p1.Y - p2.Y);

										// Draw only if the rectangle is at least partially within the image
										if (rectX + rectWidth >= 0 && rectX < width &&
												rectY + rectHeight >= 0 && rectY < height)
										{
											Rectangle rect = new Rectangle(rectX, rectY, rectWidth, rectHeight);
											g.DrawRectangle(elementPen, rect);
										}
									}
								}
							}
						}
					}

					// Save temporarily the image to upload it by SCP
					selectedArea.Save(tempFile, ImageFormat.Png);
					Console.WriteLine($"Image saved in: {tempFile}");

					// Execute the SCP command
					string scpCommand = settings.ScpCommand.Replace("<image>", tempFile);
					Console.WriteLine($"Executing command: {scpCommand}");

					// Create a process to execute the command
					using (System.Diagnostics.Process process = new System.Diagnostics.Process())
					{
						process.StartInfo.FileName = "cmd.exe";
						process.StartInfo.Arguments = $"/c {scpCommand}";
						process.StartInfo.UseShellExecute = false;
						process.StartInfo.CreateNoWindow = true;
						process.StartInfo.RedirectStandardOutput = true;
						process.StartInfo.RedirectStandardError = true;
						process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
						process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

						StringBuilder output = new StringBuilder();
						StringBuilder error = new StringBuilder();

						process.OutputDataReceived += (sender, e) =>
						{
							if (!string.IsNullOrEmpty(e.Data))
							{
								output.AppendLine(e.Data);
								Console.WriteLine($"SCP Output: {e.Data}");
							}
						};

						process.ErrorDataReceived += (sender, e) =>
						{
							if (!string.IsNullOrEmpty(e.Data))
							{
								error.AppendLine(e.Data);
								Console.WriteLine($"SCP Error: {e.Data}");
							}
						};

						process.Start();
						process.BeginOutputReadLine();
						process.BeginErrorReadLine();

						// Wait for the process to finish
						process.WaitForExit();

						// Verify the result
						if (process.ExitCode == 0)
						{
							Console.WriteLine("SCP completed successfully");

							// Copy to clipboard if configured
							if (!string.IsNullOrWhiteSpace(settings.ScpClipboardText) &&
									settings.ScpClipboardText.Contains("<image>"))
							{
								string clipboardText = settings.ScpClipboardText.Replace("<image>",
										Path.GetFileName(tempFile));

								Clipboard.SetText(clipboardText);
								Console.WriteLine($"Text copied to clipboard: {clipboardText}");
							}

							// Notify the user
							this.BeginInvoke(new Action(() =>
							{
								this.Close();
								NotifyScpCompleted(Path.GetFileName(tempFile));
							}));
						}
						else
						{
							Console.WriteLine($"Error in SCP: {error}");
							string errorMsg = error.ToString().Trim();
							if (string.IsNullOrEmpty(errorMsg))
							{
								errorMsg = "No specific error message received. Please verify the SCP command configuration.";
							}

							MessageBox.Show(
									$"Error executing SCP:\n{errorMsg}",
									"Error SCP",
									MessageBoxButtons.OK,
									MessageBoxIcon.Error);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error general in SCP: {ex.Message}");
				MessageBox.Show($"Error performing SCP: {ex.Message}",
						"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				// Restore the cursor
				Cursor = Cursors.Default;
				Console.WriteLine("FIN DE EJECUCIÓN SCP");
			}
		}

		private void NotifyScpCompleted(string fileName)
		{
			try
			{
				// Search for the MainForm to show the notification
				foreach (Form form in Application.OpenForms)
				{
					if (form is MainForm mainForm)
					{
						string clipboardInfo = string.IsNullOrWhiteSpace(settings.ScpClipboardText)
								? ""
								: "\nEl enlace ha sido copiado al portapapeles.";

						// Use a specific method for notifications in MainForm
						mainForm.ShowNotification("SCP completed",
								$"The image has been uploaded successfully.{clipboardInfo}");
						return;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error notifying SCP: {ex.Message}");
			}
		}

		// Method to diagnose configuration problems
		private void PrintSettingsDiagnostic()
		{
			Console.WriteLine("\n===== CONFIGURATION DIAGNOSTIC =====");
			Console.WriteLine($"Undo Shortcut: {settings.UndoShortcut} ({GetKeyDescription(settings.UndoShortcut)})");
			Console.WriteLine($"Save Shortcut: {settings.SaveShortcut} ({GetKeyDescription(settings.SaveShortcut)})");
			Console.WriteLine($"Copy Shortcut: {settings.CopyShortcut} ({GetKeyDescription(settings.CopyShortcut)})");
			Console.WriteLine($"Cancel Shortcut: {settings.CancelShortcut} ({GetKeyDescription(settings.CancelShortcut)})");
			Console.WriteLine($"OCR Shortcut: {settings.OcrShortcut} ({GetKeyDescription(settings.OcrShortcut)})");
			Console.WriteLine($"SCP Shortcut: {settings.ScpShortcut} ({GetKeyDescription(settings.ScpShortcut)})");
			Console.WriteLine($"ColorPicker Shortcut: {settings.ColorPickerShortcut} ({GetKeyDescription(settings.ColorPickerShortcut)})");
			Console.WriteLine($"Color Format: {settings.ColorFormat}");
			Console.WriteLine($"Numeric value of Control+V: {Keys.Control | Keys.V}");
			Console.WriteLine("========================================\n");
		}

		private string GetKeyDescription(Keys key)
		{
			string description = "";

			if ((key & Keys.Control) == Keys.Control)
				description += "Control + ";
			if ((key & Keys.Shift) == Keys.Shift)
				description += "Shift + ";
			if ((key & Keys.Alt) == Keys.Alt)
				description += "Alt + ";

			Keys keyCode = key & Keys.KeyCode;
			description += keyCode.ToString();

			return description;
		}

		// Color format conversion methods
		private string GetColorString(Color color, string format)
		{
			switch (format)
			{
				case "RGB":
					return $"rgb({color.R}, {color.G}, {color.B})";
				case "HEX":
					return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
				case "HSL":
					float h, s, l;
					ColorToHSL(color, out h, out s, out l);
					return $"hsl({Math.Round(h)}, {Math.Round(s * 100)}%, {Math.Round(l * 100)}%)";
				default:
					return $"rgb({color.R}, {color.G}, {color.B})";
			}
		}

		// Convert RGB to HSL
		private void ColorToHSL(Color color, out float h, out float s, out float l)
		{
			float r = color.R / 255f;
			float g = color.G / 255f;
			float b = color.B / 255f;
			
			float max = Math.Max(r, Math.Max(g, b));
			float min = Math.Min(r, Math.Min(g, b));
			
			// Calculate lightness
			l = (max + min) / 2;
			
			// If max equals min, it's a shade of gray
			if (Math.Abs(max - min) < 0.0001f)
			{
				h = s = 0; // No saturation, and hue is undefined
			}
			else
			{
				float delta = max - min;
				
				// Calculate saturation
				s = l > 0.5f ? delta / (2 - max - min) : delta / (max + min);
				
				// Calculate hue
				if (max == r)
				{
					h = (g - b) / delta + (g < b ? 6 : 0);
				}
				else if (max == g)
				{
					h = (b - r) / delta + 2;
				}
				else // max == b
				{
					h = (r - g) / delta + 4;
				}
				
				h *= 60; // Convert to degrees
			}
		}

		// Add after ProcessCmdKey method

		private void ActivateColorPicker()
		{
			if (!isScreenshotValid) return;
			
			Console.WriteLine("Activating color picker mode");
			
			// Reset state
			isColorPickerMode = true;
			isSelecting = false;
			isResizing = false;
			isDrawing = false;
			isColorSelected = false;
			
			// Hide buttons if visible
			if (buttonsPanel != null)
			{
				buttonsPanel.Visible = false;
			}
			
			// Create preview bitmap if needed
			if (colorPickerPreview == null)
			{
				colorPickerPreview = new Bitmap(ColorPickerPreviewSize, ColorPickerPreviewSize);
			}
			
			// Update cursor
			this.Cursor = Cursors.Cross;
			
			// Force redraw
			this.Invalidate();
		}

		private void ProcessColorPick(Point location)
		{
			if (!isColorPickerMode || !isScreenshotValid) return;
			
			try
			{
				// Adjust coordinates for the screenshot
				int offsetX = this.Bounds.X - totalScreenBounds.X;
				int offsetY = this.Bounds.Y - totalScreenBounds.Y;
				
				Point adjustedPoint = new Point(
					location.X - offsetX,
					location.Y - offsetY
				);
				
				// Ensure the point is within the bounds of the screenshot
				if (adjustedPoint.X >= 0 && adjustedPoint.X < screenshotWidth &&
					adjustedPoint.Y >= 0 && adjustedPoint.Y < screenshotHeight)
				{
					// Get color from the screenshot at the cursor position
					selectedColor = screenshot.GetPixel(adjustedPoint.X, adjustedPoint.Y);
					colorPickerPoint = location;
					
					// Update preview
					UpdateColorPickerPreview(location);
					
					// Force redraw
					this.Invalidate();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in ProcessColorPick: {ex.Message}");
			}
		}

		private void UpdateColorPickerPreview(Point location)
		{
			if (colorPickerPreview == null || !isScreenshotValid) return;
			
			try
			{
				// Calculate preview area on the screenshot
				int offsetX = this.Bounds.X - totalScreenBounds.X;
				int offsetY = this.Bounds.Y - totalScreenBounds.Y;
				
				Point screenshotPoint = new Point(
					location.X - offsetX,
					location.Y - offsetY
				);
				
				// Calculate preview area dimensions (smaller area when zoomed)
				int previewSourceSize = ColorPickerPreviewSize / ColorPickerZoomFactor;
				int halfSourceSize = previewSourceSize / 2;
				
				// Center of the preview area
				int previewCenterX = screenshotPoint.X;
				int previewCenterY = screenshotPoint.Y;
				
				// Calculate top-left of the source preview area
				int previewX = Math.Max(0, previewCenterX - halfSourceSize);
				int previewY = Math.Max(0, previewCenterY - halfSourceSize);
				
				// Ensure we don't go out of bounds
				previewX = Math.Min(previewX, screenshotWidth - previewSourceSize);
				previewY = Math.Min(previewY, screenshotHeight - previewSourceSize);
				
				// If we can't get a full size preview, adjust
				if (previewX < 0) previewX = 0;
				if (previewY < 0) previewY = 0;
				
				// Create source and destination rectangles for zoomed drawing
				Rectangle sourceRect = new Rectangle(
					previewX, 
					previewY, 
					Math.Min(previewSourceSize, screenshotWidth - previewX),
					Math.Min(previewSourceSize, screenshotHeight - previewY)
				);
				
				Rectangle destRect = new Rectangle(
					0, 
					0, 
					ColorPickerPreviewSize,
					ColorPickerPreviewSize
				);
				
				// Draw the preview with zoom
				using (Graphics g = Graphics.FromImage(colorPickerPreview))
				{
					// Fill with a solid background to remove any transparency
					g.FillRectangle(Brushes.White, 0, 0, ColorPickerPreviewSize, ColorPickerPreviewSize);
					
					// Set interpolation mode to NearestNeighbor to see individual pixels
					g.InterpolationMode = InterpolationMode.NearestNeighbor;
					g.PixelOffsetMode = PixelOffsetMode.Half;
					
					// Draw the zoomed preview
					g.DrawImage(screenshot, destRect, sourceRect, GraphicsUnit.Pixel);
					
					// Draw crosshair at the pixel we're selecting
					int crosshairX = (screenshotPoint.X - previewX) * ColorPickerZoomFactor;
					int crosshairY = (screenshotPoint.Y - previewY) * ColorPickerZoomFactor;
					
					// Make sure crosshair is within the preview bounds
					if (crosshairX >= 0 && crosshairX < ColorPickerPreviewSize &&
						crosshairY >= 0 && crosshairY < ColorPickerPreviewSize)
					{
						// Draw a pixel outline around the selected pixel
						int pixelSize = ColorPickerZoomFactor;
						
						// Draw black box around the pixel
						using (Pen pixelOutline = new Pen(Color.Black, 2))
						{
							int boxX = crosshairX - (pixelSize / 2);
							int boxY = crosshairY - (pixelSize / 2);
							g.DrawRectangle(pixelOutline, boxX, boxY, pixelSize, pixelSize);
						}
						
						// Draw white inner box
						using (Pen pixelInnerOutline = new Pen(Color.White, 1))
						{
							pixelInnerOutline.DashStyle = DashStyle.Dot;
							int boxX = crosshairX - (pixelSize / 2);
							int boxY = crosshairY - (pixelSize / 2);
							g.DrawRectangle(pixelInnerOutline, boxX, boxY, pixelSize, pixelSize);
						}
						
						// Draw crosshair
						using (Pen crosshairPen = new Pen(Color.Black, 1))
						{
							// Draw outer black lines
							g.DrawLine(crosshairPen, 0, crosshairY, ColorPickerPreviewSize, crosshairY);
							g.DrawLine(crosshairPen, crosshairX, 0, crosshairX, ColorPickerPreviewSize);
						}
						
						using (Pen crosshairPen = new Pen(Color.White, 1))
						{
							// Draw inner white lines (dashed effect)
							crosshairPen.DashStyle = DashStyle.Dot;
							g.DrawLine(crosshairPen, 0, crosshairY, ColorPickerPreviewSize, crosshairY);
							g.DrawLine(crosshairPen, crosshairX, 0, crosshairX, ColorPickerPreviewSize);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in UpdateColorPickerPreview: {ex.Message}");
			}
		}

		private void FinishColorPick()
		{
			if (!isColorPickerMode || !isScreenshotValid || selectedColor == Color.Empty) return;
			
			try
			{
				// Copy the color to clipboard in the specified format
				string colorString = GetColorString(selectedColor, settings.ColorFormat);
				Clipboard.SetText(colorString);
				
				// Mark as selected so we can show a notification
				isColorSelected = true;
				
				// Notify about the selected color
				string formatName = settings.ColorFormat;
				this.BeginInvoke(new Action(() =>
				{
					this.Close();
					NotifyColorPicked(selectedColor, colorString, formatName);
				}));
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in FinishColorPick: {ex.Message}");
				MessageBox.Show($"Error copying color: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void NotifyColorPicked(Color color, string colorString, string format)
		{
			try
			{
				// Search for the MainForm to show the notification
				foreach (Form form in Application.OpenForms)
				{
					if (form is MainForm mainForm)
					{
						// Use a specific method for notifications in MainForm
						mainForm.ShowNotification("Color Picked", 
							$"Color {format}: {colorString}\nCopied to clipboard.");
						return;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error notifying color pick: {ex.Message}");
			}
		}
	}
}