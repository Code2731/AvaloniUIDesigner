using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace AvaloniaUIDesigner.App.Models;

public static class DesignerStyleApplicationMetadata
{
    private static readonly ConditionalWeakTable<Control, HashSet<string>> AppliedProperties = new();
    private static readonly ConditionalWeakTable<Control, UpdateState> UpdateStates = new();

    public static bool IsProgrammaticUpdate(Control control)
        => UpdateStates.TryGetValue(control, out var state) && state.Depth > 0;

    public static void BeginProgrammaticUpdate(Control control)
        => UpdateStates.GetOrCreateValue(control).Depth++;

    public static void EndProgrammaticUpdate(Control control)
    {
        if (UpdateStates.TryGetValue(control, out var state) && state.Depth > 0)
        {
            state.Depth--;
        }
    }

    public static bool IsApplied(Control control, string propertyName)
        => AppliedProperties.TryGetValue(control, out var properties)
            && properties.Contains(propertyName);

    public static IReadOnlyCollection<string> GetAppliedProperties(Control control)
        => AppliedProperties.TryGetValue(control, out var properties)
            ? new List<string>(properties)
            : new List<string>();

    public static void MarkApplied(Control control, string propertyName)
        => AppliedProperties.GetOrCreateValue(control).Add(propertyName);

    public static void ClearApplied(Control control, string propertyName)
    {
        if (AppliedProperties.TryGetValue(control, out var properties))
        {
            properties.Remove(propertyName);
        }
    }

    public static void ClearAll(Control control)
    {
        if (AppliedProperties.TryGetValue(control, out var properties))
        {
            properties.Clear();
        }
    }

    private sealed class UpdateState
    {
        public int Depth { get; set; }
    }
}
