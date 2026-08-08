using System.Collections.Generic;

namespace AvaloniaUIDesigner.App.Models;

public sealed class ComponentPackDocument
{
    public string? Name { get; set; }
    public List<ComponentPackComponent> Components { get; set; } = [];
}

public sealed class ComponentPackComponent
{
    public string? DisplayName { get; set; }
    public string? AvaloniaTypeName { get; set; }
    public string? NamePrefix { get; set; }
    public double? DefaultWidth { get; set; }
    public double? DefaultHeight { get; set; }
    public Dictionary<string, string?>? DefaultProperties { get; set; }
    public bool DesignOnly { get; set; }
    public string? PreviewText { get; set; }
}
