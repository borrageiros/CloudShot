using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Windows.UI.Notifications;
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;

namespace CloudShot.Core
{
	public static class ToastNotificationService
	{
		private const string AppId = "Borrageiros.CloudShot";
		private const string ShortcutFileName = "CloudShot.lnk";

		[DllImport("shell32.dll", SetLastError = true)]
		private static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appID);

		public static void Initialize()
		{
			try
			{
				EnsureShortcut();
				SetCurrentProcessExplicitAppUserModelID(AppId);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error initializing toast notifications: {ex.Message}");
			}
		}

		public static void Show(string title, string message)
		{
			ShowInternal(title, message, null);
		}

		public static void ShowWithUrl(string title, string message, string url)
		{
			ShowInternal(title, message, url);
		}

		private static void ShowInternal(string title, string message, string url)
		{
			try
			{
				string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
				string imageXml = File.Exists(iconPath)
					? $"<image placement=\"appLogoOverride\" src=\"file:///{iconPath.Replace("\\", "/")}\"/>"
					: string.Empty;

				string xml = $"<toast><visual><binding template=\"ToastGeneric\">{imageXml}<text>{Escape(title)}</text><text>{Escape(message)}</text></binding></visual></toast>";

				XmlDocument document = new XmlDocument();
				document.LoadXml(xml);

				ToastNotification toast = new ToastNotification(document);

				if (!string.IsNullOrEmpty(url))
				{
					string target = url;
					toast.Activated += (s, e) => OpenUrl(target);
				}

				ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error showing toast notification: {ex.Message}");
			}
		}

		private static void OpenUrl(string url)
		{
			try
			{
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error opening URL: {ex.Message}");
			}
		}

		private static string Escape(string value)
		{
			return System.Security.SecurityElement.Escape(value ?? string.Empty);
		}

		private static void EnsureShortcut()
		{
			string shortcutPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"Microsoft", "Windows", "Start Menu", "Programs", ShortcutFileName);

			string exePath = Process.GetCurrentProcess().MainModule.FileName;
			string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");

			if (File.Exists(shortcutPath))
			{
				return;
			}

			IShellLinkW link = (IShellLinkW)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046")));
			link.SetPath(exePath);
			link.SetArguments(string.Empty);
			link.SetWorkingDirectory(Path.GetDirectoryName(exePath));

			if (File.Exists(iconPath))
			{
				link.SetIconLocation(iconPath, 0);
			}

			IPropertyStore store = (IPropertyStore)link;
			PropertyKey appIdKey = new PropertyKey(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);

			PropVariant value = new PropVariant
			{
				vt = 31,
				pointerValue = Marshal.StringToCoTaskMemUni(AppId)
			};

			store.SetValue(ref appIdKey, ref value);
			store.Commit();
			Marshal.FreeCoTaskMem(value.pointerValue);

			((IPersistFile)link).Save(shortcutPath, true);
		}

		[ComImport]
		[Guid("000214F9-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IShellLinkW
		{
			void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
			void GetIDList(out IntPtr ppidl);
			void SetIDList(IntPtr pidl);
			void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
			void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
			void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
			void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
			void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
			void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
			void GetHotkey(out short pwHotkey);
			void SetHotkey(short wHotkey);
			void GetShowCmd(out int piShowCmd);
			void SetShowCmd(int iShowCmd);
			void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
			void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
			void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
			void Resolve(IntPtr hwnd, uint fFlags);
			void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
		}

		[ComImport]
		[Guid("0000010b-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IPersistFile
		{
			void GetClassID(out Guid pClassID);
			[PreserveSig]
			int IsDirty();
			void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
			void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
			void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
			void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
		}

		[ComImport]
		[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IPropertyStore
		{
			void GetCount(out uint cProps);
			void GetAt(uint iProp, out PropertyKey pkey);
			void GetValue(ref PropertyKey key, out PropVariant pv);
			void SetValue(ref PropertyKey key, ref PropVariant pv);
			void Commit();
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct PropertyKey
		{
			public Guid fmtid;
			public uint pid;

			public PropertyKey(Guid id, uint propertyId)
			{
				fmtid = id;
				pid = propertyId;
			}
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct PropVariant
		{
			[FieldOffset(0)]
			public ushort vt;
			[FieldOffset(8)]
			public IntPtr pointerValue;
			[FieldOffset(8)]
			public long longValue;
		}
	}
}
