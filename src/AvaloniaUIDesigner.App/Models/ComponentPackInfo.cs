using System;
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaUIDesigner.App.Models;

public enum ComponentPackSourceKind
{
    Json,
    Plugin,
}

public sealed record ComponentPackInfo(
    string SourceId,
    string Name,
    ComponentPackSourceKind SourceKind,
    string SourcePath,
    int ComponentCount,
    IReadOnlyList<string> ComponentNames)
{
    public string SourceKindLabel => SourceKind == ComponentPackSourceKind.Plugin
        ? "Plugin DLL"
        : "Component Pack JSON";

    public string SourceLabel => string.IsNullOrWhiteSpace(SourcePath)
        ? "In-memory pack"
        : SourcePath;

    public string ComponentSummary => string.Join(", ", ComponentNames);

    public override string ToString()
        => $"{Name} ({ComponentCount} component{(ComponentCount == 1 ? string.Empty : "s")}) - {SourceKindLabel}";
}
