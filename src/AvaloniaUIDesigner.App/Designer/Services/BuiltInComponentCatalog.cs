using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;

namespace AvaloniaUIDesigner.App.Designer.Services;

public sealed class BuiltInComponentCatalog : IComponentCatalog
{
    private readonly IReadOnlyList<DesignerComponentDefinition> _definitions = new List<DesignerComponentDefinition>
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
            DisplayName: "CheckBox",
            AvaloniaTypeName: "Avalonia.Controls.CheckBox",
            DefaultWidth: 160,
            DefaultHeight: 32,
            VisualFactory: static () => new CheckBox { Content = "CheckBox" }),
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
            DisplayName: "Grid",
            AvaloniaTypeName: "Avalonia.Controls.Grid",
            DefaultWidth: 240,
            DefaultHeight: 160,
            VisualFactory: static () => new Grid
            {
                ShowGridLines = true
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
                Children =
                {
                    new TextBlock { Text = "StackPanel" },
                    new Button { Content = "Item" }
                }
            })
    };

    public IReadOnlyList<DesignerComponentDefinition> GetAll() => _definitions;

    public bool TryGet(string avaloniaTypeName, out DesignerComponentDefinition definition)
    {
        foreach (var candidate in _definitions)
        {
            if (candidate.AvaloniaTypeName == avaloniaTypeName)
            {
                definition = candidate;
                return true;
            }
        }

        definition = default!;
        return false;
    }
}
