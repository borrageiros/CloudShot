using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CloudShot.Core
{
	public class ScpUploadResult
	{
		public bool Success { get; }
		public string ErrorMessage { get; }

		private ScpUploadResult(bool success, string errorMessage)
		{
			Success = success;
			ErrorMessage = errorMessage;
		}

		public static ScpUploadResult Ok()
		{
			return new ScpUploadResult(true, null);
		}

		public static ScpUploadResult Fail(string errorMessage)
		{
			return new ScpUploadResult(false, errorMessage);
		}
	}

	public class ScpUploadService
	{
		public ScpUploadResult Upload(string localFilePath, string host, int port, string remotePath, string keyPath, string keyPassphrase)
		{
			if (string.IsNullOrWhiteSpace(host))
			{
				return ScpUploadResult.Fail("No destination host configured.");
			}

			if (string.IsNullOrWhiteSpace(localFilePath) || !File.Exists(localFilePath))
			{
				return ScpUploadResult.Fail("The file to upload does not exist.");
			}

			string expandedKey = ExpandUserPath(keyPath);
			if (!string.IsNullOrWhiteSpace(expandedKey) && !File.Exists(expandedKey))
			{
				return ScpUploadResult.Fail($"The private key file does not exist:\n{expandedKey}");
			}

			string resolvedHost = SshConfigResolver.ResolveScpHost(host.Trim());
			bool usePassphrase = !string.IsNullOrEmpty(keyPassphrase);
			int effectivePort = port > 0 ? port : 22;
			string target = BuildRemoteTarget(resolvedHost, remotePath);
			string arguments = BuildArguments(localFilePath, target, effectivePort, expandedKey, !usePassphrase);

			string passphraseFile = null;
			string askPassScript = null;

			try
			{
				if (usePassphrase)
				{
					passphraseFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pass");
					File.WriteAllText(passphraseFile, keyPassphrase, new UTF8Encoding(false));

					askPassScript = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cmd");
					string escapedPath = passphraseFile.Replace("'", "''");
					string scriptContent =
						"@echo off\r\n" +
						$"powershell -NoProfile -ExecutionPolicy Bypass -Command \"Write-Output ([IO.File]::ReadAllText('{escapedPath}'))\"\r\n";
					File.WriteAllText(askPassScript, scriptContent, Encoding.ASCII);
				}

				using (Process process = new Process())
				{
					process.StartInfo.FileName = "scp";
					process.StartInfo.Arguments = arguments;
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.CreateNoWindow = true;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.RedirectStandardError = true;
					process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
					process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

					if (usePassphrase)
					{
						process.StartInfo.EnvironmentVariables["SSH_ASKPASS"] = askPassScript;
						process.StartInfo.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
						process.StartInfo.EnvironmentVariables["DISPLAY"] = "1";
					}

					StringBuilder error = new StringBuilder();
					process.OutputDataReceived += (s, e) => { };
					process.ErrorDataReceived += (s, e) =>
					{
						if (!string.IsNullOrEmpty(e.Data))
						{
							error.AppendLine(e.Data);
						}
					};

					try
					{
						process.Start();
					}
					catch (System.ComponentModel.Win32Exception)
					{
						return ScpUploadResult.Fail(
							"Could not start 'scp'.\nMake sure the OpenSSH client is installed and available in PATH.");
					}

					process.BeginOutputReadLine();
					process.BeginErrorReadLine();
					process.WaitForExit();

					if (process.ExitCode == 0)
					{
						return ScpUploadResult.Ok();
					}

					string message = error.ToString().Trim();
					if (string.IsNullOrEmpty(message))
					{
						message = "scp exited with an error. Verify the host, port, private key and remote path.";
					}

					message = AppendHints(message, host, resolvedHost, usePassphrase, expandedKey);
					return ScpUploadResult.Fail(message);
				}
			}
			catch (Exception ex)
			{
				return ScpUploadResult.Fail(ex.Message);
			}
			finally
			{
				DeleteTempFile(passphraseFile);
				DeleteTempFile(askPassScript);
			}
		}

		private static string BuildRemoteTarget(string host, string remotePath)
		{
			string path = remotePath ?? string.Empty;
			if (string.IsNullOrWhiteSpace(path))
			{
				return host;
			}

			if (!path.StartsWith("/") && !path.StartsWith("~"))
			{
				path = path.TrimStart('/');
			}

			return host + ":" + path;
		}

		private static string AppendHints(string message, string configuredHost, string resolvedHost, bool usePassphrase, string keyPath)
		{
			if (message.IndexOf("permission denied", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				if (!configuredHost.Contains("@") &&
				    !string.Equals(configuredHost, resolvedHost, StringComparison.OrdinalIgnoreCase))
				{
					message += $"\n\nResolved host from SSH config: {resolvedHost}";
				}
				else if (!configuredHost.Contains("@"))
				{
					message += "\n\nTry setting Host to user@server (for example root@143.47.49.167) or an SSH config alias.";
				}

				if (!usePassphrase && !string.IsNullOrWhiteSpace(keyPath))
				{
					message += "\n\nIf your private key has a passphrase, enter it in Settings → SCP → Key passphrase.";
				}
			}

			return message;
		}

		private static string BuildArguments(string localFilePath, string target, int port, string keyPath, bool batchMode)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("-P ").Append(port).Append(' ');
			sb.Append("-o StrictHostKeyChecking=accept-new ");

			if (batchMode)
			{
				sb.Append("-o BatchMode=yes ");
			}

			if (!string.IsNullOrWhiteSpace(keyPath))
			{
				sb.Append("-i \"").Append(keyPath).Append("\" ");
			}

			sb.Append('"').Append(localFilePath).Append("\" ");
			sb.Append('"').Append(target).Append('"');
			return sb.ToString();
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

		public static string ExpandUserPath(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return path;
			}

			if (path.StartsWith("~"))
			{
				return path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
			}

			return path;
		}
	}
}
