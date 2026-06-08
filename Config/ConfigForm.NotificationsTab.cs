using System;
using System.Drawing;
using System.Windows.Forms;

namespace CloudShot
{
	public partial class ConfigForm
	{
		private TabPage BuildNotificationsTab()
		{
			var page = new TabPage("Notifications")
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
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

			chkNotificationsEnabled = CreateNotificationCheckBox("Show notifications");
			chkNotificationsEnabled.CheckedChanged += (s, e) => UpdateNotificationCheckboxesState();

			chkNotifyOnCopy = CreateNotificationCheckBox("Notify when copying to clipboard");
			chkNotifyOnSave = CreateNotificationCheckBox("Notify when saving to file");
			chkNotifyOnOcr = CreateNotificationCheckBox("Notify when extracting text (OCR)");
			chkNotifyOnScp = CreateNotificationCheckBox("Notify when uploading via SCP");
			chkNotifyOnColorPicker = CreateNotificationCheckBox("Notify when picking a color");
			chkNotifyOnUpdate = CreateNotificationCheckBox("Notify about new updates");

			int row = 0;
			AddCardRow(layout, ref row, BuildCard(
				"General",
				"Master switch for all Windows notifications shown by CloudShot.",
				AlignCardContent(chkNotificationsEnabled)));
			AddCardRow(layout, ref row, BuildCard(
				"Events",
				"Choose which actions trigger a notification.",
				BuildFieldStack(
					chkNotifyOnCopy,
					chkNotifyOnSave,
					chkNotifyOnOcr,
					chkNotifyOnScp,
					chkNotifyOnColorPicker,
					chkNotifyOnUpdate)));

			page.Controls.Add(layout);
			return page;
		}

		private CheckBox CreateNotificationCheckBox(string text)
		{
			return new CheckBox
			{
				Text = text,
				Font = BodyFont,
				AutoSize = true,
				Margin = new Padding(0)
			};
		}

		private void UpdateNotificationCheckboxesState()
		{
			bool enabled = chkNotificationsEnabled != null && chkNotificationsEnabled.Checked;

			foreach (CheckBox checkBox in new[]
			{
				chkNotifyOnCopy,
				chkNotifyOnSave,
				chkNotifyOnOcr,
				chkNotifyOnScp,
				chkNotifyOnColorPicker,
				chkNotifyOnUpdate
			})
			{
				if (checkBox != null)
				{
					checkBox.Enabled = enabled;
				}
			}
		}
	}
}
