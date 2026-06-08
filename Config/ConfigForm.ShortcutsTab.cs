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
		private TabPage BuildShortcutsTab()
		{
			var page = new TabPage("Shortcuts")
			{
				BackColor = Color.White,
				Padding = new Padding(0)
			};

			var container = new Panel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				Padding = new Padding(16, 12, 16, 16),
				BackColor = Color.White
			};

			var hint = new Label
			{
				Text = "Click a shortcut cell and press the key combination you want to assign.",
				Font = HintFont,
				ForeColor = Color.FromArgb(100, 100, 110),
				Dock = DockStyle.Top,
				AutoSize = true,
				MaximumSize = new Size(460, 0),
				Padding = new Padding(0, 0, 0, 12)
			};

			var actionsTable = BuildShortcutTable(8);
			AddTableHeaderCell(actionsTable, 0, 0, "Action");
			AddTableHeaderCell(actionsTable, 0, 1, "Shortcut");

			txtUndo = AddTableShortcutRow(actionsTable, 1, "Undo last edit");
			txtSave = AddTableShortcutRow(actionsTable, 2, "Save to computer");
			txtCopy = AddTableShortcutRow(actionsTable, 3, "Copy to clipboard");
			txtOcr = AddTableShortcutRow(actionsTable, 4, "Extract text (OCR)");
			txtScp = AddTableShortcutRow(actionsTable, 5, "Upload via SCP");
			txtColorPicker = AddTableShortcutRow(actionsTable, 6, "Color picker");
			txtCancel = AddTableShortcutRow(actionsTable, 7, "Cancel capture");

			var toolsSection = new Label
			{
				Text = "Drawing tools",
				Font = TitleFont,
				ForeColor = Color.FromArgb(50, 50, 60),
				Dock = DockStyle.Top,
				AutoSize = true,
				Padding = new Padding(0, 16, 0, 8)
			};

			var toolsTable = BuildShortcutTable(12);
			AddTableHeaderCell(toolsTable, 0, 0, "Tool");
			AddTableHeaderCell(toolsTable, 0, 1, "Shortcut");

			txtPenTool = AddTableShortcutRow(toolsTable, 1, "Pen");
			txtEraserTool = AddTableShortcutRow(toolsTable, 2, "Eraser");
			txtRectangleTool = AddTableShortcutRow(toolsTable, 3, "Rectangle");
			txtFilledRectangleTool = AddTableShortcutRow(toolsTable, 4, "Filled rectangle");
			txtPixelateTool = AddTableShortcutRow(toolsTable, 5, "Pixelate");
			txtArrowTool = AddTableShortcutRow(toolsTable, 6, "Arrow");
			txtHighlighterTool = AddTableShortcutRow(toolsTable, 7, "Highlighter");
			txtLineTool = AddTableShortcutRow(toolsTable, 8, "Line");
			txtStepsTool = AddTableShortcutRow(toolsTable, 9, "Steps");
			txtTextTool = AddTableShortcutRow(toolsTable, 10, "Text");
			txtMoveTool = AddTableShortcutRow(toolsTable, 11, "Move selection");

			container.Controls.Add(toolsTable);
			container.Controls.Add(toolsSection);
			container.Controls.Add(actionsTable);
			container.Controls.Add(hint);
			page.Controls.Add(container);
			return page;
		}

		private TableLayoutPanel BuildShortcutTable(int rowCount)
		{
			var table = new TableLayoutPanel
			{
				ColumnCount = 2,
				RowCount = rowCount,
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
				BackColor = TableBorderColor,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
			table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
			table.RowStyles.Add(new RowStyle(SizeType.Absolute, TableHeaderHeight));

			for (int i = 1; i < rowCount; i++)
				table.RowStyles.Add(new RowStyle(SizeType.Absolute, TableRowHeight));

			return table;
		}

		private static void AddTableHeaderCell(TableLayoutPanel table, int row, int column, string text)
		{
			var label = new Label
			{
				Text = text,
				Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
				TextAlign = ContentAlignment.MiddleLeft,
				Dock = DockStyle.Fill,
				BackColor = TableHeaderColor,
				Padding = new Padding(10, 0, 10, 0),
				Margin = Padding.Empty
			};
			table.Controls.Add(label, column, row);
		}

		private HotkeyControl AddTableShortcutRow(TableLayoutPanel table, int row, string action)
		{
			var actionLabel = new Label
			{
				Text = action,
				Font = BodyFont,
				TextAlign = ContentAlignment.MiddleLeft,
				Dock = DockStyle.Fill,
				BackColor = Color.White,
				Padding = new Padding(10, 0, 10, 0),
				Margin = Padding.Empty
			};
			table.Controls.Add(actionLabel, 0, row);

			var hotkey = new HotkeyControl();
			shortcutBindings.Add((action, hotkey));
			hotkey.DuplicateValidator = key => GetShortcutConflict(hotkey, key);

			var hotkeyCell = new Panel
			{
				Dock = DockStyle.Fill,
				BackColor = Color.White,
				Padding = new Padding(6, 5, 6, 5),
				Margin = Padding.Empty
			};
			hotkey.Dock = DockStyle.Fill;
			hotkeyCell.Controls.Add(hotkey);
			table.Controls.Add(hotkeyCell, 1, row);

			return hotkey;
		}

		private string GetShortcutConflict(HotkeyControl source, Keys key)
		{
			if (key == Keys.None)
				return null;

			foreach (var binding in shortcutBindings)
			{
				if (binding.Control != source && binding.Control.Hotkey == key)
					return binding.Label;
			}

			return null;
		}

		private bool ValidateAllShortcuts()
		{
			var assigned = new Dictionary<Keys, string>();

			foreach (var binding in shortcutBindings)
			{
				Keys key = binding.Control.Hotkey;
				if (key == Keys.None)
					continue;

				if (assigned.TryGetValue(key, out string existingLabel))
				{
					MessageBox.Show(
						$"The shortcut \"{HotkeyControl.FormatHotkey(key)}\" is assigned to both \"{existingLabel}\" and \"{binding.Label}\".",
						"Duplicate shortcut",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					binding.Control.Focus();
					return false;
				}

				assigned[key] = binding.Label;
			}

			return true;
		}

	}
}
