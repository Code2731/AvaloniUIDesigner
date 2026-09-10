using System;
using System.Collections.Generic;
using Avalonia.Controls;
using AvaloniaUIDesigner.App.Models;

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
    string? SourceId = null,
    string? Category = null,
    IReadOnlyList<string>? DeclaredProperties = null,
    IReadOnlyList<DesignerCustomPropertyDefinition>? PropertyDefinitions = null);
