using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CloudShot.Core
{
	public class UpdateCheckResult
	{
		public bool UpdateAvailable { get; }
		public string LatestVersion { get; }
		public string CurrentVersion { get; }
		public string ReleaseUrl { get; }

		public UpdateCheckResult(bool updateAvailable, string latestVersion, string currentVersion, string releaseUrl)
		{
			UpdateAvailable = updateAvailable;
			LatestVersion = latestVersion;
			CurrentVersion = currentVersion;
			ReleaseUrl = releaseUrl;
		}
	}

	public static class UpdateService
	{
		private const string LatestReleaseApiUrl = "https://api.github.com/repos/borrageiros/CloudShot/releases/latest";
		private const string DownloadPageUrl = "https://borrageiros.github.io/CloudShot/";

		public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
		{
			string currentVersion = GetCurrentVersion();

			try
			{
				string json = await DownloadStringAsync(LatestReleaseApiUrl).ConfigureAwait(false);

				string tag = ExtractValue(json, "tag_name");
				if (string.IsNullOrEmpty(tag))
				{
					return new UpdateCheckResult(false, null, currentVersion, null);
				}

				bool available = IsNewer(tag, currentVersion);

				return new UpdateCheckResult(available, NormalizeVersion(tag), currentVersion, DownloadPageUrl);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error checking for updates: {ex.Message}");
				return new UpdateCheckResult(false, null, currentVersion, null);
			}
		}

		private static string GetCurrentVersion()
		{
			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			return version != null ? version.ToString() : "0.0";
		}

		private static Task<string> DownloadStringAsync(string url)
		{
			return Task.Run(() =>
			{
				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
				request.UserAgent = "CloudShot-UpdateChecker";
				request.Accept = "application/vnd.github+json";
				request.Timeout = 10000;

				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				using (Stream stream = response.GetResponseStream())
				using (StreamReader reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			});
		}

		private static string ExtractValue(string json, string key)
		{
			Match match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
			return match.Success ? match.Groups[1].Value : null;
		}

		private static bool IsNewer(string latestRaw, string currentRaw)
		{
			if (!TryParseVersion(latestRaw, out Version latest))
			{
				return false;
			}

			if (!TryParseVersion(currentRaw, out Version current))
			{
				return false;
			}

			return latest > current;
		}

		private static bool TryParseVersion(string raw, out Version version)
		{
			version = null;

			if (string.IsNullOrWhiteSpace(raw))
			{
				return false;
			}

			Match match = Regex.Match(raw, @"\d+(\.\d+)*");
			if (!match.Success)
			{
				return false;
			}

			string cleaned = match.Value;
			if (!cleaned.Contains("."))
			{
				cleaned += ".0";
			}

			return Version.TryParse(cleaned, out version);
		}

		private static string NormalizeVersion(string raw)
		{
			return TryParseVersion(raw, out Version version) ? version.ToString() : raw;
		}
	}
}
