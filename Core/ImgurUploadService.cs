using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CloudShot.Core
{
	public class ImgurUploadResult
	{
		public bool Success { get; }
		public string Link { get; }
		public string ErrorMessage { get; }

		private ImgurUploadResult(bool success, string link, string errorMessage)
		{
			Success = success;
			Link = link;
			ErrorMessage = errorMessage;
		}

		public static ImgurUploadResult Ok(string link)
		{
			return new ImgurUploadResult(true, link, null);
		}

		public static ImgurUploadResult Fail(string errorMessage)
		{
			return new ImgurUploadResult(false, null, errorMessage);
		}
	}

	public class ImgurUploadService
	{
		private const string UploadUrl = "https://api.imgur.com/3/image";

		public ImgurUploadResult Upload(string localFilePath, string clientId)
		{
			if (string.IsNullOrWhiteSpace(clientId))
			{
				return ImgurUploadResult.Fail(
					"No Imgur Client-ID is available.\nThis build was compiled without an embedded Client-ID and none was set in Settings.");
			}

			if (string.IsNullOrWhiteSpace(localFilePath) || !File.Exists(localFilePath))
			{
				return ImgurUploadResult.Fail("The file to upload does not exist.");
			}

			try
			{
				string base64Image = Convert.ToBase64String(File.ReadAllBytes(localFilePath));
				byte[] body = Encoding.UTF8.GetBytes("type=base64&image=" + Uri.EscapeDataString(base64Image));

				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(UploadUrl);
				request.Method = "POST";
				request.UserAgent = "CloudShot";
				request.Headers["Authorization"] = "Client-ID " + clientId.Trim();
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = body.Length;
				request.Timeout = 60000;

				using (Stream requestStream = request.GetRequestStream())
				{
					requestStream.Write(body, 0, body.Length);
				}

				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				using (Stream stream = response.GetResponseStream())
				using (StreamReader reader = new StreamReader(stream))
				{
					string json = reader.ReadToEnd();
					string link = ExtractLink(json);

					if (string.IsNullOrEmpty(link))
					{
						return ImgurUploadResult.Fail("Imgur did not return an image link.");
					}

					return ImgurUploadResult.Ok(link);
				}
			}
			catch (WebException ex)
			{
				return ImgurUploadResult.Fail(BuildWebExceptionMessage(ex));
			}
			catch (Exception ex)
			{
				return ImgurUploadResult.Fail(ex.Message);
			}
		}

		private static string ExtractLink(string json)
		{
			Match match = Regex.Match(json, "\"link\"\\s*:\\s*\"([^\"]*)\"");
			if (!match.Success)
			{
				return null;
			}

			return match.Groups[1].Value.Replace("\\/", "/");
		}

		private static string BuildWebExceptionMessage(WebException ex)
		{
			if (ex.Response is HttpWebResponse errorResponse)
			{
				try
				{
					using (Stream stream = errorResponse.GetResponseStream())
					using (StreamReader reader = new StreamReader(stream))
					{
						string json = reader.ReadToEnd();
						Match match = Regex.Match(json, "\"error\"\\s*:\\s*\"([^\"]*)\"");
						string detail = match.Success ? match.Groups[1].Value : json;

						return $"Imgur returned {(int)errorResponse.StatusCode} ({errorResponse.StatusCode}).\n{detail}".Trim();
					}
				}
				catch
				{
					return $"Imgur returned {(int)errorResponse.StatusCode} ({errorResponse.StatusCode}).";
				}
			}

			return ex.Message;
		}
	}
}
