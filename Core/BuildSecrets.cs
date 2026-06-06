using System;
using System.Linq;
using System.Reflection;

namespace CloudShot.Core
{
	public static class BuildSecrets
	{
		private const string ImgurClientIdKey = "ImgurClientId";

		private static readonly Lazy<string> embeddedImgurClientId =
			new Lazy<string>(() => ReadMetadata(ImgurClientIdKey));

		public static string EmbeddedImgurClientId => embeddedImgurClientId.Value;

		public static string ResolveImgurClientId(string userClientId)
		{
			if (!string.IsNullOrWhiteSpace(userClientId))
			{
				return userClientId.Trim();
			}

			return EmbeddedImgurClientId;
		}

		private static string ReadMetadata(string key)
		{
			try
			{
				AssemblyMetadataAttribute attribute = Assembly
					.GetExecutingAssembly()
					.GetCustomAttributes<AssemblyMetadataAttribute>()
					.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.Ordinal));

				return attribute?.Value ?? string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}
