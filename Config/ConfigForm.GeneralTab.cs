using System;
using System.Collections.Generic;
using System.Linq;
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
		private TabPage BuildGeneralTab()
		{
			var page = new TabPage("General")
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

			chkStartWithWindows = new CheckBox
			{
				Text = "Start CloudShot automatically with Windows",
				Font = BodyFont,
				AutoSize = true,
				Margin = new Padding(0)
			};

			cmbColorFormat = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Font = BodyFont,
				Margin = Padding.Empty
			};
			cmbColorFormat.Items.AddRange(new object[] { "RGB", "HEX", "HSL" });

			cmbDefaultTool = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Font = BodyFont,
				Margin = Padding.Empty
			};
			foreach (var entry in DefaultToolEntries)
				cmbDefaultTool.Items.Add(entry.Label);

			cmbToolbarPosition = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Font = BodyFont,
				Margin = Padding.Empty
			};
			foreach (var entry in ToolbarPositionEntries)
				cmbToolbarPosition.Items.Add(entry.Label);

			chkReSelectAreaOnOutsideClick = new CheckBox
			{
				Text = "Re-select area when clicking outside the selection",
				Font = BodyFont,
				AutoSize = true,
				Margin = new Padding(0)
			};

			numMaxHistory = new NumericUpDown
			{
				Minimum = 1,
				Maximum = 1000,
				Value = 100,
				Font = BodyFont,
				Margin = Padding.Empty
			};

			int row = 0;
			AddCardRow(layout, ref row, BuildCard(
				"Startup",
				"CloudShot runs in the system tray. Use Print Screen or the tray icon to capture.",
				AlignCardContent(chkStartWithWindows)));
			AddCardRow(layout, ref row, BuildCard(
				"Color picker",
				"Choose how picked colors are formatted when copied to the clipboard.",
				AlignInputRow(cmbColorFormat, 200)));
			AddCardRow(layout, ref row, BuildCard(
				"Drawing",
				"Default color used for pen, rectangle and filled rectangle annotations.",
				BuildDefaultColorPicker()));
			AddCardRow(layout, ref row, BuildCard(
				"Default tool",
				"Tool preselected automatically when a capture starts.",
				AlignInputRow(cmbDefaultTool, 200)));
			AddCardRow(layout, ref row, BuildCard(
				"Toolbar position",
				"Preferred position of the capture toolbar around the selection. Falls back automatically when there is no room.",
				AlignInputRow(cmbToolbarPosition, 200)));
			AddCardRow(layout, ref row, BuildCard(
				"Capture",
				"When disabled, drawing tools can be used anywhere on the screen once a selection exists.",
				AlignCardContent(chkReSelectAreaOnOutsideClick)));
			AddCardRow(layout, ref row, BuildCard(
				"History",
				"Maximum number of annotation steps that can be undone (Ctrl+Z).",
				AlignInputRow(numMaxHistory, 200)));
			AddCardRow(layout, ref row, BuildCard(
				"Toolbar tools",
				"Choose which tools appear in the capture toolbar.",
				BuildToolbarToolsGrid()));

			page.Controls.Add(layout);
			return page;
		}

		private Control BuildToolbarToolsGrid()
		{
			var grid = new TableLayoutPanel
			{
				ColumnCount = 3,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = new Padding(0),
				Dock = DockStyle.Top,
				BackColor = Color.Transparent
			};
			grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
			grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
			grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));

			var toolEntries = CaptureToolRegistry.Definitions
				.Select(d => (d.ToolbarAction, d.DisplayLabel))
				.ToArray();

			int rowCount = (toolEntries.Length + grid.ColumnCount - 1) / grid.ColumnCount;
			for (int r = 0; r < rowCount; r++)
				grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

			toolCheckBoxes.Clear();
			for (int i = 0; i < toolEntries.Length; i++)
			{
				var entry = toolEntries[i];
				var checkBox = new CheckBox
				{
					Text = entry.Item2,
					Font = BodyFont,
					AutoSize = false,
					Dock = DockStyle.Fill,
					TextAlign = ContentAlignment.MiddleLeft,
					Margin = new Padding(0),
					Padding = new Padding(0)
				};
				toolCheckBoxes[entry.Item1] = checkBox;
				grid.Controls.Add(checkBox, i % grid.ColumnCount, i / grid.ColumnCount);
			}

			return grid;
		}

		private Control BuildDefaultColorPicker()
		{
			var row = new TableLayoutPanel
			{
				ColumnCount = 2,
				RowCount = 1,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Top,
				BackColor = Color.Transparent,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			row.RowStyles.Add(new RowStyle(SizeType.Absolute, InputRowHeight));

			defaultColorPreview = new Panel
			{
				Size = new Size(28, 28),
				BackColor = defaultDrawingColor,
				Margin = new Padding(0, 0, 10, 0),
				Cursor = Cursors.Hand
			};
			defaultColorPreview.Paint += (s, e) =>
			{
				using (var pen = new Pen(CardBorderColor))
					e.Graphics.DrawRectangle(pen, 0, 0, defaultColorPreview.Width - 1, defaultColorPreview.Height - 1);
			};
			defaultColorPreview.Click += (s, e) => ChooseDefaultColor();

			defaultColorHexLabel = new Label
			{
				Text = ToHex(defaultDrawingColor),
				Font = BodyFont,
				ForeColor = CardTitleColor,
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};

			row.Controls.Add(defaultColorPreview, 0, 0);
			row.Controls.Add(defaultColorHexLabel, 1, 0);
			return row;
		}

		private void ChooseDefaultColor()
		{
			using (var dialog = new ColorDialog
			{
				Color = defaultDrawingColor,
				FullOpen = true,
				AnyColor = true
			})
			{
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					defaultDrawingColor = dialog.Color;
					defaultColorPreview.BackColor = defaultDrawingColor;
					if (defaultColorHexLabel != null)
						defaultColorHexLabel.Text = ToHex(defaultDrawingColor);
				}
			}
		}
	}
}
