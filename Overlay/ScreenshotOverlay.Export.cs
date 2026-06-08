using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using CloudShot.Core;
using CloudShot.Export;

namespace CloudShot
{
	public partial class ScreenshotOverlay
	{
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

					if (ImageExporter.SaveToFile(selectedArea, settings))
					{
						NotifyImageSaved();
					}
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
				bool notifyScp = settings.ShouldNotify(NotificationCategory.Scp);

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

										NotifyScpCompleted(fileName, clipboardText, notifyScp);
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

		private void NotifyImageSaved()
		{
			if (!settings.ShouldNotify(NotificationCategory.Save))
			{
				return;
			}

			foreach (Form form in Application.OpenForms)
			{
				if (form is MainForm mainForm)
				{
					mainForm.ShowNotification("CloudShot", "Screenshot saved to file.");
					return;
				}
			}
		}

		private void NotifyTextExtracted(string text)
		{
			if (!settings.ShouldNotify(NotificationCategory.Ocr))
			{
				return;
			}

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

		private static void NotifyScpCompleted(string fileName, string scpClipboardText, bool notify)
		{
			if (!notify)
			{
				return;
			}

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
	}
}
