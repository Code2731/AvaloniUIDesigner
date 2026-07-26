using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed class BuiltInComponentCatalog : IComponentCatalog
{
    private readonly List<DesignerComponentDefinition> _definitions = new()
    {
        new(
            DisplayName: "Button",
            AvaloniaTypeName: "Avalonia.Controls.Button",
            DefaultWidth: 100,
            DefaultHeight: 32,
            VisualFactory: static () => new Button { Content = "Button" }),
        new(
            DisplayName: "TextBox",
            AvaloniaTypeName: "Avalonia.Controls.TextBox",
            DefaultWidth: 180,
            DefaultHeight: 32,
            VisualFactory: static () => new TextBox { Watermark = "Type here" }),
        new(
            DisplayName: "TextBlock",
            AvaloniaTypeName: "Avalonia.Controls.TextBlock",
            DefaultWidth: 160,
            DefaultHeight: 24,
            VisualFactory: static () => new TextBlock { Text = "Text" }),
        new(
            DisplayName: "Label",
            AvaloniaTypeName: "Avalonia.Controls.Label",
            DefaultWidth: 160,
            DefaultHeight: 28,
            VisualFactory: static () => new Label { Content = "Label" }),
        new(
            DisplayName: "Image",
            AvaloniaTypeName: "Avalonia.Controls.Image",
            DefaultWidth: 240,
            DefaultHeight: 160,
            VisualFactory: static () => new Image { Stretch = Stretch.Uniform }),
        new(
            DisplayName: "CheckBox",
            AvaloniaTypeName: "Avalonia.Controls.CheckBox",
            DefaultWidth: 160,
            DefaultHeight: 32,
            VisualFactory: static () => new CheckBox { Content = "CheckBox" }),
        new(
            DisplayName: "RadioButton",
            AvaloniaTypeName: "Avalonia.Controls.RadioButton",
            DefaultWidth: 160,
            DefaultHeight: 32,
            VisualFactory: static () => new RadioButton { Content = "Option", GroupName = "Options" }),
        new(
            DisplayName: "ToggleSwitch",
            AvaloniaTypeName: "Avalonia.Controls.ToggleSwitch",
            DefaultWidth: 180,
            DefaultHeight: 32,
            VisualFactory: static () => new ToggleSwitch { Content = "Toggle" }),
        new(
            DisplayName: "ToggleButton",
            AvaloniaTypeName: "Avalonia.Controls.Primitives.ToggleButton",
            DefaultWidth: 120,
            DefaultHeight: 32,
            VisualFactory: static () => new Avalonia.Controls.Primitives.ToggleButton { Content = "Toggle" }),
        new(
            DisplayName: "ComboBox",
            AvaloniaTypeName: "Avalonia.Controls.ComboBox",
            DefaultWidth: 180,
            DefaultHeight: 32,
            VisualFactory: static () => new ComboBox
            {
                SelectedIndex = 0,
                Items = { "Option 1", "Option 2", "Option 3" },
            }),
        new(
            DisplayName: "ListBox",
            AvaloniaTypeName: "Avalonia.Controls.ListBox",
            DefaultWidth: 180,
            DefaultHeight: 120,
            VisualFactory: static () => new ListBox
            {
                SelectedIndex = 0,
                Items = { "Item 1", "Item 2", "Item 3" },
            }),
        new(
            DisplayName: "Slider",
            AvaloniaTypeName: "Avalonia.Controls.Slider",
            DefaultWidth: 180,
            DefaultHeight: 32,
            VisualFactory: static () => new Slider { Minimum = 0, Maximum = 100, Value = 50 }),
        new(
            DisplayName: "ProgressBar",
            AvaloniaTypeName: "Avalonia.Controls.ProgressBar",
            DefaultWidth: 180,
            DefaultHeight: 20,
            VisualFactory: static () => new ProgressBar { Minimum = 0, Maximum = 100, Value = 50 }),
        new(
            DisplayName: "DatePicker",
            AvaloniaTypeName: "Avalonia.Controls.DatePicker",
            DefaultWidth: 180,
            DefaultHeight: 32,
            VisualFactory: static () => new DatePicker()),
        new(
            DisplayName: "CalendarDatePicker",
            AvaloniaTypeName: "Avalonia.Controls.CalendarDatePicker",
            DefaultWidth: 180,
            DefaultHeight: 32,
            VisualFactory: static () => new CalendarDatePicker { Watermark = "Select date" }),
        new(
            DisplayName: "TimePicker",
            AvaloniaTypeName: "Avalonia.Controls.TimePicker",
            DefaultWidth: 180,
            DefaultHeight: 32,
            VisualFactory: static () => new TimePicker()),
        new(
            DisplayName: "NumericUpDown",
            AvaloniaTypeName: "Avalonia.Controls.NumericUpDown",
            DefaultWidth: 180,
            DefaultHeight: 32,
            VisualFactory: static () => new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Increment = 1,
                Value = 50,
            }),
        new(
            DisplayName: "TabControl",
            AvaloniaTypeName: "Avalonia.Controls.TabControl",
            DefaultWidth: 320,
            DefaultHeight: 180,
            VisualFactory: static () => new TabControl
            {
                SelectedIndex = 0,
                Items =
                {
                    new TabItem { Header = "Overview", Content = new TextBlock { Text = "Overview content", Margin = new Thickness(12) } },
                    new TabItem { Header = "Details", Content = new TextBlock { Text = "Details content", Margin = new Thickness(12) } },
                },
            }),
        new(
            DisplayName: "Expander",
            AvaloniaTypeName: "Avalonia.Controls.Expander",
            DefaultWidth: 260,
            DefaultHeight: 100,
            VisualFactory: static () => new Expander
            {
                Header = "Advanced options",
                IsExpanded = true,
                Content = new TextBlock { Text = "Expanded content", Margin = new Thickness(8) },
            }),
        new(
            DisplayName: "ScrollViewer",
            AvaloniaTypeName: "Avalonia.Controls.ScrollViewer",
            DefaultWidth: 260,
            DefaultHeight: 160,
            VisualFactory: static () => new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = "Scrollable content\n\nUse Edit Content... to add more text.",
                    Margin = new Thickness(8),
                },
            }),
        new(
            DisplayName: "Border",
            AvaloniaTypeName: "Avalonia.Controls.Border",
            DefaultWidth: 240,
            DefaultHeight: 120,
            VisualFactory: static () => new Border
            {
                Background = Brush.Parse("#F1F5F9"),
                BorderBrush = Brush.Parse("#94A3B8"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Child = new TextBlock
                {
                    Text = "Border content",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            }),
        new(
            DisplayName: "Grid",
            AvaloniaTypeName: "Avalonia.Controls.Grid",
            DefaultWidth: 240,
            DefaultHeight: 160,
            VisualFactory: static () => new Grid
            {
                RowDefinitions = new RowDefinitions("*,*"),
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                ShowGridLines = true,
            }),
        new(
            DisplayName: "StackPanel",
            AvaloniaTypeName: "Avalonia.Controls.StackPanel",
            DefaultWidth: 220,
            DefaultHeight: 140,
            VisualFactory: static () => new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
            })
    };

    private readonly Dictionary<string, DesignerComponentDefinition> _baseDefinitions;

    public BuiltInComponentCatalog()
    {
        _baseDefinitions = _definitions.ToDictionary(
            definition => definition.AvaloniaTypeName,
            StringComparer.Ordinal);
    }

    public IReadOnlyList<DesignerComponentDefinition> GetAll() => _definitions;

    public bool TryGet(string avaloniaTypeName, out DesignerComponentDefinition definition)
    {
        return _baseDefinitions.TryGetValue(avaloniaTypeName, out definition!);
    }

    public bool TryRegister(DesignerComponentDefinition definition, out string error)
    {
        if (string.IsNullOrWhiteSpace(definition.DisplayName))
        {
            error = "Component display name is required.";
            return false;
        }

        if (!_baseDefinitions.TryGetValue(definition.AvaloniaTypeName, out var baseDefinition))
        {
            error = $"Unsupported Avalonia type: {definition.AvaloniaTypeName}";
            return false;
        }

        if (_definitions.Any(candidate => string.Equals(
            candidate.DisplayName,
            definition.DisplayName,
            StringComparison.OrdinalIgnoreCase)))
        {
            error = $"A Toolbox item named '{definition.DisplayName}' already exists.";
            return false;
        }

        _definitions.Add(definition with { VisualFactory = baseDefinition.VisualFactory });
        error = string.Empty;
        return true;
    }
}
