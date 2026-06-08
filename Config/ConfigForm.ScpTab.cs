using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using CloudShot.Core;
using CloudShot.Overlay;
using Microsoft.Win32;

namespace CloudShot
{
	public partial class ConfigForm
	{
		private TabPage BuildScpTab()
		{
			var page = new TabPage("SCP")
			{
				BackColor = Color.White,
				Padding = new Padding(16, 16, 16, 20),
				AutoScroll = true
			};

			var layout = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				ColumnCount = 1,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				BackColor = Color.White,
				Margin = new Padding(0)
			};
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

			txtScpHost = CreateInputField();
			SetupPlaceholder(txtScpHost, ScpHostPlaceholder);

			numScpPort = new NumericUpDown
			{
				Minimum = 1,
				Maximum = 65535,
				Value = 22,
				Font = BodyFont,
				Margin = Padding.Empty
			};

			txtScpRemotePath = CreateInputField();
			SetupPlaceholder(txtScpRemotePath, ScpRemotePathPlaceholder);

			txtScpKeyPath = CreateInputField();
			SetupPlaceholder(txtScpKeyPath, ScpKeyPlaceholder);

			var keyHint = CreateCardHintLabel("Path to your SSH private key (id_ed25519, id_rsa, ...).");

			txtScpKeyPassphrase = CreateInputField();
			txtScpKeyPassphrase.UseSystemPasswordChar = true;

			var passphraseHint = CreateCardHintLabel("Required if your private key is protected with a passphrase.");

			var clipboardHint = CreateCardHintLabel("Text copied after a successful upload. <image> is replaced with the remote filename.");

			txtScpClipboardText = CreateInputField();
			SetupPlaceholder(txtScpClipboardText, ScpClipboardPlaceholder);

			int row = 0;
			AddCardRow(layout, ref row, BuildCard(
				"Connection",
				"Upload captures using the built-in scp command (OpenSSH). Host accepts user@server or an alias from ~/.ssh/config.",
				BuildFieldStack(
					CreateFieldGroup("Host", null, txtScpHost),
					CreateFieldGroup("Port", null, numScpPort, 90),
					CreateFieldGroup("Remote path", null, txtScpRemotePath))));
			AddCardRow(layout, ref row, BuildCard(
				"SSH authentication",
				null,
				BuildFieldStack(
					CreateFieldGroup("SSH private key", keyHint, WrapWithBrowse(txtScpKeyPath)),
					CreateFieldGroup("Key passphrase (optional)", passphraseHint, WrapWithPasswordToggle(txtScpKeyPassphrase)))));
			AddCardRow(layout, ref row, BuildCard(
				"After upload",
				null,
				BuildFieldStack(
					CreateFieldGroup("Clipboard text (optional)", clipboardHint, txtScpClipboardText))));

			page.Controls.Add(layout);
			return page;
		}
	}
}
