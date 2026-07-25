using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IComponentCatalog _componentCatalog;
    private readonly IDesignerSerializer _serializer;
    private readonly Stack<HistoryEntry> _undoStack = new();
    private readonly Stack<HistoryEntry> _redoStack = new();

    private PendingMutation? _pendingMutation;
    private bool _isSyncingSelection;
    private string? _currentDocumentPath;
    private DesignerCanvasDocument _lastSavedSnapshot = new(Array.Empty<DesignerElementSnapshot>());
    private List<DesignerElementSnapshot>? _clipboardSnapshots;

    public MainWindowViewModel()
        : this(new BuiltInComponentCatalog(), new DefaultControlRenderer(), new AxamlDocumentSerializer())
    {
    }

    internal MainWindowViewModel(
        IComponentCatalog componentCatalog,
        IControlRenderer renderer,
        IDesignerSerializer serializer)
    {
        _componentCatalog = componentCatalog;
        _serializer = serializer;

        Toolbox = new ToolboxViewModel(componentCatalog);
        Canvas = new CanvasViewModel(componentCatalog, renderer);
        ObjectTree = new ObjectTreeViewModel();
        PropertyInspector = new PropertyInspectorViewModel();
        RecentFiles = new ObservableCollection<string>();

        ObjectTree.PropertyChanged += OnObjectTreePropertyChanged;
        LoadRecentFilesFromDisk();
    }

    public ToolboxViewModel Toolbox { get; }
    public CanvasViewModel Canvas { get; }
    public ObjectTreeViewModel ObjectTree { get; }
    public PropertyInspectorViewModel PropertyInspector { get; }
    public ObservableCollection<string> RecentFiles { get; }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public bool CanPaste => _clipboardSnapshots is { Count: > 0 };
    public string? CurrentDocumentPath => _currentDocumentPath;
    public string WindowTitle => $"Avalonia UI Designer - {GetDisplayDocumentName()}{(IsDirty ? "*" : string.Empty)}";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool _isDirty;

    public void PlaceFromToolbox(double x, double y)
    {
        var item = Toolbox.SelectedItem;
        if (item is null)
        {
            StatusText = "Select a control in Toolbox first.";
            return;
        }

        var snappedX = Canvas.SnapPosition(x);
        var snappedY = Canvas.SnapPosition(y);

        BeginCanvasMutation(HistoryActionType.AddElement, "Added control to canvas.");
        var element = Canvas.PlaceElement(item, snappedX, snappedY);
        ObjectTree.Add(element);
        ObjectTree.SelectByElement(element);
        CommitCanvasMutation();

        StatusText = $"Placed {element.DisplayName} ({snappedX:0}, {snappedY:0})";
    }

    public void SelectElement(DesignElement? element, bool toggle = false)
    {
        _isSyncingSelection = true;
        try
        {
            Canvas.Select(element, toggle);
            ObjectTree.SelectByElement(Canvas.SelectedElement);
        }
        finally
        {
            _isSyncingSelection = false;
        }

        StatusText = Canvas.SelectedElement is null
            ? "Ready"
            : $"Selected {Canvas.SelectedElements.Count} control(s)";
    }

    public void SelectElements(IEnumerable<DesignElement> elements, bool append = false)
    {
        var selection = append
            ? Canvas.SelectedElements.Concat(elements).Distinct().ToList()
            : elements.Distinct().ToList();

        _isSyncingSelection = true;
        try
        {
            Canvas.SelectMany(selection);
            ObjectTree.SelectByElement(Canvas.SelectedElement);
        }
        finally
        {
            _isSyncingSelection = false;
        }

        StatusText = selection.Count == 0 ? "Ready" : $"Selected {selection.Count} control(s)";
    }

    public void MoveSelectedElement(double deltaX, double deltaY)
    {
        var targets = Canvas.SelectedElements.ToList();
        if (targets.Count == 0)
        {
            return;
        }

        BeginCanvasMutation(HistoryActionType.TransformElement, "Moved control with keyboard.");
        foreach (var target in targets)
        {
            target.X = Math.Max(0, target.X + deltaX);
            target.Y = Math.Max(0, target.Y + deltaY);
        }

        CommitCanvasMutation();
        StatusText = $"Moved {targets.Count} control(s)";
    }

    public void ArrangeSelectedElements(SelectionLayoutAction action)
    {
        var targets = Canvas.SelectedElements.ToList();
        if (targets.Count < 2)
        {
            StatusText = "Select at least two controls to arrange.";
            return;
        }

        if ((action is SelectionLayoutAction.DistributeHorizontally or SelectionLayoutAction.DistributeVertically)
            && targets.Count < 3)
        {
            StatusText = "Select at least three controls to distribute.";
            return;
        }

        var primary = Canvas.SelectedElement ?? targets[^1];
        BeginCanvasMutation(HistoryActionType.TransformElement, "Arranged selected controls.");

        switch (action)
        {
            case SelectionLayoutAction.AlignLeft:
            {
                var left = targets.Min(element => element.X);
                foreach (var element in targets) element.X = left;
                break;
            }
            case SelectionLayoutAction.AlignCenter:
            {
                var center = (targets.Min(element => element.X) + targets.Max(element => element.X + element.Width)) / 2;
                foreach (var element in targets) element.X = center - element.Width / 2;
                break;
            }
            case SelectionLayoutAction.AlignRight:
            {
                var right = targets.Max(element => element.X + element.Width);
                foreach (var element in targets) element.X = right - element.Width;
                break;
            }
            case SelectionLayoutAction.AlignTop:
            {
                var top = targets.Min(element => element.Y);
                foreach (var element in targets) element.Y = top;
                break;
            }
            case SelectionLayoutAction.AlignMiddle:
            {
                var middle = (targets.Min(element => element.Y) + targets.Max(element => element.Y + element.Height)) / 2;
                foreach (var element in targets) element.Y = middle - element.Height / 2;
                break;
            }
            case SelectionLayoutAction.AlignBottom:
            {
                var bottom = targets.Max(element => element.Y + element.Height);
                foreach (var element in targets) element.Y = bottom - element.Height;
                break;
            }
            case SelectionLayoutAction.DistributeHorizontally:
                DistributeHorizontally(targets);
                break;
            case SelectionLayoutAction.DistributeVertically:
                DistributeVertically(targets);
                break;
            case SelectionLayoutAction.MakeSameWidth:
                foreach (var element in targets) element.Width = primary.Width;
                break;
            case SelectionLayoutAction.MakeSameHeight:
                foreach (var element in targets) element.Height = primary.Height;
                break;
            case SelectionLayoutAction.MakeSameSize:
                foreach (var element in targets)
                {
                    element.Width = primary.Width;
                    element.Height = primary.Height;
                }
                break;
            default:
                _pendingMutation = null;
                return;
        }

        CommitCanvasMutation();
        StatusText = $"{DescribeLayoutAction(action)} {targets.Count} control(s)";
    }

    public void MoveSelectedElementsInLayerOrder(LayerOrderAction action)
    {
        var targets = Canvas.SelectedElements.ToList();
        if (targets.Count == 0)
        {
            StatusText = "No selected controls to reorder.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.TransformElement, "Changed control layer order.");
        var changed = action switch
        {
            LayerOrderAction.BringToFront => Canvas.MoveElementsToFront(targets),
            LayerOrderAction.SendToBack => Canvas.MoveElementsToBack(targets),
            _ => false,
        };

        if (!changed)
        {
            _pendingMutation = null;
            StatusText = action == LayerOrderAction.BringToFront
                ? "Selected controls are already at the front."
                : "Selected controls are already at the back.";
            return;
        }

        _isSyncingSelection = true;
        try
        {
            ObjectTree.RebuildFrom(Canvas.Elements);
            ObjectTree.SelectByElement(Canvas.SelectedElement);
        }
        finally
        {
            _isSyncingSelection = false;
        }

        CommitCanvasMutation();
        StatusText = action == LayerOrderAction.BringToFront
            ? $"Brought {targets.Count} control(s) to front."
            : $"Sent {targets.Count} control(s) to back.";
    }

    public void RemoveSelectedElement()
    {
        var targets = Canvas.SelectedElements.ToList();
        if (targets.Count == 0)
        {
            StatusText = "No selected element to delete.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.RemoveElement, "Removed control from canvas.");
        foreach (var target in targets)
        {
            Canvas.RemoveElement(target);
        }

        ObjectTree.RebuildFrom(Canvas.Elements);
        CommitCanvasMutation();
        StatusText = $"Deleted {targets.Count} control(s)";
    }

    public void DuplicateSelectedElement()
    {
        var targets = Canvas.SelectedElements.ToList();
        if (targets.Count == 0)
        {
            StatusText = "No selected element to duplicate.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.DuplicateElement, "Duplicated control.");

        var duplicates = new List<DesignElement>();
        foreach (var target in targets)
        {
            var duplicatedSnapshot = CreateSnapshot(
                target,
                BuildDuplicateDisplayName(target.DisplayName),
                target.X + 16,
                target.Y + 16);
            var duplicated = Canvas.AddElementFromSnapshot(duplicatedSnapshot, select: false);
            duplicates.Add(duplicated);
            ObjectTree.Add(duplicated);
        }

        Canvas.SelectMany(duplicates);
        ObjectTree.SelectByElement(Canvas.SelectedElement);
        CommitCanvasMutation();

        StatusText = $"Duplicated {targets.Count} control(s)";
    }

    public void CopySelectedElement()
    {
        var targets = Canvas.SelectedElements.ToList();
        if (targets.Count == 0)
        {
            StatusText = "No selected element to copy.";
            return;
        }

        _clipboardSnapshots = targets
            .Select(target => CreateSnapshot(target, target.DisplayName, target.X, target.Y))
            .ToList();
        OnPropertyChanged(nameof(CanPaste));
        StatusText = $"Copied {targets.Count} control(s)";
    }

    public void PasteElement()
    {
        if (_clipboardSnapshots is not { Count: > 0 })
        {
            StatusText = "Clipboard is empty.";
            return;
        }

        BeginCanvasMutation(HistoryActionType.PasteElement, "Pasted control.");

        var pastedSnapshots = new List<DesignerElementSnapshot>();
        var pastedElements = new List<DesignElement>();
        foreach (var snapshot in _clipboardSnapshots)
        {
            var pastedSnapshot = snapshot with
            {
                DisplayName = BuildDuplicateDisplayName(snapshot.DisplayName),
                X = snapshot.X + 16,
                Y = snapshot.Y + 16,
                VisualProperties = CloneProperties(snapshot.VisualProperties),
            };
            var pasted = Canvas.AddElementFromSnapshot(pastedSnapshot, select: false);
            pastedSnapshots.Add(pastedSnapshot);
            pastedElements.Add(pasted);
            ObjectTree.Add(pasted);
        }

        Canvas.SelectMany(pastedElements);
        ObjectTree.SelectByElement(Canvas.SelectedElement);
        CommitCanvasMutation();

        // Cascade subsequent pastes so they remain visible instead of stacking.
        _clipboardSnapshots = pastedSnapshots;
        StatusText = $"Pasted {pastedElements.Count} control(s)";
    }

    public void NewDocument()
    {
        ApplyDocument(new DesignerCanvasDocument(Array.Empty<DesignerElementSnapshot>()));
        _currentDocumentPath = null;
        _pendingMutation = null;
        ClearHistory();
        AcceptCurrentAsSaved();
        OnPropertyChanged(nameof(CurrentDocumentPath));
        OnPropertyChanged(nameof(WindowTitle));
        StatusText = "Created a new document.";
    }

    public void MarkDocumentLoaded(string path)
    {
        _currentDocumentPath = path;
        RegisterRecentFile(path);
        AcceptCurrentAsSaved();
        OnPropertyChanged(nameof(CurrentDocumentPath));
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void MarkDocumentSaved(string path)
    {
        _currentDocumentPath = path;
        RegisterRecentFile(path);
        AcceptCurrentAsSaved();
        OnPropertyChanged(nameof(CurrentDocumentPath));
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void MarkCurrentStateSaved()
    {
        AcceptCurrentAsSaved();
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void MarkDocumentLoadedWithoutPath()
    {
        _currentDocumentPath = null;
        AcceptCurrentAsSaved();
        OnPropertyChanged(nameof(CurrentDocumentPath));
        OnPropertyChanged(nameof(WindowTitle));
    }

    public string ExportDraftAxaml() => _serializer.Serialize(CaptureDocument());

    public DesignerCanvasDocument CreatePreviewDocument() => CaptureDocument();

    public string ExportFullAxaml()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Window xmlns=\"https://github.com/avaloniaui\"");
        sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
        sb.AppendLine("        Width=\"1280\" Height=\"800\">");
        sb.AppendLine("  <Canvas>");

        foreach (var element in Canvas.Elements)
        {
            WriteTopLevelElementAxaml(sb, element, "    ");
        }

        sb.AppendLine("  </Canvas>");
        sb.AppendLine("</Window>");
        return sb.ToString();
    }

    public bool TryImportDraftAxaml(string axaml, out string error, out string warning)
    {
        error = string.Empty;
        warning = string.Empty;

        DesignerCanvasDocument parsed;
        var warnings = new List<string>();
        try
        {
            parsed = ParseDraftDocument(axaml, warnings);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        ApplyDocument(parsed);
        _pendingMutation = null;
        ClearHistory();
        AcceptCurrentAsSaved();
        StatusText = "Loaded AXAML document.";
        warning = FormatWarnings(warnings);
        return true;
    }

    public bool TryValidateCurrentAxaml(out string result)
    {
        var warnings = new List<string>();
        try
        {
            var parsed = ParseDraftDocument(ExportFullAxaml(), warnings);
            if (parsed.Elements.Count != Canvas.Elements.Count)
            {
                result = "Validation failed: exported control count does not match the canvas.";
                return false;
            }
        }
        catch (Exception ex)
        {
            result = $"Validation failed: {ex.Message}";
            return false;
        }

        var warning = FormatWarnings(warnings);
        result = string.IsNullOrEmpty(warning)
            ? $"AXAML structure is valid ({Canvas.Elements.Count} control(s))."
            : $"AXAML structure is valid. {warning}";
        return true;
    }

    public void BeginCanvasMutation(HistoryActionType actionType, string message)
    {
        _pendingMutation ??= new PendingMutation(CaptureDocument(), actionType, message);
    }

    public void CommitCanvasMutation()
    {
        if (_pendingMutation is null)
        {
            return;
        }

        var pending = _pendingMutation;
        _pendingMutation = null;

        var after = CaptureDocument();
        if (AreSameDocument(pending.Before, after))
        {
            return;
        }

        var entry = new HistoryEntry(pending.Before, after, pending.ActionType, pending.Message);
        _undoStack.Push(entry);
        _redoStack.Clear();
        RaiseHistoryChanged();
        RefreshDirtyState();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            StatusText = "Nothing to undo.";
            return;
        }

        var entry = _undoStack.Pop();
        _redoStack.Push(entry);
        ApplyDocument(entry.Before);
        RaiseHistoryChanged();
        RefreshDirtyState();
        StatusText = $"Undo: {DescribeAction(entry.ActionType)}";
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            StatusText = "Nothing to redo.";
            return;
        }

        var entry = _redoStack.Pop();
        _undoStack.Push(entry);
        ApplyDocument(entry.After);
        RaiseHistoryChanged();
        RefreshDirtyState();
        StatusText = $"Redo: {DescribeAction(entry.ActionType)}";
    }

    private void OnObjectTreePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSyncingSelection)
        {
            return;
        }

        if (e.PropertyName != nameof(ObjectTreeViewModel.SelectedNode))
        {
            return;
        }

        var element = ObjectTree.SelectedNode?.Element;
        Canvas.Select(element);
        StatusText = element is null ? "Ready" : $"Selected {element.DisplayName} from Object Tree";
    }

    private static void DistributeHorizontally(IReadOnlyList<DesignElement> elements)
    {
        if (elements.Count < 3)
        {
            return;
        }

        var ordered = elements.OrderBy(element => element.X).ToList();
        var left = ordered[0].X;
        var right = ordered[^1].X + ordered[^1].Width;
        var gap = (right - left - ordered.Sum(element => element.Width)) / (ordered.Count - 1);
        var x = left;

        foreach (var element in ordered)
        {
            element.X = x;
            x += element.Width + gap;
        }
    }

    private static void DistributeVertically(IReadOnlyList<DesignElement> elements)
    {
        if (elements.Count < 3)
        {
            return;
        }

        var ordered = elements.OrderBy(element => element.Y).ToList();
        var top = ordered[0].Y;
        var bottom = ordered[^1].Y + ordered[^1].Height;
        var gap = (bottom - top - ordered.Sum(element => element.Height)) / (ordered.Count - 1);
        var y = top;

        foreach (var element in ordered)
        {
            element.Y = y;
            y += element.Height + gap;
        }
    }

    private void ApplyDocument(DesignerCanvasDocument document)
    {
        _isSyncingSelection = true;
        try
        {
            Canvas.Clear();
            foreach (var snapshot in document.Elements)
            {
                Canvas.AddElementFromSnapshot(snapshot, select: false);
            }

            ObjectTree.RebuildFrom(Canvas.Elements);
            Canvas.Select(null);
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private DesignerCanvasDocument CaptureDocument()
    {
        var snapshots = Canvas.Elements
            .Select(e => new DesignerElementSnapshot(
                e.DisplayName,
                e.TypeName,
                e.X,
                e.Y,
                e.Width,
                e.Height,
                CaptureVisualProperties(e.Visual)))
            .ToList();

        return new DesignerCanvasDocument(snapshots);
    }

    private static DesignerElementSnapshot CreateSnapshot(
        DesignElement element,
        string displayName,
        double x,
        double y)
    {
        return new DesignerElementSnapshot(
            displayName,
            element.TypeName,
            x,
            y,
            element.Width,
            element.Height,
            CloneProperties(CaptureVisualProperties(element.Visual)));
    }

    private static IReadOnlyDictionary<string, string>? CaptureVisualProperties(Control visual)
    {
        if (visual is Button button)
        {
            return new Dictionary<string, string>
            {
                ["Content"] = button.Content?.ToString() ?? string.Empty,
            };
        }

        if (visual is TextBox textBox)
        {
            return new Dictionary<string, string>
            {
                ["Text"] = textBox.Text ?? string.Empty,
                ["Watermark"] = textBox.Watermark?.ToString() ?? string.Empty,
            };
        }

        if (visual is TextBlock textBlock)
        {
            return new Dictionary<string, string>
            {
                ["Text"] = textBlock.Text ?? string.Empty,
            };
        }

        if (visual is CheckBox checkBox)
        {
            return new Dictionary<string, string>
            {
                ["Content"] = checkBox.Content?.ToString() ?? string.Empty,
                ["IsChecked"] = (checkBox.IsChecked ?? false).ToString(),
            };
        }

        if (visual is Slider slider)
        {
            return new Dictionary<string, string>
            {
                ["Minimum"] = slider.Minimum.ToString("0.###", CultureInfo.InvariantCulture),
                ["Maximum"] = slider.Maximum.ToString("0.###", CultureInfo.InvariantCulture),
                ["Value"] = slider.Value.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        if (visual is StackPanel stackPanel)
        {
            return new Dictionary<string, string>
            {
                ["Orientation"] = stackPanel.Orientation.ToString(),
                ["Spacing"] = stackPanel.Spacing.ToString("0.###", CultureInfo.InvariantCulture),
                ["__children"] = SerializeStackPanelChildren(stackPanel),
            };
        }

        if (visual is Grid grid)
        {
            return new Dictionary<string, string>
            {
                ["ShowGridLines"] = grid.ShowGridLines.ToString(),
            };
        }

        return null;
    }

    private static bool AreSameDocument(DesignerCanvasDocument left, DesignerCanvasDocument right)
    {
        if (left.Elements.Count != right.Elements.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Elements.Count; i++)
        {
            var a = left.Elements[i];
            var b = right.Elements[i];

            if (a.DisplayName != b.DisplayName
                || a.TypeName != b.TypeName
                || a.X != b.X
                || a.Y != b.Y
                || a.Width != b.Width
                || a.Height != b.Height
                || !DictionaryEquals(a.VisualProperties, b.VisualProperties))
            {
                return false;
            }
        }

        return true;
    }

    private DesignerCanvasDocument ParseDraftDocument(string axaml, ICollection<string> warnings)
    {
        var doc = XDocument.Parse(axaml);
        var root = doc.Root ?? throw new InvalidOperationException("AXAML root element is missing.");
        var parseRoot = FindParseRoot(root);

        if (!string.Equals(root.Name.LocalName, "Canvas", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(parseRoot.Name.LocalName, "Canvas", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("No Canvas element was found; imported direct child controls from the root element.");
        }

        var snapshots = new List<DesignerElementSnapshot>();

        foreach (var child in parseRoot.Elements())
        {
            var tagName = child.Name.LocalName;
            if (IsIgnoredContainerTag(tagName))
            {
                continue;
            }

            if (!TryResolveTypeName(tagName, out var typeName))
            {
                warnings.Add($"Unsupported control <{tagName}> was imported as a placeholder.");
            }

            var displayName = BuildImportedDisplayName(tagName, snapshots.Count + 1);

            var x = ReadDouble(child, "Canvas.Left", 0);
            var y = ReadDouble(child, "Canvas.Top", 0);
            var width = ReadDouble(child, "Width", 120);
            var height = ReadDouble(child, "Height", 40);
            var props = ReadVisualProperties(child, warnings);

            snapshots.Add(new DesignerElementSnapshot(displayName, typeName, x, y, width, height, props));
        }

        return new DesignerCanvasDocument(snapshots);
    }

    private bool TryResolveTypeName(string tagName, out string typeName)
    {
        foreach (var definition in _componentCatalog.GetAll())
        {
            var shortName = definition.AvaloniaTypeName[(definition.AvaloniaTypeName.LastIndexOf('.') + 1)..];
            if (string.Equals(shortName, tagName, StringComparison.OrdinalIgnoreCase))
            {
                typeName = definition.AvaloniaTypeName;
                return true;
            }
        }

        typeName = $"Avalonia.Controls.{tagName}";
        return false;
    }

    private static string BuildImportedDisplayName(string tagName, int sequence)
        => $"{tagName}{sequence}";

    private static XElement FindParseRoot(XElement root)
    {
        if (string.Equals(root.Name.LocalName, "Canvas", StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        var canvas = root
            .Descendants()
            .FirstOrDefault(e => string.Equals(e.Name.LocalName, "Canvas", StringComparison.OrdinalIgnoreCase));
        return canvas ?? root;
    }

    private static bool IsIgnoredContainerTag(string tagName)
        => tagName is "Styles" or "Resources" || tagName.Contains('.', StringComparison.Ordinal);

    private static IReadOnlyDictionary<string, string>? ReadVisualProperties(XElement element, ICollection<string> warnings)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tagName = element.Name.LocalName;

        foreach (var attr in element.Attributes())
        {
            var name = attr.Name.LocalName;
            if (attr.IsNamespaceDeclaration || name is "Canvas.Left" or "Canvas.Top" or "Width" or "Height")
            {
                continue;
            }

            if (IsSupportedVisualProperty(tagName, name))
            {
                map[name] = attr.Value;
            }
            else
            {
                warnings.Add($"Ignored unsupported property {tagName}.{name}.");
            }
        }

        if (string.Equals(element.Name.LocalName, "StackPanel", StringComparison.OrdinalIgnoreCase))
        {
            map["__children"] = SerializeStackPanelChildren(element);
        }

        return map.Count == 0 ? null : map;
    }

    private static bool IsSupportedVisualProperty(string tagName, string propertyName)
    {
        return tagName switch
        {
            "Button" => propertyName == "Content",
            "TextBox" => propertyName is "Text" or "Watermark",
            "TextBlock" => propertyName == "Text",
            "CheckBox" => propertyName is "Content" or "IsChecked",
            "Slider" => propertyName is "Minimum" or "Maximum" or "Value",
            "Grid" => propertyName == "ShowGridLines",
            "StackPanel" => propertyName is "Orientation" or "Spacing",
            _ => false,
        };
    }

    private static string FormatWarnings(IReadOnlyCollection<string> warnings)
    {
        if (warnings.Count == 0)
        {
            return string.Empty;
        }

        var preview = string.Join(" ", warnings.Take(3));
        return warnings.Count > 3
            ? $"Warnings ({warnings.Count}): {preview}"
            : $"Warnings: {preview}";
    }

    private static bool DictionaryEquals(IReadOnlyDictionary<string, string>? left, IReadOnlyDictionary<string, string>? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var value) || !string.Equals(value, pair.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static double ReadDouble(XElement element, string attributeName, double fallback)
    {
        var raw = element.Attribute(attributeName)?.Value;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private void RaiseHistoryChanged()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    private void ClearHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        RaiseHistoryChanged();
    }

    private void AcceptCurrentAsSaved()
    {
        _lastSavedSnapshot = CaptureDocument();
        IsDirty = false;
    }

    private void RefreshDirtyState()
    {
        IsDirty = !AreSameDocument(_lastSavedSnapshot, CaptureDocument());
    }

    private static string DescribeAction(HistoryActionType action)
    {
        return action switch
        {
            HistoryActionType.AddElement => "add element",
            HistoryActionType.DuplicateElement => "duplicate element",
            HistoryActionType.PasteElement => "paste element",
            HistoryActionType.RemoveElement => "remove element",
            HistoryActionType.TransformElement => "move/resize element",
            HistoryActionType.EditProperty => "edit properties",
            HistoryActionType.LoadDocument => "load document",
            HistoryActionType.NewDocument => "new document",
            _ => "change",
        };
    }

    private static string DescribeLayoutAction(SelectionLayoutAction action)
    {
        return action switch
        {
            SelectionLayoutAction.AlignLeft => "Aligned left",
            SelectionLayoutAction.AlignCenter => "Aligned center",
            SelectionLayoutAction.AlignRight => "Aligned right",
            SelectionLayoutAction.AlignTop => "Aligned top",
            SelectionLayoutAction.AlignMiddle => "Aligned middle",
            SelectionLayoutAction.AlignBottom => "Aligned bottom",
            SelectionLayoutAction.DistributeHorizontally => "Distributed horizontally",
            SelectionLayoutAction.DistributeVertically => "Distributed vertically",
            SelectionLayoutAction.MakeSameWidth => "Matched width for",
            SelectionLayoutAction.MakeSameHeight => "Matched height for",
            SelectionLayoutAction.MakeSameSize => "Matched size for",
            _ => "Arranged",
        };
    }

    private void RegisterRecentFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        for (var i = RecentFiles.Count - 1; i >= 0; i--)
        {
            if (string.Equals(RecentFiles[i], path, StringComparison.OrdinalIgnoreCase))
            {
                RecentFiles.RemoveAt(i);
            }
        }

        RecentFiles.Insert(0, path);
        while (RecentFiles.Count > 8)
        {
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }

        SaveRecentFilesToDisk();
    }

    public void RemoveRecentFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        for (var i = RecentFiles.Count - 1; i >= 0; i--)
        {
            if (string.Equals(RecentFiles[i], path, StringComparison.OrdinalIgnoreCase))
            {
                RecentFiles.RemoveAt(i);
            }
        }

        SaveRecentFilesToDisk();
    }

    private void LoadRecentFilesFromDisk()
    {
        try
        {
            var path = GetRecentFilesStorePath();
            if (!File.Exists(path))
            {
                return;
            }

            var json = File.ReadAllText(path);
            var entries = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            foreach (var entry in entries.Where(e => !string.IsNullOrWhiteSpace(e)).Take(8))
            {
                RecentFiles.Add(entry);
            }
        }
        catch
        {
            // ignore persistence failures
        }
    }

    private void SaveRecentFilesToDisk()
    {
        try
        {
            var path = GetRecentFilesStorePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(RecentFiles.ToList());
            File.WriteAllText(path, json);
        }
        catch
        {
            // ignore persistence failures
        }
    }

    private static string GetRecentFilesStorePath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "AvaloniaUIDesigner", "recent-files.json");
    }

    private string BuildDuplicateDisplayName(string sourceName)
    {
        var candidate = $"{sourceName}_copy";
        var suffix = 2;

        while (Canvas.Elements.Any(e => string.Equals(e.DisplayName, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{sourceName}_copy{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static IReadOnlyDictionary<string, string>? CloneProperties(IReadOnlyDictionary<string, string>? source)
    {
        if (source is null)
        {
            return null;
        }

        return source.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private string GetDisplayDocumentName()
    {
        if (string.IsNullOrWhiteSpace(_currentDocumentPath))
        {
            return "Untitled";
        }

        return Path.GetFileName(_currentDocumentPath);
    }

    private static string SerializeStackPanelChildren(StackPanel stackPanel)
    {
        var children = stackPanel.Children
            .Select(child => child switch
            {
                TextBlock textBlock => new StackPanelChildSnapshot(
                    TypeName: "TextBlock",
                    Text: textBlock.Text ?? string.Empty),
                Button button => new StackPanelChildSnapshot(
                    TypeName: "Button",
                    Content: button.Content?.ToString() ?? string.Empty),
                TextBox textBox => new StackPanelChildSnapshot(
                    TypeName: "TextBox",
                    Text: textBox.Text ?? string.Empty,
                    Watermark: textBox.Watermark?.ToString()),
                _ => null,
            })
            .Where(snapshot => snapshot is not null)
            .Cast<StackPanelChildSnapshot>()
            .ToList();

        return JsonSerializer.Serialize(children);
    }

    private static string SerializeStackPanelChildren(XElement stackPanelElement)
    {
        var children = new List<StackPanelChildSnapshot>();
        foreach (var child in stackPanelElement.Elements())
        {
            var tag = child.Name.LocalName;
            switch (tag)
            {
                case "TextBlock":
                    children.Add(new StackPanelChildSnapshot(
                        TypeName: "TextBlock",
                        Text: child.Attribute("Text")?.Value ?? string.Empty));
                    break;
                case "Button":
                    children.Add(new StackPanelChildSnapshot(
                        TypeName: "Button",
                        Content: child.Attribute("Content")?.Value ?? string.Empty));
                    break;
                case "TextBox":
                    children.Add(new StackPanelChildSnapshot(
                        TypeName: "TextBox",
                        Text: child.Attribute("Text")?.Value ?? string.Empty,
                        Watermark: child.Attribute("Watermark")?.Value));
                    break;
            }
        }

        return JsonSerializer.Serialize(children);
    }

    private static void WriteTopLevelElementAxaml(StringBuilder sb, DesignElement element, string indent)
    {
        switch (element.Visual)
        {
            case Button button:
                sb.Append(indent);
                sb.Append("<Button");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Content", button.Content?.ToString() ?? string.Empty);
                sb.AppendLine(" />");
                break;

            case TextBox textBox:
                sb.Append(indent);
                sb.Append("<TextBox");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Text", textBox.Text ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(textBox.Watermark?.ToString()))
                {
                    AppendAttribute(sb, "Watermark", textBox.Watermark?.ToString() ?? string.Empty);
                }

                sb.AppendLine(" />");
                break;

            case TextBlock textBlock:
                sb.Append(indent);
                sb.Append("<TextBlock");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Text", textBlock.Text ?? string.Empty);
                sb.AppendLine(" />");
                break;

            case CheckBox checkBox:
                sb.Append(indent);
                sb.Append("<CheckBox");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Content", checkBox.Content?.ToString() ?? string.Empty);
                AppendAttribute(sb, "IsChecked", (checkBox.IsChecked ?? false).ToString());
                sb.AppendLine(" />");
                break;

            case Slider slider:
                sb.Append(indent);
                sb.Append("<Slider");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Minimum", slider.Minimum.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "Maximum", slider.Maximum.ToString("0.###", CultureInfo.InvariantCulture));
                AppendAttribute(sb, "Value", slider.Value.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine(" />");
                break;

            case Grid grid:
                sb.Append(indent);
                sb.Append("<Grid");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "ShowGridLines", grid.ShowGridLines.ToString());
                sb.AppendLine(" />");
                break;

            case StackPanel stackPanel:
                sb.Append(indent);
                sb.Append("<StackPanel");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Orientation", stackPanel.Orientation.ToString());
                AppendAttribute(sb, "Spacing", stackPanel.Spacing.ToString("0.###", CultureInfo.InvariantCulture));

                if (stackPanel.Children.Count == 0)
                {
                    sb.AppendLine(" />");
                    break;
                }

                sb.AppendLine(">");
                foreach (var child in stackPanel.Children)
                {
                    WriteChildControlAxaml(sb, child, indent + "  ");
                }

                sb.Append(indent);
                sb.AppendLine("</StackPanel>");
                break;

            default:
                sb.Append(indent);
                sb.Append("<TextBlock");
                AppendCanvasLayoutAttributes(sb, element);
                AppendAttribute(sb, "Text", element.DisplayName);
                sb.AppendLine(" />");
                break;
        }
    }

    private static void WriteChildControlAxaml(StringBuilder sb, Control child, string indent)
    {
        switch (child)
        {
            case TextBlock textBlock:
                sb.Append(indent);
                sb.Append("<TextBlock");
                AppendAttribute(sb, "Text", textBlock.Text ?? string.Empty);
                sb.AppendLine(" />");
                break;

            case Button button:
                sb.Append(indent);
                sb.Append("<Button");
                AppendAttribute(sb, "Content", button.Content?.ToString() ?? string.Empty);
                sb.AppendLine(" />");
                break;

            case TextBox textBox:
                sb.Append(indent);
                sb.Append("<TextBox");
                AppendAttribute(sb, "Text", textBox.Text ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(textBox.Watermark?.ToString()))
                {
                    AppendAttribute(sb, "Watermark", textBox.Watermark?.ToString() ?? string.Empty);
                }

                sb.AppendLine(" />");
                break;

            default:
                sb.Append(indent);
                sb.Append("<Border />");
                sb.AppendLine();
                break;
        }
    }

    private static void AppendCanvasLayoutAttributes(StringBuilder sb, DesignElement element)
    {
        AppendAttribute(sb, "Canvas.Left", element.X.ToString("0.###", CultureInfo.InvariantCulture));
        AppendAttribute(sb, "Canvas.Top", element.Y.ToString("0.###", CultureInfo.InvariantCulture));
        AppendAttribute(sb, "Width", element.Width.ToString("0.###", CultureInfo.InvariantCulture));
        AppendAttribute(sb, "Height", element.Height.ToString("0.###", CultureInfo.InvariantCulture));
    }

    private static void AppendAttribute(StringBuilder sb, string name, string value)
    {
        sb.Append(" ");
        sb.Append(name);
        sb.Append("=\"");
        sb.Append(EscapeXmlAttribute(value));
        sb.Append("\"");
    }

    private static string EscapeXmlAttribute(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    public enum HistoryActionType
    {
        AddElement,
        DuplicateElement,
        PasteElement,
        RemoveElement,
        TransformElement,
        EditProperty,
        LoadDocument,
        NewDocument,
    }

    public enum SelectionLayoutAction
    {
        AlignLeft,
        AlignCenter,
        AlignRight,
        AlignTop,
        AlignMiddle,
        AlignBottom,
        DistributeHorizontally,
        DistributeVertically,
        MakeSameWidth,
        MakeSameHeight,
        MakeSameSize,
    }

    public enum LayerOrderAction
    {
        BringToFront,
        SendToBack,
    }

    private sealed record PendingMutation(DesignerCanvasDocument Before, HistoryActionType ActionType, string Message);

    private sealed record HistoryEntry(
        DesignerCanvasDocument Before,
        DesignerCanvasDocument After,
        HistoryActionType ActionType,
        string Message);

    private sealed record StackPanelChildSnapshot(
        string TypeName,
        string? Text = null,
        string? Content = null,
        string? Watermark = null);
}
