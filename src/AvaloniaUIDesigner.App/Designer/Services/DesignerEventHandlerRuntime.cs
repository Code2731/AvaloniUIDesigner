using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaloniaUIDesigner.App.Models;
using AvaloniaUIDesigner.App.ViewModels;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed record DesignerEventHandlerMapEntry(
    string ControlName,
    string EventName,
    string HandlerName);

public sealed record DesignerEventHandlerAttribute(string Name, string Value);

public static class DesignerEventHandlerRuntime
{
    private const string InternalPropertyName = "__eventHandlers";
    private static readonly ConditionalWeakTable<Control, Dictionary<string, string>> Handlers = new();
    private static readonly string[] CommonEventNames =
    [
        "AttachedToVisualTree",
        "ContextRequested",
        "DataContextChanged",
        "DetachedFromVisualTree",
        "DoubleTapped",
        "GotFocus",
        "Initialized",
        "KeyDown",
        "KeyUp",
        "LayoutUpdated",
        "LostFocus",
        "PointerCaptureLost",
        "PointerEntered",
        "PointerExited",
        "PointerMoved",
        "PointerPressed",
        "PointerReleased",
        "PointerWheelChanged",
        "SizeChanged",
        "Tapped",
        "TextInput",
        "Unloaded",
    ];

    private static readonly IReadOnlyDictionary<string, string[]> SpecificEventNames =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Button"] = ["Click"],
            ["ToggleButton"] = ["Click", "Checked", "Unchecked", "Indeterminate"],
            ["CheckBox"] = ["Checked", "Unchecked", "Indeterminate"],
            ["RadioButton"] = ["Checked", "Unchecked", "Indeterminate"],
            ["ToggleSwitch"] = ["Checked", "Unchecked", "Indeterminate"],
            ["TextBox"] = ["TextChanged"],
            ["MaskedTextBox"] = ["TextChanged"],
            ["ComboBox"] = ["SelectionChanged"],
            ["ListBox"] = ["SelectionChanged"],
            ["TreeView"] = ["SelectionChanged"],
            ["TabControl"] = ["SelectionChanged"],
            ["DataGrid"] = ["SelectionChanged"],
            ["Slider"] = ["ValueChanged"],
            ["ProgressBar"] = ["ValueChanged"],
            ["NumericUpDown"] = ["ValueChanged"],
            ["Expander"] = ["Expanded", "Collapsed"],
            ["Calendar"] = ["SelectedDatesChanged"],
            ["DatePicker"] = ["SelectedDateChanged"],
            ["CalendarDatePicker"] = ["SelectedDateChanged"],
            ["ColorPicker"] = ["ColorChanged"],
        };

    public static IReadOnlyList<string> SupportedCommonEventNames { get; } = CommonEventNames;

    public static IReadOnlyDictionary<string, string> Read(Control control)
    {
        var result = Handlers.TryGetValue(control, out var stored)
            ? new Dictionary<string, string>(stored, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (control is Button button
            && control is not ToggleButton
            && button.Tag is ButtonClickHandlerMetadata clickMetadata
            && !string.IsNullOrWhiteSpace(clickMetadata.HandlerName))
        {
            result["Click"] = clickMetadata.HandlerName;
        }

        return result;
    }

    public static void ReplaceForMap(
        Control control,
        IReadOnlyDictionary<string, string> handlers)
    {
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in handlers)
        {
            if (string.Equals(pair.Key, "Click", StringComparison.OrdinalIgnoreCase)
                && control is Button button
                && control is not ToggleButton)
            {
                button.Tag = string.IsNullOrWhiteSpace(pair.Value)
                    ? null
                    : new ButtonClickHandlerMetadata(pair.Value);
                continue;
            }

            normalized[pair.Key] = pair.Value;
        }

        var stored = Handlers.GetOrCreateValue(control);
        stored.Clear();
        foreach (var pair in normalized)
        {
            stored[pair.Key] = pair.Value;
        }
    }

    public static void Apply(
        Control control,
        IReadOnlyDictionary<string, string> properties)
    {
        if (!properties.TryGetValue(InternalPropertyName, out var json)
            || !TryDeserialize(json, out var handlers))
        {
            ClearStoredHandlers(control);
            return;
        }

        ReplaceForMap(control, handlers);
    }

    public static void Capture(
        Control control,
        IDictionary<string, string> properties)
    {
        var handlers = Read(control);
        if (control is Button and not ToggleButton)
        {
            handlers = handlers
                .Where(pair => !string.Equals(pair.Key, "Click", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        if (handlers.Count == 0)
        {
            properties.Remove(InternalPropertyName);
            return;
        }

        properties[InternalPropertyName] = Serialize(handlers);
    }

    public static IReadOnlyList<DesignerEventHandlerAttribute> GetAxamlAttributes(
        IReadOnlyDictionary<string, string>? properties)
    {
        if (properties is null
            || !properties.TryGetValue(InternalPropertyName, out var json)
            || !TryDeserialize(json, out var handlers))
        {
            return [];
        }

        return handlers
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new DesignerEventHandlerAttribute(pair.Key, pair.Value))
            .ToList();
    }

    public static string GetEditorText(IEnumerable<DesignElement> elements)
    {
        var lines = new List<string>
        {
            "# Format: ControlName | EventName | HandlerName",
            $"# Common events: {string.Join(", ", CommonEventNames)}",
            "# Leave the editable lines empty to clear all event handlers on unlocked controls.",
        };

        foreach (var element in elements)
        {
            foreach (var pair in Read(element.Visual).OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                var prefix = element.IsLocked ? "# LOCKED | " : string.Empty;
                lines.Add($"{prefix}{element.DisplayName} | {pair.Key} | {pair.Value}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static bool TryParseEditorText(
        string? text,
        out IReadOnlyList<DesignerEventHandlerMapEntry> entries,
        out string error)
    {
        var parsed = new List<DesignerEventHandlerMapEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var fields = line.Split('|');
            if (fields.Length != 3)
            {
                entries = [];
                error = $"Line {index + 1} must use ControlName | EventName | HandlerName.";
                return false;
            }

            var controlName = fields[0].Trim();
            var eventName = fields[1].Trim();
            var handlerName = fields[2].Trim();
            if (controlName.Length == 0 || handlerName.Length == 0)
            {
                entries = [];
                error = $"Line {index + 1} must include a control name and handler name.";
                return false;
            }

            if (!TryNormalizeEventName(eventName, out var canonicalEventName, out var eventError))
            {
                entries = [];
                error = $"Line {index + 1}: {eventError}";
                return false;
            }

            if (!TryNormalizeHandlerName(handlerName, out var normalizedHandlerName, out var handlerError))
            {
                entries = [];
                error = $"Line {index + 1}: {handlerError}";
                return false;
            }

            var key = $"{controlName}\u001F{canonicalEventName}";
            if (!seen.Add(key))
            {
                entries = [];
                error = $"Line {index + 1}: the same control event is listed more than once.";
                return false;
            }

            parsed.Add(new DesignerEventHandlerMapEntry(
                controlName,
                canonicalEventName,
                normalizedHandlerName));
        }

        entries = parsed;
        error = string.Empty;
        return true;
    }

    public static bool IsSupportedEventForControl(string tagName, string eventName)
    {
        if (!TryNormalizeEventName(eventName, out var canonicalEventName, out _))
        {
            return false;
        }

        if (Array.Exists(
                CommonEventNames,
                candidate => string.Equals(candidate, canonicalEventName, StringComparison.Ordinal)))
        {
            return true;
        }

        return SpecificEventNames.TryGetValue(tagName, out var specificEvents)
            && Array.Exists(
                specificEvents,
                candidate => string.Equals(candidate, canonicalEventName, StringComparison.OrdinalIgnoreCase));
    }

    public static bool TryNormalizeEventName(
        string rawValue,
        out string normalizedValue,
        out string error)
    {
        var candidate = rawValue.Trim();
        var match = CommonEventNames.FirstOrDefault(
            name => string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            foreach (var eventNames in SpecificEventNames.Values)
            {
                match = eventNames.FirstOrDefault(
                    name => string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                {
                    break;
                }
            }
        }

        if (match is null)
        {
            normalizedValue = string.Empty;
            error = $"Unsupported event '{candidate}'.";
            return false;
        }

        normalizedValue = match;
        error = string.Empty;
        return true;
    }

    public static bool TryNormalizeHandlerName(
        string rawValue,
        out string normalizedValue,
        out string error)
    {
        normalizedValue = rawValue.Trim();
        if (normalizedValue.Length == 0 || !IsValidIdentifier(normalizedValue))
        {
            error = "Handler names must start with a letter or underscore and contain only letters, numbers, or underscores.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static string Serialize(IReadOnlyDictionary<string, string> handlers)
        => JsonSerializer.Serialize(
            handlers
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));

    public static bool TryDeserialize(
        string? json,
        out IReadOnlyDictionary<string, string> handlers)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (parsed is not null)
                {
                    handlers = new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
                    return true;
                }
            }
            catch (JsonException)
            {
                // Ignore malformed internal metadata and keep the control editable.
            }
        }

        handlers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return false;
    }

    private static bool IsValidIdentifier(string value)
    {
        if (value.Length == 0 || !(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        for (var index = 1; index < value.Length; index++)
        {
            if (!(char.IsLetterOrDigit(value[index]) || value[index] == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static void ClearStoredHandlers(Control control)
    {
        if (Handlers.TryGetValue(control, out var stored))
        {
            stored.Clear();
        }
    }
}
