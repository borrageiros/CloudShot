using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CloudShot.Core
{
	public class SshConfigResolver
	{
		private class SshConfigEntry
		{
			public List<string> HostPatterns { get; } = new List<string>();
			public string HostName { get; set; }
			public string User { get; set; }
		}

		public static string ResolveScpHost(string host)
		{
			if (string.IsNullOrWhiteSpace(host))
			{
				return host;
			}

			string trimmed = host.Trim();
			if (trimmed.Contains("@"))
			{
				return trimmed;
			}

			string configPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				".ssh",
				"config");

			if (!File.Exists(configPath))
			{
				return trimmed;
			}

			List<SshConfigEntry> entries = ParseConfig(File.ReadAllLines(configPath));

			SshConfigEntry aliasMatch = entries.FirstOrDefault(entry =>
				entry.HostPatterns.Any(pattern =>
					string.Equals(pattern, trimmed, StringComparison.OrdinalIgnoreCase)));

			if (aliasMatch != null)
			{
				return trimmed;
			}

			SshConfigEntry hostNameMatch = entries.FirstOrDefault(entry =>
				!string.IsNullOrWhiteSpace(entry.HostName) &&
				string.Equals(entry.HostName, trimmed, StringComparison.OrdinalIgnoreCase));

			if (hostNameMatch == null)
			{
				return trimmed;
			}

			string alias = hostNameMatch.HostPatterns.FirstOrDefault(pattern =>
				!string.IsNullOrWhiteSpace(pattern) &&
				!pattern.Contains("*") &&
				!pattern.Contains("?"));

			if (!string.IsNullOrWhiteSpace(alias))
			{
				return alias;
			}

			if (!string.IsNullOrWhiteSpace(hostNameMatch.User))
			{
				return hostNameMatch.User + "@" + trimmed;
			}

			return trimmed;
		}

		private static List<SshConfigEntry> ParseConfig(string[] lines)
		{
			List<SshConfigEntry> entries = new List<SshConfigEntry>();
			SshConfigEntry current = null;

			foreach (string rawLine in lines)
			{
				string line = rawLine.Trim();
				if (line.Length == 0 || line.StartsWith("#"))
				{
					continue;
				}

				int spaceIndex = line.IndexOf(' ');
				if (spaceIndex < 0)
				{
					continue;
				}

				string keyword = line.Substring(0, spaceIndex);
				string value = line.Substring(spaceIndex + 1).Trim();
				if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
				{
					value = value.Substring(1, value.Length - 2);
				}

				if (string.Equals(keyword, "Host", StringComparison.OrdinalIgnoreCase))
				{
					current = new SshConfigEntry();
					current.HostPatterns.AddRange(value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
					entries.Add(current);
					continue;
				}

				if (current == null)
				{
					continue;
				}

				if (string.Equals(keyword, "HostName", StringComparison.OrdinalIgnoreCase))
				{
					current.HostName = value;
				}
				else if (string.Equals(keyword, "User", StringComparison.OrdinalIgnoreCase))
				{
					current.User = value;
				}
			}

			return entries;
		}
	}
}
