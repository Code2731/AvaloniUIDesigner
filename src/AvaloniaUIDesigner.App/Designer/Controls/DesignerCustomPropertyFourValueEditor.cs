using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Controls;

public sealed class DesignerCustomPropertyFourValueEditor : Grid
{
    private readonly NumericUpDown[] _valueEditors;

    public DesignerCustomPropertyFourValueEditor(DesignerCustomPropertyValueState state)
    {
        if (!state.UsesFourValueEditor)
        {
            throw new ArgumentException("The property state does not support four-value editing.", nameof(state));
        }

        var labels = state.Type == DesignerCustomPropertyType.Thickness
            ? new[] { "L", "T", "R", "B" }
            : new[] { "TL", "TR", "BR", "BL" };
        var accessibleLabels = state.Type == DesignerCustomPropertyType.Thickness
            ? new[] { "left", "top", "right", "bottom" }
            : new[] { "top-left", "top-right", "bottom-right", "bottom-left" };
        var initialValue = state.Source is DesignerCustomPropertyValueSource.Local
                or DesignerCustomPropertyValueSource.Style
            ? state.Value
            : "0";
        if (!DesignerCustomPropertyRuntime.TryGetFourValueComponents(
                state.Type,
                initialValue,
                out var components,
                out _))
        {
            components = [0, 0, 0, 0];
        }

        ColumnDefinitions = new ColumnDefinitions("*,*,*,*");
        ColumnSpacing = 4;
        _valueEditors = new NumericUpDown[4];
        for (var index = 0; index < _valueEditors.Length; index++)
        {
            var valueEditor = new NumericUpDown
            {
                Tag = $"{state.PropertyName}.{labels[index]}",
                Minimum = state.Type == DesignerCustomPropertyType.CornerRadius
                    ? 0
                    : decimal.MinValue,
                Maximum = decimal.MaxValue,
                Increment = 0.5m,
                FormatString = "0.###############",
                Value = (decimal)components[index],
                MinHeight = 28,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            valueEditor.Classes.Add("custom-property-four-value-input");
            AutomationProperties.SetName(
                valueEditor,
                $"{state.DisplayName} {accessibleLabels[index]} value");
            _valueEditors[index] = valueEditor;

            var field = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock
                    {
                        Text = labels[index],
                        FontSize = 8,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                    valueEditor,
                },
            };
            Children.Add(field);
            SetColumn(field, index);
        }
    }

    public IReadOnlyList<NumericUpDown> ValueEditors => _valueEditors;

    public bool TryGetValue(out string value, out string error)
    {
        var values = new string[4];
        for (var index = 0; index < _valueEditors.Length; index++)
        {
            if (_valueEditors[index].Value is not { } component)
            {
                value = string.Empty;
                error = "Every directional value is required.";
                return false;
            }

            values[index] = component.ToString(CultureInfo.InvariantCulture);
        }

        value = string.Join(",", values);
        error = string.Empty;
        return true;
    }

    public void SetValues(decimal first, decimal second, decimal third, decimal fourth)
    {
        _valueEditors[0].Value = first;
        _valueEditors[1].Value = second;
        _valueEditors[2].Value = third;
        _valueEditors[3].Value = fourth;
    }
}
