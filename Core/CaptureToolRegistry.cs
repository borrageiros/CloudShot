using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CloudShot.Overlay;

namespace CloudShot.Core
{
	public sealed class CaptureToolDefinition
	{
		public CaptureToolbarAction ToolbarAction { get; }
		public DrawingToolMode? DrawingMode { get; }
		public string DisplayLabel { get; }
		public int ToolbarGroup { get; }

		private readonly Func<AppSettings, bool> getEnabled;
		private readonly Action<AppSettings, bool> setEnabled;
		private readonly Func<AppSettings, Keys> getToolbarShortcut;
		private readonly Func<AppSettings, bool> additionalVisibilityCheck;

		internal CaptureToolDefinition(
			CaptureToolbarAction toolbarAction,
			DrawingToolMode? drawingMode,
			string displayLabel,
			int toolbarGroup,
			Func<AppSettings, bool> getEnabled,
			Action<AppSettings, bool> setEnabled,
			Func<AppSettings, Keys> getToolbarShortcut,
			Func<AppSettings, bool> additionalVisibilityCheck = null)
		{
			ToolbarAction = toolbarAction;
			DrawingMode = drawingMode;
			DisplayLabel = displayLabel;
			ToolbarGroup = toolbarGroup;
			this.getEnabled = getEnabled;
			this.setEnabled = setEnabled;
			this.getToolbarShortcut = getToolbarShortcut;
			this.additionalVisibilityCheck = additionalVisibilityCheck;
		}

		public bool GetEnabled(AppSettings settings) => getEnabled(settings);

		public void SetEnabled(AppSettings settings, bool enabled) => setEnabled(settings, enabled);

		public Keys GetToolbarShortcut(AppSettings settings) => getToolbarShortcut(settings);

		public bool IsToolbarVisible(AppSettings settings)
		{
			if (!getEnabled(settings))
				return false;

			return additionalVisibilityCheck == null || additionalVisibilityCheck(settings);
		}
	}

	public static class CaptureToolRegistry
	{
		private static readonly CaptureToolDefinition[] OrderedDefinitions =
		{
			Define(CaptureToolbarAction.PenMode, DrawingToolMode.Pen, "Pen", 0,
				s => s.ToolPenEnabled, (s, v) => s.ToolPenEnabled = v, s => s.PenToolShortcut),
			Define(CaptureToolbarAction.EraserMode, DrawingToolMode.Eraser, "Eraser", 0,
				s => s.ToolEraserEnabled, (s, v) => s.ToolEraserEnabled = v, s => s.EraserToolShortcut),
			Define(CaptureToolbarAction.RectangleMode, DrawingToolMode.Rectangle, "Rectangle", 0,
				s => s.ToolRectangleEnabled, (s, v) => s.ToolRectangleEnabled = v, s => s.RectangleToolShortcut),
			Define(CaptureToolbarAction.FilledRectangleMode, DrawingToolMode.FilledRectangle, "Filled rectangle", 0,
				s => s.ToolFilledRectangleEnabled, (s, v) => s.ToolFilledRectangleEnabled = v, s => s.FilledRectangleToolShortcut),
			Define(CaptureToolbarAction.PixelateMode, DrawingToolMode.Pixelate, "Pixelate", 0,
				s => s.ToolPixelateEnabled, (s, v) => s.ToolPixelateEnabled = v, s => s.PixelateToolShortcut),
			Define(CaptureToolbarAction.ArrowMode, DrawingToolMode.Arrow, "Arrow", 0,
				s => s.ToolArrowEnabled, (s, v) => s.ToolArrowEnabled = v, s => s.ArrowToolShortcut),
			Define(CaptureToolbarAction.HighlighterMode, DrawingToolMode.Highlighter, "Highlighter", 0,
				s => s.ToolHighlighterEnabled, (s, v) => s.ToolHighlighterEnabled = v, s => s.HighlighterToolShortcut),
			Define(CaptureToolbarAction.LineMode, DrawingToolMode.Line, "Line", 0,
				s => s.ToolLineEnabled, (s, v) => s.ToolLineEnabled = v, s => s.LineToolShortcut),
			Define(CaptureToolbarAction.StepsMode, DrawingToolMode.Steps, "Steps", 0,
				s => s.ToolStepsEnabled, (s, v) => s.ToolStepsEnabled = v, s => s.StepsToolShortcut),
			Define(CaptureToolbarAction.TextMode, DrawingToolMode.Text, "Text", 0,
				s => s.ToolTextEnabled, (s, v) => s.ToolTextEnabled = v, s => s.TextToolShortcut),
			Define(CaptureToolbarAction.Move, null, "Move", 1,
				s => s.ToolMoveEnabled, (s, v) => s.ToolMoveEnabled = v, s => s.MoveToolShortcut),
			Define(CaptureToolbarAction.ColorPicker, null, "Color", 1,
				s => s.ToolColorPickerEnabled, (s, v) => s.ToolColorPickerEnabled = v, _ => Keys.None),
			Define(CaptureToolbarAction.Undo, null, "Undo", 1,
				s => s.ToolUndoEnabled, (s, v) => s.ToolUndoEnabled = v, s => s.UndoShortcut),
			Define(CaptureToolbarAction.Copy, null, "Copy", 2,
				s => s.ToolCopyEnabled, (s, v) => s.ToolCopyEnabled = v, s => s.CopyShortcut),
			Define(CaptureToolbarAction.Save, null, "Save", 2,
				s => s.ToolSaveEnabled, (s, v) => s.ToolSaveEnabled = v, s => s.SaveShortcut),
			Define(CaptureToolbarAction.Ocr, null, "OCR", 2,
				s => s.ToolOcrEnabled, (s, v) => s.ToolOcrEnabled = v, s => s.OcrShortcut),
			Define(CaptureToolbarAction.Scp, null, "Upload", 2,
				s => s.ToolScpEnabled, (s, v) => s.ToolScpEnabled = v, s => s.ScpShortcut,
				s => !string.IsNullOrWhiteSpace(s.ScpHost)),
			Define(CaptureToolbarAction.Close, null, "Cancel", 2,
				s => s.ToolCloseEnabled, (s, v) => s.ToolCloseEnabled = v, s => s.CancelShortcut)
		};

		private static readonly Dictionary<CaptureToolbarAction, CaptureToolDefinition> ByToolbarAction =
			OrderedDefinitions.ToDictionary(d => d.ToolbarAction);

		private static readonly Dictionary<DrawingToolMode, CaptureToolDefinition> ByDrawingMode =
			OrderedDefinitions.Where(d => d.DrawingMode.HasValue).ToDictionary(d => d.DrawingMode.Value);

		public static IReadOnlyList<CaptureToolDefinition> Definitions => OrderedDefinitions;

		public static IReadOnlyList<CaptureToolbarAction> ToolbarDisplayOrder { get; } =
			OrderedDefinitions.Select(d => d.ToolbarAction).ToArray();

		public static IReadOnlyList<DrawingToolMode> DrawingModeFallbackOrder { get; } =
			OrderedDefinitions.Where(d => d.DrawingMode.HasValue).Select(d => d.DrawingMode.Value).ToArray();

		public static bool IsToolbarActionVisible(AppSettings settings, CaptureToolbarAction action)
		{
			return TryGetByToolbarAction(action, out CaptureToolDefinition definition) &&
			       definition.IsToolbarVisible(settings);
		}

		public static bool IsDrawingToolEnabled(AppSettings settings, DrawingToolMode mode)
		{
			return TryGetByDrawingMode(mode, out CaptureToolDefinition definition) &&
			       definition.GetEnabled(settings);
		}

		public static bool TryGetByToolbarAction(CaptureToolbarAction action, out CaptureToolDefinition definition)
		{
			return ByToolbarAction.TryGetValue(action, out definition);
		}

		public static bool TryGetByDrawingMode(DrawingToolMode mode, out CaptureToolDefinition definition)
		{
			return ByDrawingMode.TryGetValue(mode, out definition);
		}

		public static string GetDisplayLabel(CaptureToolbarAction action)
		{
			return TryGetByToolbarAction(action, out CaptureToolDefinition definition)
				? definition.DisplayLabel
				: action.ToString();
		}

		public static int GetToolbarGroup(CaptureToolbarAction action)
		{
			return TryGetByToolbarAction(action, out CaptureToolDefinition definition)
				? definition.ToolbarGroup
				: 2;
		}

		public static void ResetToolEnabledToDefaults(AppSettings settings)
		{
			foreach (CaptureToolDefinition definition in OrderedDefinitions)
				definition.SetEnabled(settings, true);
		}

		public static void ResetDrawingToolShortcutsToDefaults(AppSettings settings)
		{
			settings.PenToolShortcut = Keys.P;
			settings.RectangleToolShortcut = Keys.R;
			settings.FilledRectangleToolShortcut = Keys.F;
			settings.PixelateToolShortcut = Keys.X;
			settings.ArrowToolShortcut = Keys.A;
			settings.HighlighterToolShortcut = Keys.H;
			settings.LineToolShortcut = Keys.L;
			settings.StepsToolShortcut = Keys.N;
			settings.TextToolShortcut = Keys.T;
			settings.EraserToolShortcut = Keys.E;
			settings.MoveToolShortcut = Keys.M;
		}

		private static CaptureToolDefinition Define(
			CaptureToolbarAction toolbarAction,
			DrawingToolMode? drawingMode,
			string displayLabel,
			int toolbarGroup,
			Func<AppSettings, bool> getEnabled,
			Action<AppSettings, bool> setEnabled,
			Func<AppSettings, Keys> getToolbarShortcut,
			Func<AppSettings, bool> additionalVisibilityCheck = null)
		{
			return new CaptureToolDefinition(
				toolbarAction,
				drawingMode,
				displayLabel,
				toolbarGroup,
				getEnabled,
				setEnabled,
				getToolbarShortcut,
				additionalVisibilityCheck);
		}
	}
}
