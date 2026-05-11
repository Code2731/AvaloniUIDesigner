using System;
using Avalonia.Controls;

namespace AvaloniaUIDesigner.App.Designer.Core;

public sealed record DesignerComponentDefinition(
    string DisplayName,
    string AvaloniaTypeName,
    double DefaultWidth,
    double DefaultHeight,
    Func<Control> VisualFactory);
