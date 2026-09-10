using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;

namespace AvaloniaUIDesigner.App.Designer.Controls;

public sealed class DesignerCustomPropertyEditorPanel : Border
{
    private readonly List<EditorRow> _rows = [];
    private string _filterText = string.Empty;
    private bool _localOnly;
    private bool _isUpdatingLocalOverrides;

    public event EventHandler? FilterResultChanged;

    public int VisibleRowCount { get; private set; }

    public DesignerCustomPropertyEditorPanel(
        IEnumerable<DesignerCustomPropertyValueState> valueStates)
    {
        var rows = new StackPanel { Spacing = 6 };
        Border? categoryHeader = null;
        foreach (var state in DesignerCustomPropertyRuntime.ApplyCategoryHeaders(valueStates))
        {
            if (state.ShowsCategoryHeader)
            {
                categoryHeader = CreateCategoryHeader(state.CategoryLabel);
                rows.Children.Add(categoryHeader);
            }

            rows.Children.Add(CreatePropertyRow(
                state,
                categoryHeader ?? throw new InvalidOperationException("A property category header is required.")));
        }

        Padding = new Thickness(1);
        BorderBrush = new SolidColorBrush(Color.Parse("#334155"));
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(4);
        Child = new ScrollViewer
        {
            Content = rows,
            Padding = new Thickness(8),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        ApplyFilter();
    }

    public void SetFilter(string? text, bool localOnly)
    {
        _filterText = text?.Trim() ?? string.Empty;
        _localOnly = localOnly;
        ApplyFilter();
    }

    public void ClearLocalValues()
    {
        _isUpdatingLocalOverrides = true;
        try
        {
            foreach (var row in _rows)
            {
                row.LocalOverride.IsChecked = false;
            }
        }
        finally
        {
            _isUpdatingLocalOverrides = false;
        }

        ApplyFilter();
    }

    public bool TryCreateEditorLines(
        out IReadOnlyList<string> lines,
        out string error)
    {
        var result = new List<string>();
        foreach (var row in _rows.Where(row => row.LocalOverride.IsChecked == true))
        {
            if (!TryReadEditorValue(row, out var value, out error))
            {
                lines = [];
                return false;
            }

            if (!DesignerCustomPropertyRuntime.TryNormalizeValue(
                    row.State.Definition,
                    value,
                    out var normalizedValue,
                    out var valueError))
            {
                lines = [];
                error = $"{row.State.DisplayName} ({row.State.PropertyName}) {valueError}";
                return false;
            }

            result.Add($"{row.State.PropertyName} = {normalizedValue}");
        }

        lines = result;
        error = string.Empty;
        return true;
    }

    private Control CreatePropertyRow(
        DesignerCustomPropertyValueState state,
        Border categoryHeader)
    {
        var localOverride = new CheckBox
        {
            Content = "Local",
            Tag = state.PropertyName,
            IsChecked = state.Source == DesignerCustomPropertyValueSource.Local,
            VerticalAlignment = VerticalAlignment.Center,
        };
        localOverride.Classes.Add("custom-property-local-toggle");
        AutomationProperties.SetName(localOverride, $"Write {state.DisplayName} as a local value");

        var label = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = state.DisplayName,
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                },
                new TextBlock
                {
                    Text = state.MetadataLabel,
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                },
            },
        };
        ToolTip.SetTip(label, state.InspectorToolTip);

        var editor = CreateValueEditor(state);
        editor.Tag = state.PropertyName;
        editor.IsEnabled = localOverride.IsChecked == true;
        editor.Classes.Add("custom-property-value-editor");
        AutomationProperties.SetName(editor, $"{state.DisplayName} value");

        var sourceLabel = new TextBlock
        {
            Text = state.SourceLabel,
            Tag = state.PropertyName,
            FontSize = 8,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#CBD5E1")),
        };
        sourceLabel.Classes.Add("custom-property-source-label");
        var source = new Border
        {
            Padding = new Thickness(5, 2),
            Background = new SolidColorBrush(Color.Parse("#334155")),
            CornerRadius = new CornerRadius(3),
            VerticalAlignment = VerticalAlignment.Center,
            Child = sourceLabel,
        };
        localOverride.IsCheckedChanged += (_, _) =>
        {
            var writesLocalValue = localOverride.IsChecked == true;
            editor.IsEnabled = writesLocalValue;
            sourceLabel.Text = writesLocalValue
                ? "LOCAL"
                : state.Source == DesignerCustomPropertyValueSource.Local
                    ? "RESET"
                    : state.SourceLabel;
            if (!_isUpdatingLocalOverrides)
            {
                ApplyFilter();
            }
        };

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,150,*,Auto"),
            ColumnSpacing = 10,
            Children = { localOverride, label, editor, source },
        };
        Grid.SetColumn(label, 1);
        Grid.SetColumn(editor, 2);
        Grid.SetColumn(source, 3);
        var row = new Border
        {
            Tag = state.PropertyName,
            Padding = new Thickness(8, 6),
            Background = new SolidColorBrush(Color.Parse("#1F2937")),
            CornerRadius = new CornerRadius(3),
            Child = fields,
        };
        row.Classes.Add("custom-property-editor-row");
        _rows.Add(new EditorRow(
            state,
            localOverride,
            editor,
            sourceLabel,
            row,
            categoryHeader));
        return row;
    }

    private void ApplyFilter()
    {
        foreach (var row in _rows)
        {
            row.Container.IsVisible = (!_localOnly || row.LocalOverride.IsChecked == true)
                && MatchesFilter(row);
        }

        foreach (var categoryHeader in _rows.Select(row => row.CategoryHeader).Distinct())
        {
            categoryHeader.IsVisible = _rows.Any(row => ReferenceEquals(
                row.CategoryHeader,
                categoryHeader) && row.Container.IsVisible);
        }

        VisibleRowCount = _rows.Count(row => row.Container.IsVisible);
        FilterResultChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool MatchesFilter(EditorRow row)
    {
        if (_filterText.Length == 0)
        {
            return true;
        }

        var state = row.State;
        return ContainsFilter(state.PropertyName)
            || ContainsFilter(state.DisplayName)
            || ContainsFilter(state.CategoryLabel)
            || ContainsFilter(state.Description)
            || ContainsFilter(state.TypeLabel)
            || ContainsFilter(state.DisplayValue)
            || ContainsFilter(row.SourceLabel.Text);
    }

    private bool ContainsFilter(string? value)
        => value?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) == true;

    private static Control CreateValueEditor(DesignerCustomPropertyValueState state)
    {
        if (state.UsesChoiceEditor)
        {
            return new ComboBox
            {
                ItemsSource = state.EditorChoices,
                SelectedItem = GetInitialChoice(state),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinHeight = 28,
            };
        }

        if (state.UsesNumericEditor)
        {
            return new NumericUpDown
            {
                Minimum = state.NumericEditorMinimum,
                Maximum = state.NumericEditorMaximum,
                Increment = state.NumericEditorIncrement,
                FormatString = state.NumericEditorFormatString,
                Value = GetInitialNumericValue(state),
                Watermark = state.EditorWatermark,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinHeight = 28,
            };
        }

        if (state.UsesColorEditor)
        {
            return new ColorPicker
            {
                Color = GetInitialColor(state),
                IsAlphaEnabled = true,
                IsAlphaVisible = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinHeight = 28,
            };
        }

        if (state.UsesFourValueEditor)
        {
            return new DesignerCustomPropertyFourValueEditor(state)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
        }

        if (state.UsesGridLengthEditor)
        {
            return new DesignerCustomPropertyGridLengthEditor(state)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
        }

        return new TextBox
        {
            Text = state.Source is DesignerCustomPropertyValueSource.Local
                or DesignerCustomPropertyValueSource.Style
                    ? state.Value
                    : string.Empty,
            Watermark = state.EditorWatermark,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = 28,
        };
    }

    private static string? GetInitialChoice(DesignerCustomPropertyValueState state)
    {
        if (state.Source is DesignerCustomPropertyValueSource.Local
                or DesignerCustomPropertyValueSource.Style
            && state.EditorChoices.FirstOrDefault(choice => string.Equals(
                choice,
                state.Value,
                StringComparison.OrdinalIgnoreCase)) is { } currentChoice)
        {
            return currentChoice;
        }

        return state.EditorChoices.FirstOrDefault();
    }

    private static decimal GetInitialNumericValue(DesignerCustomPropertyValueState state)
    {
        if (state.Source is DesignerCustomPropertyValueSource.Local
                or DesignerCustomPropertyValueSource.Style
            && decimal.TryParse(
                state.Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var currentValue))
        {
            return currentValue;
        }

        return Math.Clamp(0m, state.NumericEditorMinimum, state.NumericEditorMaximum);
    }

    private static Color GetInitialColor(DesignerCustomPropertyValueState state)
        => state.Source is DesignerCustomPropertyValueSource.Local
                or DesignerCustomPropertyValueSource.Style
            && Color.TryParse(state.Value, out var currentColor)
                ? currentColor
                : Colors.DodgerBlue;

    private static bool TryReadEditorValue(
        EditorRow row,
        out string value,
        out string error)
    {
        switch (row.Editor)
        {
            case TextBox text:
                value = text.Text ?? string.Empty;
                break;
            case ComboBox { SelectedItem: not null } choice:
                value = choice.SelectedItem.ToString() ?? string.Empty;
                break;
            case NumericUpDown { Value: decimal number }:
                value = number.ToString(CultureInfo.InvariantCulture);
                break;
            case ColorPicker color:
                value = $"#{color.Color.A:x2}{color.Color.R:x2}{color.Color.G:x2}{color.Color.B:x2}";
                break;
            case DesignerCustomPropertyFourValueEditor fourValueEditor:
                if (!fourValueEditor.TryGetValue(out value, out error))
                {
                    error = $"{row.State.DisplayName} ({row.State.PropertyName}) {error}";
                    return false;
                }

                break;
            case DesignerCustomPropertyGridLengthEditor gridLengthEditor:
                if (!gridLengthEditor.TryGetValue(out value, out error))
                {
                    error = $"{row.State.DisplayName} ({row.State.PropertyName}) {error}";
                    return false;
                }

                break;
            default:
                value = string.Empty;
                error = $"Choose a value for {row.State.DisplayName}.";
                return false;
        }

        error = string.Empty;
        return true;
    }

    private static Border CreateCategoryHeader(string category)
    {
        var header = new Border
        {
            Tag = category,
            Margin = new Thickness(0, 6, 0, 0),
            Padding = new Thickness(8, 4),
            Background = new SolidColorBrush(Color.Parse("#263241")),
            CornerRadius = new CornerRadius(3),
            Child = new TextBlock
            {
                Text = category,
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#93C5FD")),
            },
        };
        header.Classes.Add("custom-property-category-header");
        return header;
    }

    private sealed record EditorRow(
        DesignerCustomPropertyValueState State,
        CheckBox LocalOverride,
        Control Editor,
        TextBlock SourceLabel,
        Border Container,
        Border CategoryHeader);
}
