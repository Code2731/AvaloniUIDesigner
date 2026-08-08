using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace AvaloniaUIDesigner.App.Designer.Core;

public sealed record DesignerComponentDefinition(
    string DisplayName,
    string AvaloniaTypeName,
    double DefaultWidth,
    double DefaultHeight,
    Func<Control> VisualFactory,
    IReadOnlyDictionary<string, string>? DefaultProperties = null,
    string? NamePrefix = null,
    bool IsDesignOnly = false,
    string? PreviewText = null,
    string? SourceId = null);
