using System;
using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUIDesigner.App.Designer.Services;

namespace AvaloniaUIDesigner.App.Designer.Controls;

public sealed class DesignerCustomPropertyGridLengthEditor : Grid
{
    public DesignerCustomPropertyGridLengthEditor(DesignerCustomPropertyValueState state)
    {
        if (!state.UsesGridLengthEditor)
        {
            throw new ArgumentException("The property state does not support GridLength editing.", nameof(state));
        }

        var initialValue = state.Source is DesignerCustomPropertyValueSource.Local
                or DesignerCustomPropertyValueSource.Style
            ? state.Value
            : "Auto";
        if (!DesignerCustomPropertyRuntime.TryGetGridLengthParts(
                initialValue,
                out var value,
                out var unit,
                out _))
        {
            value = 1;
            unit = DesignerCustomGridLengthUnit.Auto;
        }

        ColumnDefinitions = new ColumnDefinitions("110,*");
        ColumnSpacing = 6;
        UnitEditor = new ComboBox
        {
            Tag = $"{state.PropertyName}.Unit",
            ItemsSource = Enum.GetNames<DesignerCustomGridLengthUnit>(),
            SelectedItem = unit.ToString(),
            MinHeight = 28,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        UnitEditor.Classes.Add("custom-property-grid-length-unit");
        AutomationProperties.SetName(UnitEditor, $"{state.DisplayName} unit");
        ValueEditor = new NumericUpDown
        {
            Tag = $"{state.PropertyName}.Value",
            Minimum = 0,
            Maximum = decimal.MaxValue,
            Increment = 0.5m,
            FormatString = "0.###############",
            Value = (decimal)value,
            MinHeight = 28,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        ValueEditor.Classes.Add("custom-property-grid-length-value");
        AutomationProperties.SetName(ValueEditor, $"{state.DisplayName} value");
        UnitEditor.SelectionChanged += (_, _) => UpdateValueEditorState();
        Children.Add(UnitEditor);
        Children.Add(ValueEditor);
        SetColumn(ValueEditor, 1);
        UpdateValueEditorState();
    }

    public ComboBox UnitEditor { get; }

    public NumericUpDown ValueEditor { get; }

    public bool TryGetValue(out string value, out string error)
    {
        if (!Enum.TryParse<DesignerCustomGridLengthUnit>(
                UnitEditor.SelectedItem?.ToString(),
                out var unit))
        {
            value = string.Empty;
            error = "Choose Pixel, Star, or Auto.";
            return false;
        }

        if (unit == DesignerCustomGridLengthUnit.Auto)
        {
            value = "Auto";
            error = string.Empty;
            return true;
        }

        if (ValueEditor.Value is not { } numericValue)
        {
            value = string.Empty;
            error = "Enter a non-negative GridLength value.";
            return false;
        }

        var formattedValue = numericValue.ToString(CultureInfo.InvariantCulture);
        value = unit == DesignerCustomGridLengthUnit.Star
            ? numericValue == 1 ? "*" : $"{formattedValue}*"
            : formattedValue;
        error = string.Empty;
        return true;
    }

    public void SetValue(DesignerCustomGridLengthUnit unit, decimal value)
    {
        UnitEditor.SelectedItem = unit.ToString();
        ValueEditor.Value = value;
        UpdateValueEditorState();
    }

    private void UpdateValueEditorState()
    {
        ValueEditor.IsEnabled = !string.Equals(
            UnitEditor.SelectedItem?.ToString(),
            nameof(DesignerCustomGridLengthUnit.Auto),
            StringComparison.Ordinal);
    }
}
