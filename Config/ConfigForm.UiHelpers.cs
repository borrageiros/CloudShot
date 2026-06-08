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
		private static void AddCardRow(TableLayoutPanel layout, ref int row, Control card)
		{
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.Controls.Add(card, 0, row++);
		}

		private Control BuildCard(string title, string description, Control content)
		{
			var card = new Panel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				BackColor = CardColor,
				Margin = new Padding(0, 0, 0, CardSpacing),
				Padding = new Padding(16, 13, 16, 15)
			};
			card.Paint += (s, e) =>
			{
				using (var pen = new Pen(CardBorderColor))
					e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
			};

			var inner = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				ColumnCount = 1,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				BackColor = Color.Transparent,
				Margin = new Padding(0)
			};
			inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

			int innerRow = 0;
			inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			inner.Controls.Add(new Label
			{
				Text = title,
				Font = CardTitleFont,
				ForeColor = CardTitleColor,
				AutoSize = true,
				Margin = new Padding(0, 0, 0, description != null ? 3 : 10)
			}, 0, innerRow++);

			if (description != null)
			{
				inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
				inner.Controls.Add(new Label
				{
					Text = description,
					Font = HintFont,
					ForeColor = CardDescriptionColor,
					AutoSize = true,
					MaximumSize = new Size(440, 0),
					Margin = new Padding(0, 0, 0, 12)
				}, 0, innerRow++);
			}

			content.Dock = DockStyle.Fill;
			content.Margin = Padding.Empty;
			inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			inner.Controls.Add(content, 0, innerRow++);

			card.Controls.Add(inner);
			card.Resize += (s, e) => inner.Width = card.ClientSize.Width;
			inner.Width = card.ClientSize.Width;
			return card;
		}

		private static Control AlignCardContent(Control control)
		{
			control.Margin = Padding.Empty;

			if (control is CheckBox checkBox)
			{
				checkBox.AutoSize = true;
				checkBox.Padding = Padding.Empty;
				checkBox.Margin = Padding.Empty;
				var host = new Panel
				{
					AutoSize = true,
					AutoSizeMode = AutoSizeMode.GrowAndShrink,
					Dock = DockStyle.Top,
					BackColor = Color.Transparent,
					Margin = Padding.Empty,
					Padding = Padding.Empty
				};
				checkBox.Location = new Point(0, 0);
				host.Controls.Add(checkBox);
				return host;
			}

			return AlignInputRow(control);
		}

		private static Control AlignInputRow(Control control, int? fixedWidth = null)
		{
			control.Margin = Padding.Empty;

			var row = new Panel
			{
				Height = InputRowHeight,
				Dock = DockStyle.Top,
				BackColor = Color.Transparent,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};

			if (fixedWidth.HasValue)
			{
				control.Size = new Size(fixedWidth.Value, InputRowHeight);
				control.Location = new Point(0, 0);
				control.Anchor = AnchorStyles.Left | AnchorStyles.Top;
			}
			else
			{
				control.Dock = DockStyle.Fill;
			}

			row.Controls.Add(control);
			return row;
		}

		private static Control EnsureInputRow(Control input, int? fixedWidth = null)
		{
			if (fixedWidth == null && input is TableLayoutPanel table && table.RowCount == 1)
				return input;

			if (fixedWidth == null && input is Panel panel && panel.Height == InputRowHeight)
				return input;

			return AlignInputRow(input, fixedWidth);
		}

		private static Label CreateCardHintLabel(string text)
		{
			return new Label
			{
				Text = text,
				Font = HintFont,
				ForeColor = CardDescriptionColor,
				AutoSize = true,
				MaximumSize = new Size(440, 0),
				Margin = new Padding(0, 0, 0, 4)
			};
		}

		private static Control BuildFieldStack(params Control[] fields)
		{
			var stack = new TableLayoutPanel
			{
				ColumnCount = 1,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Top,
				BackColor = Color.Transparent,
				Margin = new Padding(0)
			};
			stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

			for (int i = 0; i < fields.Length; i++)
			{
				fields[i].Margin = new Padding(0, 0, 0, i < fields.Length - 1 ? 10 : 0);
				stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
				stack.Controls.Add(fields[i], 0, i);
			}

			return stack;
		}

		private Button CreateSecondaryButton(string text, int width)
		{
			var button = new Button
			{
				Text = text,
				Size = new Size(width, 28),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.White,
				ForeColor = SecondaryButtonForeColor,
				Font = BodyFont,
				Cursor = Cursors.Hand
			};
			button.FlatAppearance.BorderColor = SecondaryButtonBorderColor;
			button.FlatAppearance.BorderSize = 1;
			button.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 245, 248);
			button.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 235, 240);
			return button;
		}

		private Control CreateFieldGroup(string labelText, Label hint, Control input, int? fixedWidth = null)
		{
			var group = new TableLayoutPanel
			{
				ColumnCount = 1,
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				BackColor = Color.Transparent,
				Margin = new Padding(0, 0, 0, 10),
				Padding = Padding.Empty
			};
			group.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

			int row = 0;
			if (labelText != null)
			{
				group.RowStyles.Add(new RowStyle(SizeType.AutoSize));
				group.Controls.Add(new Label
				{
					Text = labelText,
					Font = BodyFont,
					AutoSize = true,
					Margin = new Padding(0, 0, 0, 4)
				}, 0, row++);
			}

			if (hint != null)
			{
				group.RowStyles.Add(new RowStyle(SizeType.AutoSize));
				group.Controls.Add(hint, 0, row++);
			}

			group.RowStyles.Add(new RowStyle(SizeType.Absolute, InputRowHeight));
			group.Controls.Add(EnsureInputRow(input, fixedWidth), 0, row);
			return group;
		}

		private Control WrapWithBrowse(TextBox input)
		{
			var panel = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 1,
				Height = InputRowHeight,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));

			input.Dock = DockStyle.Fill;
			input.Margin = new Padding(0, 0, 6, 0);

			var browse = CreateSecondaryButton("Browse...", 92);
			browse.Dock = DockStyle.Fill;
			browse.Margin = new Padding(0);
			browse.Click += (s, e) => BrowseForKeyFile(input);

			panel.Controls.Add(input, 0, 0);
			panel.Controls.Add(browse, 1, 0);
			return panel;
		}

		private Control WrapWithPasswordToggle(TextBox input)
		{
			var panel = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 1,
				Height = InputRowHeight,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
			panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));

			input.Dock = DockStyle.Fill;
			input.Margin = new Padding(0, 0, 6, 0);

			var toggle = new CheckBox
			{
				Text = "Show",
				Dock = DockStyle.Fill,
				Font = HintFont,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(0)
			};
			toggle.CheckedChanged += (s, e) => input.UseSystemPasswordChar = !toggle.Checked;

			panel.Controls.Add(input, 0, 0);
			panel.Controls.Add(toggle, 1, 0);
			return panel;
		}

		private void BrowseForKeyFile(TextBox target)
		{
			using (var dialog = new OpenFileDialog
			{
				Title = "Select SSH private key",
				Filter = "All files (*.*)|*.*",
				CheckFileExists = true
			})
			{
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					target.Text = dialog.FileName;
					target.ForeColor = SystemColors.WindowText;
				}
			}
		}

		private Panel BuildFooter()
		{
			var footer = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 52,
				BackColor = FooterColor,
				Padding = new Padding(16, 10, 16, 10)
			};

			var separator = new Panel
			{
				Dock = DockStyle.Top,
				Height = 1,
				BackColor = Color.FromArgb(220, 220, 225)
			};
			footer.Controls.Add(separator);

			btnReset = CreateFooterButton("Reset defaults", 120);
			btnReset.Location = new Point(16, 14);
			btnReset.Click += BtnReset_Click;

			btnCancel = CreateFooterButton("Cancel", 88);
			btnSave = CreateAccentButton("Save", 88);

			btnCancel.Location = new Point(footer.Width - 200, 14);
			btnSave.Location = new Point(footer.Width - 104, 14);
			btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;

			btnCancel.Click += BtnCancel_Click;
			btnSave.Click += BtnSave_Click;

			footer.Controls.Add(btnReset);
			footer.Controls.Add(btnCancel);
			footer.Controls.Add(btnSave);
			footer.Resize += (s, e) =>
			{
				btnCancel.Location = new Point(footer.Width - 200, 14);
				btnSave.Location = new Point(footer.Width - 104, 14);
			};

			return footer;
		}

		private static Button CreateFooterButton(string text, int width)
		{
			return new Button
			{
				Text = text,
				Size = new Size(width, 30),
				FlatStyle = FlatStyle.System,
				Font = BodyFont
			};
		}

		private Button CreateAccentButton(string text, int width)
		{
			var button = new Button
			{
				Text = text,
				Size = new Size(width, 30),
				FlatStyle = FlatStyle.Flat,
				BackColor = AccentColor,
				ForeColor = Color.White,
				Font = BodyFont
			};
			button.FlatAppearance.BorderSize = 0;
			return button;
		}

		private static TextBox CreateInputField()
		{
			return new TextBox
			{
				Dock = DockStyle.Fill,
				Font = BodyFont,
				BorderStyle = BorderStyle.FixedSingle
			};
		}

		private static void SetupPlaceholder(TextBox textBox, string placeholder)
		{
			textBox.Tag = placeholder;
			textBox.GotFocus += (s, e) =>
			{
				if ((string)textBox.Tag == textBox.Text)
				{
					textBox.Text = "";
					textBox.ForeColor = SystemColors.WindowText;
				}
			};
			textBox.LostFocus += (s, e) =>
			{
				if (string.IsNullOrWhiteSpace(textBox.Text))
				{
					textBox.Text = (string)textBox.Tag;
					textBox.ForeColor = Color.Gray;
				}
			};
		}

		private static void ApplyPlaceholder(TextBox textBox, string value, string placeholder)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				textBox.Text = placeholder;
				textBox.ForeColor = Color.Gray;
			}
			else
			{
				textBox.Text = value;
				textBox.ForeColor = SystemColors.WindowText;
			}
		}

		private static string GetTextBoxValue(TextBox textBox)
		{
			string placeholder = textBox.Tag as string;
			if (placeholder != null && textBox.Text == placeholder)
				return "";
			return textBox.Text.Trim();
		}

		private static Color ParseColorOrDefault(string value, Color fallback)
		{
			if (string.IsNullOrWhiteSpace(value))
				return fallback;

			try
			{
				return ColorTranslator.FromHtml(value);
			}
			catch
			{
				return fallback;
			}
		}

		private static string ToHex(Color color)
		{
			return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
		}

	}
}
