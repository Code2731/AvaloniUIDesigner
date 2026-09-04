using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;
using AvaloniaUIDesigner.App.ViewModels;

namespace AvaloniaUIDesigner.App.Views;

public partial class MainWindow : Window
{
    private const string KeyboardShortcutsHelpText = """
        Ctrl+N              New document
        Ctrl+O              Open AXAML document
        Ctrl+Shift+O        Open recent AXAML files
        Ctrl+Shift+P        Open project explorer
        Ctrl+Shift+R        Reload current file after an external change
        Ctrl+S              Save document
        Ctrl+Alt+S          Save all dirty document tabs
        Ctrl+Alt+D          Duplicate active document tab
        Ctrl+Alt+R          Rename document tab
        Ctrl+Shift+S        Save document as...
        Ctrl+W              Close active document tab
        Ctrl+Shift+W        Close all document tabs
        Ctrl+Shift+T        Reopen last closed document tab
        Ctrl+Tab            Next document tab
        Ctrl+Shift+Tab      Previous document tab
        Ctrl+Alt+Tab        Switch to recent document tab
        Ctrl+Alt+Shift+Tab  Switch to oldest recent document tab
        Ctrl+1..9           Activate document tab 1-9
        Ctrl+K              Quick switch document tab
        Ctrl+Shift+PageUp   Move active tab left
        Ctrl+Shift+PageDown Move active tab right
        Middle-click tab    Close document tab
        Ctrl+Shift+Arrow    Align selected controls to an edge
        Ctrl+Shift+E/M      Align selected controls center/middle
        Ctrl+G              Group selected controls into a Canvas
        Ctrl+Shift+U        Ungroup selected Canvas
        Ctrl+Shift+B        Break selected layout
        Ctrl+Alt+Shift+1    Lay out selected controls horizontally in a StackPanel
        Ctrl+Alt+Shift+2    Lay out selected controls vertically in a StackPanel
        Ctrl+Alt+Shift+3    Lay out selected controls in a Grid
        Ctrl+Alt+Shift+4    Lay out selected controls in a UniformGrid
        Ctrl+Alt+Shift+5    Lay out selected controls in a horizontal DockPanel
        Ctrl+Alt+Shift+6    Lay out selected controls in a vertical DockPanel
        Ctrl+Alt+Shift+7    Lay out selected controls in a horizontal WrapPanel
        Ctrl+Alt+Shift+8    Lay out selected controls in a vertical WrapPanel
        Ctrl+Alt+H/V        Distribute selected controls horizontally/vertically
        Ctrl+Alt+Shift+W/H/S Match selected control widths/heights/sizes
        Ctrl+Alt+Shift+X/Y Center selection on artboard horizontally/vertically
        Ctrl+Alt+Shift+C   Center selection on artboard on both axes
        Ctrl+Alt+Shift+Arrow Align selection to the matching artboard edge
        Ctrl+Alt+B          Edit numeric bounds for a multi-selection
        Ctrl+Alt+M          Edit common properties for a multi-selection
        Ctrl+Alt+L          Edit layout properties for the selected control
        Ctrl+Alt+Y          Edit typography properties for the selected control
        Ctrl+Alt+X          Edit transform properties for the selected control
        Ctrl+Alt+A          Edit accessibility and navigation properties for the selected control
        Ctrl+Alt+E          Edit interaction and rendering properties for the selected control
        Ctrl+Alt+F          Edit visual effects for the selected control
        Ctrl+Alt+Shift+R    Edit range and value properties for the selected control
        Ctrl+Alt+Shift+T    Edit text input properties for the selected TextBox
        Ctrl+Alt+Shift+B    Edit SelectableTextBlock selection styling
        Ctrl+Alt+Shift+M    Edit MaskedTextBox mask behavior
        Ctrl+Alt+Shift+Q    Edit selection behavior for ComboBox/ListBox/TreeView
        Ctrl+Alt+Shift+D    Edit DatePicker/Calendar/TimePicker date and time input
        Ctrl+Alt+Shift+P    Edit ColorPicker color and palette behavior
        Ctrl+Alt+Shift+A    Edit AutoCompleteBox completion and drop-down behavior
        Ctrl+Alt+Shift+O    Edit Toggle and Choice behavior
        Ctrl+Alt+Shift+U    Edit Expander disclosure and ScrollViewer scrolling behavior
        Ctrl+Alt+Shift+V    Edit SplitView pane presentation and scrolling behavior
        Ctrl+Alt+Shift+K    Edit TabControl tab strip and content alignment behavior
        Ctrl+Alt+Shift+E    Edit DataGrid table behavior and sizing policy
        Ctrl+Alt+G          Toggle design grid
        Ctrl+Alt+Shift+G    Toggle snap to grid
        Ctrl+Alt+1          Toggle Toolbox panel
        Ctrl+Alt+2          Toggle Object Tree panel
        Ctrl+Alt+3          Toggle Property Inspector panel
        Ctrl+Alt+0          Reset panel layout
        Ctrl+]              Bring selected controls forward
        Ctrl+[              Send selected controls backward
        Ctrl+Shift+]        Bring selected controls to front
        Ctrl+Shift+[        Send selected controls to back
        Ctrl+= / Ctrl+Plus  Zoom in
        Ctrl+- / Ctrl+Minus Zoom out
        Ctrl+F              Focus Object Tree search
        Ctrl+Alt+I          Focus Property Inspector filter
        Ctrl+Alt+T          Focus Toolbox search
        Ctrl+Alt+P          Toggle Toolbox placement mode
        Design > Select     Return to the canvas selection tool
        Design > Pick       Toggle Toolbox placement mode
        Ctrl+0              Actual size (100%)
        Ctrl+Shift+F        Fit selected controls to view
        F                   Fit canvas to view
        H                   Activate the Pan tool (press H again to return to Select)
        Space + left-drag   Temporarily pan the design viewport
        Middle-drag         Pan the design viewport
        View > Zoom Presets Choose 25-200% or a custom zoom scale
        Ctrl+R              Open runtime Preview
        Ctrl+Z              Undo
        Ctrl+Y              Redo
        Edit > History      Inspect and jump through Undo/Redo history
        Ctrl+A              Select all visible unlocked controls
        Ctrl/Shift+drag     Add marquee results to the current selection
        Alt+drag             Remove marquee results from the current selection
        Ctrl+D              Duplicate selection
        Ctrl+C              Copy selection
        Ctrl+X              Cut selection
        Ctrl+V              Paste selection
        Ctrl+Alt+V          Paste AXAML controls from the OS clipboard
        Ctrl+Shift+C        Copy visual style
        Ctrl+Shift+V        Paste visual style
        Ctrl+Shift+G        Clear design guides
        Tab                 Select next enabled focusable tab-stop control in Tab Order
        Shift+Tab           Add the contiguous Tab Order range to the selection
        Home/End            Select the first/last visible control in Canvas order
        Shift+Home/End      Add the contiguous Canvas boundary range to selection
        PageUp/PageDown     Select previous/next visible control in Canvas order
        Shift+PageUp/Down   Add the contiguous Canvas range to the selection
        Ctrl+Arrow          Select nearest visible control in that direction
        Escape              Select the parent container on the canvas, or clear selection at the root
        Enter               Select the first child of the selected container
        Shift+Enter         Select the last child of the selected container
        Alt+Arrow            Select the previous/next sibling on the canvas
        Arrow keys           Nudge selection by 1 px
        Shift+Arrow keys     Nudge selection by 10 px
        Shift+corner handle  Lock aspect ratio while resizing
        Alt+click           Cycle through overlapping controls at the pointer
        Alt+Shift+click     Cycle backward through overlapping controls
        Shift+click         Select a visible Canvas range
        Ctrl+Shift+click    Add a visible Canvas range
        Double-click element Quick edit visible content
        Delete / Backspace   Remove selection
        Object Tree F2       Rename selected control
        Object Tree Ctrl+L   Lock / unlock selected control
        Object Tree Shift+Arrow Extend the visible row range
        Object Tree Ctrl+Shift+Arrow Add a visible row range
        Object Tree Shift+Home/End Extend to a visible boundary
        Object Tree Ctrl+Shift+Home/End Add a visible boundary range
        Object Tree Shift+click Select a visible row range
        Object Tree Ctrl+Shift+click Add a visible row range
        Object Tree arrows   Navigate the hierarchy without nudging the canvas
        Project Explorer arrows  Collapse, expand, and navigate the project tree
        Project Explorer double-click  Open files or toggle folders
        Project Explorer Ctrl+N  Create a new AXAML file from a UserControl or Window template
        Project Explorer Ctrl+C  Copy the selected full path
        Project Explorer file manager  Open the selected location in the OS file manager
        """;

    private const string AboutHelpText = """
        AvaloniaUIDesigner

        A Qt Designer-style visual designer for Avalonia UI.
        Place controls, edit properties, preview the result, and round-trip AXAML.

        Runtime: .NET 8
        UI framework: Avalonia 11.3.12
        """;

    private const string NewProjectExplorerUserControlAxaml = """
        <UserControl xmlns="https://github.com/avaloniaui"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
        </UserControl>
        """;

    private const string NewProjectExplorerWindowAxaml = """
        <Window xmlns="https://github.com/avaloniaui"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
        </Window>
        """;

    private enum DragMode { None, Move, N, S, E, W, NE, NW, SE, SW }
    private enum UnsavedChoice { Save, Discard, Cancel }
    private enum PreviewThemeMode { Default, Light, Dark }
    private sealed record ComponentPackExportOptions(string PackName, string DisplayName, string NamePrefix);
    private sealed record ProjectExplorerNewFileResult(string FilePath, string Content);
    private sealed record ComponentPackManagementAction(string SourceId);
    private sealed record CommonPropertiesDialogResult(
        string Margin,
        string HorizontalAlignment,
        string VerticalAlignment,
        string Opacity,
        bool? IsEnabled,
        bool? IsVisible,
        bool? IsHitTestVisible);
    private sealed record ColorResourceApplicationOptions(string ResourceName, string PropertyName);
    private sealed record GridDefinitionOptions(string RowDefinitions, string ColumnDefinitions, bool ShowGridLines);
    private sealed record GridCellAssignmentOptions(
        string ParentName,
        int Row,
        int Column,
        int RowSpan,
        int ColumnSpan);
    private sealed record StackPanelAssignmentOptions(
        string ParentName,
        int ItemIndex,
        double ItemSize);
    private sealed record DockPanelAssignmentOptions(
        string ParentName,
        int ItemIndex,
        DesignerDockSide Dock,
        double ItemSize,
        bool LastChildFill);
    private sealed record WrapPanelAssignmentOptions(string ParentName, int ItemIndex);
    private sealed record UniformGridAssignmentOptions(string ParentName, int ItemIndex);
    private sealed record CanvasAssignmentOptions(
        string ParentName,
        int ItemIndex,
        double Left,
        double Top);
    private sealed record TabControlAssignmentOptions(string ParentName, int TabIndex);
    private sealed record SplitViewAssignmentOptions(string ParentName, DesignerSplitViewSlot Slot);
    private sealed record SplitViewSlotChoice(DesignerSplitViewSlot Slot, string? ChildName)
    {
        public override string ToString()
            => string.IsNullOrWhiteSpace(ChildName) ? Slot.ToString() : $"{Slot} ({ChildName})";
    }
    private sealed record ContentAssignmentOptions(string ParentName);
    private sealed record RecentFileSwitcherItem(string Path, int Index, bool Exists)
    {
        public string DisplayName => System.IO.Path.GetFileName(Path);
        public string StatusLabel => Exists ? "Available" : "Missing";

        public override string ToString()
            => $"{Index}. {DisplayName} - {Path} [{StatusLabel}]";
    }

    private sealed record ProjectFileSwitcherItem(ProjectWorkspaceFile File, int Index)
    {
        public string DisplayName => File.DisplayName;
        public string RelativePath => File.RelativePath;

        public override string ToString()
            => $"{Index}. {DisplayName} - {RelativePath}";
    }

    private sealed class ProjectExplorerNode
    {
        public ProjectExplorerNode(
            string displayName,
            string relativePath,
            string? fullPath,
            int depth,
            IReadOnlyList<ProjectExplorerNode>? children = null,
            bool isExpanded = true)
        {
            DisplayName = displayName;
            RelativePath = relativePath;
            FullPath = fullPath;
            Depth = depth;
            Children = children ?? Array.Empty<ProjectExplorerNode>();
            IsExpanded = isExpanded;
        }

        public string DisplayName { get; }
        public string RelativePath { get; }
        public string? FullPath { get; }
        public int Depth { get; }
        public IReadOnlyList<ProjectExplorerNode> Children { get; }
        public bool IsFile => FullPath is not null;
        public bool IsExpanded { get; set; }

        public override string ToString()
        {
            var indentation = new string(' ', Depth * 2);
            var marker = IsFile ? "[F]" : IsExpanded ? "[-]" : "[+]";
            return IsFile
                ? $"{indentation}{marker} {DisplayName} - {RelativePath}"
                : $"{indentation}{marker} {DisplayName}/";
        }
    }

    private sealed record DocumentTabSwitcherItem(
        DocumentTabViewModel Tab,
        int Index,
        string Path)
    {
        private bool IsUnsaved => string.IsNullOrWhiteSpace(Tab.DocumentPath);

        public int? RecentRank { get; init; }

        public string ShortcutLabel => Index is >= 1 and <= 9 ? $"Ctrl+{Index}" : $"Tab {Index}";

        public string StatusLabel => Tab.IsDirty
            ? IsUnsaved ? "Modified (unsaved)" : "Modified"
            : IsUnsaved ? "Unsaved" : "Saved";

        public override string ToString()
            => $"{ShortcutLabel} | {Tab.Header} - {Path} [{StatusLabel}]"
                + (RecentRank is { } rank ? $" [Recent #{rank}]" : string.Empty);
    }

    private const double HandlePixelSize = 10;
    private const double SelectionOutlinePixelSize = 1;
    private const double MinSize = 10;
    private const double MarqueeThreshold = 3;
    private const double SmartSnapThreshold = 6;
    private const double GuideHitThreshold = 8;
    private const double ViewportEdgePanThreshold = 48;
    private const double ViewportEdgePanStep = 24;
    private const double ViewportKeyboardPanStep = 96;
    private static readonly DataFormat<string> ToolboxDragDataFormat =
        DataFormat.CreateStringApplicationFormat("AvaloniaUIDesigner.ToolboxItem");
    private static readonly DataFormat<string> ObjectTreeDragDataFormat =
        DataFormat.CreateStringApplicationFormat("AvaloniaUIDesigner.ObjectTreeElement");
    private static readonly DataFormat<string> DocumentTabDragDataFormat =
        DataFormat.CreateStringApplicationFormat("AvaloniaUIDesigner.DocumentTab");

    private enum GuideOrientation { Horizontal, Vertical }

    private DragMode _dragMode = DragMode.None;
    private Point _dragStart;
    private double _origX, _origY, _origW, _origH;
    private DesignElement? _dragTarget;
    private readonly System.Collections.Generic.Dictionary<DesignElement, Point> _dragOrigins = new();
    private readonly System.Collections.Generic.Dictionary<DesignElement, Rect> _selectionResizeOrigins = new();
    private Rect _selectionResizeBounds;
    private bool _isSelectionResize;
    private bool _isMarqueeSelecting;
    private bool _marqueeAdditive;
    private bool _marqueeSubtractive;
    private Point _marqueeStart;
    private bool _isPanningViewport;
    private bool _isPanToolActive;
    private bool _isSpacePanModifier;
    private bool _isSpacePanGesture;
    private Point _panStart;
    private Vector _panStartOffset;
    private Point? _viewportPointer;
    private readonly List<double> _horizontalGuides = new();
    private readonly List<double> _verticalGuides = new();
    private readonly Dictionary<DocumentTabViewModel, DocumentGuideState> _documentGuideStates = new();
    private DocumentTabViewModel? _guideStateTab;
    private DocumentTabViewModel? _viewportStateTab;
    private bool _isRestoringViewport;
    private bool _isViewportRestorePending;
    private int _viewportRestoreVersion;
    private bool _showDesignGuides = true;
    private bool _snapToGuides = true;
    private bool _isDraggingGuide;
    private GuideOrientation _guideOrientation;
    private int _guideIndex = -1;
    private ToolboxItem? _pendingToolboxDragItem;
    private Point _toolboxDragStart;
    private DesignElement? _pendingObjectTreeDragElement;
    private Point _objectTreeDragStart;
    private DocumentTabViewModel? _pendingDocumentTabDrag;
    private Point _documentTabDragStart;
    private DocumentTabViewModel? _documentTabDropTarget;
    private DesignElement? _objectTreeSelectionAnchor;
    private DesignElement? _canvasSelectionAnchor;
    private bool _isObjectTreeSelectionGesture;

    private CanvasViewModel? _boundCanvas;
    private DesignElement? _boundElement;
    private Control? _boundVisual;
    private MainWindowViewModel? _boundVm;
    private PreviewWindow? _previewWindow;
    private PreviewThemeMode _previewThemeMode = PreviewThemeMode.Default;

    private readonly DispatcherTimer _propertyEditTimer;
    private readonly DispatcherTimer _projectWorkspaceRefreshTimer;
    private readonly Dictionary<string, DateTime> _knownProjectFileWriteTimes = new(StringComparer.OrdinalIgnoreCase);
    private FileSystemWatcher? _projectWorkspaceWatcher;
    private bool _hasPendingPropertyEdit;
    private bool _hasPendingLayoutEdit;
    private bool _allowCloseWithoutPrompt;
    private string _propertyInspectorFilterText = string.Empty;
    private double _toolboxPaneWidth = 220;
    private double _inspectorPaneWidth = 280;
    private double _objectTreePaneHeight;

    public MainWindow()
    {
        InitializeComponent();
        PropGrid.CustomPropertyDescriptorFilter += OnPropertyInspectorDescriptorFilter;
        DesignScrollViewer.PropertyChanged += OnDesignScrollViewerPropertyChanged;
        _propertyEditTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(450)
        };
        _propertyEditTimer.Tick += OnPropertyEditTimerTick;
        _projectWorkspaceRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(350),
        };
        _projectWorkspaceRefreshTimer.Tick += OnProjectWorkspaceRefreshTimerTick;

        DataContextChanged += OnDataContextChanged;
        OnDataContextChanged(this, EventArgs.Empty);
    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    private async void OnOpenMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await HandleOpenCommandAsync();
    }

    private async void OnOpenRecentFilesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await OpenRecentFilesAsync();

    private async Task OpenRecentFilesAsync()
    {
        if (Vm is null)
        {
            return;
        }

        if (Vm.RecentFiles.Count == 0)
        {
            Vm.StatusText = "No recent AXAML files are available.";
            return;
        }

        FlushPendingPropertyHistory();
        var selectedPath = await ShowRecentFilesAsync();
        if (selectedPath is not null)
        {
            await OpenRecentFileAsync(selectedPath);
        }
    }

    private static List<RecentFileSwitcherItem> FilterRecentFileSwitcherItems(
        IReadOnlyList<string> paths,
        string? query)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        return paths
            .Select((path, index) => new RecentFileSwitcherItem(
                path,
                index + 1,
                File.Exists(path)))
            .Where(item => string.IsNullOrWhiteSpace(normalizedQuery)
                || item.DisplayName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || item.Path.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static int MoveRecentFileSwitcherSelection(
        int selectedIndex,
        int offset,
        int itemCount)
    {
        if (itemCount <= 0)
        {
            return -1;
        }

        var currentIndex = selectedIndex < 0
            ? offset > 0 ? 0 : itemCount - 1
            : (selectedIndex + offset) % itemCount;
        if (currentIndex < 0)
        {
            currentIndex += itemCount;
        }

        return currentIndex;
    }

    private async Task<string?> ShowRecentFilesAsync()
    {
        if (Vm is null)
        {
            return null;
        }

        var paths = Vm.RecentFiles.ToList();
        var dialog = new Window
        {
            Title = "Open Recent AXAML Files",
            Width = 680,
            Height = 470,
            MinWidth = 500,
            MinHeight = 360,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var search = new TextBox
        {
            Watermark = "Search recent file name or path",
            MinWidth = 440,
        };
        var matchSummary = new TextBlock
        {
            Foreground = Brush.Parse("#64748B"),
        };
        var list = new ListBox
        {
            MinHeight = 280,
            MaxHeight = 330,
            SelectionMode = SelectionMode.Single,
        };
        var filteredItems = new List<RecentFileSwitcherItem>();

        void RefreshResults()
        {
            filteredItems = FilterRecentFileSwitcherItems(paths, search.Text);
            list.ItemsSource = filteredItems;
            list.SelectedIndex = filteredItems.Count > 0 ? 0 : -1;
            matchSummary.Text = filteredItems.Count == 0
                ? "No matching recent files."
                : $"{filteredItems.Count} recent file(s)";
        }

        void MoveSelection(int offset)
        {
            if (filteredItems.Count == 0)
            {
                return;
            }

            list.SelectedIndex = MoveRecentFileSwitcherSelection(
                list.SelectedIndex,
                offset,
                filteredItems.Count);
        }

        void SelectCurrent()
        {
            if (list.SelectedItem is RecentFileSwitcherItem item)
            {
                dialog.Close(item.Path);
            }
        }

        var cancelButton = new Button { Content = "Cancel", MinWidth = 86 };
        var openButton = new Button { Content = "Open", MinWidth = 86 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        openButton.Click += (_, _) => SelectCurrent();
        search.TextChanged += (_, _) => RefreshResults();
        search.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SelectCurrent();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                MoveSelection(1);
                list.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveSelection(-1);
                list.Focus();
                e.Handled = true;
            }
        };
        list.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SelectCurrent();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                MoveSelection(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveSelection(-1);
                e.Handled = true;
            }
        };
        dialog.Opened += (_, _) =>
        {
            search.Focus();
            search.SelectAll();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "Search recently opened AXAML files by name or path.",
                    TextWrapping = TextWrapping.Wrap,
                },
                search,
                matchSummary,
                list,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancelButton, openButton },
                },
            },
        };

        RefreshResults();
        return await dialog.ShowDialog<string?>(this);
    }

    private async Task OpenRecentFileAsync(string path)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        if (!File.Exists(path))
        {
            Vm.RemoveRecentFile(path);
            Vm.StatusText = $"Recent file not found: {path}";
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(path);
            if (!Vm.TryOpenDocumentTab(content, path, out var error, out var warning))
            {
                Vm.StatusText = $"Open failed: {error}";
                return;
            }

            ClearDesignGuides();
            Vm.StatusText = BuildOpenStatus(System.IO.Path.GetFileName(path), warning);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not open recent file: {exception.Message}";
        }
    }

    private async void OnOpenProjectFolderMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await OpenProjectFolderAsync();

    private async void OnProjectExplorerMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await OpenProjectExplorerAsync();

    private void OnRefreshProjectFilesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => RefreshProjectFiles();

    private async Task OpenProjectFolderAsync()
    {
        if (Vm is null)
        {
            return;
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Avalonia Project Folder",
            AllowMultiple = false,
        });
        if (folders.Count == 0)
        {
            return;
        }

        var path = folders[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            Vm.StatusText = "The selected project folder is not available as a local path.";
            return;
        }

        if (!Vm.TryOpenProjectWorkspace(path, out var error))
        {
            Vm.StatusText = $"Could not open project folder: {error}";
            return;
        }

        Vm.StatusText = $"Opened project {Vm.ProjectWorkspaceName} ({Vm.ProjectFiles.Count} AXAML file(s)).";
    }

    private async Task OpenProjectExplorerAsync()
    {
        if (Vm is null)
        {
            return;
        }

        if (!Vm.HasProjectWorkspace)
        {
            await OpenProjectFolderAsync();
            return;
        }

        FlushPendingPropertyHistory();
        var selectedPath = await ShowProjectExplorerAsync();
        if (selectedPath is not null)
        {
            await OpenRecentFileAsync(selectedPath);
        }
    }

    private void RefreshProjectFiles()
    {
        if (Vm is null)
        {
            return;
        }

        if (!Vm.RefreshProjectWorkspace(out var error))
        {
            Vm.StatusText = $"Could not refresh project files: {error}";
            return;
        }

        Vm.StatusText = $"Refreshed {Vm.ProjectFiles.Count} AXAML file(s) in {Vm.ProjectWorkspaceName}.";
    }

    private async void OnReloadCurrentFileMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await ReloadCurrentFileAsync();

    private async Task ReloadCurrentFileAsync()
    {
        if (Vm is null || string.IsNullOrWhiteSpace(Vm.CurrentDocumentPath))
        {
            return;
        }

        var path = Vm.CurrentDocumentPath;
        FlushPendingPropertyHistory();
        if (!await EnsureCanContinueWithUnsavedChangesAsync())
        {
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(path);
            if (!Vm.TryImportDraftAxaml(content, out var error, out var warning))
            {
                Vm.StatusText = $"Reload failed: {error}";
                return;
            }

            Vm.ClearExternalDocumentChange();
            RememberKnownDocumentWriteTime(path);
            ClearDesignGuides();
            Vm.StatusText = BuildOpenStatus(System.IO.Path.GetFileName(path), warning);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not reload {System.IO.Path.GetFileName(path)}: {exception.Message}";
        }
    }

    private void ConfigureProjectWorkspaceWatcher()
    {
        DisposeProjectWorkspaceWatcher();
        _knownProjectFileWriteTimes.Clear();

        var rootPath = _boundVm?.ProjectWorkspacePath;
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return;
        }

        RememberKnownDocumentWriteTime(_boundVm?.CurrentDocumentPath);
        try
        {
            _projectWorkspaceWatcher = new FileSystemWatcher(rootPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size,
                Filter = "*",
            };
            _projectWorkspaceWatcher.Changed += OnProjectWorkspaceFileSystemChanged;
            _projectWorkspaceWatcher.Created += OnProjectWorkspaceFileSystemChanged;
            _projectWorkspaceWatcher.Deleted += OnProjectWorkspaceFileSystemChanged;
            _projectWorkspaceWatcher.Renamed += OnProjectWorkspaceRenamed;
            _projectWorkspaceWatcher.Error += OnProjectWorkspaceWatcherError;
            _projectWorkspaceWatcher.EnableRaisingEvents = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _boundVm?.StatusText = $"Project file watching is unavailable: {exception.Message}";
            DisposeProjectWorkspaceWatcher();
        }
    }

    private void DisposeProjectWorkspaceWatcher()
    {
        _projectWorkspaceRefreshTimer.Stop();
        if (_projectWorkspaceWatcher is null)
        {
            return;
        }

        _projectWorkspaceWatcher.Changed -= OnProjectWorkspaceFileSystemChanged;
        _projectWorkspaceWatcher.Created -= OnProjectWorkspaceFileSystemChanged;
        _projectWorkspaceWatcher.Deleted -= OnProjectWorkspaceFileSystemChanged;
        _projectWorkspaceWatcher.Renamed -= OnProjectWorkspaceRenamed;
        _projectWorkspaceWatcher.Error -= OnProjectWorkspaceWatcherError;
        _projectWorkspaceWatcher.Dispose();
        _projectWorkspaceWatcher = null;
    }

    private void OnProjectWorkspaceFileSystemChanged(object? sender, FileSystemEventArgs e)
    {
        if (!IsRelevantProjectWorkspacePath(e.FullPath))
        {
            return;
        }

        var path = e.FullPath;
        Dispatcher.UIThread.Post(() =>
        {
            if (Vm is not null
                && string.Equals(Vm.CurrentDocumentPath, path, StringComparison.OrdinalIgnoreCase)
                && HasKnownDocumentFileChanged(path))
            {
                Vm.MarkExternalDocumentChanged(path);
            }

            QueueProjectWorkspaceRefresh();
        });
    }

    private void OnProjectWorkspaceRenamed(object? sender, RenamedEventArgs e)
    {
        if (!IsRelevantProjectWorkspacePath(e.FullPath)
            && !IsRelevantProjectWorkspacePath(e.OldFullPath))
        {
            return;
        }

        Dispatcher.UIThread.Post(QueueProjectWorkspaceRefresh);
    }

    private void OnProjectWorkspaceWatcherError(object? sender, ErrorEventArgs e)
        => Dispatcher.UIThread.Post(QueueProjectWorkspaceRefresh);

    private void QueueProjectWorkspaceRefresh()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(QueueProjectWorkspaceRefresh);
            return;
        }

        _projectWorkspaceRefreshTimer.Stop();
        _projectWorkspaceRefreshTimer.Start();
    }

    private void OnProjectWorkspaceRefreshTimerTick(object? sender, EventArgs e)
    {
        _projectWorkspaceRefreshTimer.Stop();
        if (Vm is null)
        {
            return;
        }

        var before = Vm.ProjectFiles.Select(file => file.FullPath).ToArray();
        if (!Vm.RefreshProjectWorkspace(out var error))
        {
            Vm.StatusText = $"Could not refresh project files: {error}";
            return;
        }

        var after = Vm.ProjectFiles.Select(file => file.FullPath).ToArray();
        if (!before.SequenceEqual(after, StringComparer.OrdinalIgnoreCase))
        {
            Vm.StatusText = $"Project files updated ({after.Length} AXAML file(s)).";
        }
    }

    private bool IsRelevantProjectWorkspacePath(string path)
    {
        var rootPath = _boundVm?.ProjectWorkspacePath;
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return false;
        }

        try
        {
            var relativePath = System.IO.Path.GetRelativePath(rootPath, path);
            if (relativePath == ".")
            {
                return true;
            }

            var extension = System.IO.Path.GetExtension(path);
            return string.IsNullOrEmpty(extension)
                || string.Equals(extension, ".axaml", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".xaml", StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private void RememberKnownDocumentWriteTime(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        try
        {
            _knownProjectFileWriteTimes[path] = File.GetLastWriteTimeUtc(path);
        }
        catch (IOException)
        {
            // A transient file access failure will be retried on the next watcher event.
        }
    }

    private bool HasKnownDocumentFileChanged(string path)
    {
        if (!File.Exists(path))
        {
            return true;
        }

        try
        {
            var currentWriteTime = File.GetLastWriteTimeUtc(path);
            if (!_knownProjectFileWriteTimes.TryGetValue(path, out var knownWriteTime))
            {
                _knownProjectFileWriteTimes[path] = currentWriteTime;
                return false;
            }

            if (currentWriteTime == knownWriteTime)
            {
                return false;
            }

            _knownProjectFileWriteTimes[path] = currentWriteTime;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static List<ProjectFileSwitcherItem> FilterProjectFileSwitcherItems(
        IReadOnlyList<ProjectWorkspaceFile> files,
        string? query)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        return files
            .Select((file, index) => new ProjectFileSwitcherItem(file, index + 1))
            .Where(item => string.IsNullOrWhiteSpace(normalizedQuery)
                || item.DisplayName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || item.RelativePath.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static int MoveProjectFileSwitcherSelection(
        int selectedIndex,
        int offset,
        int itemCount)
    {
        if (itemCount <= 0)
        {
            return -1;
        }

        var currentIndex = selectedIndex < 0
            ? offset > 0 ? 0 : itemCount - 1
            : (selectedIndex + offset) % itemCount;
        if (currentIndex < 0)
        {
            currentIndex += itemCount;
        }

        return currentIndex;
    }

    private static List<ProjectExplorerNode> BuildProjectExplorerTree(
        IReadOnlyList<ProjectWorkspaceFile> files)
    {
        var roots = new List<ProjectExplorerNode>();
        foreach (var file in files)
        {
            var segments = file.RelativePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            var siblings = roots;
            var relativePath = string.Empty;
            for (var index = 0; index < segments.Length; index++)
            {
                var segment = segments[index];
                relativePath = string.IsNullOrEmpty(relativePath)
                    ? segment
                    : $"{relativePath}/{segment}";
                var isFile = index == segments.Length - 1;
                var node = siblings.FirstOrDefault(candidate =>
                    candidate.IsFile == isFile
                    && string.Equals(candidate.DisplayName, segment, StringComparison.OrdinalIgnoreCase));
                if (node is null)
                {
                    var children = new List<ProjectExplorerNode>();
                    node = new ProjectExplorerNode(
                        segment,
                        relativePath,
                        isFile ? file.FullPath : null,
                        index,
                        children);
                    siblings.Add(node);
                }

                if (!isFile)
                {
                    siblings = (List<ProjectExplorerNode>)node.Children;
                }
            }
        }

        return SortProjectExplorerNodes(roots);
    }

    private static void ApplyProjectExplorerCollapsedFolders(
        IReadOnlyList<ProjectExplorerNode> roots,
        IReadOnlyCollection<string> collapsedFolders)
    {
        foreach (var node in roots)
        {
            ApplyProjectExplorerCollapsedFolder(node, collapsedFolders);
        }

        static void ApplyProjectExplorerCollapsedFolder(
            ProjectExplorerNode node,
            IReadOnlyCollection<string> collapsedPaths)
        {
            if (node.IsFile)
            {
                return;
            }

            node.IsExpanded = !collapsedPaths.Any(path =>
                string.Equals(path, node.RelativePath, StringComparison.OrdinalIgnoreCase));
            foreach (var child in node.Children)
            {
                ApplyProjectExplorerCollapsedFolder(child, collapsedPaths);
            }
        }
    }

    private static void RevealProjectExplorerPath(
        IReadOnlyList<ProjectExplorerNode> roots,
        string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var segments = relativePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return;
        }

        var currentNodes = roots;
        var currentPath = string.Empty;
        for (var index = 0; index < segments.Length - 1; index++)
        {
            currentPath = string.IsNullOrEmpty(currentPath)
                ? segments[index]
                : $"{currentPath}/{segments[index]}";
            var folder = currentNodes.FirstOrDefault(node =>
                !node.IsFile
                && string.Equals(node.RelativePath, currentPath, StringComparison.OrdinalIgnoreCase));
            if (folder is null)
            {
                return;
            }

            folder.IsExpanded = true;
            currentNodes = folder.Children;
        }
    }

    private static ProjectExplorerNode? FindProjectExplorerNode(
        IReadOnlyList<ProjectExplorerNode> roots,
        string relativePath)
    {
        foreach (var node in roots)
        {
            if (string.Equals(node.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var descendant = FindProjectExplorerNode(node.Children, relativePath);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static string? NavigateProjectExplorerTree(
        IReadOnlyList<ProjectExplorerNode> roots,
        string relativePath,
        Key key)
    {
        var node = FindProjectExplorerNode(roots, relativePath);
        if (node is null)
        {
            return null;
        }

        if (key == Key.Right && !node.IsFile)
        {
            if (!node.IsExpanded)
            {
                node.IsExpanded = true;
                return node.RelativePath;
            }

            return node.Children.FirstOrDefault()?.RelativePath;
        }

        if (key == Key.Left)
        {
            if (!node.IsFile && node.IsExpanded)
            {
                node.IsExpanded = false;
                return node.RelativePath;
            }

            var separatorIndex = node.RelativePath.LastIndexOf('/');
            return separatorIndex < 0
                ? null
                : node.RelativePath[..separatorIndex];
        }

        return null;
    }

    private static List<string> CollectProjectExplorerCollapsedFolders(
        IReadOnlyList<ProjectExplorerNode> roots)
    {
        var collapsedFolders = new List<string>();
        foreach (var node in roots)
        {
            CollectProjectExplorerCollapsedFolder(node, collapsedFolders);
        }

        return collapsedFolders;

        static void CollectProjectExplorerCollapsedFolder(
            ProjectExplorerNode node,
            List<string> target)
        {
            if (!node.IsFile)
            {
                if (!node.IsExpanded)
                {
                    target.Add(node.RelativePath);
                }

                foreach (var child in node.Children)
                {
                    CollectProjectExplorerCollapsedFolder(child, target);
                }
            }
        }
    }

    private static string? FindProjectExplorerRelativePath(
        IReadOnlyList<ProjectWorkspaceFile> files,
        string? currentDocumentPath)
    {
        if (string.IsNullOrWhiteSpace(currentDocumentPath))
        {
            return null;
        }

        foreach (var file in files)
        {
            try
            {
                if (string.Equals(
                        System.IO.Path.GetFullPath(file.FullPath),
                        System.IO.Path.GetFullPath(currentDocumentPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return file.RelativePath;
                }
            }
            catch (ArgumentException)
            {
                if (string.Equals(file.FullPath, currentDocumentPath, StringComparison.OrdinalIgnoreCase))
                {
                    return file.RelativePath;
                }
            }
        }

        return null;
    }

    private static string? GetProjectExplorerClipboardPath(
        ProjectExplorerNode node,
        string? workspacePath,
        bool fullPath)
    {
        if (!fullPath)
        {
            return node.RelativePath;
        }

        if (!string.IsNullOrWhiteSpace(node.FullPath))
        {
            return node.FullPath;
        }

        return string.IsNullOrWhiteSpace(workspacePath)
            ? null
            : System.IO.Path.Combine(
                workspacePath,
                node.RelativePath.Replace(
                    '/',
                    System.IO.Path.DirectorySeparatorChar));
    }

    private static string? GetProjectExplorerFileManagerPath(
        ProjectExplorerNode node,
        string? workspacePath)
    {
        var fullPath = GetProjectExplorerClipboardPath(
            node,
            workspacePath,
            fullPath: true);
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return null;
        }

        if (!node.IsFile)
        {
            return fullPath;
        }

        try
        {
            return System.IO.Path.GetDirectoryName(fullPath);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string? GetProjectExplorerCreationDirectory(
        ProjectExplorerNode? node,
        string? workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return null;
        }

        var path = node is null
            ? workspacePath
            : GetProjectExplorerFileManagerPath(node, workspacePath);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Directory.Exists(path)
                ? System.IO.Path.GetFullPath(path)
                : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string? GetProjectExplorerNewFilePath(
        string? fileName,
        string? directory)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        var normalizedName = fileName.Trim();
        if (normalizedName is "." or ".."
            || normalizedName.Length > 180
            || normalizedName.Any(char.IsControl))
        {
            return null;
        }

        if (!System.IO.Path.HasExtension(normalizedName))
        {
            normalizedName += ".axaml";
        }

        var extension = System.IO.Path.GetExtension(normalizedName);
        if (!string.Equals(extension, ".axaml", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".xaml", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                System.IO.Path.GetFileName(normalizedName),
                normalizedName,
                StringComparison.Ordinal))
        {
            return null;
        }

        if (normalizedName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        try
        {
            var fullDirectory = System.IO.Path.GetFullPath(directory);
            return Directory.Exists(fullDirectory)
                ? System.IO.Path.Combine(fullDirectory, normalizedName)
                : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string? GetProjectExplorerNewFileContent(string? rootKind)
        => rootKind switch
        {
            nameof(DesignerRootKind.Window) => NewProjectExplorerWindowAxaml,
            nameof(DesignerRootKind.UserControl) => NewProjectExplorerUserControlAxaml,
            _ => null,
        };

    private static bool TryOpenProjectExplorerFileManager(string path)
    {
        try
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            }) is not null;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or IOException
            or NotSupportedException
            or UnauthorizedAccessException
            or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static List<ProjectExplorerNode> FilterProjectExplorerTree(
        IReadOnlyList<ProjectExplorerNode> roots,
        string? query)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return roots.ToList();
        }

        return roots
            .Select(node => FilterProjectExplorerNode(node, normalizedQuery))
            .OfType<ProjectExplorerNode>()
            .ToList();
    }

    private static ProjectExplorerNode? FilterProjectExplorerNode(
        ProjectExplorerNode node,
        string query)
    {
        if (node.IsFile)
        {
            return node.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || node.RelativePath.Contains(query, StringComparison.OrdinalIgnoreCase)
                ? node
                : null;
        }

        var children = node.Children
            .Select(child => FilterProjectExplorerNode(child, query))
            .OfType<ProjectExplorerNode>()
            .ToList();
        return children.Count == 0
            ? null
            : new ProjectExplorerNode(
                node.DisplayName,
                node.RelativePath,
                null,
                node.Depth,
                children,
                isExpanded: true);
    }

    private static List<ProjectExplorerNode> SortProjectExplorerNodes(
        IReadOnlyList<ProjectExplorerNode> nodes)
        => nodes
            .OrderBy(node => node.IsFile)
            .ThenBy(node => node.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(node => new ProjectExplorerNode(
                node.DisplayName,
                node.RelativePath,
                node.FullPath,
                node.Depth,
                SortProjectExplorerNodes(node.Children),
                node.IsExpanded))
            .ToList();

    private static List<ProjectExplorerNode> FlattenProjectExplorerTree(
        IReadOnlyList<ProjectExplorerNode> roots)
    {
        var visibleNodes = new List<ProjectExplorerNode>();
        foreach (var root in roots)
        {
            AddVisibleProjectExplorerNode(root, visibleNodes);
        }

        return visibleNodes;

        static void AddVisibleProjectExplorerNode(
            ProjectExplorerNode node,
            List<ProjectExplorerNode> target)
        {
            target.Add(node);
            if (!node.IsFile && node.IsExpanded)
            {
                foreach (var child in node.Children)
                {
                    AddVisibleProjectExplorerNode(child, target);
                }
            }
        }
    }

    private static IDataTemplate CreateProjectExplorerItemTemplate()
        => new FuncDataTemplate<ProjectExplorerNode>((node, _) =>
        {
            if (node is null)
            {
                return new TextBlock();
            }

            var marker = node.IsFile
                ? "[F]"
                : node.IsExpanded ? "[-]" : "[+]";
            var pathText = node.IsFile
                ? node.RelativePath
                : $"{node.RelativePath}/";
            return new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(8 + node.Depth * 16, 4, 8, 4),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Width = 28,
                            Text = marker,
                            Foreground = Brush.Parse("#64748B"),
                        },
                        new TextBlock
                        {
                            Text = node.DisplayName,
                            FontWeight = node.IsFile ? FontWeight.Normal : FontWeight.SemiBold,
                        },
                        new TextBlock
                        {
                            Text = pathText,
                            Foreground = Brush.Parse("#94A3B8"),
                            TextTrimming = TextTrimming.CharacterEllipsis,
                        },
                    },
                },
            };
        }, supportsRecycling: true);

    private static ProjectExplorerNode? FindProjectExplorerContextMenuNode(object? source)
    {
        if (source is ListBoxItem { Content: ProjectExplorerNode directNode })
        {
            return directNode;
        }

        return (source as Visual)?.FindAncestorOfType<ListBoxItem>()?.Content as ProjectExplorerNode;
    }

    private async Task<string?> ShowProjectExplorerAsync()
    {
        if (Vm is null || !Vm.HasProjectWorkspace)
        {
            return null;
        }

        var dialog = new Window
        {
            Title = $"Project Explorer - {Vm.ProjectWorkspaceName}",
            Width = Vm.GetWorkspacePanelState().ProjectExplorerWidth,
            Height = Vm.GetWorkspacePanelState().ProjectExplorerHeight,
            MinWidth = 560,
            MinHeight = 400,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var rootText = new TextBlock
        {
            Text = Vm.ProjectWorkspacePath,
            Foreground = Brush.Parse("#94A3B8"),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var search = new TextBox
        {
            Watermark = "Search AXAML file name or relative path",
            MinWidth = 520,
        };
        var matchSummary = new TextBlock
        {
            Foreground = Brush.Parse("#64748B"),
        };
        var list = new ListBox
        {
            MinHeight = 330,
            MaxHeight = 390,
            SelectionMode = SelectionMode.Single,
            ItemTemplate = CreateProjectExplorerItemTemplate(),
        };
        var paths = new List<ProjectWorkspaceFile>();
        var treeRoots = new List<ProjectExplorerNode>();
        string? currentRelativePath = null;
        var visibleNodes = new List<ProjectExplorerNode>();
        var projectFilesRefreshPending = false;
        var isDialogOpen = true;
        var shouldRevealCurrentDocument = true;

        void RefreshResults(string? selectedRelativePath = null)
        {
            var filteredRoots = FilterProjectExplorerTree(treeRoots, search.Text);
            visibleNodes = FlattenProjectExplorerTree(filteredRoots);
            list.ItemsSource = visibleNodes;
            var selectionPath = selectedRelativePath ?? currentRelativePath;
            var selectedIndex = string.IsNullOrWhiteSpace(selectionPath)
                ? -1
                : visibleNodes.FindIndex(node =>
                    string.Equals(node.RelativePath, selectionPath, StringComparison.OrdinalIgnoreCase));
            if (selectedIndex < 0)
            {
                selectedIndex = visibleNodes.FindIndex(node => node.IsFile);
            }

            list.SelectedIndex = selectedIndex;
            var fileCount = visibleNodes.Count(node => node.IsFile);
            matchSummary.Text = fileCount == 0
                ? "No AXAML files found."
                : $"{fileCount} matching AXAML file(s) in the project tree";
        }

        void RefreshProjectTree()
        {
            var selectedRelativePath = (list.SelectedItem as ProjectExplorerNode)?.RelativePath;
            paths = Vm.ProjectFiles.ToList();
            treeRoots = BuildProjectExplorerTree(paths);
            ApplyProjectExplorerCollapsedFolders(
                treeRoots,
                Vm.ProjectWorkspaceCollapsedFolders);
            currentRelativePath = FindProjectExplorerRelativePath(
                paths,
                Vm.CurrentDocumentPath);
            if (shouldRevealCurrentDocument)
            {
                RevealProjectExplorerPath(treeRoots, currentRelativePath);
                shouldRevealCurrentDocument = false;
            }

            RefreshResults(selectedRelativePath);
        }

        void OnProjectFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!isDialogOpen || projectFilesRefreshPending)
            {
                return;
            }

            projectFilesRefreshPending = true;
            Dispatcher.UIThread.Post(() =>
            {
                projectFilesRefreshPending = false;
                if (isDialogOpen)
                {
                    RefreshProjectTree();
                }
            });
        }

        void StopProjectFileTracking()
        {
            if (!isDialogOpen)
            {
                return;
            }

            isDialogOpen = false;
            Vm.ProjectFiles.CollectionChanged -= OnProjectFilesChanged;
        }

        async Task CopyProjectExplorerPathAsync(bool fullPath)
        {
            if (list.SelectedItem is not ProjectExplorerNode node)
            {
                return;
            }

            var path = GetProjectExplorerClipboardPath(
                node,
                Vm.ProjectWorkspacePath,
                fullPath);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            await CopyAxamlToClipboardAsync(
                path,
                fullPath ? "Copied full path to clipboard." : "Copied relative path to clipboard.");
        }

        void OpenProjectExplorerFileManager()
        {
            if (list.SelectedItem is not ProjectExplorerNode node)
            {
                return;
            }

            var path = GetProjectExplorerFileManagerPath(
                node,
                Vm.ProjectWorkspacePath);
            if (string.IsNullOrWhiteSpace(path))
            {
                Vm.StatusText = "The selected Project Explorer location is unavailable.";
                return;
            }

            if (!TryOpenProjectExplorerFileManager(path))
            {
                Vm.StatusText = $"Could not open the location in the file manager: {path}";
                return;
            }

            Vm.StatusText = $"Opened the location in the file manager: {path}";
        }

        async Task<ProjectExplorerNewFileResult?> ShowNewProjectExplorerFileDialogAsync(
            string directory)
        {
            var nameDialog = new Window
            {
                Title = "New AXAML File",
                Width = 460,
                Height = 240,
                MinWidth = 380,
                MinHeight = 210,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            var nameEditor = new TextBox
            {
                Text = "NewView.axaml",
                Watermark = "File name (.axaml or .xaml)",
                MinWidth = 340,
            };
            var templateEditor = new ComboBox
            {
                ItemsSource = Enum.GetNames<DesignerRootKind>(),
                SelectedItem = nameof(DesignerRootKind.UserControl),
                MinWidth = 340,
            };
            var errorText = new TextBlock
            {
                Foreground = Brush.Parse("#B91C1C"),
                TextWrapping = TextWrapping.Wrap,
            };
            var createButton = new Button { Content = "Create", MinWidth = 86 };
            var cancelButton = new Button { Content = "Cancel", MinWidth = 86 };

            void CreateFile()
            {
                var path = GetProjectExplorerNewFilePath(nameEditor.Text, directory);
                if (path is null)
                {
                    errorText.Text = "Use a file name with an .axaml or .xaml extension, without folders.";
                    return;
                }

                var content = GetProjectExplorerNewFileContent(
                    templateEditor.SelectedItem?.ToString());
                if (content is null)
                {
                    errorText.Text = "Choose a UserControl or Window template.";
                    return;
                }

                if (File.Exists(path))
                {
                    errorText.Text = $"A file named {System.IO.Path.GetFileName(path)} already exists.";
                    return;
                }

                nameDialog.Close(new ProjectExplorerNewFileResult(path, content));
            }

            createButton.Click += (_, _) => CreateFile();
            cancelButton.Click += (_, _) => nameDialog.Close(null);
            nameEditor.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    CreateFile();
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    nameDialog.Close(null);
                    e.Handled = true;
                }
            };
            nameDialog.Opened += (_, _) =>
            {
                nameEditor.Focus();
                nameEditor.SelectAll();
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Children = { cancelButton, createButton },
            };
            nameDialog.Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = $"Create a new AXAML file in {directory}." },
                    nameEditor,
                    new TextBlock { Text = "Root template" },
                    templateEditor,
                    errorText,
                    buttons,
                },
            };

            return await nameDialog.ShowDialog<ProjectExplorerNewFileResult?>(dialog);
        }

        async Task CreateNewProjectExplorerFileAsync()
        {
            var directory = GetProjectExplorerCreationDirectory(
                list.SelectedItem as ProjectExplorerNode,
                Vm.ProjectWorkspacePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                Vm.StatusText = "The selected Project Explorer folder is unavailable.";
                return;
            }

            var file = await ShowNewProjectExplorerFileDialogAsync(directory);
            if (file is null)
            {
                return;
            }

            try
            {
                await AtomicFileWriter.WriteAllTextAsync(
                    file.FilePath,
                    file.Content);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Vm.StatusText = $"Could not create {System.IO.Path.GetFileName(file.FilePath)}: {exception.Message}";
                return;
            }

            dialog.Close(file.FilePath);
        }

        void MoveSelection(int offset)
        {
            if (visibleNodes.Count == 0)
            {
                return;
            }

            list.SelectedIndex = MoveProjectFileSwitcherSelection(
                list.SelectedIndex,
                offset,
                visibleNodes.Count);
        }

        void SelectCurrent()
        {
            if (list.SelectedItem is not ProjectExplorerNode node)
            {
                return;
            }

            if (node.IsFile && node.FullPath is not null)
            {
                dialog.Close(node.FullPath);
                return;
            }

            var sourceNode = FindProjectExplorerNode(treeRoots, node.RelativePath);
            if (sourceNode is not null)
            {
                sourceNode.IsExpanded = !sourceNode.IsExpanded;
                Vm.SetProjectWorkspaceCollapsedFolders(
                    CollectProjectExplorerCollapsedFolders(treeRoots));
            }

            RefreshResults(node.RelativePath);
        }

        void NavigateTree(Key key)
        {
            if (list.SelectedItem is not ProjectExplorerNode node)
            {
                return;
            }

            var targetPath = NavigateProjectExplorerTree(
                treeRoots,
                node.RelativePath,
                key);
            if (targetPath is null)
            {
                return;
            }

            Vm.SetProjectWorkspaceCollapsedFolders(
                CollectProjectExplorerCollapsedFolders(treeRoots));
            RefreshResults(targetPath);
        }

        var cancelButton = new Button { Content = "Cancel", MinWidth = 86 };
        var openButton = new Button { Content = "Open", MinWidth = 86 };
        var newFileMenu = new MenuItem { Header = "New AXAML File..." };
        var copyRelativePathMenu = new MenuItem { Header = "Copy Relative Path" };
        var copyFullPathMenu = new MenuItem { Header = "Copy Full Path" };
        var openFileManagerMenu = new MenuItem { Header = "Open in File Manager" };
        cancelButton.Click += (_, _) => dialog.Close(null);
        openButton.Click += (_, _) => SelectCurrent();
        list.DoubleTapped += (_, _) => SelectCurrent();
        newFileMenu.Click += async (_, _) => await CreateNewProjectExplorerFileAsync();
        copyRelativePathMenu.Click += async (_, _) => await CopyProjectExplorerPathAsync(fullPath: false);
        copyFullPathMenu.Click += async (_, _) => await CopyProjectExplorerPathAsync(fullPath: true);
        openFileManagerMenu.Click += (_, _) => OpenProjectExplorerFileManager();
        list.ContextMenu = new ContextMenu
        {
            Items =
            {
                newFileMenu,
                new Separator(),
                copyRelativePathMenu,
                copyFullPathMenu,
                openFileManagerMenu,
            },
        };
        list.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.N
                && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                await CreateNewProjectExplorerFileAsync();
                e.Handled = true;
            }
        };
        list.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(list).Properties.PointerUpdateKind
                != PointerUpdateKind.RightButtonPressed)
            {
                return;
            }

            var node = FindProjectExplorerContextMenuNode(e.Source);
            if (node is not null)
            {
                list.SelectedItem = node;
            }
        };
        search.TextChanged += (_, _) => RefreshResults();
        search.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SelectCurrent();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                MoveSelection(1);
                list.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveSelection(-1);
                list.Focus();
                e.Handled = true;
            }
        };
        list.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.C
                && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                await CopyProjectExplorerPathAsync(fullPath: true);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                SelectCurrent();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                MoveSelection(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveSelection(-1);
                e.Handled = true;
            }
            else if (e.Key == Key.Left || e.Key == Key.Right)
            {
                NavigateTree(e.Key);
                e.Handled = true;
            }
        };
        dialog.Opened += (_, _) =>
        {
            search.Focus();
            search.SelectAll();
        };
        dialog.Closed += (_, _) => StopProjectFileTracking();
        Vm.ProjectFiles.CollectionChanged += OnProjectFilesChanged;

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "Browse the project tree. Double-click an AXAML file to open it; press Enter on a folder or double-click a folder to expand or collapse it, use Left/Right to navigate, Ctrl+N or the context menu to create a new AXAML file from a UserControl or Window template, Ctrl+C to copy the selected full path, or the context menu to open its location in the file manager.",
                    TextWrapping = TextWrapping.Wrap,
                },
                rootText,
                search,
                matchSummary,
                list,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancelButton, openButton },
                },
            },
        };

        RefreshProjectTree();
        var selectedPath = await dialog.ShowDialog<string?>(this);
        var workspacePanelState = Vm.GetWorkspacePanelState();
        Vm.SetWorkspacePanelState(workspacePanelState with
        {
            ProjectExplorerWidth = dialog.Bounds.Width > 0
                ? dialog.Bounds.Width
                : workspacePanelState.ProjectExplorerWidth,
            ProjectExplorerHeight = dialog.Bounds.Height > 0
                ? dialog.Bounds.Height
                : workspacePanelState.ProjectExplorerHeight,
        });
        StopProjectFileTracking();
        Vm.SetProjectWorkspaceCollapsedFolders(
            CollectProjectExplorerCollapsedFolders(treeRoots));
        return selectedPath;
    }

    private async void OnQuickSwitchDocumentTabMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await QuickSwitchDocumentTabAsync();

    private void OnRecentDocumentTabMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.ActivateRecentDocumentTab();
    }

    private async Task QuickSwitchDocumentTabAsync()
    {
        if (Vm is null)
        {
            return;
        }

        if (Vm.DocumentTabs.Count < 2)
        {
            Vm.StatusText = "At least two document tabs are required for quick switching.";
            return;
        }

        FlushPendingPropertyHistory();
        var selectedTab = await ShowDocumentTabSwitcherAsync();
        if (selectedTab is not null)
        {
            Vm.ActivateDocumentTab(selectedTab);
        }
    }

    private static List<DocumentTabSwitcherItem> FilterDocumentTabSwitcherItems(
        IReadOnlyList<DocumentTabViewModel> tabs,
        string? query)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        return tabs
            .Select((tab, index) => new { Tab = tab, Index = index + 1 })
            .Where(entry => MatchesDocumentTabSwitcherQuery(entry.Tab, entry.Index, normalizedQuery))
            .Select(entry => new DocumentTabSwitcherItem(
                entry.Tab,
                entry.Index,
                string.IsNullOrWhiteSpace(entry.Tab.DocumentPath)
                    ? "Unsaved document"
                    : entry.Tab.DocumentPath!))
            .ToList();
    }

    private static bool MatchesDocumentTabSwitcherQuery(
        DocumentTabViewModel tab,
        int index,
        string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return true;
        }

        if (MatchesDocumentTabSwitcherIndex(normalizedQuery, index))
        {
            return true;
        }

        if (MatchesDocumentTabSwitcherStatus(tab, normalizedQuery) is { } statusMatch)
        {
            return statusMatch;
        }

        var statusTerms = tab.IsDirty ? "dirty modified changed" : "clean";
        statusTerms += string.IsNullOrWhiteSpace(tab.DocumentPath)
            ? " unsaved new untitled"
            : " saved";

        var searchableText = $"{tab.DisplayName} {tab.DocumentPath} {statusTerms}";
        return searchableText.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase);
    }

    private static bool? MatchesDocumentTabSwitcherStatus(
        DocumentTabViewModel tab,
        string query)
    {
        return query.ToLowerInvariant() switch
        {
            "dirty" or "modified" or "changed" => tab.IsDirty,
            "clean" => !tab.IsDirty,
            "saved" => !tab.IsDirty && !string.IsNullOrWhiteSpace(tab.DocumentPath),
            "unsaved" or "new" or "untitled" => string.IsNullOrWhiteSpace(tab.DocumentPath),
            _ => null,
        };
    }

    private static bool MatchesDocumentTabSwitcherIndex(string query, int index)
    {
        var indexQuery = query;
        if (indexQuery.StartsWith('#'))
        {
            indexQuery = indexQuery[1..].Trim();
        }
        else if (indexQuery.StartsWith("tab ", StringComparison.OrdinalIgnoreCase))
        {
            indexQuery = indexQuery[4..].Trim();
        }

        return int.TryParse(
                indexQuery,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var requestedIndex)
            && requestedIndex == index;
    }

    private static int MoveDocumentTabSwitcherSelection(
        int selectedIndex,
        int offset,
        int itemCount)
    {
        if (itemCount <= 0)
        {
            return -1;
        }

        var currentIndex = selectedIndex < 0
            ? offset > 0 ? 0 : itemCount - 1
            : (selectedIndex + offset) % itemCount;
        if (currentIndex < 0)
        {
            currentIndex += itemCount;
        }

        return currentIndex;
    }

    private async Task<DocumentTabViewModel?> ShowDocumentTabSwitcherAsync()
    {
        if (Vm is null)
        {
            return null;
        }

        var tabs = Vm.DocumentTabs.ToList();
        var dialog = new Window
        {
            Title = "Quick Switch Document Tab",
            Width = 580,
            Height = 430,
            MinWidth = 440,
            MinHeight = 340,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var search = new TextBox
        {
            Watermark = "Search tab name or file path",
            MinWidth = 380,
        };
        var matchSummary = new TextBlock
        {
            Foreground = Brush.Parse("#64748B"),
        };
        var list = new ListBox
        {
            MinHeight = 250,
            MaxHeight = 290,
            SelectionMode = SelectionMode.Single,
        };
        var filteredItems = new List<DocumentTabSwitcherItem>();

        void RefreshResults()
        {
            filteredItems = FilterDocumentTabSwitcherItems(tabs, search.Text)
                .Select(item =>
                {
                    var recentRank = Vm.GetRecentDocumentTabRank(item.Tab);
                    return item with { RecentRank = recentRank > 0 ? recentRank : null };
                })
                .ToList();
            list.ItemsSource = filteredItems;

            var activeIndex = filteredItems.FindIndex(
                item => ReferenceEquals(item.Tab, Vm.SelectedDocumentTab));
            list.SelectedIndex = activeIndex >= 0
                ? activeIndex
                : filteredItems.Count > 0 ? 0 : -1;
            matchSummary.Text = filteredItems.Count == 0
                ? "No matching document tabs."
                : $"{filteredItems.Count} document tab(s)";
        }

        void MoveSelection(int offset)
        {
            if (filteredItems.Count == 0)
            {
                return;
            }

            list.SelectedIndex = MoveDocumentTabSwitcherSelection(
                list.SelectedIndex,
                offset,
                filteredItems.Count);
        }

        void SelectCurrent()
        {
            if (list.SelectedItem is DocumentTabSwitcherItem item)
            {
                dialog.Close(item.Tab);
            }
        }

        var cancelButton = new Button { Content = "Cancel", MinWidth = 86 };
        var switchButton = new Button { Content = "Switch", MinWidth = 86 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        switchButton.Click += (_, _) => SelectCurrent();
        search.TextChanged += (_, _) => RefreshResults();
        search.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SelectCurrent();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                MoveSelection(1);
                list.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveSelection(-1);
                list.Focus();
                e.Handled = true;
            }
        };
        list.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SelectCurrent();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                MoveSelection(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveSelection(-1);
                e.Handled = true;
            }
        };
        dialog.Opened += (_, _) =>
        {
            search.Focus();
            search.SelectAll();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "Search open document tabs by alias or file path.",
                    TextWrapping = TextWrapping.Wrap,
                },
                search,
                matchSummary,
                list,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancelButton, switchButton },
                },
            },
        };

        RefreshResults();
        return await dialog.ShowDialog<DocumentTabViewModel?>(this);
    }

    private void OnDocumentTabClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is not null && sender is Button { Tag: DocumentTabViewModel tab })
        {
            FlushPendingPropertyHistory();
            Vm.ActivateDocumentTab(tab);
        }
    }

    private async void OnDocumentTabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Button { Tag: DocumentTabViewModel tab } tabButton)
        {
            return;
        }

        var point = e.GetCurrentPoint(tabButton);
        if (await TryCloseDocumentTabFromPointerAsync(tab, point.Properties.PointerUpdateKind))
        {
            e.Handled = true;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        _pendingDocumentTabDrag = tab;
        _documentTabDragStart = e.GetPosition(this);
        e.Pointer.Capture(tabButton);
    }

    private async Task<bool> TryCloseDocumentTabFromPointerAsync(
        DocumentTabViewModel tab,
        PointerUpdateKind updateKind)
    {
        if (updateKind != PointerUpdateKind.MiddleButtonPressed)
        {
            return false;
        }

        await CloseDocumentTabAsync(tab);
        return true;
    }

    private async void OnDocumentTabPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pendingDocumentTabDrag is not { } tab)
        {
            return;
        }

        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _documentTabDragStart.X) < MarqueeThreshold
            && Math.Abs(point.Y - _documentTabDragStart.Y) < MarqueeThreshold)
        {
            return;
        }

        _pendingDocumentTabDrag = null;
        e.Pointer.Capture(null);
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(DocumentTabDragDataFormat, tab.DragId));
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
        ClearDocumentTabDropFeedback();
        e.Handled = true;
    }

    private void OnDocumentTabPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pendingDocumentTabDrag = null;
        e.Pointer.Capture(null);
        ClearDocumentTabDropFeedback();
    }

    private void OnDocumentTabDragOver(object? sender, DragEventArgs e)
    {
        if (Vm is null
            || sender is not Border { DataContext: DocumentTabViewModel target } targetBorder
            || e.DataTransfer.TryGetValue(DocumentTabDragDataFormat) is not string dragId
            || Vm.DocumentTabs.FirstOrDefault(tab => tab.DragId == dragId) is not { } source
            || ReferenceEquals(source, target))
        {
            ClearDocumentTabDropFeedback();
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        SetDocumentTabDropTarget(target);
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnDocumentTabDragLeave(object? sender, DragEventArgs e)
    {
        ClearDocumentTabDropFeedback();
    }

    private void OnDocumentTabDrop(object? sender, DragEventArgs e)
    {
        if (Vm is null
            || sender is not Border { DataContext: DocumentTabViewModel target } targetBorder
            || e.DataTransfer.TryGetValue(DocumentTabDragDataFormat) is not string dragId
            || Vm.DocumentTabs.FirstOrDefault(tab => tab.DragId == dragId) is not { } source
            || ReferenceEquals(source, target))
        {
            e.DragEffects = DragDropEffects.None;
            ClearDocumentTabDropFeedback();
            e.Handled = true;
            return;
        }

        var targetIndex = Vm.DocumentTabs.IndexOf(target);
        if (e.GetPosition(targetBorder).X >= targetBorder.Bounds.Width / 2)
        {
            targetIndex++;
        }

        var sourceIndex = Vm.DocumentTabs.IndexOf(source);
        if (sourceIndex < targetIndex)
        {
            targetIndex--;
        }

        var changed = Vm.MoveDocumentTab(source, targetIndex);
        e.DragEffects = changed ? DragDropEffects.Move : DragDropEffects.None;
        ClearDocumentTabDropFeedback();
        e.Handled = true;
    }

    private void SetDocumentTabDropTarget(DocumentTabViewModel target)
    {
        if (ReferenceEquals(_documentTabDropTarget, target))
        {
            return;
        }

        ClearDocumentTabDropFeedback();
        _documentTabDropTarget = target;
        target.SetDropTarget(true);
    }

    private void ClearDocumentTabDropFeedback()
    {
        _documentTabDropTarget?.SetDropTarget(false);
        _documentTabDropTarget = null;
    }

    private void OnMoveDocumentTabLeftMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => MoveDocumentTabFromMenu(sender, -1);

    private void OnMoveDocumentTabRightMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => MoveDocumentTabFromMenu(sender, 1);

    private void OnDuplicateDocumentTabMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        var sourceTab = sender is MenuItem { Tag: DocumentTabViewModel tab }
            ? tab
            : null;
        Vm.DuplicateDocumentTab(sourceTab);
        ClearDesignGuides();
    }

    private async void OnRenameDocumentTabMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var tab = sender is MenuItem { Tag: DocumentTabViewModel taggedTab }
            ? taggedTab
            : Vm.SelectedDocumentTab;
        await RenameDocumentTabAsync(tab);
    }

    private async Task RenameDocumentTabAsync(DocumentTabViewModel? tab)
    {
        if (Vm is null || tab is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        var displayName = await ShowDocumentTabRenameDialogAsync(tab);
        if (displayName is not null)
        {
            Vm.RenameDocumentTab(tab, displayName);
        }
    }

    private async Task<string?> ShowDocumentTabRenameDialogAsync(DocumentTabViewModel tab)
    {
        var dialog = new Window
        {
            Title = "Rename Document Tab",
            Width = 440,
            Height = 210,
            MinWidth = 360,
            MinHeight = 190,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var editor = new TextBox
        {
            Text = tab.DisplayName,
            Watermark = "Document tab name",
            MinWidth = 320,
        };
        var errorText = new TextBlock
        {
            Foreground = Brush.Parse("#B91C1C"),
            TextWrapping = TextWrapping.Wrap,
        };
        var applyButton = new Button { Content = "Rename", MinWidth = 86 };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 86 };

        void ApplyRename()
        {
            var normalizedName = editor.Text?.Trim() ?? string.Empty;
            if (normalizedName.Length == 0
                || normalizedName.Length > 80
                || normalizedName.Any(char.IsControl))
            {
                errorText.Text = "Use 1-80 visible characters without line breaks.";
                return;
            }

            dialog.Close(normalizedName);
        }

        applyButton.Click += (_, _) => ApplyRename();
        cancelButton.Click += (_, _) => dialog.Close(null);
        editor.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                ApplyRename();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close(null);
                e.Handled = true;
            }
        };
        dialog.Opened += (_, _) =>
        {
            editor.Focus();
            editor.SelectAll();
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { cancelButton, applyButton },
        };
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = $"Choose a label for {tab.DisplayName}." },
                editor,
                errorText,
                buttons,
            },
        };

        return await dialog.ShowDialog<string?>(this);
    }

    private async void OnCloseDocumentTabMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: DocumentTabViewModel tab })
        {
            await CloseDocumentTabAsync(tab);
        }
    }

    private async void OnCloseDocumentTabsToRightMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: DocumentTabViewModel tab })
        {
            var tabIndex = Vm?.DocumentTabs.IndexOf(tab) ?? -1;
            if (tabIndex >= 0 && Vm is not null)
            {
                await CloseDocumentTabSetAsync(
                    tab,
                    Vm.DocumentTabs.Skip(tabIndex + 1).ToList());
            }
        }
    }

    private async void OnCloseOtherDocumentTabsMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is not null && sender is MenuItem { Tag: DocumentTabViewModel tab })
        {
            await CloseDocumentTabSetAsync(
                tab,
                Vm.DocumentTabs.Where(candidate => !ReferenceEquals(candidate, tab)).ToList());
        }
    }

    private async Task<bool> CloseDocumentTabSetAsync(
        DocumentTabViewModel preferredTab,
        IReadOnlyList<DocumentTabViewModel> tabsToClose)
    {
        if (Vm is null)
        {
            return false;
        }

        var targets = tabsToClose
            .Where(tab => Vm.DocumentTabs.Contains(tab))
            .ToList();
        if (targets.Count == 0)
        {
            return true;
        }

        var originalTab = Vm.SelectedDocumentTab;
        foreach (var tab in targets)
        {
            FlushPendingPropertyHistory();
            if (!ReferenceEquals(Vm.SelectedDocumentTab, tab))
            {
                Vm.ActivateDocumentTab(tab);
            }

            if (!await EnsureCanContinueWithUnsavedChangesAsync())
            {
                RestoreDocumentTabIfPresent(originalTab ?? preferredTab);
                return false;
            }
        }

        RestoreDocumentTabIfPresent(preferredTab);
        foreach (var tab in targets)
        {
            if (Vm.DocumentTabs.Contains(tab))
            {
                Vm.CloseDocumentTab(tab);
            }
        }

        RestoreDocumentTabIfPresent(preferredTab);
        return true;
    }

    private void RestoreDocumentTabIfPresent(DocumentTabViewModel tab)
    {
        if (Vm is not null
            && Vm.DocumentTabs.Contains(tab)
            && !ReferenceEquals(Vm.SelectedDocumentTab, tab))
        {
            Vm.ActivateDocumentTab(tab);
        }
    }

    private void MoveDocumentTabFromMenu(object? sender, int offset)
    {
        if (Vm is null
            || sender is not MenuItem { Tag: DocumentTabViewModel tab })
        {
            return;
        }

        var currentIndex = Vm.DocumentTabs.IndexOf(tab);
        if (currentIndex < 0)
        {
            return;
        }

        Vm.MoveDocumentTab(tab, currentIndex + offset);
    }

    private async void OnDocumentTabCloseClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null || sender is not Button { Tag: DocumentTabViewModel tab })
        {
            return;
        }

        await CloseDocumentTabAsync(tab);
    }

    private async void OnCloseCurrentTabMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm?.SelectedDocumentTab is { } tab)
        {
            await CloseDocumentTabAsync(tab);
        }
    }

    private async void OnCloseAllTabsMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = await CloseAllDocumentTabsAsync();
    }

    private void OnReopenClosedTabMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.ReopenClosedDocumentTab();
    }

    private async Task CloseDocumentTabAsync(DocumentTabViewModel tab)
    {
        if (Vm is null || Vm.DocumentTabs.Count <= 1)
        {
            return;
        }

        var originalTab = Vm.SelectedDocumentTab;
        if (!ReferenceEquals(originalTab, tab))
        {
            Vm.ActivateDocumentTab(tab);
        }

        FlushPendingPropertyHistory();
        if (!await EnsureCanContinueWithUnsavedChangesAsync())
        {
            if (originalTab is not null && !ReferenceEquals(originalTab, tab))
            {
                Vm.ActivateDocumentTab(originalTab);
            }

            return;
        }

        Vm.CloseDocumentTab(tab);
    }

    private async Task<bool> CloseAllDocumentTabsAsync()
    {
        if (Vm is null || Vm.DocumentTabs.Count <= 1)
        {
            return false;
        }

        FlushPendingPropertyHistory();
        var originalTab = Vm.SelectedDocumentTab;
        var tabs = Vm.DocumentTabs.ToList();
        foreach (var tab in tabs)
        {
            if (!ReferenceEquals(Vm.SelectedDocumentTab, tab))
            {
                Vm.ActivateDocumentTab(tab);
            }

            FlushPendingPropertyHistory();
            if (!await EnsureCanContinueWithUnsavedChangesAsync())
            {
                if (originalTab is not null)
                {
                    RestoreDocumentTabIfPresent(originalTab);
                }

                return false;
            }
        }

        var keepTab = originalTab ?? tabs[0];
        RestoreDocumentTabIfPresent(keepTab);
        foreach (var tab in tabs)
        {
            if (!ReferenceEquals(tab, keepTab) && Vm.DocumentTabs.Contains(tab))
            {
                Vm.CloseDocumentTab(tab);
            }
        }

        Vm.NewDocument();
        ClearDesignGuides();
        Vm.StatusText = "Closed all document tabs and created a new document.";
        return true;
    }

    private async void OnLoadComponentPackMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Component Pack",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Component Pack JSON") { Patterns = ["*.json"] }
            ]
        });

        if (files.Count == 0)
        {
            return;
        }

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        if (!Vm.TryLoadComponentPack(json, files[0].Path.LocalPath, out var result))
        {
            Vm.StatusText = $"Could not load component pack: {result}";
        }
    }

    private async void OnLoadComponentPackPluginMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Component Pack Plugin",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Component Pack Plugin DLL") { Patterns = ["*.dll"] }
            ]
        });

        if (files.Count == 0)
        {
            return;
        }

        if (!Vm.TryLoadComponentPackPlugin(files[0].Path.LocalPath, out var result))
        {
            Vm.StatusText = $"Could not load component pack plugin: {result}";
        }
    }

    private async void OnManageComponentPacksMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        while (true)
        {
            var action = await ShowComponentPackManagerDialogAsync(Vm.ComponentPacks);
            if (action is null)
            {
                return;
            }

            if (!Vm.TryRemoveComponentPack(action.SourceId, out var result))
            {
                Vm.StatusText = $"Could not remove component pack: {result}";
                return;
            }
        }
    }

    private async void OnLoadToolboxPresetPackMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Toolbox Preset Pack",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Toolbox Preset JSON") { Patterns = ["*.toolbox-preset.json", "*.json"] }
            ]
        });

        if (files.Count == 0)
        {
            return;
        }

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        if (!Vm.TryLoadToolboxPresetPack(json, files[0].Path.LocalPath, out var result))
        {
            Vm.StatusText = $"Could not load Toolbox preset pack: {result}";
        }
    }

    private async void OnExportSelectedComponentPackMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedComponentPackDefaults(out var packName, out var displayName, out var namePrefix))
        {
            return;
        }

        var options = await ShowComponentPackExportDialogAsync(packName, displayName, namePrefix);
        if (options is null)
        {
            return;
        }

        if (!Vm.TryExportSelectedComponentPack(
                options.PackName,
                options.DisplayName,
                options.NamePrefix,
                out var json,
                out var error))
        {
            Vm.StatusText = $"Could not export component pack: {error}";
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Component Pack",
            SuggestedFileName = $"{options.NamePrefix}.component-pack.json",
            DefaultExtension = "json",
            FileTypeChoices =
            [
                new FilePickerFileType("Component Pack JSON") { Patterns = ["*.json"] }
            ]
        });

        if (file is null)
        {
            return;
        }

        try
        {
            var localPath = file.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(localPath))
            {
                await AtomicFileWriter.WriteAllTextAsync(localPath, json);
            }
            else
            {
                await using var stream = await file.OpenWriteAsync();
                stream.SetLength(0);
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(json);
                await writer.FlushAsync();
            }

            Vm.StatusText = $"Exported {options.DisplayName} component pack to {file.Name}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not export {file.Name}: {exception.Message}";
        }
    }

    private async void OnExportSelectedToolboxPresetMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedToolboxPresetExportDefaults(out var displayName))
        {
            return;
        }

        if (!Vm.TryExportSelectedToolboxPreset(out var json, out var error))
        {
            Vm.StatusText = $"Could not export Toolbox preset: {error}";
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Toolbox Preset",
            SuggestedFileName = "toolbox-preset.toolbox-preset.json",
            DefaultExtension = "json",
            FileTypeChoices =
            [
                new FilePickerFileType("Toolbox Preset JSON") { Patterns = ["*.toolbox-preset.json", "*.json"] }
            ]
        });
        if (file is null)
        {
            return;
        }

        try
        {
            var localPath = file.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(localPath))
            {
                await AtomicFileWriter.WriteAllTextAsync(localPath, json);
            }
            else
            {
                await using var stream = await file.OpenWriteAsync();
                stream.SetLength(0);
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(json);
                await writer.FlushAsync();
            }

            Vm.StatusText = $"Exported {displayName} Toolbox preset to {file.Name}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not export {file.Name}: {exception.Message}";
        }
    }

    private async void OnAddSelectionToToolboxMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedToolboxPresetDefaults(out var displayName))
        {
            return;
        }

        var updatedName = await ShowTextEditorDialogAsync(
            "Add Selection to Toolbox",
            displayName,
            "Name the reusable root-control layout preset.",
            multiline: false);
        if (updatedName is null)
        {
            return;
        }

        if (!Vm.TryAddSelectedAsToolboxPreset(updatedName, out var error))
        {
            Vm.StatusText = $"Could not add Toolbox preset: {error}";
        }
    }

    private async Task HandleOpenCommandAsync()
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open AXAML",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("AXAML") { Patterns = ["*.axaml", "*.xaml"] }
            ]
        });

        if (files.Count == 0)
        {
            return;
        }

        await OpenStorageFileAsync(files[0]);
    }

    private async void OnSaveMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = await SaveDocumentAsync(forceSaveAs: false);
    }

    private async void OnSaveAsMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = await SaveDocumentAsync(forceSaveAs: true);
    }

    private async void OnSaveAllMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = await SaveAllDocumentsAsync();
    }

    private async void OnRecoverBackupMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null || string.IsNullOrWhiteSpace(Vm.CurrentDocumentPath))
        {
            return;
        }

        FlushPendingPropertyHistory();
        if (!await EnsureCanContinueWithUnsavedChangesAsync())
        {
            return;
        }

        var documentPath = Vm.CurrentDocumentPath;
        if (string.IsNullOrWhiteSpace(documentPath))
        {
            UpdateDocumentBackupMenu();
            return;
        }

        var backupPath = GetDocumentBackupPath(documentPath);
        if (!File.Exists(backupPath))
        {
            UpdateDocumentBackupMenu();
            Vm.StatusText = "No document backup is available.";
            return;
        }

        try
        {
            var backupAxaml = await File.ReadAllTextAsync(backupPath);
            if (!Vm.TryApplyAxamlSource(backupAxaml, out var result))
            {
                Vm.StatusText = $"Could not recover backup: {result}";
                return;
            }

            ClearDesignGuides();
            Vm.StatusText = $"Recovered backup for {System.IO.Path.GetFileName(documentPath)}. {result}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not recover backup: {exception.Message}";
        }
    }

    private async void OnCopyAxamlMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        await CopyAxamlToClipboardAsync(Vm.ExportFullAxaml(), "Copied Window AXAML to clipboard.");
    }

    private async void OnCopySelectedAxamlMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        if (!Vm.TryExportSelectedAxaml(out var controlName, out var axaml, out var error))
        {
            Vm.StatusText = $"Could not export selected AXAML: {error}";
            return;
        }

        await CopyAxamlToClipboardAsync(
            axaml,
            $"Copied {controlName} AXAML to clipboard.");
    }

    private async void OnExportSelectedAxamlMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        if (!Vm.TryExportSelectedAxaml(out var controlName, out var axaml, out var error))
        {
            Vm.StatusText = $"Could not export selected AXAML: {error}";
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Selected AXAML",
            SuggestedFileName = $"{controlName}.axaml",
            DefaultExtension = "axaml",
            FileTypeChoices =
            [
                new FilePickerFileType("AXAML") { Patterns = ["*.axaml", "*.xaml"] }
            ]
        });

        if (file is null)
        {
            return;
        }

        try
        {
            var localPath = file.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(localPath))
            {
                await AtomicFileWriter.WriteAllTextAsync(localPath, axaml);
            }
            else
            {
                await using var stream = await file.OpenWriteAsync();
                stream.SetLength(0);
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(axaml);
                await writer.FlushAsync();
            }

            Vm.StatusText = $"Exported {controlName} AXAML to {file.Name}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not export {file.Name}: {exception.Message}";
        }
    }

    private async void OnCopyUserControlAxamlMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        await CopyAxamlToClipboardAsync(Vm.ExportUserControlAxaml(), "Copied UserControl AXAML to clipboard.");
    }

    private async void OnExportUserControlAxamlMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export UserControl AXAML",
            SuggestedFileName = "DesignView.axaml",
            DefaultExtension = "axaml",
            FileTypeChoices =
            [
                new FilePickerFileType("AXAML") { Patterns = ["*.axaml"] }
            ]
        });

        if (file is null)
        {
            return;
        }

        try
        {
            var axaml = Vm.ExportUserControlAxaml();
            var localPath = file.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(localPath))
            {
                await AtomicFileWriter.WriteAllTextAsync(localPath, axaml);
            }
            else
            {
                await using var stream = await file.OpenWriteAsync();
                stream.SetLength(0);
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(axaml);
                await writer.FlushAsync();
            }

            Vm.StatusText = $"Exported UserControl AXAML to {file.Name}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not export {file.Name}: {exception.Message}";
        }
    }

    private async Task CopyAxamlToClipboardAsync(string axaml, string successStatus)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            if (Vm is not null)
            {
                Vm.StatusText = "Clipboard is unavailable.";
            }

            return;
        }

        await clipboard.SetTextAsync(axaml);
        if (Vm is not null)
        {
            Vm.StatusText = successStatus;
        }
    }

    private async Task PasteAxamlFromClipboardAsync()
    {
        if (Vm is null)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            Vm.StatusText = "Clipboard is unavailable.";
            return;
        }

        string? axaml;
        try
        {
            axaml = await clipboard.TryGetTextAsync();
        }
        catch (Exception exception)
        {
            Vm.StatusText = $"Could not read AXAML from the clipboard: {exception.Message}";
            return;
        }

        if (string.IsNullOrWhiteSpace(axaml))
        {
            Vm.StatusText = "The clipboard does not contain AXAML text.";
            return;
        }

        FlushPendingPropertyHistory();
        Vm.TryPasteAxamlFragment(axaml, out _);
    }

    private void OnValidateAxamlMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is not null)
        {
            Vm.TryValidateCurrentAxaml(out var result);
            Vm.StatusText = result;
        }
    }

    private async void OnNewMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await HandleNewCommandAsync();
    }

    private void OnTemplateMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string templateName })
        {
            return;
        }

        FlushPendingPropertyHistory();
        Vm?.CreateDocumentTabFromTemplate(templateName);
        ClearDesignGuides();
    }

    private Task HandleNewCommandAsync()
    {
        FlushPendingPropertyHistory();
        Vm?.CreateNewDocumentTab();
        ClearDesignGuides();
        return Task.CompletedTask;
    }

    private async void OnExitMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await RequestCloseAsync();
    }

    private void OnUndoMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.Undo();
    }

    private void OnRedoMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.Redo();
    }

    private async void OnHistoryMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await ShowHistoryTimelineAsync();

    private async Task ShowHistoryTimelineAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        var dialog = new Window
        {
            Title = "Undo History",
            Width = 560,
            Height = 520,
            MinWidth = 420,
            MinHeight = 320,
            CanResize = true,
        };
        var historyItems = new StackPanel { Spacing = 4 };
        foreach (var entry in Vm.HistoryTimeline)
        {
            if (entry.IsCurrent)
            {
                historyItems.Children.Add(new Border
                {
                    Padding = new Thickness(10, 8),
                    Background = Brush.Parse("#DBEAFE"),
                    BorderBrush = Brush.Parse("#60A5FA"),
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = $"Current: {entry.Label}",
                        FontWeight = FontWeight.Bold,
                        Foreground = Brush.Parse("#1E3A8A"),
                    },
                });
                continue;
            }

            var historyButton = new Button
            {
                Content = entry.Label,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(10, 7),
            };
            historyButton.Click += (_, _) =>
            {
                Vm.JumpToHistory(entry);
                dialog.Close();
            };
            historyItems.Children.Add(historyButton);
        }

        var closeButton = new Button
        {
            Content = "Close",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 84,
            Margin = new Thickness(0, 8, 0, 0),
        };
        closeButton.Click += (_, _) => dialog.Close();
        var dialogLayout = new DockPanel();
        DockPanel.SetDock(closeButton, Dock.Bottom);
        dialogLayout.Children.Add(closeButton);
        dialogLayout.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = historyItems,
        });
        dialog.Content = new Border
        {
            Padding = new Thickness(16),
            Child = dialogLayout,
        };

        await dialog.ShowDialog(this);
    }

    private void OnSelectAllMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm?.SelectAllVisibleUnlockedElements();
    }

    private void OnToggleLockMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.ToggleSelectedLock();

    private void OnPropertyInspectorCategoriesClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        PropGrid.IsCategoryVisible = true;
        CapturePropertyInspectorState();
        Vm?.StatusText = "Property categories shown.";
    }

    private void OnPropertyInspectorFlatClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        PropGrid.IsCategoryVisible = false;
        CapturePropertyInspectorState();
        Vm?.StatusText = "Property categories hidden.";
    }

    private void OnPropertyInspectorExpandAllClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        PropGrid.AllCategoriesExpanded = true;
        CapturePropertyInspectorState();
        Vm?.StatusText = "All property categories expanded.";
    }

    private void OnPropertyInspectorCollapseAllClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        PropGrid.AllCategoriesExpanded = false;
        CapturePropertyInspectorState();
        Vm?.StatusText = "All property categories collapsed.";
    }

    private void OnPropertyInspectorFilterChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyPropertyInspectorFilter();
        CapturePropertyInspectorState();
    }

    private void OnPropertyInspectorDescriptorFilter(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (e is not Avalonia.PropertyGrid.Controls.CustomPropertyDescriptorFilterEventArgs filter)
        {
            return;
        }

        var query = _propertyInspectorFilterText;
        filter.IsVisible = string.IsNullOrWhiteSpace(query)
            || filter.PropertyDescriptor.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || filter.PropertyDescriptor.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void OnPropertyInspectorFilterClearClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        PropertyInspectorFilter.Text = string.Empty;
        PropertyInspectorFilter.Focus();
        Vm?.StatusText = "Property filter cleared.";
    }

    private void OnPropertyInspectorFilterKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            PropertyInspectorFilter.Text = string.Empty;
            PropGrid.Focus();
            Vm?.StatusText = "Property Inspector filter cleared.";
            e.Handled = true;
        }
    }

    private void ApplyPropertyInspectorFilter()
    {
        _propertyInspectorFilterText = PropertyInspectorFilter.Text?.Trim() ?? string.Empty;
        PropGrid.Content = null;
        PropGrid.Content = _boundElement?.Visual;
    }

    private void CapturePropertyInspectorState()
        => Vm?.SetPropertyInspectorState(
            PropertyInspectorFilter.Text,
            PropGrid.IsCategoryVisible,
            PropGrid.AllCategoriesExpanded);

    private void ApplyPropertyInspectorState()
    {
        if (Vm is null)
        {
            return;
        }

        var state = Vm.GetPropertyInspectorState();
        _propertyInspectorFilterText = state.FilterText;
        PropGrid.IsCategoryVisible = state.CategoriesVisible;
        PropGrid.AllCategoriesExpanded = state.AllCategoriesExpanded;
        PropertyInspectorFilter.Text = state.FilterText;
        ApplyPropertyInspectorFilter();
    }

    private void OnShowToolboxMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => SetToolboxPaneVisible(!ToolboxPane.IsVisible);

    private void OnShowObjectTreeMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => SetObjectTreePaneVisible(!ObjectTreePane.IsVisible);

    private void OnShowPropertyInspectorMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => SetPropertyInspectorPaneVisible(!PropertyInspectorPane.IsVisible);

    private void OnResetPanelLayoutMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => ResetWorkspacePanelLayout();

    private void ResetWorkspacePanelLayout()
    {
        Vm?.SetWorkspacePanelState(WorkspacePanelState.Default);
        ApplyWorkspacePanelState();
        Vm?.StatusText = "Panel layout reset.";
    }

    private void SetToolboxPaneVisible(bool isVisible)
    {
        if (!isVisible)
        {
            CaptureWorkspacePanelDimensions();
        }

        ToolboxPane.IsVisible = isVisible;
        ToolboxSplitter.IsVisible = isVisible;
        UpdateWorkspacePanelLayout();
        SyncWorkspacePanelState();
        Vm?.StatusText = isVisible ? "Toolbox panel shown." : "Toolbox panel hidden.";
    }

    private void SetObjectTreePaneVisible(bool isVisible)
    {
        if (!isVisible)
        {
            CaptureWorkspacePanelDimensions();
        }

        ObjectTreePane.IsVisible = isVisible;
        UpdateWorkspacePanelLayout();
        SyncWorkspacePanelState();
        Vm?.StatusText = isVisible ? "Object Tree panel shown." : "Object Tree panel hidden.";
    }

    private void SetPropertyInspectorPaneVisible(bool isVisible)
    {
        if (!isVisible)
        {
            CaptureWorkspacePanelDimensions();
        }

        PropertyInspectorPane.IsVisible = isVisible;
        UpdateWorkspacePanelLayout();
        SyncWorkspacePanelState();
        Vm?.StatusText = isVisible
            ? "Property Inspector panel shown."
            : "Property Inspector panel hidden.";
    }

    private void ApplyWorkspacePanelState()
    {
        if (Vm is null)
        {
            return;
        }

        var state = Vm.GetWorkspacePanelState();
        _toolboxPaneWidth = NormalizeToolboxPaneWidth(state.ToolboxWidth);
        _inspectorPaneWidth = NormalizeInspectorPaneWidth(state.InspectorWidth);
        _objectTreePaneHeight = NormalizeObjectTreePaneHeight(state.ObjectTreeHeight);
        ToolboxPane.IsVisible = state.ToolboxVisible;
        ToolboxSplitter.IsVisible = state.ToolboxVisible;
        ObjectTreePane.IsVisible = state.ObjectTreeVisible;
        PropertyInspectorPane.IsVisible = state.PropertyInspectorVisible;
        UpdateWorkspacePanelLayout();
        UpdateWorkspacePanelMenuChecks();
    }

    private void UpdateWorkspacePanelLayout()
    {
        var columnDefinitions = WorkspaceGrid.ColumnDefinitions;
        columnDefinitions[0].Width = ToolboxPane.IsVisible
            ? new GridLength(_toolboxPaneWidth, GridUnitType.Pixel)
            : new GridLength(0, GridUnitType.Pixel);
        columnDefinitions[1].Width = ToolboxPane.IsVisible
            ? new GridLength(4, GridUnitType.Pixel)
            : new GridLength(0, GridUnitType.Pixel);

        var hasInspectorPane = ObjectTreePane.IsVisible || PropertyInspectorPane.IsVisible;
        columnDefinitions[3].Width = hasInspectorPane
            ? new GridLength(4, GridUnitType.Pixel)
            : new GridLength(0, GridUnitType.Pixel);
        columnDefinitions[4].Width = hasInspectorPane
            ? new GridLength(_inspectorPaneWidth, GridUnitType.Pixel)
            : new GridLength(0, GridUnitType.Pixel);

        var rowDefinitions = InspectorPane.RowDefinitions;
        rowDefinitions[0].Height = ObjectTreePane.IsVisible
            ? PropertyInspectorPane.IsVisible && _objectTreePaneHeight > 0
                ? new GridLength(_objectTreePaneHeight, GridUnitType.Pixel)
                : new GridLength(1, GridUnitType.Star)
            : new GridLength(0, GridUnitType.Pixel);
        rowDefinitions[1].Height = ObjectTreePane.IsVisible && PropertyInspectorPane.IsVisible
            ? new GridLength(4, GridUnitType.Pixel)
            : new GridLength(0, GridUnitType.Pixel);
        rowDefinitions[2].Height = PropertyInspectorPane.IsVisible
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0, GridUnitType.Pixel);
        ObjectTreeSplitter.IsVisible = ObjectTreePane.IsVisible && PropertyInspectorPane.IsVisible;
        UpdateWorkspacePanelMenuChecks();
    }

    private void UpdateWorkspacePanelMenuChecks()
    {
        ShowToolboxMenu.IsChecked = ToolboxPane.IsVisible;
        ShowObjectTreeMenu.IsChecked = ObjectTreePane.IsVisible;
        ShowPropertyInspectorMenu.IsChecked = PropertyInspectorPane.IsVisible;
    }

    private void CaptureWorkspacePanelDimensions()
    {
        if (ToolboxPane.IsVisible && ToolboxPane.Bounds.Width > 0)
        {
            _toolboxPaneWidth = NormalizeToolboxPaneWidth(ToolboxPane.Bounds.Width);
        }

        if (InspectorPane.IsVisible && InspectorPane.Bounds.Width > 0)
        {
            _inspectorPaneWidth = NormalizeInspectorPaneWidth(InspectorPane.Bounds.Width);
        }

        if (ObjectTreePane.IsVisible && ObjectTreePane.Bounds.Height > 0)
        {
            _objectTreePaneHeight = NormalizeObjectTreePaneHeight(ObjectTreePane.Bounds.Height);
        }
    }

    private void SyncWorkspacePanelState()
    {
        CaptureWorkspacePanelDimensions();
        Vm?.SetWorkspacePanelState(new WorkspacePanelState(
            ToolboxPane.IsVisible,
            ObjectTreePane.IsVisible,
            PropertyInspectorPane.IsVisible,
            _toolboxPaneWidth,
            _inspectorPaneWidth,
            _objectTreePaneHeight));
    }

    private static double NormalizeToolboxPaneWidth(double value)
        => double.IsFinite(value) ? Math.Clamp(value, 160, 520) : 220;

    private static double NormalizeInspectorPaneWidth(double value)
        => double.IsFinite(value) ? Math.Clamp(value, 220, 560) : 280;

    private static double NormalizeObjectTreePaneHeight(double value)
        => double.IsFinite(value) && value > 0 ? Math.Clamp(value, 140, 1200) : 0;

    private void OnOpacity100MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetSelectionOpacity(1);

    private void OnOpacity75MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetSelectionOpacity(0.75);

    private void OnOpacity50MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetSelectionOpacity(0.5);

    private void OnTextSize14MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextSize(14);
    private void OnTextSize18MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextSize(18);
    private void OnTextSize24MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextSize(24);
    private void OnTextColorInkMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextColor("#111827");
    private void OnTextColorBlueMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextColor("#2563EB");
    private void OnTextColorRedMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextColor("#DC2626");
    private void OnTextWeightRegularMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextWeight("Regular");
    private void OnTextWeightSemiboldMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextWeight("Semibold");
    private void OnTextWeightBoldMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.SetSelectedTextWeight("Bold");

    private async void OnEditAppearanceMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedAppearance(out var controlName, out var appearance))
        {
            return;
        }

        var updatedAppearance = await ShowAppearanceEditorDialogAsync(controlName, appearance);
        if (updatedAppearance is not null)
        {
            Vm.SetSelectedAppearance(updatedAppearance);
        }
    }

    private async void OnEditColorResourcesMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        var updatedResources = await ShowTextEditorDialogAsync(
            "Edit Color Resources",
            Vm.GetColorResourceEditorText(),
            "Enter one SolidColorBrush per line using Key = Brush, for example PrimaryBrush = #2563EB.");
        if (updatedResources is not null)
        {
            Vm.SetColorResourcesFromText(updatedResources);
        }
    }

    private async void OnEditDocumentStylesMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        var updatedStyles = await ShowTextEditorDialogAsync(
            "Edit Document Styles",
            Vm.GetDocumentStyleEditorText(),
            "Use [Control.class] or [Control.class:pseudo] sections with Property = Value setters. Example: [Button.primary:pointerover].");
        if (updatedStyles is not null)
        {
            Vm.SetDocumentStylesFromText(updatedStyles);
        }
    }

    private async void OnEditSelectedClassesMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedStyleClasses(out var controlName, out var classes))
        {
            return;
        }

        var updatedClasses = await ShowTextEditorDialogAsync(
            $"Edit Style Classes - {controlName}",
            classes,
            "Enter space-separated Avalonia style classes. Remove all text to clear the classes.",
            multiline: false);
        if (updatedClasses is not null)
        {
            Vm.SetSelectedStyleClassesFromText(updatedClasses);
        }
    }

    private void OnPreviewStateNormalMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState(null);

    private void OnPreviewStatePointerOverMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("pointerover");

    private void OnPreviewStatePressedMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("pressed");

    private void OnPreviewStateDisabledMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("disabled");

    private void OnPreviewStateFocusMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("focus");

    private void OnPreviewStateFocusVisibleMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("focus-visible");

    private void OnPreviewStateCheckedMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("checked");

    private void OnPreviewStateExpandedMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetStylePreviewState("expanded");

    private void SetStylePreviewState(string? pseudoClass)
    {
        FlushPendingPropertyHistory();
        Vm?.SetSelectedStylePreviewState(pseudoClass);
    }

    private async void OnApplyColorResourceMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetColorResourceApplicationOptions(
                out var controlName,
                out var resourceNames,
                out var propertyNames))
        {
            return;
        }

        var options = await ShowColorResourceApplicationDialogAsync(controlName, resourceNames, propertyNames);
        if (options is not null)
        {
            Vm.ApplyColorResource(options.ResourceName, options.PropertyName);
        }
    }

    private void SetSelectionOpacity(double opacity)
    {
        FlushPendingPropertyHistory();
        Vm?.SetSelectedOpacity(opacity);
    }

    private void OnDeleteMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.RemoveSelectedElement();
    }

    private void OnCopyMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.CopySelectedElement();
    }

    private void OnCopyStyleMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => CopySelectedStyle();

    private void CopySelectedStyle()
    {
        FlushPendingPropertyHistory();
        Vm?.CopySelectedStyle();
    }

    private void OnCutMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.CutSelectedElement();
    }

    private void OnPasteMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.PasteElement();
    }

    private async void OnPasteAxamlFromClipboardMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await PasteAxamlFromClipboardAsync();

    private void OnPasteStyleMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => PasteSelectedStyle();

    private void PasteSelectedStyle()
    {
        FlushPendingPropertyHistory();
        Vm?.PasteSelectedStyle();
    }

    private void OnDuplicateMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.DuplicateSelectedElement();
    }

    private async void OnRenameSelectedControlMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await RenameSelectedControlAsync();

    private async Task RenameSelectedControlAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedElementName(out var controlName, out var elementName))
        {
            return;
        }

        var updatedName = await ShowTextEditorDialogAsync(
            $"Rename Control - {controlName}",
            elementName,
            "Enter a unique x:Name. Names must start with a letter or underscore and contain only letters, numbers, or underscores.",
            multiline: false);
        if (updatedName is not null)
        {
            Vm.RenameSelectedElement(updatedName);
        }
    }

    private async void OnEditItemsMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedItemsEditor(out var state))
        {
            return;
        }

        var updatedItems = await ShowItemsEditorDialogAsync(state);
        if (updatedItems is not null)
        {
            Vm.SetSelectedItems(updatedItems);
        }
    }

    private async void OnEditDataGridBehaviorPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditDataGridBehaviorPropertiesAsync();

    private async Task EditDataGridBehaviorPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null
            || !Vm.TryGetSelectedDataGridBehaviorProperties(out var state))
        {
            return;
        }

        await ShowDataGridBehaviorPropertiesDialogAsync(state);
    }

    private async void OnEditBindingsMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedBindings(out var state))
        {
            return;
        }

        var updatedBindings = await ShowBindingEditorDialogAsync(state);
        if (updatedBindings is not null)
        {
            Vm.SetSelectedBindings(updatedBindings);
        }
    }

    private async void OnEditCommonPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditCommonPropertiesAsync();

    private async Task EditCommonPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedCommonProperties(out var state))
        {
            return;
        }

        var updated = await ShowCommonPropertiesDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedCommonProperties(
                updated.Margin,
                updated.HorizontalAlignment,
                updated.VerticalAlignment,
                updated.Opacity,
                updated.IsEnabled,
                updated.IsVisible,
                updated.IsHitTestVisible);
        }
    }

    private async void OnEditLayoutPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditLayoutPropertiesAsync();

    private async Task EditLayoutPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedLayoutProperties(out var state))
        {
            return;
        }

        await ShowLayoutPropertiesDialogAsync(state);
    }

    private async void OnEditSelectionBoundsMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditSelectionBoundsAsync();

    private async Task EditSelectionBoundsAsync()
    {
        FlushPendingLayoutHistory();
        if (Vm is null
            || Vm.Canvas.SelectedElements.Count < 2
            || !CanResizeSelection()
            || !TryGetSelectionBounds(out var currentBounds))
        {
            Vm?.StatusText = "Select at least two unlocked controls with the same root or Canvas parent to edit shared bounds.";
            return;
        }

        var updatedBounds = await ShowSelectionBoundsDialogAsync(currentBounds);
        if (updatedBounds is null
            || Vm is null
            || !CanResizeSelection()
            || !TryGetSelectionBounds(out currentBounds))
        {
            return;
        }

        ApplySelectionBounds(currentBounds, updatedBounds.Value);
    }

    private async void OnEditTypographyPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditTypographyPropertiesAsync();

    private async Task EditTypographyPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedTypographyProperties(out var state))
        {
            return;
        }

        await ShowTypographyPropertiesDialogAsync(state);
    }

    private async void OnEditTransformPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditTransformPropertiesAsync();

    private async Task EditTransformPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedTransformProperties(out var state))
        {
            return;
        }

        await ShowTransformPropertiesDialogAsync(state);
    }

    private async void OnEditAccessibilityPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditAccessibilityPropertiesAsync();

    private async Task EditAccessibilityPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedAccessibilityProperties(out var state))
        {
            return;
        }

        await ShowAccessibilityPropertiesDialogAsync(state);
    }

    private async void OnEditInteractionPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditInteractionPropertiesAsync();

    private async Task EditInteractionPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedInteractionProperties(out var state))
        {
            return;
        }

        await ShowInteractionPropertiesDialogAsync(state);
    }

    private async void OnEditEffectPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditEffectPropertiesAsync();

    private async Task EditEffectPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedEffectProperties(out var state))
        {
            return;
        }

        await ShowEffectPropertiesDialogAsync(state);
    }

    private async void OnEditRangePropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditRangePropertiesAsync();

    private async Task EditRangePropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedRangeProperties(out var state))
        {
            return;
        }

        await ShowRangePropertiesDialogAsync(state);
    }

    private async void OnEditTextInputPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditTextInputPropertiesAsync();

    private async Task EditTextInputPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedTextInputProperties(out var state))
        {
            return;
        }

        await ShowTextInputPropertiesDialogAsync(state);
    }

    private async void OnEditSelectionPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditSelectionPropertiesAsync();

    private async Task EditSelectionPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedSelectionProperties(out var state))
        {
            return;
        }

        await ShowSelectionPropertiesDialogAsync(state);
    }

    private async void OnEditMaskedTextBoxPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditMaskedTextBoxPropertiesAsync();

    private async Task EditMaskedTextBoxPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedMaskedTextBoxProperties(out var state))
        {
            return;
        }

        await ShowMaskedTextBoxPropertiesDialogAsync(state);
    }

    private async void OnEditSelectableTextBlockPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditSelectableTextBlockPropertiesAsync();

    private async Task EditSelectableTextBlockPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedSelectableTextBlockProperties(out var state))
        {
            return;
        }

        await ShowSelectableTextBlockPropertiesDialogAsync(state);
    }

    private async void OnEditDateTimePropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditDateTimePropertiesAsync();

    private async Task EditDateTimePropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedDateTimeProperties(out var state))
        {
            return;
        }

        await ShowDateTimePropertiesDialogAsync(state);
    }

    private async void OnEditColorPickerPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditColorPickerPropertiesAsync();

    private async Task EditColorPickerPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedColorPickerProperties(out var state))
        {
            return;
        }

        await ShowColorPickerPropertiesDialogAsync(state);
    }

    private async void OnEditAutoCompleteBoxPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditAutoCompleteBoxPropertiesAsync();

    private async Task EditAutoCompleteBoxPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedAutoCompleteBoxProperties(out var state))
        {
            return;
        }

        await ShowAutoCompleteBoxPropertiesDialogAsync(state);
    }

    private async void OnEditTogglePropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditTogglePropertiesAsync();

    private async Task EditTogglePropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedToggleProperties(out var state))
        {
            return;
        }

        await ShowTogglePropertiesDialogAsync(state);
    }

    private async void OnEditContainerBehaviorPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditContainerBehaviorPropertiesAsync();

    private async Task EditContainerBehaviorPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null
            || !Vm.TryGetSelectedContainerBehaviorProperties(out var state))
        {
            return;
        }

        await ShowContainerBehaviorPropertiesDialogAsync(state);
    }

    private async void OnEditSplitViewPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditSplitViewPropertiesAsync();

    private async Task EditSplitViewPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedSplitViewProperties(out var state))
        {
            return;
        }

        await ShowSplitViewPropertiesDialogAsync(state);
    }

    private async void OnEditTabControlBehaviorPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditTabControlBehaviorPropertiesAsync();

    private async Task EditTabControlBehaviorPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null
            || !Vm.TryGetSelectedTabControlBehaviorProperties(out var state))
        {
            return;
        }

        await ShowTabControlBehaviorPropertiesDialogAsync(state);
    }

    private async void OnEditImagePropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditImagePropertiesAsync();

    private async Task EditImagePropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedImageProperties(out var state))
        {
            return;
        }

        await ShowImagePropertiesDialogAsync(state);
    }

    private async void OnEditButtonPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditButtonPropertiesAsync();

    private async Task EditButtonPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedButtonProperties(out var state))
        {
            return;
        }

        await ShowButtonPropertiesDialogAsync(state);
    }

    private async void OnEditGridDefinitionsMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedGridDefinitions(out var state))
        {
            return;
        }

        var updated = await ShowGridDefinitionsDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedGridDefinitions(
                updated.RowDefinitions,
                updated.ColumnDefinitions,
                updated.ShowGridLines);
        }
    }

    private async void OnEditGridSplitterPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditGridSplitterPropertiesAsync();

    private async Task EditGridSplitterPropertiesAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedGridSplitterProperties(out var state))
        {
            return;
        }

        await ShowGridSplitterPropertiesDialogAsync(state);
    }

    private async void OnAssignToGridCellMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedGridCellAssignment(out var state))
        {
            return;
        }

        var updated = await ShowGridCellAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedGridCellAssignment(
                updated.ParentName,
                updated.Row,
                updated.Column,
                updated.RowSpan,
                updated.ColumnSpan);
        }
    }

    private async void OnAssignToStackPanelMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedStackPanelAssignment(out var state))
        {
            return;
        }

        var updated = await ShowStackPanelAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedStackPanelAssignment(
                updated.ParentName,
                updated.ItemIndex,
                updated.ItemSize);
        }
    }

    private void OnMoveStackPanelItemEarlierMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedStackPanelItem(-1);

    private void OnMoveStackPanelItemLaterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedStackPanelItem(1);

    private async void OnAssignToDockPanelMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedDockPanelAssignment(out var state))
        {
            return;
        }

        var updated = await ShowDockPanelAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedDockPanelAssignment(
                updated.ParentName,
                updated.ItemIndex,
                updated.Dock,
                updated.ItemSize,
                updated.LastChildFill);
        }
    }

    private void OnMoveDockPanelItemEarlierMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedDockPanelItem(-1);

    private void OnMoveDockPanelItemLaterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedDockPanelItem(1);

    private async void OnAssignToWrapPanelMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedWrapPanelAssignment(out var state))
        {
            return;
        }

        var updated = await ShowWrapPanelAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedWrapPanelAssignment(updated.ParentName, updated.ItemIndex);
        }
    }

    private void OnMoveWrapPanelItemEarlierMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedWrapPanelItem(-1);

    private void OnMoveWrapPanelItemLaterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedWrapPanelItem(1);

    private async void OnAssignToUniformGridMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedUniformGridAssignment(out var state))
        {
            return;
        }

        var updated = await ShowUniformGridAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedUniformGridAssignment(updated.ParentName, updated.ItemIndex);
        }
    }

    private void OnMoveUniformGridItemEarlierMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedUniformGridItem(-1);

    private void OnMoveUniformGridItemLaterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedUniformGridItem(1);

    private async void OnAssignToCanvasMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedCanvasAssignment(out var state))
        {
            return;
        }

        var updated = await ShowCanvasAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedCanvasAssignment(
                updated.ParentName,
                updated.ItemIndex,
                updated.Left,
                updated.Top);
        }
    }

    private void OnMoveCanvasItemEarlierMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedCanvasItem(-1);

    private void OnMoveCanvasItemLaterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.MoveSelectedCanvasItem(1);

    private async void OnAssignToTabControlMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedTabControlAssignment(out var state))
        {
            return;
        }

        var updated = await ShowTabControlAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedTabControlAssignment(updated.ParentName, updated.TabIndex);
        }
    }

    private async void OnAssignToSplitViewMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedSplitViewAssignment(out var state))
        {
            return;
        }

        var updated = await ShowSplitViewAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedSplitViewAssignment(updated.ParentName, updated.Slot);
        }
    }

    private void OnRemoveFromContainerMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.RemoveSelectedFromContainer();
    }

    private async void OnAssignAsContainerContentMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedContentAssignment(out var state))
        {
            return;
        }

        var updated = await ShowContentAssignmentDialogAsync(state);
        if (updated is not null)
        {
            Vm.SetSelectedContentAssignment(updated.ParentName);
        }
    }

    private async void OnChooseImageMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Image files")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp"]
                }
            ]
        });

        if (files.Count == 0 || !files[0].Path.IsFile)
        {
            return;
        }

        Vm.TrySetSelectedImageSource(files[0].Path.AbsoluteUri);
    }

    private async void OnEditPathDataMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedPathData(out var controlName, out var data))
        {
            return;
        }

        var updated = await ShowTextEditorDialogAsync(
            $"Edit Path Data - {controlName}",
            data,
            "Enter Avalonia geometry mini-language, for example M 10,70 L 50,10 90,70 Z.");
        if (updated is not null)
        {
            Vm.SetSelectedPathData(updated);
        }
    }

    private async void OnEditExpanderContentMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedExpanderContent(out var controlName, out var content))
        {
            return;
        }

        var updatedContent = await ShowTextEditorDialogAsync(
            $"Edit Content - {controlName}",
            content,
            "Enter the text shown inside the selected Expander, ScrollViewer, or Border.");
        if (updatedContent is not null)
        {
            Vm.SetSelectedExpanderContent(updatedContent);
        }
    }

    private async void OnEditToolTipMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedToolTip(out var controlName, out var toolTip))
        {
            return;
        }

        var updatedToolTip = await ShowTextEditorDialogAsync(
            $"Edit Tooltip - {controlName}",
            toolTip,
            "Enter a short hint shown when the pointer rests over this control.");
        if (updatedToolTip is not null)
        {
            Vm.SetSelectedToolTip(updatedToolTip);
        }
    }

    private async void OnEditButtonClickHandlerMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedButtonClickHandler(out var buttonName, out var handlerName))
        {
            return;
        }

        var updatedHandler = await ShowTextEditorDialogAsync(
            $"Edit Click Handler - {buttonName}",
            handlerName,
            "Enter a handler method name, for example SaveButton_Click. Leave blank to remove the handler.");
        if (updatedHandler is not null)
        {
            Vm.SetSelectedButtonClickHandler(updatedHandler);
        }
    }

    private async void OnEditEventHandlerMapMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await EditEventHandlerMapAsync();

    private async Task EditEventHandlerMapAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        var updatedMap = await ShowTextEditorDialogAsync(
            "Edit Event Handler Map",
            Vm.GetEventHandlerMapText(),
            "Use ControlName | EventName | HandlerName. Common events include PointerPressed, PointerReleased, GotFocus, LostFocus, KeyDown, KeyUp, Tapped, and TextChanged. Locked controls are preserved.");
        if (updatedMap is not null)
        {
            Vm.SetEventHandlerMapFromText(updatedMap);
        }
    }

    private async void OnEditAccessibleNameMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedAutomationName(out var controlName, out var automationName))
        {
            return;
        }

        var updatedName = await ShowTextEditorDialogAsync(
            $"Edit Accessible Name - {controlName}",
            automationName,
            "Enter the label announced by screen readers and used by UI automation.");
        if (updatedName is not null)
        {
            Vm.SetSelectedAutomationName(updatedName);
        }
    }

    private void OnToggleEnabledMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.ToggleSelectedEnabledState();
    }

    private void OnToggleVisibilityMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.ToggleSelectedVisibility();
    }

    private void OnToggleTextBoxMultilineMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.ToggleSelectedTextBoxMultiline();
    }

    private async void OnEditLabelTargetMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedLabelTarget(out var labelName, out var targetName))
        {
            return;
        }

        var updatedTargetName = await ShowTextEditorDialogAsync(
            $"Edit Label Target - {labelName}",
            targetName,
            "Enter another control's name, or leave blank to clear the focus association.");
        if (updatedTargetName is not null)
        {
            Vm.SetSelectedLabelTarget(updatedTargetName);
        }
    }

    private async void OnEditTabOrderMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null || !Vm.TryGetSelectedTabIndex(out var controlName, out var tabIndex))
        {
            return;
        }

        var updatedTabIndex = await ShowTextEditorDialogAsync(
            $"Edit Tab Order - {controlName}",
            tabIndex == int.MaxValue
                ? "auto"
                : tabIndex.ToString(CultureInfo.InvariantCulture),
            "Enter 0-10000, auto, or -1. Lower values receive keyboard focus first.");
        if (updatedTabIndex is null)
        {
            return;
        }

        if (!MainWindowViewModel.TryParseTabOrderIndex(updatedTabIndex, out var parsedTabIndex))
        {
            Vm.StatusText = "Tab order must be auto, -1, or between 0 and 10000.";
            return;
        }

        Vm.SetSelectedTabIndex(parsedTabIndex == int.MaxValue ? -1 : parsedTabIndex);
    }

    private async void OnEditTabOrderMapMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        var updated = await ShowTextEditorDialogAsync(
            "Edit Tab Order Map",
            Vm.GetTabOrderEditorText(),
            "Enter one control per line using TabIndex | ControlName. Use -1 for automatic order; duplicate non-negative indexes are rejected.");
        if (updated is not null)
        {
            Vm.SetTabOrderFromText(updated);
        }
    }

    private void OnToggleTabStopMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.ToggleSelectedTabStop();
    }

    private void OnMoveSelectedElementEarlierMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.MoveSelectedElementInParentOrder(-1);
    }

    private void OnMoveSelectedElementLaterMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.MoveSelectedElementInParentOrder(1);
    }

    private void OnBringToFrontMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction.BringToFront);

    private void OnSendToBackMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction.SendToBack);

    private void OnBringForwardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction.BringForward);

    private void OnSendBackwardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction.SendBackward);

    private void MoveSelectedElementsInLayerOrder(MainWindowViewModel.LayerOrderAction action)
    {
        FlushPendingPropertyHistory();
        Vm?.MoveSelectedElementsInLayerOrder(action);
    }

    private void OnPreviewMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => OpenPreviewWindow(refreshDocument: true);

    private void OnPreviewThemeDefaultMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => SetPreviewTheme(PreviewThemeMode.Default);

    private void OnPreviewThemeLightMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => SetPreviewTheme(PreviewThemeMode.Light);

    private void OnPreviewThemeDarkMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => SetPreviewTheme(PreviewThemeMode.Dark);

    private void OnPreviewInteractionLogMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => OpenPreviewWindow(refreshDocument: false);

    private void SetPreviewTheme(PreviewThemeMode mode)
    {
        _previewThemeMode = mode;
        UpdatePreviewThemeMenuStates();
        _previewWindow?.SetThemeVariant(GetPreviewThemeVariant(mode));
        if (Vm is not null)
        {
            Vm.StatusText = $"Live preview theme: {GetPreviewThemeLabel(mode)}.";
        }
    }

    private static ThemeVariant GetPreviewThemeVariant(PreviewThemeMode mode)
        => mode switch
        {
            PreviewThemeMode.Light => ThemeVariant.Light,
            PreviewThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

    private static string GetPreviewThemeLabel(PreviewThemeMode mode)
        => mode switch
        {
            PreviewThemeMode.Light => "Light",
            PreviewThemeMode.Dark => "Dark",
            _ => "System Default",
        };

    private void OpenPreviewWindow(bool refreshDocument)
    {
        if (Vm is null)
        {
            return;
        }

        FlushPendingPropertyHistory();
        if (_previewWindow is not null)
        {
            if (refreshDocument)
            {
                _previewWindow.RefreshDocument(Vm.CreatePreviewDocument());
            }

            _previewWindow.Activate();
            Vm.StatusText = refreshDocument
                ? "Refreshed live preview."
                : "Activated live preview Interaction Log.";
            return;
        }

        var preview = new PreviewWindow(
            Vm.CreatePreviewDocument(),
            GetPreviewThemeVariant(_previewThemeMode));
        _previewWindow = preview;
        preview.Closed += (_, _) =>
        {
            if (ReferenceEquals(_previewWindow, preview))
            {
                _previewWindow = null;
            }
        };
        preview.Show(this);
        Vm.StatusText = refreshDocument
            ? "Opened live preview. Changes update automatically."
            : "Opened live preview with Interaction Log.";
    }

    private void OnSelectionParentButtonClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.SelectParentOfSelectedElement();
    }

    private void OnSelectionChildButtonClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.SelectChildOfSelectedElement();
    }

    private void OnSelectionBreadcrumbSegmentClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null || (sender as Button)?.Tag is not DesignElement element)
        {
            return;
        }

        FlushPendingPropertyHistory();
        Vm.SelectElement(element);
    }

    private async void OnEditAxamlSourceMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        await ShowAxamlSourceEditorDialogAsync(Vm.ExportFullAxaml());
    }

    private async void OnEditRootPropertiesMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        await ShowRootPropertiesDialogAsync(Vm.GetRootEditorState());
    }

    private async void OnEditSampleDataMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        await ShowSampleDataEditorDialogAsync(Vm.GetSampleDataEditorText());
    }

    private void OnZoomInMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ZoomViewportAtCenter(1);
    }

    private void OnZoomOutMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ZoomViewportAtCenter(-1);
    }

    private void OnResetZoomMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetZoomViewportAtCenter(1);
    }

    private void OnFitToViewMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Vm?.Canvas.FitToViewport(DesignViewport.Bounds.Width, DesignViewport.Bounds.Height);
        UpdateZoomStatus();
    }

    private void OnFitSelectedToViewMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FitSelectedElementsToViewport();
    }

    private void FitSelectedElementsToViewport()
    {
        if (Vm is null || !TryGetSelectionBounds(out var bounds))
        {
            Vm?.StatusText = "Select at least one control to fit it to view.";
            return;
        }

        var canvas = Vm.Canvas;
        var viewport = DesignScrollViewer.Viewport;
        var viewportWidth = viewport.Width > 0 ? viewport.Width : DesignViewport.Bounds.Width;
        var viewportHeight = viewport.Height > 0 ? viewport.Height : DesignViewport.Bounds.Height;
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return;
        }

        const double padding = 48;
        var availableWidth = Math.Max(1, viewportWidth - (padding * 2));
        var availableHeight = Math.Max(1, viewportHeight - (padding * 2));
        var selectionWidth = Math.Max(MinSize, bounds.Width);
        var selectionHeight = Math.Max(MinSize, bounds.Height);
        var zoom = Math.Clamp(
            Math.Min(availableWidth / selectionWidth, availableHeight / selectionHeight),
            0.25,
            2);
        canvas.SetZoomScale(zoom);
        UpdateZoomStatus();

        void CenterSelection()
        {
            var currentViewport = DesignScrollViewer.Viewport;
            var currentWidth = currentViewport.Width > 0 ? currentViewport.Width : viewportWidth;
            var currentHeight = currentViewport.Height > 0 ? currentViewport.Height : viewportHeight;
            var centerX = (bounds.X + (bounds.Width / 2)) * canvas.ZoomScale;
            var centerY = (bounds.Y + (bounds.Height / 2)) * canvas.ZoomScale;
            var maxX = Math.Max(0, DesignScrollViewer.Extent.Width - currentWidth);
            var maxY = Math.Max(0, DesignScrollViewer.Extent.Height - currentHeight);
            DesignScrollViewer.Offset = new Vector(
                Math.Clamp(centerX - (currentWidth / 2), 0, maxX),
                Math.Clamp(centerY - (currentHeight / 2), 0, maxY));
        }

        CenterSelection();
        EventHandler? layoutUpdated = null;
        layoutUpdated = (_, _) =>
        {
            DesignScrollViewer.LayoutUpdated -= layoutUpdated;
            CenterSelection();
        };
        DesignScrollViewer.LayoutUpdated += layoutUpdated;
        Dispatcher.UIThread.Post(CenterSelection, DispatcherPriority.Normal);
        Vm.StatusText = $"Fitted selected controls to view at {zoom * 100:0}% zoom.";
    }

    private void OnZoomPresetMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null
            || sender is not MenuItem { Tag: string rawScale }
            || !double.TryParse(rawScale, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale)
            || !double.IsFinite(scale)
            || scale < 0.25
            || scale > 2)
        {
            return;
        }

        SetZoomViewportAtCenter(scale);
    }

    private async void OnCustomZoomMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var zoomScale = await ShowZoomScaleDialogAsync(Vm.Canvas.ZoomScale);
        if (zoomScale is double value)
        {
            SetZoomViewportAtCenter(value);
        }
    }

    private static bool TryParseZoomPercentage(string? text, out double zoomScale)
    {
        zoomScale = 0;
        if (!double.TryParse(
                text?.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var percentage)
            || !double.IsFinite(percentage)
            || percentage < 25
            || percentage > 200)
        {
            return false;
        }

        zoomScale = percentage / 100;
        return true;
    }

    private async Task<double?> ShowZoomScaleDialogAsync(double currentZoomScale)
    {
        var dialog = new Window
        {
            Title = "Custom Zoom",
            Width = 420,
            Height = 230,
            MinWidth = 360,
            MinHeight = 210,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var editor = new TextBox
        {
            Text = (currentZoomScale * 100).ToString("0.##", CultureInfo.InvariantCulture),
            Watermark = "Zoom percentage (25-200)",
            MinWidth = 260,
        };
        var errorText = new TextBlock
        {
            Foreground = Brush.Parse("#B91C1C"),
            TextWrapping = TextWrapping.Wrap,
        };

        void ApplyZoom()
        {
            if (!TryParseZoomPercentage(editor.Text, out var zoomScale))
            {
                errorText.Text = "Enter a zoom percentage from 25 to 200.";
                return;
            }

            dialog.Close(zoomScale);
        }

        var cancelButton = new Button { Content = "Cancel", MinWidth = 86 };
        var applyButton = new Button { Content = "Apply", MinWidth = 86 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        applyButton.Click += (_, _) => ApplyZoom();
        editor.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                ApplyZoom();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close(null);
                e.Handled = true;
            }
        };
        dialog.Opened += (_, _) =>
        {
            editor.Focus();
            editor.SelectAll();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "Choose a custom canvas zoom percentage." },
                editor,
                errorText,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancelButton, applyButton },
                },
            },
        };

        return await dialog.ShowDialog<double?>(this);
    }

    private void OnGridSize4MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.SetCanvasGridSize(4);

    private void OnGridSize8MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.SetCanvasGridSize(8);

    private void OnGridSize16MenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.SetCanvasGridSize(16);

    private async void OnCustomGridSizeMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var editor = new TextBox
        {
            Text = Vm.Canvas.GridSize.ToString("0.###", CultureInfo.InvariantCulture),
            Watermark = "4-32",
            Width = 160,
        };
        var previewText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
        };
        var applyButton = new Button
        {
            Content = "Apply",
            MinWidth = 84,
        };
        var cancelButton = new Button
        {
            Content = "Cancel",
            MinWidth = 84,
        };
        var dialog = new Window
        {
            Title = "Custom Grid Size",
            Width = 400,
            Height = 240,
            MinWidth = 340,
            MinHeight = 210,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void RefreshPreview()
        {
            if (TryParseCustomGridSize(editor.Text, out var gridSize, out var error))
            {
                applyButton.IsEnabled = true;
                previewText.Foreground = Brushes.SeaGreen;
                previewText.Text = $"Preview: {gridSize:0.###} px grid spacing";
            }
            else
            {
                applyButton.IsEnabled = false;
                previewText.Foreground = Brushes.IndianRed;
                previewText.Text = error;
            }
        }

        editor.TextChanged += (_, _) => RefreshPreview();
        applyButton.Click += (_, _) =>
        {
            if (!TryParseCustomGridSize(editor.Text, out var gridSize, out _))
            {
                RefreshPreview();
                return;
            }

            Vm.SetCanvasGridSize(gridSize);
            dialog.Close();
        };
        cancelButton.Click += (_, _) => dialog.Close();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Enter the design grid spacing in pixels." },
                editor,
                previewText,
                new TextBlock
                {
                    Text = "Allowed range: 4-32 px",
                    Foreground = Brushes.Gray,
                },
                buttons,
            },
        };
        RefreshPreview();
        await dialog.ShowDialog(this);
    }

    private void OnShowGridMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.SetCanvasGridVisibility(!Vm.Canvas.IsGridVisible);

    private void OnSnapToGridMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Vm?.SetCanvasSnapToGrid(!Vm.Canvas.SnapToGrid);

    private void OnShowDesignGuidesMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _showDesignGuides = !_showDesignGuides;
        SyncDesignGuideToggles();
        GuideOverlay.IsVisible = _showDesignGuides;
    }

    private void OnSnapToGuidesMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _snapToGuides = !_snapToGuides;
        SyncDesignGuideToggles();
    }

    private void SyncDesignGuideToggles()
    {
        ShowDesignGuidesMenu.IsChecked = _showDesignGuides;
        SnapToGuidesMenu.IsChecked = _snapToGuides;
        ShowDesignGuidesToolbar.IsChecked = _showDesignGuides;
        SnapToGuidesToolbar.IsChecked = _snapToGuides;
    }

    private void OnClearDesignGuidesMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ClearDesignGuides();
        if (Vm is not null)
        {
            Vm.StatusText = "Design guides cleared.";
        }
    }

    private void OnDesktopArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboard(1280, 800);

    private void OnTabletArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboard(1024, 768);

    private void OnMobileArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboard(390, 844);

    private async void OnCustomArtboardSizeMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var widthEditor = new TextBox
        {
            Text = Vm.Canvas.ArtboardWidth.ToString("0", CultureInfo.InvariantCulture),
            Watermark = "320-3840",
            Width = 128,
        };
        var heightEditor = new TextBox
        {
            Text = Vm.Canvas.ArtboardHeight.ToString("0", CultureInfo.InvariantCulture),
            Watermark = "240-2160",
            Width = 128,
        };
        var previewText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
        };
        var applyButton = new Button
        {
            Content = "Apply",
            MinWidth = 84,
        };
        var cancelButton = new Button
        {
            Content = "Cancel",
            MinWidth = 84,
        };
        var dialog = new Window
        {
            Title = "Custom Artboard Size",
            Width = 420,
            Height = 280,
            MinWidth = 360,
            MinHeight = 240,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void RefreshPreview()
        {
            if (TryParseCustomArtboardSize(
                    widthEditor.Text,
                    heightEditor.Text,
                    out var width,
                    out var height,
                    out var error))
            {
                applyButton.IsEnabled = true;
                previewText.Foreground = Brushes.SeaGreen;
                previewText.Text = $"Preview: {width:0} x {height:0} px";
            }
            else
            {
                applyButton.IsEnabled = false;
                previewText.Foreground = Brushes.IndianRed;
                previewText.Text = error;
            }
        }

        widthEditor.TextChanged += (_, _) => RefreshPreview();
        heightEditor.TextChanged += (_, _) => RefreshPreview();
        applyButton.Click += (_, _) =>
        {
            if (!TryParseCustomArtboardSize(
                    widthEditor.Text,
                    heightEditor.Text,
                    out var width,
                    out var height,
                    out _))
            {
                RefreshPreview();
                return;
            }

            SetArtboard(width, height);
            dialog.Close();
        };
        cancelButton.Click += (_, _) => dialog.Close();

        var dimensions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Width", VerticalAlignment = VerticalAlignment.Center },
                widthEditor,
                new TextBlock { Text = "Height", VerticalAlignment = VerticalAlignment.Center },
                heightEditor,
            },
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Enter the design artboard dimensions in pixels." },
                dimensions,
                previewText,
                new TextBlock
                {
                    Text = "Width: 320-3840 px | Height: 240-2160 px",
                    Foreground = Brushes.Gray,
                },
                buttons,
            },
        };
        RefreshPreview();
        await dialog.ShowDialog(this);
    }

    private async void OnKeyboardShortcutsMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await ShowHelpDialogAsync(
            "Keyboard Shortcuts",
            "Keyboard shortcuts",
            KeyboardShortcutsHelpText);

    private async void OnAboutMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => await ShowHelpDialogAsync(
            "About AvaloniaUIDesigner",
            "About",
            AboutHelpText);

    private async Task ShowHelpDialogAsync(string title, string heading, string body)
    {
        var closeButton = new Button
        {
            Content = "Close",
            MinWidth = 84,
        };
        var dialog = new Window
        {
            Title = title,
            Width = 520,
            Height = 460,
            MinWidth = 380,
            MinHeight = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        closeButton.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(18),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = heading,
                    FontSize = 18,
                    FontWeight = FontWeight.SemiBold,
                },
                new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new TextBlock
                    {
                        Text = body,
                        TextWrapping = TextWrapping.NoWrap,
                        FontFamily = new FontFamily("Consolas"),
                    },
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { closeButton },
                },
            },
        };
        await dialog.ShowDialog(this);
    }

    private void OnRotateArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        if (Math.Abs(Vm.Canvas.ArtboardWidth - Vm.Canvas.ArtboardHeight) < double.Epsilon)
        {
            Vm.StatusText = "The square artboard orientation is unchanged.";
            return;
        }

        SetArtboard(Vm.Canvas.ArtboardHeight, Vm.Canvas.ArtboardWidth);
        Vm.StatusText = $"Rotated artboard: {Vm.Canvas.ArtboardWidth:0} x {Vm.Canvas.ArtboardHeight:0}";
    }

    private void OnWhiteArtboardBackgroundMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboardBackground("#FFFFFF");

    private void OnGrayArtboardBackgroundMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboardBackground("#F1F5F9");

    private void OnInkArtboardBackgroundMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetArtboardBackground("#1E293B");

    private async void OnCustomArtboardBackgroundMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var editor = new TextBox
        {
            Text = Vm.Canvas.ArtboardBackground,
            Watermark = "#RRGGBB or #AARRGGBB",
        };
        var preview = new Border
        {
            Height = 44,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
        };
        var errorText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = "Custom Artboard Background",
            Width = 420,
            Height = 260,
            MinWidth = 360,
            MinHeight = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void RefreshPreview()
        {
            if (TryNormalizeArtboardColor(editor.Text, out var normalized, out var error))
            {
                preview.Background = Brush.Parse(normalized);
                errorText.Foreground = Brushes.SeaGreen;
                errorText.Text = $"Preview: {normalized}";
            }
            else
            {
                preview.Background = Brushes.Transparent;
                errorText.Foreground = Brushes.IndianRed;
                errorText.Text = error;
            }
        }

        editor.TextChanged += (_, _) => RefreshPreview();
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!TryNormalizeArtboardColor(editor.Text, out var normalized, out var error))
            {
                errorText.Foreground = Brushes.IndianRed;
                errorText.Text = error;
                return;
            }

            SetArtboardBackground(normalized);
            dialog.Close();
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "Enter a solid ARGB color for the design artboard." },
                editor,
                preview,
                errorText,
                buttons,
            },
        };
        RefreshPreview();
        await dialog.ShowDialog(this);
    }

    private void SetArtboard(double width, double height)
    {
        if (Vm is null)
        {
            return;
        }

        var preserveViewport = TryGetViewportDocumentPointAtCenter(
            out var documentPoint,
            out var viewportPoint);
        Vm.BeginCanvasMutation(MainWindowViewModel.HistoryActionType.TransformElement, "Updated artboard size.");
        Vm.Canvas.SetArtboard(width, height);
        Vm.CommitCanvasMutation();
        if (preserveViewport)
        {
            RestoreViewportDocumentPoint(documentPoint, viewportPoint);
        }

        Vm.StatusText = $"Artboard: {Vm.Canvas.ArtboardWidth:0} x {Vm.Canvas.ArtboardHeight:0}";
    }

    private void SetArtboardBackground(string background)
    {
        if (Vm is null)
        {
            return;
        }

        Vm.BeginCanvasMutation(MainWindowViewModel.HistoryActionType.TransformElement, "Updated artboard background.");
        Vm.Canvas.SetArtboard(Vm.Canvas.ArtboardWidth, Vm.Canvas.ArtboardHeight, background);
        Vm.CommitCanvasMutation();
        Vm.StatusText = "Updated artboard background.";
    }

    private static bool TryNormalizeArtboardColor(
        string? value,
        out string normalized,
        out string error)
    {
        normalized = string.Empty;
        error = "Enter a color as #RRGGBB or #AARRGGBB.";
        var text = value?.Trim() ?? string.Empty;
        if (text.Length is not (7 or 9) || !text.StartsWith('#'))
        {
            return false;
        }

        try
        {
            var color = Color.Parse(text);
            normalized = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            error = string.Empty;
            return true;
        }
        catch (FormatException)
        {
            error = "The artboard color contains invalid hexadecimal digits.";
            return false;
        }
    }

    private static bool TryParseCustomArtboardSize(
        string? widthText,
        string? heightText,
        out double width,
        out double height,
        out string error)
    {
        width = 0;
        height = 0;
        error = "Enter whole-number artboard dimensions.";

        if (!int.TryParse(
                widthText?.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsedWidth)
            || !int.TryParse(
                heightText?.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsedHeight))
        {
            return false;
        }

        if (parsedWidth is < 320 or > 3840)
        {
            error = "Width must be between 320 and 3840 px.";
            return false;
        }

        if (parsedHeight is < 240 or > 2160)
        {
            error = "Height must be between 240 and 2160 px.";
            return false;
        }

        width = parsedWidth;
        height = parsedHeight;
        error = string.Empty;
        return true;
    }

    private static bool TryParseCustomGridSize(
        string? value,
        out double gridSize,
        out string error)
    {
        gridSize = 0;
        error = "Enter a finite grid size.";
        if (!double.TryParse(
                value?.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed)
            || !double.IsFinite(parsed))
        {
            return false;
        }

        if (parsed < 4 || parsed > 32)
        {
            error = "Grid size must be between 4 and 32 px.";
            return false;
        }

        gridSize = parsed;
        error = string.Empty;
        return true;
    }

    private void UpdateZoomStatus()
    {
        if (Vm is not null)
        {
            Vm.StatusText = $"Zoom: {Vm.Canvas.ZoomPercentage}";
        }
    }

    private void OnAlignLeftMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignLeft);

    private void OnAlignCenterMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignCenter);

    private void OnAlignRightMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignRight);

    private void OnAlignTopMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignTop);

    private void OnAlignMiddleMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignMiddle);

    private void OnAlignBottomMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.AlignBottom);

    private void OnDistributeHorizontallyMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.DistributeHorizontally);

    private void OnDistributeVerticallyMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.DistributeVertically);

    private void OnMakeSameWidthMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.MakeSameWidth);

    private void OnMakeSameHeightMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.MakeSameHeight);

    private void OnMakeSameSizeMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction.MakeSameSize);

    private void OnCenterHorizontallyOnArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => CenterSelectedElementsOnArtboard(horizontally: true, vertically: false);

    private void OnCenterVerticallyOnArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => CenterSelectedElementsOnArtboard(horizontally: false, vertically: true);

    private void OnCenterOnArtboardMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => CenterSelectedElementsOnArtboard(horizontally: true, vertically: true);

    private void OnAlignLeftToArtboardMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => AlignSelectedElementsToArtboard(MainWindowViewModel.ArtboardAlignment.Left);

    private void OnAlignRightToArtboardMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => AlignSelectedElementsToArtboard(MainWindowViewModel.ArtboardAlignment.Right);

    private void OnAlignTopToArtboardMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => AlignSelectedElementsToArtboard(MainWindowViewModel.ArtboardAlignment.Top);

    private void OnAlignBottomToArtboardMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => AlignSelectedElementsToArtboard(MainWindowViewModel.ArtboardAlignment.Bottom);

    private void OnLayoutSelectedHorizontallyMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => LayoutSelectedControls(Orientation.Horizontal);

    private void OnLayoutSelectedVerticallyMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
        => LayoutSelectedControls(Orientation.Vertical);

    private void OnLayoutSelectedGridMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.LayoutSelectedIntoGrid();
    }

    private void OnLayoutSelectedUniformGridMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.LayoutSelectedIntoUniformGrid();
    }

    private void OnLayoutSelectedDockPanelHorizontallyMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.LayoutSelectedIntoDockPanel(Orientation.Horizontal);
    }

    private void OnLayoutSelectedDockPanelVerticallyMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.LayoutSelectedIntoDockPanel(Orientation.Vertical);
    }

    private void OnLayoutSelectedWrapPanelHorizontallyMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.LayoutSelectedIntoWrapPanel(Orientation.Horizontal);
    }

    private void OnLayoutSelectedWrapPanelVerticallyMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.LayoutSelectedIntoWrapPanel(Orientation.Vertical);
    }

    private void LayoutSelectedControls(Orientation orientation)
    {
        FlushPendingPropertyHistory();
        Vm?.LayoutSelectedIntoStackPanel(orientation);
    }

    private bool ApplyKeyboardLayoutShortcut(Key key)
    {
        switch (key)
        {
            case Key.D1:
            case Key.NumPad1:
                LayoutSelectedControls(Orientation.Horizontal);
                return true;
            case Key.D2:
            case Key.NumPad2:
                LayoutSelectedControls(Orientation.Vertical);
                return true;
            case Key.D3:
            case Key.NumPad3:
                FlushPendingPropertyHistory();
                Vm?.LayoutSelectedIntoGrid();
                return true;
            case Key.D4:
            case Key.NumPad4:
                FlushPendingPropertyHistory();
                Vm?.LayoutSelectedIntoUniformGrid();
                return true;
            case Key.D5:
            case Key.NumPad5:
                FlushPendingPropertyHistory();
                Vm?.LayoutSelectedIntoDockPanel(Orientation.Horizontal);
                return true;
            case Key.D6:
            case Key.NumPad6:
                FlushPendingPropertyHistory();
                Vm?.LayoutSelectedIntoDockPanel(Orientation.Vertical);
                return true;
            case Key.D7:
            case Key.NumPad7:
                FlushPendingPropertyHistory();
                Vm?.LayoutSelectedIntoWrapPanel(Orientation.Horizontal);
                return true;
            case Key.D8:
            case Key.NumPad8:
                FlushPendingPropertyHistory();
                Vm?.LayoutSelectedIntoWrapPanel(Orientation.Vertical);
                return true;
            default:
                return false;
        }
    }

    private void ArrangeSelectedElements(MainWindowViewModel.SelectionLayoutAction action)
    {
        FlushPendingPropertyHistory();
        Vm?.ArrangeSelectedElements(action);
    }

    private void OnGroupSelectedMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.GroupSelectedElements();
    }

    private void OnUngroupSelectedMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.UngroupSelectedCanvas();
    }

    private void OnBreakSelectedLayoutMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        FlushPendingPropertyHistory();
        Vm?.BreakSelectedLayout();
    }

    private void CenterSelectedElementsOnArtboard(bool horizontally, bool vertically)
    {
        FlushPendingPropertyHistory();
        Vm?.CenterSelectedElementsOnArtboard(horizontally, vertically);
    }

    private void AlignSelectedElementsToArtboard(MainWindowViewModel.ArtboardAlignment alignment)
    {
        FlushPendingPropertyHistory();
        Vm?.AlignSelectedElementsToArtboard(alignment);
    }

    private void OnObjectTreeSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            ObjectTreeSearch.Text = string.Empty;
            ObjectTreeView.Focus();
            Vm.StatusText = "Object Tree search cleared.";
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            var reverse = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            if (!Vm.ObjectTree.SelectNextMatch(reverse))
            {
                Vm.StatusText = "No matching controls.";
            }

            e.Handled = true;
        }
    }

    private async void OnObjectTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        if (shift
            && !alt
            && e.Key is (Key.Up or Key.Down)
            && (_objectTreeSelectionAnchor ?? Vm.Canvas.SelectedElement) is { } anchor)
        {
            _isObjectTreeSelectionGesture = true;
            bool rangeSelected;
            try
            {
                rangeSelected = Vm.TrySelectNextObjectTreeRange(
                    anchor,
                    reverse: e.Key == Key.Up,
                    append: ctrl);
            }
            finally
            {
                _isObjectTreeSelectionGesture = false;
            }

            if (rangeSelected)
            {
                _objectTreeSelectionAnchor = anchor;
                e.Handled = true;
                return;
            }
        }

        if (shift
            && !alt
            && e.Key is (Key.Home or Key.End)
            && (_objectTreeSelectionAnchor ?? Vm.Canvas.SelectedElement) is { } boundaryAnchor)
        {
            _isObjectTreeSelectionGesture = true;
            bool rangeSelected;
            try
            {
                rangeSelected = Vm.TrySelectObjectTreeBoundaryRange(
                    boundaryAnchor,
                    last: e.Key == Key.End,
                    append: ctrl);
            }
            finally
            {
                _isObjectTreeSelectionGesture = false;
            }

            if (rangeSelected)
            {
                _objectTreeSelectionAnchor = boundaryAnchor;
                e.Handled = true;
                return;
            }
        }

        if (!ctrl && e.Key == Key.F2)
        {
            await RenameSelectedControlAsync();
            e.Handled = true;
            return;
        }

        if (!ctrl && e.Key is Key.Delete or Key.Back)
        {
            FlushPendingPropertyHistory();
            Vm.RemoveSelectedElement();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.L)
        {
            FlushPendingPropertyHistory();
            Vm.ToggleSelectedLock();
            e.Handled = true;
        }
    }

    private void OnObjectTreeNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null
            || sender is not Control { DataContext: ObjectNodeViewModel node }
            || node.Element is not { } element)
        {
            return;
        }

        _canvasSelectionAnchor = null;
        var point = e.GetCurrentPoint((Control)sender);
        if (!point.Properties.IsLeftButtonPressed)
        {
            if (point.Properties.IsRightButtonPressed)
            {
                _isObjectTreeSelectionGesture = true;
                try
                {
                    Vm.SelectElement(element);
                }
                finally
                {
                    _isObjectTreeSelectionGesture = false;
                }

                _objectTreeSelectionAnchor = element;
            }

            return;
        }

        var toggleSelection = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var rangeSelection = false;
        var anchor = _objectTreeSelectionAnchor ?? Vm.Canvas.SelectedElement;
        _isObjectTreeSelectionGesture = true;
        try
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                && anchor is not null)
            {
                rangeSelection = Vm.TrySelectObjectTreeRange(anchor, element, toggleSelection);
            }

            if (!rangeSelection)
            {
                Vm.SelectElement(element, toggleSelection);
            }
        }
        finally
        {
            _isObjectTreeSelectionGesture = false;
        }

        _objectTreeSelectionAnchor = rangeSelection && anchor is not null
            ? anchor
            : element;
        if (!element.IsLocked && !toggleSelection && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _pendingObjectTreeDragElement = element;
            _objectTreeDragStart = e.GetPosition(this);
            e.Pointer.Capture((Control)sender);
        }

        e.Handled = true;
    }

    private async void OnObjectTreeNodePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pendingObjectTreeDragElement is not { } element)
        {
            return;
        }

        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _objectTreeDragStart.X) < MarqueeThreshold
            && Math.Abs(point.Y - _objectTreeDragStart.Y) < MarqueeThreshold)
        {
            return;
        }

        _pendingObjectTreeDragElement = null;
        e.Pointer.Capture(null);
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(ObjectTreeDragDataFormat, element.DisplayName));
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
        Vm?.ObjectTree.ClearDropFeedback();
        e.Handled = true;
    }

    private void OnObjectTreeNodePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pendingObjectTreeDragElement = null;
        e.Pointer.Capture(null);
        Vm?.ObjectTree.ClearDropFeedback();
    }

    private void OnObjectTreeDragOver(object? sender, DragEventArgs e)
    {
        var targetNode = FindObjectTreeNode(e.Source);
        if (e.DataTransfer.TryGetValue(ObjectTreeDragDataFormat) is not string)
        {
            Vm?.ObjectTree.ClearDropFeedback();
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var accepted = TryGetObjectTreeDropElements(e, out var source, out var target);
        var siblingDrop = accepted && IsObjectTreeSiblingDrop(source, target);
        var insertAfter = siblingDrop && IsObjectTreeDropAfter(e);
        Vm?.ObjectTree.SetDropFeedback(
            targetNode,
            accepted,
            insertBefore: siblingDrop && !insertAfter,
            insertAfter: insertAfter);
        e.DragEffects = accepted
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnObjectTreeDragLeave(object? sender, DragEventArgs e)
    {
        Vm?.ObjectTree.ClearDropFeedback();
    }

    private void OnObjectTreeDrop(object? sender, DragEventArgs e)
    {
        Vm?.ObjectTree.ClearDropFeedback();
        if (Vm is null || !TryGetObjectTreeDropElements(e, out var source, out var target))
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var siblingDrop = IsObjectTreeSiblingDrop(source, target);
        var insertAfter = siblingDrop && IsObjectTreeDropAfter(e);
        Vm.SelectElement(source);
        var changed = siblingDrop
            ? insertAfter
                ? Vm.ReorderSelectedElementAfter(target)
                : Vm.ReorderSelectedElementBefore(target)
            : Vm.TryReparentSelectedElementTo(target);
        e.DragEffects = changed
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private bool TryGetObjectTreeDropElements(
        DragEventArgs e,
        out DesignElement source,
        out DesignElement target)
    {
        source = null!;
        target = null!;
        if (Vm is null
            || e.DataTransfer.TryGetValue(ObjectTreeDragDataFormat) is not string sourceName
            || FindObjectTreeNode(e.Source)?.Element is not { } targetElement)
        {
            return false;
        }

        var sourceElement = Vm.Canvas.Elements.FirstOrDefault(element =>
            string.Equals(element.DisplayName, sourceName, StringComparison.OrdinalIgnoreCase));
        if (sourceElement is null || ReferenceEquals(sourceElement, targetElement))
        {
            return false;
        }

        if (!IsObjectTreeSiblingDrop(sourceElement, targetElement)
            && !Vm.CanReparentElementTo(sourceElement, targetElement))
        {
            return false;
        }

        source = sourceElement;
        target = targetElement;
        return true;
    }

    private static bool IsObjectTreeSiblingDrop(DesignElement source, DesignElement target)
        => source.IsContainerChild
            && target.IsContainerChild
            && source.ParentLayout == target.ParentLayout
            && source.ParentLayout is (DesignerParentLayoutKind.StackPanel
                or DesignerParentLayoutKind.DockPanel
                or DesignerParentLayoutKind.WrapPanel
                or DesignerParentLayoutKind.UniformGrid
                or DesignerParentLayoutKind.Canvas)
            && string.Equals(source.ParentName, target.ParentName, StringComparison.OrdinalIgnoreCase);

    private static bool IsObjectTreeDropAfter(DragEventArgs e)
    {
        var targetControl = FindObjectTreeNodeControl(e.Source);
        return targetControl is not null
            && targetControl.Bounds.Height > 0
            && e.GetPosition(targetControl).Y >= targetControl.Bounds.Height / 2;
    }

    private static ObjectNodeViewModel? FindObjectTreeNode(object? source)
        => FindObjectTreeNodeControl(source)?.DataContext as ObjectNodeViewModel;

    private bool IsObjectTreeSource(object? source)
    {
        for (var current = source as Visual; current is not null; current = current.GetVisualParent())
        {
            if (ReferenceEquals(current, ObjectTreeView))
            {
                return true;
            }
        }

        return false;
    }

    private static Control? FindObjectTreeNodeControl(object? source)
    {
        for (var current = source as Visual; current is not null; current = current.GetVisualParent())
        {
            if (current is Control { DataContext: ObjectNodeViewModel })
            {
                return (Control)current;
            }
        }

        return null;
    }

    private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        if (e.Key == Key.Space)
        {
            _isSpacePanModifier = true;
            return;
        }

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);

        if (ctrl && alt && !shift && e.Key == Key.T)
        {
            if (!ToolboxPane.IsVisible)
            {
                SetToolboxPaneVisible(true);
            }
            ToolboxSearch.Focus();
            ToolboxSearch.SelectAll();
            Vm.StatusText = "Toolbox focused. Search or press Enter to quick-place.";
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.P)
        {
            ToggleToolboxPlacementMode();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.I)
        {
            if (!PropertyInspectorPane.IsVisible)
            {
                SetPropertyInspectorPaneVisible(true);
            }

            PropertyInspectorFilter.Focus();
            PropertyInspectorFilter.SelectAll();
            Vm.StatusText = "Property Inspector filter focused.";
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift)
        {
            switch (e.Key)
            {
                case Key.D1:
                case Key.NumPad1:
                    SetToolboxPaneVisible(!ToolboxPane.IsVisible);
                    e.Handled = true;
                    return;
                case Key.D2:
                case Key.NumPad2:
                    SetObjectTreePaneVisible(!ObjectTreePane.IsVisible);
                    e.Handled = true;
                    return;
                case Key.D3:
                case Key.NumPad3:
                    SetPropertyInspectorPaneVisible(!PropertyInspectorPane.IsVisible);
                    e.Handled = true;
                    return;
                case Key.D0:
                case Key.NumPad0:
                    ResetWorkspacePanelLayout();
                    e.Handled = true;
                    return;
            }
        }

        // Keep text editing inside the PropertyGrid native to the focused editor.
        if (e.Source is TextBox)
        {
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.B)
        {
            await EditSelectionBoundsAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.M)
        {
            await EditCommonPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.L)
        {
            await EditLayoutPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.Y)
        {
            await EditTypographyPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.X)
        {
            await EditTransformPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.A)
        {
            await EditAccessibilityPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.E)
        {
            await EditInteractionPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.F)
        {
            await EditEffectPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.R)
        {
            await EditRangePropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.T)
        {
            await EditTextInputPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.B)
        {
            await EditSelectableTextBlockPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.M)
        {
            await EditMaskedTextBoxPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.Q)
        {
            await EditSelectionPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.D)
        {
            await EditDateTimePropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.P)
        {
            await EditColorPickerPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.A)
        {
            await EditAutoCompleteBoxPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.O)
        {
            await EditTogglePropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.U)
        {
            await EditContainerBehaviorPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.V)
        {
            await EditSplitViewPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.K)
        {
            await EditTabControlBehaviorPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.E)
        {
            await EditDataGridBehaviorPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.J)
        {
            await EditGridSplitterPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.N)
        {
            await EditButtonPropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.I)
        {
            await EditImagePropertiesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.L)
        {
            await EditEventHandlerMapAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.Z)
        {
            OpenPreviewWindow(refreshDocument: false);
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.F)
        {
            await ShowHistoryTimelineAsync();
            e.Handled = true;
            return;
        }

        if (!ctrl
            && !shift
            && !alt
            && e.Key == Key.H
            && ReferenceEquals(e.Source, DesignHost))
        {
            SetPanToolActive(!_isPanToolActive);
            e.Handled = true;
            return;
        }

        if (ctrl && !shift && !alt && e.Key == Key.K)
        {
            await QuickSwitchDocumentTabAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && !alt && (e.Key is Key.OemPlus or Key.Add))
        {
            ZoomViewportAtCenter(1);
            e.Handled = true;
            return;
        }

        if (ctrl && !alt && (e.Key is Key.OemMinus or Key.Subtract))
        {
            ZoomViewportAtCenter(-1);
            e.Handled = true;
            return;
        }

        if (!ctrl && !shift && !alt && e.Key == Key.F)
        {
            Vm.Canvas.FitToViewport(DesignViewport.Bounds.Width, DesignViewport.Bounds.Height);
            UpdateZoomStatus();
            e.Handled = true;
            return;
        }

        if (ctrl && shift && !alt && e.Key == Key.F)
        {
            FitSelectedElementsToViewport();
            e.Handled = true;
            return;
        }

        // TreeView owns hierarchy navigation; arrows must not nudge the canvas selection.
        if (IsObjectTreeSource(e.Source)
            && e.Key is (Key.Left or Key.Right or Key.Up or Key.Down))
        {
            return;
        }

        if (ctrl && alt && !shift && e.Key is (Key.Left or Key.Right or Key.Up or Key.Down))
        {
            PanViewportBy(e.Key switch
            {
                Key.Left => new Vector(-ViewportKeyboardPanStep, 0),
                Key.Right => new Vector(ViewportKeyboardPanStep, 0),
                Key.Up => new Vector(0, -ViewportKeyboardPanStep),
                _ => new Vector(0, ViewportKeyboardPanStep),
            });
            e.Handled = true;
            return;
        }

        var keyboardArrangeAction = e.Key switch
        {
            Key.Left => MainWindowViewModel.SelectionLayoutAction.AlignLeft,
            Key.E => MainWindowViewModel.SelectionLayoutAction.AlignCenter,
            Key.M => MainWindowViewModel.SelectionLayoutAction.AlignMiddle,
            Key.Right => MainWindowViewModel.SelectionLayoutAction.AlignRight,
            Key.Up => MainWindowViewModel.SelectionLayoutAction.AlignTop,
            Key.Down => MainWindowViewModel.SelectionLayoutAction.AlignBottom,
            _ => (MainWindowViewModel.SelectionLayoutAction?)null,
        };
        if (ctrl && shift && !alt && keyboardArrangeAction is { } arrangeAction)
        {
            ArrangeSelectedElements(arrangeAction);
            e.Handled = true;
            return;
        }

        var keyboardDistributeAction = e.Key switch
        {
            Key.H => MainWindowViewModel.SelectionLayoutAction.DistributeHorizontally,
            Key.V => MainWindowViewModel.SelectionLayoutAction.DistributeVertically,
            _ => (MainWindowViewModel.SelectionLayoutAction?)null,
        };
        if (ctrl && alt && !shift && keyboardDistributeAction is { } distributeAction)
        {
            ArrangeSelectedElements(distributeAction);
            e.Handled = true;
            return;
        }

        var keyboardSizeAction = e.Key switch
        {
            Key.W => MainWindowViewModel.SelectionLayoutAction.MakeSameWidth,
            Key.H => MainWindowViewModel.SelectionLayoutAction.MakeSameHeight,
            Key.S => MainWindowViewModel.SelectionLayoutAction.MakeSameSize,
            _ => (MainWindowViewModel.SelectionLayoutAction?)null,
        };
        if (ctrl && alt && shift && keyboardSizeAction is { } sizeAction)
        {
            ArrangeSelectedElements(sizeAction);
            e.Handled = true;
            return;
        }

        var keyboardLayerAction = e.Key switch
        {
            Key.OemOpenBrackets => shift
                ? MainWindowViewModel.LayerOrderAction.SendToBack
                : MainWindowViewModel.LayerOrderAction.SendBackward,
            Key.OemCloseBrackets => shift
                ? MainWindowViewModel.LayerOrderAction.BringToFront
                : MainWindowViewModel.LayerOrderAction.BringForward,
            _ => (MainWindowViewModel.LayerOrderAction?)null,
        };
        if (ctrl && !alt && keyboardLayerAction is { } layerAction)
        {
            MoveSelectedElementsInLayerOrder(layerAction);
            e.Handled = true;
            return;
        }

        var keyboardArtboardCenter = e.Key switch
        {
            Key.X => (Horizontally: true, Vertically: false),
            Key.Y => (Horizontally: false, Vertically: true),
            Key.C => (Horizontally: true, Vertically: true),
            _ => ((bool Horizontally, bool Vertically)?)null,
        };
        if (ctrl && alt && shift && keyboardArtboardCenter is { } center)
        {
            CenterSelectedElementsOnArtboard(center.Horizontally, center.Vertically);
            e.Handled = true;
            return;
        }

        var keyboardArtboardEdge = e.Key switch
        {
            Key.Left => MainWindowViewModel.ArtboardAlignment.Left,
            Key.Right => MainWindowViewModel.ArtboardAlignment.Right,
            Key.Up => MainWindowViewModel.ArtboardAlignment.Top,
            Key.Down => MainWindowViewModel.ArtboardAlignment.Bottom,
            _ => (MainWindowViewModel.ArtboardAlignment?)null,
        };
        if (ctrl && alt && shift && keyboardArtboardEdge is { } artboardEdge)
        {
            AlignSelectedElementsToArtboard(artboardEdge);
            e.Handled = true;
            return;
        }

        if (!ctrl && e.Key == Key.Tab && ReferenceEquals(e.Source, DesignHost))
        {
            if (shift
                && (_canvasSelectionAnchor ?? Vm.Canvas.SelectedElement) is { } anchor
                && Vm.TrySelectNextTabOrderRange(anchor, reverse: true, append: true))
            {
                _canvasSelectionAnchor ??= anchor;
                e.Handled = true;
                return;
            }

            if (Vm.SelectNextVisibleElement(shift, append: shift))
            {
                _canvasSelectionAnchor = Vm.Canvas.SelectedElement;
                e.Handled = true;
                return;
            }
        }

        if (!ctrl
            && !alt
            && e.Key is (Key.Home or Key.End)
            && Vm.Toolbox.SelectedItem is null
            && ReferenceEquals(e.Source, DesignHost))
        {
            if (shift
                && (_canvasSelectionAnchor ?? Vm.Canvas.SelectedElement) is { } anchor
                && Vm.TrySelectCanvasBoundaryRange(
                    anchor,
                    last: e.Key == Key.End,
                    append: true))
            {
                _canvasSelectionAnchor ??= anchor;
                e.Handled = true;
                return;
            }

            if (Vm.SelectBoundaryVisibleElement(e.Key == Key.End, append: shift))
            {
                _canvasSelectionAnchor = Vm.Canvas.SelectedElement;
                e.Handled = true;
                return;
            }
        }

        if (!ctrl
            && !alt
            && e.Key is (Key.PageUp or Key.PageDown)
            && Vm.Toolbox.SelectedItem is null
            && ReferenceEquals(e.Source, DesignHost))
        {
            if (shift
                && (_canvasSelectionAnchor ?? Vm.Canvas.SelectedElement) is { } anchor
                && Vm.TrySelectNextCanvasRange(
                    anchor,
                    e.Key == Key.PageUp,
                    append: true))
            {
                _canvasSelectionAnchor ??= anchor;
                e.Handled = true;
                return;
            }

            if (Vm.SelectNextCanvasElement(e.Key == Key.PageUp, append: shift))
            {
                _canvasSelectionAnchor = Vm.Canvas.SelectedElement;
                e.Handled = true;
                return;
            }
        }

        if (!ctrl
            && !alt
            && e.Key == Key.Enter
            && Vm.Toolbox.SelectedItem is null
            && ReferenceEquals(e.Source, DesignHost)
            && Vm.SelectChildOfSelectedElement(shift))
        {
            e.Handled = true;
            return;
        }

        if (alt
            && !ctrl
            && !shift
            && e.Key is (Key.Left or Key.Right or Key.Up or Key.Down)
            && ReferenceEquals(e.Source, DesignHost)
            && Vm.SelectSiblingOfSelectedElement(e.Key is (Key.Left or Key.Up) ? -1 : 1))
        {
            e.Handled = true;
            return;
        }

        if (ctrl && !shift && !alt && e.Key == Key.G)
        {
            FlushPendingPropertyHistory();
            Vm.GroupSelectedElements();
            e.Handled = true;
            return;
        }

        if (ctrl && shift && !alt && e.Key == Key.U)
        {
            FlushPendingPropertyHistory();
            Vm.UngroupSelectedCanvas();
            e.Handled = true;
            return;
        }

        if (ctrl && shift && !alt && e.Key == Key.B)
        {
            FlushPendingPropertyHistory();
            Vm.BreakSelectedLayout();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && ApplyKeyboardLayoutShortcut(e.Key))
        {
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.G)
        {
            Vm.SetCanvasGridVisibility(!Vm.Canvas.IsGridVisible);
            e.Handled = true;
            return;
        }

        if (ctrl && alt && shift && e.Key == Key.G)
        {
            Vm.SetCanvasSnapToGrid(!Vm.Canvas.SnapToGrid);
            e.Handled = true;
            return;
        }

        if (ctrl && shift && !alt && e.Key == Key.G)
        {
            ClearDesignGuides();
            Vm.StatusText = "Design guides cleared.";
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            var wasMarqueeSelecting = _isMarqueeSelecting;
            var wasToolboxPlacement = Vm.Toolbox.SelectedItem is not null;
            _isMarqueeSelecting = false;
            MarqueeRectangle.IsVisible = false;
            Vm.Toolbox.SelectedItem = null;
            HideToolboxPlacementPreview();

            if (!wasMarqueeSelecting
                && !wasToolboxPlacement
                && ReferenceEquals(e.Source, DesignHost)
                && Vm.SelectParentOfSelectedElement())
            {
                e.Handled = true;
                return;
            }

            Vm.SelectElements(Array.Empty<DesignElement>());
            Vm.StatusText = "Selection tool active.";
            e.Handled = true;
            return;
        }

        if (ctrl && alt && e.Key == Key.S)
        {
            _ = await SaveAllDocumentsAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && !shift && e.Key == Key.D)
        {
            FlushPendingPropertyHistory();
            Vm.DuplicateDocumentTab();
            ClearDesignGuides();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && e.Key == Key.R)
        {
            await RenameDocumentTabAsync(Vm.SelectedDocumentTab);
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.S)
        {
            _ = await SaveDocumentAsync(forceSaveAs: shift);
            e.Handled = true;
            return;
        }

        if (ctrl && shift && e.Key == Key.W)
        {
            _ = await CloseAllDocumentTabsAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && shift && e.Key == Key.T)
        {
            FlushPendingPropertyHistory();
            Vm.ReopenClosedDocumentTab();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.W)
        {
            if (Vm.SelectedDocumentTab is { } tab)
            {
                await CloseDocumentTabAsync(tab);
            }

            e.Handled = true;
            return;
        }

        if (ctrl && alt && e.Key == Key.Tab)
        {
            Vm.ActivateRecentDocumentTab(reverse: shift);
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.Tab)
        {
            Vm.ActivateNextDocumentTab(reverse: shift);
            e.Handled = true;
            return;
        }

        var directTabIndex = e.Key switch
        {
            Key.D1 => 0,
            Key.D2 => 1,
            Key.D3 => 2,
            Key.D4 => 3,
            Key.D5 => 4,
            Key.D6 => 5,
            Key.D7 => 6,
            Key.D8 => 7,
            Key.D9 => 8,
            _ => -1,
        };
        if (ctrl && !shift && !alt && directTabIndex >= 0)
        {
            Vm.ActivateDocumentTabAt(directTabIndex);
            e.Handled = true;
            return;
        }

        if (ctrl && shift && e.Key is (Key.PageUp or Key.PageDown))
        {
            if (Vm.SelectedDocumentTab is { } tab)
            {
                var currentIndex = Vm.DocumentTabs.IndexOf(tab);
                var offset = e.Key == Key.PageUp ? -1 : 1;
                Vm.MoveDocumentTab(tab, currentIndex + offset);
            }

            e.Handled = true;
            return;
        }

        if (ctrl && shift && !alt && e.Key == Key.O)
        {
            await OpenRecentFilesAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && shift && !alt && e.Key == Key.P)
        {
            await OpenProjectExplorerAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && shift && !alt && e.Key == Key.R)
        {
            if (Vm?.CanReloadCurrentFile == true)
            {
                await ReloadCurrentFileAsync();
            }

            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.O)
        {
            await HandleOpenCommandAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && !shift && e.Key == Key.F)
        {
            if (!ObjectTreePane.IsVisible)
            {
                SetObjectTreePaneVisible(true);
            }

            ObjectTreeSearch.Focus();
            ObjectTreeSearch.SelectAll();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.D0)
        {
            SetZoomViewportAtCenter(1);
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.N)
        {
            await HandleNewCommandAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.Z)
        {
            FlushPendingPropertyHistory();
            Vm.Undo();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.Y)
        {
            FlushPendingPropertyHistory();
            Vm.Redo();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.A)
        {
            Vm.SelectAllVisibleUnlockedElements();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.D)
        {
            FlushPendingPropertyHistory();
            Vm.DuplicateSelectedElement();
            e.Handled = true;
            return;
        }

        if (ctrl && shift && e.Key == Key.C)
        {
            CopySelectedStyle();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.C)
        {
            FlushPendingPropertyHistory();
            Vm.CopySelectedElement();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.X)
        {
            FlushPendingPropertyHistory();
            Vm.CutSelectedElement();
            e.Handled = true;
            return;
        }

        if (ctrl && alt && e.Key == Key.V)
        {
            await PasteAxamlFromClipboardAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && shift && e.Key == Key.V)
        {
            PasteSelectedStyle();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.V)
        {
            FlushPendingPropertyHistory();
            Vm.PasteElement();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.R)
        {
            OnPreviewMenuClicked(this, e);
            e.Handled = true;
            return;
        }

        var canvasSelectionDirection = e.Key switch
        {
            Key.Left => MainWindowViewModel.CanvasSelectionDirection.Left,
            Key.Right => MainWindowViewModel.CanvasSelectionDirection.Right,
            Key.Up => MainWindowViewModel.CanvasSelectionDirection.Up,
            Key.Down => MainWindowViewModel.CanvasSelectionDirection.Down,
            _ => (MainWindowViewModel.CanvasSelectionDirection?)null,
        };
        if (ctrl
            && !shift
            && !alt
            && ReferenceEquals(e.Source, DesignHost)
            && canvasSelectionDirection is { } direction
            && Vm.SelectNearestElementInDirection(direction))
        {
            e.Handled = true;
            return;
        }

        var nudgeDistance = shift ? 10 : 1;
        if (Vm.Canvas.IsSelectionActive && e.Key is (Key.Left or Key.Right or Key.Up or Key.Down))
        {
            FlushPendingPropertyHistory();

            switch (e.Key)
            {
                case Key.Left:
                    Vm.MoveSelectedElement(-nudgeDistance, 0);
                    break;
                case Key.Right:
                    Vm.MoveSelectedElement(nudgeDistance, 0);
                    break;
                case Key.Up:
                    Vm.MoveSelectedElement(0, -nudgeDistance);
                    break;
                case Key.Down:
                    Vm.MoveSelectedElement(0, nudgeDistance);
                    break;
            }

            e.Handled = true;
            return;
        }

        if (e.Key is Key.Delete or Key.Back)
        {
            FlushPendingPropertyHistory();
            Vm.RemoveSelectedElement();
            e.Handled = true;
        }
    }

    private void OnWindowKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _isSpacePanModifier = false;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _objectTreeSelectionAnchor = null;

        if (_boundVm is not null)
        {
            CaptureDesignGuidesForTab(_guideStateTab ?? _boundVm.SelectedDocumentTab);
            CaptureViewportForTab(_viewportStateTab ?? _boundVm.SelectedDocumentTab);
        }

        if (_boundCanvas is not null)
        {
            _boundCanvas.PropertyChanged -= OnCanvasPropertyChanged;
        }

        if (_boundVm is not null)
        {
            _boundVm.RecentFiles.CollectionChanged -= OnRecentFilesChanged;
            _boundVm.PropertyChanged -= OnViewModelPropertyChanged;
            _boundVm.DocumentChanged -= OnDocumentChanged;
        }

        DisposeProjectWorkspaceWatcher();

        _previewWindow?.Close();
        _previewWindow = null;

        _boundVm = Vm;
        _boundCanvas = _boundVm?.Canvas;
        _guideStateTab = _boundVm?.SelectedDocumentTab;
        _viewportStateTab = _boundVm?.SelectedDocumentTab;
        RestoreDesignGuidesForTab(_guideStateTab);
        RestoreViewportForTab(_viewportStateTab);

        if (_boundCanvas is not null)
        {
            _boundCanvas.PropertyChanged += OnCanvasPropertyChanged;
        }

        if (_boundVm is not null)
        {
            _boundVm.RecentFiles.CollectionChanged += OnRecentFilesChanged;
            _boundVm.PropertyChanged += OnViewModelPropertyChanged;
            _boundVm.DocumentChanged += OnDocumentChanged;
        }

        ConfigureProjectWorkspaceWatcher();
        RebuildRecentFilesMenu();
        UpdateProjectWorkspaceMenuStates();
        UpdateDocumentBackupMenu();
        ApplyWorkspacePanelState();
        ApplyPropertyInspectorState();
        RebindSelection();
        UpdateViewportRulers();
    }

    private void OnRecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildRecentFilesMenu();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedDocumentTab))
        {
            CaptureViewportForTab(_viewportStateTab);
            CaptureDesignGuidesForTab(_guideStateTab);
            if (_guideStateTab is not null
                && (Vm is null || !Vm.DocumentTabs.Contains(_guideStateTab)))
            {
                _documentGuideStates.Remove(_guideStateTab);
            }

            _guideStateTab = Vm?.SelectedDocumentTab;
            _viewportStateTab = Vm?.SelectedDocumentTab;
            RestoreDesignGuidesForTab(_guideStateTab);
            RestoreViewportForTab(_viewportStateTab);
            ApplyPropertyInspectorState();
        }

        if (e.PropertyName == nameof(MainWindowViewModel.CurrentDocumentPath))
        {
            RememberKnownDocumentWriteTime(Vm?.CurrentDocumentPath);
            UpdateDocumentBackupMenu();
        }

        if (e.PropertyName == nameof(MainWindowViewModel.ProjectWorkspacePath))
        {
            ConfigureProjectWorkspaceWatcher();
        }

        if (e.PropertyName is nameof(MainWindowViewModel.ProjectWorkspacePath)
            or nameof(MainWindowViewModel.CanReloadCurrentFile))
        {
            UpdateProjectWorkspaceMenuStates();
        }
    }

    private void OnDocumentChanged(object? sender, EventArgs e)
    {
        if (Vm is not null && _previewWindow is not null)
        {
            _previewWindow.RefreshDocument(Vm.CreatePreviewDocument());
        }
    }

    private void UpdateDocumentBackupMenu()
    {
        if (RecoverBackupMenu is null)
        {
            return;
        }

        var path = Vm?.CurrentDocumentPath;
        try
        {
            RecoverBackupMenu.IsEnabled = !string.IsNullOrWhiteSpace(path)
                && File.Exists(GetDocumentBackupPath(path));
        }
        catch (ArgumentException)
        {
            RecoverBackupMenu.IsEnabled = false;
        }
    }

    private void UpdateProjectWorkspaceMenuStates()
    {
        var hasProjectWorkspace = Vm?.HasProjectWorkspace == true;
        ProjectExplorerMenu.IsEnabled = hasProjectWorkspace;
        RefreshProjectFilesMenu.IsEnabled = hasProjectWorkspace;
        ProjectExplorerContextMenu.IsEnabled = hasProjectWorkspace;
        RefreshProjectFilesContextMenu.IsEnabled = hasProjectWorkspace;

        var canReloadCurrentFile = Vm?.CanReloadCurrentFile == true;
        ReloadCurrentFileMenu.IsEnabled = canReloadCurrentFile;
        ReloadCurrentFileContextMenu.IsEnabled = canReloadCurrentFile;
    }

    private void UpdatePreviewThemeMenuStates()
    {
        PreviewThemeDefaultMenu.IsChecked = _previewThemeMode == PreviewThemeMode.Default;
        PreviewThemeLightMenu.IsChecked = _previewThemeMode == PreviewThemeMode.Light;
        PreviewThemeDarkMenu.IsChecked = _previewThemeMode == PreviewThemeMode.Dark;
    }

    private static string GetDocumentBackupPath(string documentPath)
        => $"{System.IO.Path.GetFullPath(documentPath)}.bak";

    private void RebuildRecentFilesMenu()
    {
        if (_boundVm is null || _boundVm.RecentFiles.Count == 0)
        {
            OpenRecentMenu.IsEnabled = false;
            OpenRecentMenu.ItemsSource = null;
            return;
        }

        OpenRecentMenu.IsEnabled = true;
        var items = new System.Collections.Generic.List<MenuItem>();

        for (var i = 0; i < _boundVm.RecentFiles.Count; i++)
        {
            var path = _boundVm.RecentFiles[i];
            var item = new MenuItem
            {
                Header = $"{i + 1}. {path}",
                Tag = path,
            };
            item.Click += OnOpenRecentMenuItemClicked;
            items.Add(item);
        }

        OpenRecentMenu.ItemsSource = items;
    }

    private async void OnOpenRecentMenuItemClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null || sender is not MenuItem { Tag: string path })
        {
            return;
        }

        await OpenRecentFileAsync(path);
    }

    private void OnCanvasPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CanvasViewModel.SelectedElement))
        {
            if (!_isObjectTreeSelectionGesture)
            {
                _objectTreeSelectionAnchor = null;
            }

            RebindSelection();
        }

        if (e.PropertyName == nameof(CanvasViewModel.ZoomScale))
        {
            UpdateHandlePositions();
        }

        if (e.PropertyName == nameof(CanvasViewModel.SelectionBoundsSummary))
        {
            UpdateHandlePositions();
        }

        UpdateViewportRulers();
    }

    private void OnDesignScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty)
        {
            UpdateViewportRulers();
            CaptureViewportForTab(_viewportStateTab ?? Vm?.SelectedDocumentTab);
        }
    }

    private void UpdateViewportRulers()
    {
        var zoom = Vm?.Canvas.ZoomScale ?? 1;
        var offset = DesignScrollViewer.Offset;
        HorizontalRuler.ZoomScale = zoom;
        HorizontalRuler.ScrollOffset = offset.X;
        VerticalRuler.ZoomScale = zoom;
        VerticalRuler.ScrollOffset = offset.Y;
        if (_viewportPointer is { } pointer)
        {
            UpdateViewportCursor(pointer);
        }
        else
        {
            ClearViewportCursor();
        }
    }

    private void UpdateViewportCursor(Point pointer)
    {
        _viewportPointer = pointer;
        var zoom = Math.Max(0.01, Vm?.Canvas.ZoomScale ?? 1);
        var offset = DesignScrollViewer.Offset;
        HorizontalRuler.CursorPosition = (offset.X + pointer.X) / zoom;
        VerticalRuler.CursorPosition = (offset.Y + pointer.Y) / zoom;
    }

    private void UpdatePanCursor()
        => DesignViewport.Cursor = _isPanToolActive || _isPanningViewport
            ? new Cursor(StandardCursorType.Hand)
            : null;

    private void ClearViewportCursor()
    {
        _viewportPointer = null;
        HorizontalRuler.CursorPosition = double.NaN;
        VerticalRuler.CursorPosition = double.NaN;
    }

    private void UpdateGuideOverlay()
    {
        if (Vm is not null && !_isDraggingGuide)
        {
            var width = Vm.Canvas.ArtboardWidth;
            var height = Vm.Canvas.ArtboardHeight;
            _verticalGuides.RemoveAll(value => value < 0 || value > width);
            _horizontalGuides.RemoveAll(value => value < 0 || value > height);
        }

        GuideOverlay.VerticalGuides = _verticalGuides.ToArray();
        GuideOverlay.HorizontalGuides = _horizontalGuides.ToArray();
    }

    private void CaptureDesignGuidesForTab(DocumentTabViewModel? tab)
    {
        if (tab is null)
        {
            return;
        }

        var state = new DocumentGuideState(
            _horizontalGuides.ToArray(),
            _verticalGuides.ToArray(),
            _showDesignGuides,
            _snapToGuides);
        _documentGuideStates[tab] = state;
        tab.GuideState = state;
    }

    private void RestoreDesignGuidesForTab(DocumentTabViewModel? tab)
    {
        _horizontalGuides.Clear();
        _verticalGuides.Clear();
        _isDraggingGuide = false;
        _guideIndex = -1;

        var state = tab is not null
            && _documentGuideStates.TryGetValue(tab, out var cachedState)
            ? cachedState
            : tab?.GuideState ?? DocumentGuideState.Default;
        if (tab is not null)
        {
            _documentGuideStates[tab] = state;
            tab.GuideState = state;
        }

        _horizontalGuides.AddRange(state.HorizontalGuides);
        _verticalGuides.AddRange(state.VerticalGuides);
        _showDesignGuides = state.ShowDesignGuides;
        _snapToGuides = state.SnapToGuides;

        SyncDesignGuideToggles();
        GuideOverlay.IsVisible = _showDesignGuides;
        UpdateGuideOverlay();
    }

    private void ClearDesignGuides()
    {
        _horizontalGuides.Clear();
        _verticalGuides.Clear();
        _isDraggingGuide = false;
        _guideIndex = -1;
        CaptureDesignGuidesForTab(_guideStateTab);
        UpdateGuideOverlay();
    }

    private void RebindSelection()
    {
        FlushPendingPropertyHistory();
        FlushPendingLayoutHistory();

        if (_boundVisual is not null)
        {
            _boundVisual.PropertyChanged -= OnSelectedVisualPropertyChanged;
        }

        if (_boundElement is not null)
        {
            _boundElement.PropertyChanged -= OnElementPropertyChanged;
        }

        _boundElement = _boundCanvas?.SelectedElement;
        _boundVisual = _boundElement?.Visual;

        if (_boundElement is not null)
        {
            _boundElement.PropertyChanged += OnElementPropertyChanged;
        }

        if (_boundVisual is not null)
        {
            _boundVisual.PropertyChanged += OnSelectedVisualPropertyChanged;
        }

        ApplyPropertyInspectorFilter();
        UpdateSelectionEditability();
        UpdateLayoutEditors();
        UpdateElementNameEditor();
        UpdateHandlePositions();
        QueueObjectTreeSelectionIntoView();
        QueueCanvasSelectionIntoView();
    }

    private void QueueObjectTreeSelectionIntoView()
    {
        if (Vm?.ObjectTree.SelectedNode is not { } selectedNode)
        {
            return;
        }

        Dispatcher.UIThread.Post(
            () =>
            {
                if (Vm?.ObjectTree.SelectedNode is { } currentNode
                    && ReferenceEquals(currentNode, selectedNode))
                {
                    ObjectTreeView.ScrollIntoView(currentNode);
                }
            },
            DispatcherPriority.Background);
    }

    private void QueueCanvasSelectionIntoView()
    {
        if (Vm?.Canvas.SelectedElement is not { } selectedElement
            || !Vm.Canvas.IsElementVisibleOnCanvas(selectedElement))
        {
            return;
        }

        Dispatcher.UIThread.Post(
            () =>
            {
                if (_isViewportRestorePending)
                {
                    return;
                }

                if (Vm?.Canvas.SelectedElement is { } currentElement
                    && ReferenceEquals(currentElement, selectedElement)
                    && Vm.Canvas.IsElementVisibleOnCanvas(currentElement))
                {
                    DesignHost.ScrollIntoView(currentElement);
                }
            },
            DispatcherPriority.Background);
    }

    private void OnSelectedVisualPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Control control
            || DesignerStyleApplicationMetadata.IsProgrammaticUpdate(control)
            || !IsUndoTrackedVisualProperty(control, e.Property.Name))
        {
            return;
        }

        if (DesignerStyleRuntime.IsSupportedProperty(control.GetType().Name, e.Property.Name))
        {
            DesignerStyleApplicationMetadata.ClearApplied(control, e.Property.Name);
            if (DesignerStyleRuntime.IsBrushProperty(e.Property.Name))
            {
                DesignerResourceReferenceMetadata.SetReference(control, e.Property.Name, null);
            }
        }

        if (e.Property.Name is "IsEnabled" or "IsChecked" or "IsExpanded")
        {
            Vm?.Canvas.RefreshDocumentStyles(control);
        }

        if (control is Grid
            && e.Property.Name is "RowDefinitions" or "ColumnDefinitions"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowGridChildren(_boundElement);
        }

        if (control is StackPanel
            && e.Property.Name is "Orientation" or "Spacing"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is DockPanel
            && e.Property.Name == "LastChildFill"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is WrapPanel
            && e.Property.Name is "Orientation" or "ItemWidth" or "ItemHeight"
                or "ItemSpacing" or "LineSpacing" or "ItemsAlignment"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is UniformGrid
            && e.Property.Name is "Rows" or "Columns" or "FirstColumn" or "RowSpacing" or "ColumnSpacing"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is TabControl
            && e.Property.Name == "SelectedIndex"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is SplitView
            && e.Property.Name is "DisplayMode" or "IsPaneOpen" or "OpenPaneLength"
                or "CompactPaneLength" or "PanePlacement"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (control is Border
            && e.Property.Name == "BorderThickness"
            && _boundElement is not null)
        {
            Vm?.Canvas.ReflowContainerChildren(_boundElement);
        }

        if (!_hasPendingPropertyEdit)
        {
            Vm?.BeginCanvasMutation(MainWindowViewModel.HistoryActionType.EditProperty, "Updated control properties.");
            _hasPendingPropertyEdit = true;
        }

        _propertyEditTimer.Stop();
        _propertyEditTimer.Start();
    }

    private void OnPropertyEditTimerTick(object? sender, EventArgs e)
    {
        _propertyEditTimer.Stop();
        FlushPendingPropertyHistory();
    }

    private void FlushPendingPropertyHistory()
    {
        if (!_hasPendingPropertyEdit)
        {
            return;
        }

        _hasPendingPropertyEdit = false;
        Vm?.CommitCanvasMutation();
    }

    private static bool IsUndoTrackedVisualProperty(Control control, string propertyName)
    {
        if (DesignerButtonRuntime.IsSupportedProperty(
                control.GetType().Name,
                propertyName))
        {
            return true;
        }

        if (DesignerImageRuntime.IsSupportedProperty(
                control.GetType().Name,
                propertyName))
        {
            return true;
        }

        if (DesignerContainerBehaviorRuntime.IsSupportedProperty(
                control.GetType().Name,
                propertyName))
        {
            return true;
        }

        if (DesignerToggleRuntime.IsSupportedProperty(control.GetType().Name, propertyName))
        {
            return true;
        }

        if (DesignerDateTimeRuntime.IsSupportedProperty(control.GetType().Name, propertyName))
        {
            return true;
        }

        if (propertyName is "Opacity" or "IsEnabled" or "IsVisible" or "TabIndex" or "IsTabStop")
        {
            return true;
        }

        if (control is Avalonia.Controls.Primitives.TemplatedControl
            && (propertyName is "Background" or "Foreground" or "BorderBrush" or "BorderThickness"
                or "CornerRadius" or "FontSize" or "FontWeight"))
        {
            return true;
        }

        if (control is Button)
        {
            return propertyName == "Content";
        }

        if (control is TextBox)
        {
            return propertyName is "Text" or "Watermark" or "PasswordChar" or "RevealPassword" or "AcceptsReturn" or "TextWrapping";
        }

        if (control is TextBlock)
        {
            return propertyName is "Text" or "FontSize" or "FontWeight" or "Background" or "Foreground";
        }

        if (control is Label)
        {
            return propertyName == "Content";
        }

        if (control is CheckBox or ToggleSwitch)
        {
            return propertyName is "Content" or "IsChecked";
        }

        if (control is Avalonia.Controls.Primitives.ToggleButton)
        {
            return propertyName is "Content" or "IsChecked";
        }

        if (control is RadioButton)
        {
            return propertyName is "Content" or "IsChecked" or "GroupName";
        }

        if (control is ComboBox or ListBox)
        {
            return propertyName == "SelectedIndex";
        }

        if (control is Slider or ProgressBar)
        {
            return propertyName is "Minimum" or "Maximum" or "Value";
        }

        if (control is DatePicker)
        {
            return propertyName == "SelectedDate";
        }

        if (control is CalendarDatePicker)
        {
            return propertyName is "SelectedDate" or "Watermark";
        }

        if (control is TimePicker)
        {
            return propertyName == "SelectedTime";
        }

        if (control is NumericUpDown)
        {
            return propertyName is "Minimum" or "Maximum" or "Increment" or "Value";
        }

        if (control is TabControl)
        {
            return propertyName == "SelectedIndex";
        }

        if (control is SplitView)
        {
            return propertyName is "DisplayMode" or "IsPaneOpen" or "OpenPaneLength"
                or "CompactPaneLength" or "PanePlacement" or "PaneBackground"
                or "UseLightDismissOverlayMode";
        }

        if (control is Expander)
        {
            return propertyName is "Header" or "IsExpanded";
        }

        if (control is Border)
        {
            return propertyName is "Background" or "BorderBrush" or "BorderThickness" or "CornerRadius";
        }

        if (control is StackPanel)
        {
            return propertyName is "Orientation" or "Spacing";
        }

        if (control is DockPanel)
        {
            return propertyName == "LastChildFill";
        }

        if (control is WrapPanel)
        {
            return propertyName is "Orientation" or "ItemWidth" or "ItemHeight"
                or "ItemSpacing" or "LineSpacing" or "ItemsAlignment";
        }

        if (control is UniformGrid)
        {
            return propertyName is "Rows" or "Columns" or "FirstColumn" or "RowSpacing" or "ColumnSpacing";
        }

        if (control is Canvas)
        {
            return propertyName == "Background";
        }

        if (control is Grid)
        {
            return propertyName is "RowDefinitions" or "ColumnDefinitions" or "ShowGridLines";
        }

        return false;
    }

    private void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DesignElement.DisplayName))
        {
            UpdateElementNameEditor();
        }

        if (e.PropertyName is nameof(DesignElement.IsLocked)
            or nameof(DesignElement.ParentName)
            or nameof(DesignElement.ParentLayout)
            or nameof(DesignElement.IsContainerChild)
            or nameof(DesignElement.IsCanvasChild))
        {
            UpdateSelectionEditability();
            UpdateLayoutEditors();
        }

        if (e.PropertyName is nameof(DesignElement.X)
            or nameof(DesignElement.Y)
            or nameof(DesignElement.Width)
            or nameof(DesignElement.Height)
            or nameof(DesignElement.CanvasChildLeft)
            or nameof(DesignElement.CanvasChildTop))
        {
            UpdateLayoutEditors();
            UpdateHandlePositions();
        }
    }

    private void OnLayoutEditorGotFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_boundElement?.IsLocked == true)
        {
            Vm?.StatusText = "Selected control is locked.";
            return;
        }

        if (_boundElement is null || _hasPendingLayoutEdit)
        {
            return;
        }

        FlushPendingPropertyHistory();
        Vm?.BeginCanvasMutation(MainWindowViewModel.HistoryActionType.TransformElement, "Updated element layout values.");
        _hasPendingLayoutEdit = true;
    }

    private void OnElementNameEditorLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_boundElement is null || _boundElement.IsLocked || sender is not TextBox editor)
        {
            UpdateElementNameEditor();
            return;
        }

        var name = editor.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            Vm?.TryRenameElement(_boundElement, name);
        }

        UpdateElementNameEditor();
    }

    private void UpdateElementNameEditor()
    {
        ElementNameEditor.Text = _boundElement?.DisplayName ?? string.Empty;
    }

    private void UpdateSelectionEditability()
    {
        var canEdit = _boundElement is { IsLocked: false };
        var canEditLayout = canEdit
            && (_boundElement is not { IsContainerChild: true }
                || _boundElement.IsCanvasChild);
        var canResizeSelection = CanResizeSelection();

        PropGrid.IsEnabled = canEdit;
        ElementNameEditor.IsEnabled = canEdit;
        LayoutXEditor.IsEnabled = canEditLayout;
        LayoutYEditor.IsEnabled = canEditLayout;
        LayoutWidthEditor.IsEnabled = canEditLayout;
        LayoutHeightEditor.IsEnabled = canEditLayout;

        HandleNW.IsVisible = canResizeSelection;
        HandleN.IsVisible = canResizeSelection;
        HandleNE.IsVisible = canResizeSelection;
        HandleE.IsVisible = canResizeSelection;
        HandleSE.IsVisible = canResizeSelection;
        HandleS.IsVisible = canResizeSelection;
        HandleSW.IsVisible = canResizeSelection;
        HandleW.IsVisible = canResizeSelection;
    }

    private bool CanResizeSelection()
    {
        var selected = Vm?.Canvas.SelectedElements;
        if (selected is null || selected.Count == 0 || selected.Any(element => element.IsLocked))
        {
            return false;
        }

        if (selected.Any(element => element.IsContainerChild
                && element.ParentLayout != DesignerParentLayoutKind.Canvas))
        {
            return false;
        }

        var parentNames = selected
            .Select(element => element.ParentName ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        return parentNames == 1;
    }

    private void OnLayoutEditorLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_boundElement?.IsLocked == true)
        {
            UpdateLayoutEditors();
            FlushPendingLayoutHistory();
            return;
        }

        if (sender is not TextBox editor || _boundElement is null)
        {
            FlushPendingLayoutHistory();
            return;
        }

        if (double.TryParse(
                editor.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value)
            && double.IsFinite(value))
        {
            switch (editor.Name)
            {
                case "LayoutXEditor":
                    if (_boundElement.IsCanvasChild)
                    {
                        _boundElement.CanvasChildLeft = Math.Max(0, value);
                    }
                    else
                    {
                        _boundElement.X = Math.Max(0, value);
                    }
                    break;
                case "LayoutYEditor":
                    if (_boundElement.IsCanvasChild)
                    {
                        _boundElement.CanvasChildTop = Math.Max(0, value);
                    }
                    else
                    {
                        _boundElement.Y = Math.Max(0, value);
                    }
                    break;
                case "LayoutWidthEditor":
                    _boundElement.Width = Math.Max(MinSize, value);
                    break;
                case "LayoutHeightEditor":
                    _boundElement.Height = Math.Max(MinSize, value);
                    break;
            }

            Vm?.StatusText = _boundElement.IsCanvasChild
                ? "Updated Canvas child Left/Top/Width/Height values."
                : "Updated element X/Y/Width/Height values.";
        }

        UpdateLayoutEditors();
        FlushPendingLayoutHistory();
    }

    private void UpdateLayoutEditors()
    {
        if (_boundElement is null)
        {
            LayoutXEditor.Text = string.Empty;
            LayoutYEditor.Text = string.Empty;
            LayoutWidthEditor.Text = string.Empty;
            LayoutHeightEditor.Text = string.Empty;
            LayoutXLabel.Text = "X";
            LayoutYLabel.Text = "Y";
            return;
        }

        var isCanvasChild = _boundElement.IsCanvasChild;
        LayoutXLabel.Text = isCanvasChild ? "Left" : "X";
        LayoutYLabel.Text = isCanvasChild ? "Top" : "Y";
        LayoutXEditor.Text = (isCanvasChild ? _boundElement.CanvasChildLeft : _boundElement.X)
            .ToString("0.###", CultureInfo.InvariantCulture);
        LayoutYEditor.Text = (isCanvasChild ? _boundElement.CanvasChildTop : _boundElement.Y)
            .ToString("0.###", CultureInfo.InvariantCulture);
        LayoutWidthEditor.Text = _boundElement.Width.ToString("0.###", CultureInfo.InvariantCulture);
        LayoutHeightEditor.Text = _boundElement.Height.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private void FlushPendingLayoutHistory()
    {
        if (!_hasPendingLayoutEdit)
        {
            return;
        }

        _hasPendingLayoutEdit = false;
        Vm?.CommitCanvasMutation();
    }

    private void UpdateHandlePositions()
    {
        if (!TryGetSelectionBounds(out var bounds))
        {
            return;
        }

        var left = bounds.X;
        var top = bounds.Y;
        var right = bounds.Right;
        var bottom = bounds.Bottom;
        var midX = bounds.X + bounds.Width / 2;
        var midY = bounds.Y + bounds.Height / 2;

        SelectionBoundsRectangle.Width = bounds.Width;
        SelectionBoundsRectangle.Height = bounds.Height;
        var zoom = Math.Clamp(Vm?.Canvas.ZoomScale ?? 1, 0.25, 2);
        SelectionBoundsRectangle.StrokeThickness = SelectionOutlinePixelSize / zoom;
        Canvas.SetLeft(SelectionBoundsRectangle, bounds.X);
        Canvas.SetTop(SelectionBoundsRectangle, bounds.Y);

        Place(HandleNW, left, top);
        Place(HandleN, midX, top);
        Place(HandleNE, right, top);
        Place(HandleE, right, midY);
        Place(HandleSE, right, bottom);
        Place(HandleS, midX, bottom);
        Place(HandleSW, left, bottom);
        Place(HandleW, left, midY);
    }

    private bool TryGetSelectionBounds(out Rect bounds)
    {
        var selected = Vm?.Canvas.SelectedElements;
        if (selected is null || selected.Count == 0)
        {
            bounds = default;
            return false;
        }

        var left = selected.Min(element => element.X);
        var top = selected.Min(element => element.Y);
        var right = selected.Max(element => element.X + element.Width);
        var bottom = selected.Max(element => element.Y + element.Height);
        bounds = new Rect(left, top, right - left, bottom - top);
        return true;
    }

    private void Place(Rectangle rectangle, double cx, double cy)
    {
        var zoom = Math.Clamp(Vm?.Canvas.ZoomScale ?? 1, 0.25, 2);
        var size = HandlePixelSize / zoom;
        rectangle.Width = size;
        rectangle.Height = size;
        Canvas.SetLeft(rectangle, cx - (size / 2));
        Canvas.SetTop(rectangle, cy - (size / 2));
    }

    private void OnToolboxItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Button)
        {
            return;
        }

        if (sender is not Control control
            || !TryGetToolboxItem(control, out var item)
            || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        SetPanToolActive(false);
        Vm?.Toolbox.SelectedItem = item;
        HideToolboxPlacementPreview();
        _pendingToolboxDragItem = item;
        _toolboxDragStart = e.GetPosition(this);
        e.Pointer.Capture(control);
    }

    private static bool TryGetToolboxItem(Control control, out ToolboxItem item)
    {
        item = control.DataContext switch
        {
            ToolboxItem directItem => directItem,
            ToolboxItemPresentation presentation => presentation.Item,
            _ => null!,
        };
        return item is not null;
    }

    private void OnToggleToolboxFavoriteClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null
            || sender is not Control { DataContext: ToolboxItemPresentation presentation })
        {
            return;
        }

        var isFavorite = Vm.Toolbox.ToggleFavorite(presentation.Item);
        Vm.StatusText = isFavorite
            ? $"Added {presentation.DisplayName} to Toolbox favorites."
            : $"Removed {presentation.DisplayName} from Toolbox favorites.";
        e.Handled = true;
    }

    private void OnToolboxSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (Vm.Toolbox.IsPlacementModeActive)
            {
                Vm.Toolbox.CancelPlacementMode();
                HideToolboxPlacementPreview();
                Vm.StatusText = "Toolbox placement mode cancelled.";
            }
            else
            {
                ToolboxSearch.Text = string.Empty;
                ToolboxSearch.Focus();
                Vm.StatusText = "Toolbox search cleared.";
            }

            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
        {
            return;
        }

        if (!Vm.Toolbox.SelectFirstVisibleItem())
        {
            Vm.StatusText = "No Toolbox item matches the search.";
            e.Handled = true;
            return;
        }

        Vm.PlaceSelectedToolboxItemQuickly();
        e.Handled = true;
    }

    private void OnSelectToolClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        SetPanToolActive(false);
        Vm.Toolbox.CancelPlacementMode();
        HideToolboxPlacementPreview();
        Vm.StatusText = "Selection tool active.";
        e.Handled = true;
    }

    private void OnPanToolClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetPanToolActive(!_isPanToolActive);
        e.Handled = true;
    }

    private void OnPanToolMenuClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetPanToolActive(!_isPanToolActive);
        e.Handled = true;
    }

    private void SetPanToolActive(bool isActive)
    {
        _isPanToolActive = isActive;
        PanTool.IsChecked = isActive;
        PanCanvasMenu.IsChecked = isActive;
        UpdatePanCursor();

        if (isActive)
        {
            if (Vm?.Toolbox.IsPlacementModeActive == true)
            {
                Vm.Toolbox.CancelPlacementMode();
            }

            HideToolboxPlacementPreview();
            if (Vm is not null)
            {
                Vm.StatusText = "Pan tool active. Drag the design viewport to scroll.";
            }
        }
        else if (Vm is not null)
        {
            Vm.StatusText = "Selection tool active.";
        }
    }

    private void OnToggleToolboxPlacementModeClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        ToggleToolboxPlacementMode();
        e.Handled = true;
    }

    private void ToggleToolboxPlacementMode()
    {
        if (Vm is null)
        {
            return;
        }

        SetPanToolActive(false);

        if (!ToolboxPane.IsVisible)
        {
            SetToolboxPaneVisible(true);
        }

        var isActive = Vm.Toolbox.TogglePlacementMode();
        if (!isActive)
        {
            HideToolboxPlacementPreview();
        }

        Vm.StatusText = isActive
            ? $"Toolbox placement mode: {Vm.Toolbox.SelectedItem?.DisplayName}."
            : "Toolbox placement mode cancelled.";
    }

    private void OnCancelToolboxPlacementModeClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null)
        {
            return;
        }

        Vm.Toolbox.CancelPlacementMode();
        HideToolboxPlacementPreview();
        Vm.StatusText = "Toolbox placement mode cancelled.";
        e.Handled = true;
    }

    private void OnToolboxKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is null
            || sender is not ListBox listBox
            || listBox.SelectedItem is not ToolboxItemPresentation presentation)
        {
            return;
        }

        Vm.Toolbox.SelectedItem = presentation.Item;
        HideToolboxPlacementPreview();
        if (e.Key == Key.Enter)
        {
            Vm.PlaceSelectedToolboxItemQuickly();
            e.Handled = true;
        }
        else if (e.Key == Key.Space)
        {
            var isFavorite = Vm.Toolbox.ToggleFavorite(presentation.Item);
            Vm.StatusText = isFavorite
                ? $"Added {presentation.DisplayName} to Toolbox favorites."
                : $"Removed {presentation.DisplayName} from Toolbox favorites.";
            e.Handled = true;
        }
    }

    private async void OnToolboxItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pendingToolboxDragItem is not { } item)
        {
            return;
        }

        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _toolboxDragStart.X) < MarqueeThreshold
            && Math.Abs(point.Y - _toolboxDragStart.Y) < MarqueeThreshold)
        {
            return;
        }

        _pendingToolboxDragItem = null;
        e.Pointer.Capture(null);
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(ToolboxDragDataFormat, item.DisplayName));
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Copy);
        e.Handled = true;
    }

    private void OnToolboxItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pendingToolboxDragItem = null;
        e.Pointer.Capture(null);
    }

    private void UpdateToolboxPlacementPreview(Point point)
    {
        if (Vm is null
            || Vm.Toolbox.SelectedItem is null
            || !new Rect(0, 0, Vm.Canvas.ArtboardWidth, Vm.Canvas.ArtboardHeight).Contains(point)
            || Vm.GetToolboxPlacementPreview(point.X, point.Y) is not { } preview)
        {
            HideToolboxPlacementPreview();
            return;
        }

        Canvas.SetLeft(ToolboxPlacementPreview, preview.X);
        Canvas.SetTop(ToolboxPlacementPreview, preview.Y);
        ToolboxPlacementPreview.Width = preview.Width;
        ToolboxPlacementPreview.Height = preview.Height;
        ToolboxPlacementPreviewLabel.Text = preview.DisplayName;
        var targetPreview = UpdateToolboxPlacementTargetHint(point);
        var placementDetails = $"{preview.Width:0} x {preview.Height:0} at {preview.X:0}, {preview.Y:0}";
        ToolboxPlacementPreviewDetails.Text = targetPreview is null
            ? placementDetails
            : $"{placementDetails} -> {targetPreview.TargetLabel}";
        ToolboxPlacementPreview.IsVisible = true;
    }

    private ToolboxPlacementTargetPreview? UpdateToolboxPlacementTargetHint(Point point)
    {
        if (Vm?.GetToolboxPlacementTargetPreview(point.X, point.Y) is not { } target)
        {
            ToolboxPlacementTargetHint.IsVisible = false;
            return null;
        }

        Canvas.SetLeft(ToolboxPlacementTargetHint, target.Bounds.X);
        Canvas.SetTop(ToolboxPlacementTargetHint, target.Bounds.Y);
        ToolboxPlacementTargetHint.Width = target.Bounds.Width;
        ToolboxPlacementTargetHint.Height = target.Bounds.Height;
        ToolboxPlacementTargetHint.IsVisible = true;
        return target;
    }

    private void HideToolboxPlacementPreview()
    {
        ToolboxPlacementPreview.IsVisible = false;
        ToolboxPlacementTargetHint.IsVisible = false;
    }

    private void OnDesignSurfaceDragEnter(object? sender, DragEventArgs e)
    {
        HideToolboxPlacementPreview();
        if (e.DataTransfer.Contains(ToolboxDragDataFormat))
        {
            ToolboxDropHint.Width = 0;
            ToolboxDropHint.Height = 0;
            ToolboxDropHint.IsVisible = true;
            Vm?.StatusText = "Drop the Toolbox item onto the artboard.";
        }
    }

    private void OnDesignSurfaceDragLeave(object? sender, DragEventArgs e)
    {
        HideToolboxPlacementPreview();
        ToolboxDropHint.IsVisible = false;
        ToolboxDropHint.Width = 0;
        ToolboxDropHint.Height = 0;
    }

    private void OnDesignSurfaceDragOver(object? sender, DragEventArgs e)
    {
        if (Vm is null
            || sender is not Control host
            || e.DataTransfer.TryGetValue(ToolboxDragDataFormat) is not string displayName
            || Vm.Toolbox.FindItemByDisplayName(displayName) is not { } item)
        {
            e.DragEffects = DragDropEffects.None;
            ToolboxDropHint.IsVisible = false;
            return;
        }

        TryAutoPanDesignViewport(e.GetPosition(DesignViewport));
        var parent = item.IsPreset
            ? null
            : Vm.FindDropContainer(e.GetPosition(host));
        if (parent is not null)
        {
            Canvas.SetLeft(ToolboxDropHint, parent.X);
            Canvas.SetTop(ToolboxDropHint, parent.Y);
            ToolboxDropHint.Width = parent.Width;
            ToolboxDropHint.Height = parent.Height;
            ToolboxDropHint.IsVisible = true;
            Vm.StatusText = $"Drop {item.DisplayName} into {parent.DisplayName}.";
        }
        else
        {
            ToolboxDropHint.Width = 0;
            ToolboxDropHint.Height = 0;
            ToolboxDropHint.IsVisible = false;
            Vm.StatusText = item.IsPreset
                ? "Drop the preset onto the artboard."
                : "Drop the Toolbox item onto the artboard.";
        }

        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private bool TryAutoPanDesignViewport(Point viewportPoint)
    {
        var viewport = DesignScrollViewer.Viewport;
        var extent = DesignScrollViewer.Extent;
        if (viewport.Width <= 0
            || viewport.Height <= 0
            || extent.Width <= viewport.Width && extent.Height <= viewport.Height)
        {
            return false;
        }

        var deltaX = viewportPoint.X < ViewportEdgePanThreshold
            ? -ViewportEdgePanStep
            : viewportPoint.X > viewport.Width - ViewportEdgePanThreshold
                ? ViewportEdgePanStep
                : 0;
        var deltaY = viewportPoint.Y < ViewportEdgePanThreshold
            ? -ViewportEdgePanStep
            : viewportPoint.Y > viewport.Height - ViewportEdgePanThreshold
                ? ViewportEdgePanStep
                : 0;
        var maxX = Math.Max(0, extent.Width - viewport.Width);
        var maxY = Math.Max(0, extent.Height - viewport.Height);
        var nextOffset = new Vector(
            Math.Clamp(DesignScrollViewer.Offset.X + deltaX, 0, maxX),
            Math.Clamp(DesignScrollViewer.Offset.Y + deltaY, 0, maxY));
        if (nextOffset == DesignScrollViewer.Offset)
        {
            return false;
        }

        DesignScrollViewer.Offset = nextOffset;
        return true;
    }

    private void OnDesignSurfaceDrop(object? sender, DragEventArgs e)
    {
        HideToolboxPlacementPreview();
        ToolboxDropHint.IsVisible = false;
        if (Vm is null || sender is not Control host
            || e.DataTransfer.TryGetValue(ToolboxDragDataFormat) is not string displayName
            || Vm.Toolbox.FindItemByDisplayName(displayName) is not { } item)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var point = e.GetPosition(host);
        var parent = item.IsPreset ? null : Vm.FindDropContainer(point);
        Vm.PlaceToolboxItem(item, point.X, point.Y, parent);
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnDesignHostPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null || sender is not Control host)
        {
            return;
        }

        DesignHost.Focus();
        _objectTreeSelectionAnchor = null;
        _canvasSelectionAnchor = null;

        if (TryBeginViewportPan(host, e))
        {
            return;
        }

        if (e.GetCurrentPoint(host).Properties.IsRightButtonPressed)
        {
            return;
        }

        var point = e.GetPosition(host);
        if (Vm.Toolbox.SelectedItem is not null)
        {
            Vm.PlaceFromToolbox(point.X, point.Y);
            UpdateToolboxPlacementPreview(point);
            e.Handled = true;
            return;
        }

        _isMarqueeSelecting = true;
        _marqueeAdditive = IsAdditiveMarqueeModifier(e.KeyModifiers);
        _marqueeSubtractive = IsSubtractiveMarqueeModifier(e.KeyModifiers);
        _marqueeStart = point;
        UpdateMarquee(point);
        e.Pointer.Capture(DesignHost);
        e.Handled = true;
    }

    private async void OnElementPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null || sender is not Control { DataContext: DesignElement element })
        {
            return;
        }

        DesignHost.Focus();
        _objectTreeSelectionAnchor = null;

        if (TryBeginViewportPan((Control)sender, e))
        {
            return;
        }

        if (e.GetCurrentPoint((Control)sender).Properties.IsRightButtonPressed)
        {
            if (!Vm.Canvas.SelectedElements.Contains(element))
            {
                Vm.SelectElement(element);
            }

            _canvasSelectionAnchor = element;
            Vm.StatusText = $"Selected {element.DisplayName}.";
            return;
        }

        var placementPoint = e.GetPosition(DesignHost);
        if (Vm.Toolbox.SelectedItem is not null
            && Vm.GetToolboxPlacementTarget(placementPoint.X, placementPoint.Y) is not null)
        {
            Vm.PlaceFromToolbox(placementPoint.X, placementPoint.Y);
            UpdateToolboxPlacementPreview(placementPoint);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)
            && TryCycleSelectionAtPoint(
                placementPoint,
                e.KeyModifiers.HasFlag(KeyModifiers.Shift)))
        {
            _canvasSelectionAnchor = Vm.Canvas.SelectedElement;
            e.Handled = true;
            return;
        }

        var toggleSelection = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var additiveSelection = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var anchor = _canvasSelectionAnchor ?? Vm.Canvas.SelectedElement;
        if (additiveSelection
            && anchor is not null
            && Vm.TrySelectCanvasRange(anchor, element, append: toggleSelection))
        {
            _canvasSelectionAnchor = anchor;
            e.Handled = true;
            return;
        }

        if (element.IsLocked)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                Vm.SelectElement(element, toggle: true);
                _canvasSelectionAnchor = element;
                Vm.StatusText = "Selected locked control.";
            }
            else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                Vm.AddElementToSelection(element);
                _canvasSelectionAnchor = element;
            }
            else
            {
                Vm.SelectElement(element);
                _canvasSelectionAnchor = element;
                Vm.StatusText = "Selected locked control.";
            }

            e.Handled = true;
            return;
        }

        if (additiveSelection && !toggleSelection)
        {
            Vm.AddElementToSelection(element);
            _canvasSelectionAnchor = element;
            e.Handled = true;
            return;
        }

        if (toggleSelection || !Vm.Canvas.SelectedElements.Contains(element))
        {
            Vm.SelectElement(element, toggleSelection);
        }

        _canvasSelectionAnchor = element;

        if (toggleSelection)
        {
            e.Handled = true;
            return;
        }

        if (e.ClickCount >= 2 && await TryOpenQuickContentEditorAsync(element))
        {
            e.Handled = true;
            return;
        }

        if (element.IsContainerChild)
        {
            Vm.StatusText = "Container child position and size are managed by its parent layout.";
            e.Handled = true;
            return;
        }

        BeginDrag(DragMode.Move, element, e);
        e.Handled = true;
    }

    private bool TryCycleSelectionAtPoint(Point point, bool reverse)
    {
        if (Vm is null)
        {
            return false;
        }

        // ItemsControl renders later elements on top, so walk the hit stack from front to back.
        var candidates = Vm.Canvas.GetVisibleElementsAt(point.X, point.Y).Reverse().ToList();
        if (candidates.Count == 0)
        {
            return false;
        }

        var currentIndex = Vm.Canvas.SelectedElement is { } current
            ? candidates.IndexOf(current)
            : -1;
        var nextIndex = reverse
            ? currentIndex <= 0 ? candidates.Count - 1 : currentIndex - 1
            : currentIndex < 0 || currentIndex == candidates.Count - 1 ? 0 : currentIndex + 1;
        var next = candidates[nextIndex];
        Vm.SelectElement(next);
        Vm.StatusText = $"Selected {next.DisplayName} (overlap {nextIndex + 1}/{candidates.Count}).";
        return true;
    }

    private async Task<bool> TryOpenQuickContentEditorAsync(DesignElement element)
    {
        if (Vm is null || !Vm.TryGetSelectedQuickContent(out var state))
        {
            return false;
        }

        var updatedContent = await ShowTextEditorDialogAsync(
            $"Quick Edit - {state.ControlName}",
            state.Content,
            $"Edit the visible {state.PropertyName} for {state.ControlKind}.",
            state.IsMultiline);
        if (updatedContent is not null)
        {
            Vm.SetSelectedQuickContent(updatedContent);
        }

        return true;
    }

    private void OnHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Rectangle { Tag: string tag } handle)
        {
            return;
        }

        if (TryBeginViewportPan(handle, e))
        {
            return;
        }

        if (Vm is null || !CanResizeSelection())
        {
            Vm?.StatusText = Vm?.Canvas.SelectedElements.Any(element => element.IsContainerChild)
                == true
                ? "Only root controls or siblings inside the same Canvas can be resized together."
                : "Selected controls cannot be resized together.";
            e.Handled = true;
            return;
        }

        var target = _boundElement ?? Vm.Canvas.SelectedElements.LastOrDefault();
        if (target is null)
        {
            return;
        }

        var mode = tag switch
        {
            "N" => DragMode.N,
            "S" => DragMode.S,
            "E" => DragMode.E,
            "W" => DragMode.W,
            "NE" => DragMode.NE,
            "NW" => DragMode.NW,
            "SE" => DragMode.SE,
            "SW" => DragMode.SW,
            _ => DragMode.None,
        };

        if (mode == DragMode.None)
        {
            return;
        }

        BeginDrag(mode, target, e);
        e.Handled = true;
    }

    private void BeginDrag(DragMode mode, DesignElement target, PointerPressedEventArgs e)
    {
        _dragMode = mode;
        _dragTarget = target;
        _dragStart = e.GetPosition(DesignHost);
        _origX = target.X;
        _origY = target.Y;
        _origW = target.Width;
        _origH = target.Height;
        _dragOrigins.Clear();
        _selectionResizeOrigins.Clear();
        _isSelectionResize = false;
        if (mode == DragMode.Move && Vm is not null)
        {
            foreach (var selected in Vm.Canvas.SelectedElements)
            {
                _dragOrigins[selected] = new Point(selected.X, selected.Y);
            }
        }
        else if (mode != DragMode.Move
            && Vm is not null
            && Vm.Canvas.SelectedElements.Count > 1
            && TryGetSelectionBounds(out _selectionResizeBounds))
        {
            _isSelectionResize = true;
            foreach (var selected in Vm.Canvas.SelectedElements)
            {
                _selectionResizeOrigins[selected] = new Rect(
                    selected.X,
                    selected.Y,
                    selected.Width,
                    selected.Height);
            }
        }

        Vm?.BeginCanvasMutation(MainWindowViewModel.HistoryActionType.TransformElement, "Updated element position/size.");
        e.Pointer.Capture(DesignHost);
    }

    private void OnDesignRulerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not DesignerRuler ruler
            || !e.GetCurrentPoint(ruler).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _guideOrientation = ruler.Orientation == Orientation.Horizontal
            ? GuideOrientation.Vertical
            : GuideOrientation.Horizontal;
        var coordinate = GetGuideCoordinate(ruler, e.GetPosition(ruler));
        var guides = GetGuideCollection(_guideOrientation);
        _guideIndex = FindGuideIndex(guides, coordinate);
        if (_guideIndex < 0)
        {
            _guideIndex = guides.Count;
            guides.Add(coordinate);
        }

        _isDraggingGuide = true;
        UpdateGuideOverlay();
        e.Pointer.Capture(DesignViewport);
        e.Handled = true;
    }

    private List<double> GetGuideCollection(GuideOrientation orientation)
        => orientation == GuideOrientation.Horizontal ? _horizontalGuides : _verticalGuides;

    private static int FindGuideIndex(IReadOnlyList<double> guides, double coordinate)
    {
        var index = -1;
        var distance = GuideHitThreshold;
        for (var i = 0; i < guides.Count; i++)
        {
            var candidateDistance = Math.Abs(guides[i] - coordinate);
            if (candidateDistance <= distance)
            {
                index = i;
                distance = candidateDistance;
            }
        }

        return index;
    }

    private void CaptureViewportForTab(DocumentTabViewModel? tab)
    {
        if (tab is null || _isRestoringViewport)
        {
            return;
        }

        var offset = DesignScrollViewer.Offset;
        tab.ViewportState = new DocumentViewportState(
            NormalizeViewportOffset(offset.X),
            NormalizeViewportOffset(offset.Y));
    }

    private void RestoreViewportForTab(DocumentTabViewModel? tab)
    {
        var state = tab?.ViewportState ?? DocumentViewportState.Default;
        var restoreVersion = ++_viewportRestoreVersion;
        _isViewportRestorePending = true;

        void ApplyViewportOffset()
        {
            if (restoreVersion != _viewportRestoreVersion)
            {
                return;
            }

            var viewport = DesignScrollViewer.Viewport;
            var viewportWidth = NormalizeViewportDimension(viewport.Width);
            var viewportHeight = NormalizeViewportDimension(viewport.Height);
            var extentWidth = NormalizeViewportDimension(DesignScrollViewer.Extent.Width);
            var extentHeight = NormalizeViewportDimension(DesignScrollViewer.Extent.Height);
            var maxX = Math.Max(0, extentWidth - viewportWidth);
            var maxY = Math.Max(0, extentHeight - viewportHeight);
            var offset = new Vector(
                Math.Clamp(state.HorizontalOffset, 0, maxX),
                Math.Clamp(state.VerticalOffset, 0, maxY));

            _isRestoringViewport = true;
            try
            {
                DesignScrollViewer.Offset = offset;
            }
            finally
            {
                _isRestoringViewport = false;
            }
        }

        void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (restoreVersion != _viewportRestoreVersion)
            {
                DesignScrollViewer.LayoutUpdated -= OnLayoutUpdated;
                return;
            }

            ApplyViewportOffset();
            DesignScrollViewer.LayoutUpdated -= OnLayoutUpdated;
        }

        DesignScrollViewer.LayoutUpdated += OnLayoutUpdated;
        ApplyViewportOffset();
        Dispatcher.UIThread.Post(ApplyViewportOffset, DispatcherPriority.Normal);
        Dispatcher.UIThread.Post(
            () =>
            {
                if (restoreVersion != _viewportRestoreVersion)
                {
                    return;
                }

                ApplyViewportOffset();
                _isViewportRestorePending = false;
            },
            DispatcherPriority.ApplicationIdle);
    }

    private static double NormalizeViewportOffset(double value)
        => double.IsFinite(value) && value > 0 ? value : 0;

    private static double NormalizeViewportDimension(double value)
        => double.IsFinite(value) && value > 0 ? value : 0;

    private static double GetGuideCoordinate(DesignerRuler ruler, Point point)
    {
        var position = ruler.Orientation == Orientation.Horizontal ? point.X : point.Y;
        return (ruler.ScrollOffset + position) / Math.Max(0.01, ruler.ZoomScale);
    }

    private double GetGuideCoordinate(Point viewportPoint)
    {
        var zoom = Math.Max(0.01, Vm?.Canvas.ZoomScale ?? 1);
        var offset = DesignScrollViewer.Offset;
        var position = _guideOrientation == GuideOrientation.Vertical
            ? viewportPoint.X
            : viewportPoint.Y;
        var scrollOffset = _guideOrientation == GuideOrientation.Vertical ? offset.X : offset.Y;
        return (scrollOffset + position) / zoom;
    }

    private void UpdateGuidePosition(Point viewportPoint)
    {
        if (!_isDraggingGuide || _guideIndex < 0)
        {
            return;
        }

        var guides = GetGuideCollection(_guideOrientation);
        if (_guideIndex >= guides.Count)
        {
            return;
        }

        guides[_guideIndex] = GetGuideCoordinate(viewportPoint);
        UpdateGuideOverlay();
    }

    private void CompleteGuideDrag(Point viewportPoint)
    {
        if (!_isDraggingGuide)
        {
            return;
        }

        var guides = GetGuideCollection(_guideOrientation);
        if (_guideIndex >= 0 && _guideIndex < guides.Count)
        {
            var coordinate = GetGuideCoordinate(viewportPoint);
            var maximum = _guideOrientation == GuideOrientation.Vertical
                ? Vm?.Canvas.ArtboardWidth ?? 0
                : Vm?.Canvas.ArtboardHeight ?? 0;
            guides[_guideIndex] = coordinate;
            if (coordinate < 0 || coordinate > maximum)
            {
                guides.RemoveAt(_guideIndex);
            }
        }

        _isDraggingGuide = false;
        _guideIndex = -1;
        UpdateGuideOverlay();
    }

    private bool TryBeginViewportPan(Control host, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(host);
        var isPanTool = _isPanToolActive && point.Properties.IsLeftButtonPressed;
        var isSpacePan = _isSpacePanModifier && point.Properties.IsLeftButtonPressed;
        if (!point.Properties.IsMiddleButtonPressed && !isSpacePan && !isPanTool)
        {
            return false;
        }

        _isPanningViewport = true;
        _isSpacePanGesture = isSpacePan && !isPanTool;
        _panStart = e.GetPosition(DesignScrollViewer);
        _panStartOffset = DesignScrollViewer.Offset;
        UpdatePanCursor();
        e.Pointer.Capture(DesignViewport);
        e.Handled = true;
        return true;
    }

    private void OnDesignViewportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control host)
        {
            UpdateViewportCursor(e.GetPosition(DesignScrollViewer));
            TryBeginViewportPan(host, e);
        }
    }

    private void OnDesignViewportPointerMoved(object? sender, PointerEventArgs e)
    {
        if (TryEndReleasedSpacePan(e))
        {
            return;
        }

        var point = e.GetPosition(DesignScrollViewer);
        if (_isDraggingGuide)
        {
            HideToolboxPlacementPreview();
            TryAutoPanDesignViewport(point);
            UpdateGuidePosition(point);
            e.Handled = true;
            return;
        }

        UpdateViewportCursor(point);
        if (!_isPanningViewport)
        {
            if (Vm?.Toolbox.SelectedItem is not null)
            {
                TryAutoPanDesignViewport(point);
            }

            UpdateToolboxPlacementPreview(e.GetPosition(DesignSurface));
            return;
        }

        HideToolboxPlacementPreview();
        UpdateViewportPan(point);
        e.Handled = true;
    }

    private void OnDesignViewportPointerExited(object? sender, PointerEventArgs e)
    {
        if (!_isDraggingGuide)
        {
            ClearViewportCursor();
        }

        HideToolboxPlacementPreview();
    }

    private void OnDesignViewportPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingGuide)
        {
            CompleteGuideDrag(e.GetPosition(DesignScrollViewer));
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (!_isPanningViewport)
        {
            return;
        }

        _isPanningViewport = false;
        _isSpacePanGesture = false;
        UpdatePanCursor();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnDesignViewportPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (Vm is null || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        var delta = Math.Abs(e.Delta.Y) >= double.Epsilon ? e.Delta.Y : e.Delta.X;
        if (Math.Abs(delta) < double.Epsilon)
        {
            return;
        }

        ZoomViewportAtPointer(delta, e.GetPosition(DesignScrollViewer));
        e.Handled = true;
    }

    private void ZoomViewportAtCenter(double wheelDelta)
    {
        if (Vm is null)
        {
            return;
        }

        var zoomScale = wheelDelta > 0
            ? Vm.Canvas.ZoomScale + 0.1
            : Vm.Canvas.ZoomScale - 0.1;
        SetZoomViewportAtCenter(zoomScale);
    }

    private void SetZoomViewportAtCenter(double zoomScale)
    {
        var viewport = DesignScrollViewer.Viewport;
        var width = viewport.Width > 0 ? viewport.Width : DesignViewport.Bounds.Width;
        var height = viewport.Height > 0 ? viewport.Height : DesignViewport.Bounds.Height;
        SetZoomViewportAtPointer(zoomScale, new Point(width / 2, height / 2));
        UpdateZoomStatus();
    }

    private void ZoomViewportAtPointer(double wheelDelta, Point pointer)
    {
        if (Vm is null)
        {
            return;
        }

        var zoomScale = wheelDelta > 0
            ? Vm.Canvas.ZoomScale + 0.1
            : Vm.Canvas.ZoomScale - 0.1;
        SetZoomViewportAtPointer(zoomScale, pointer);
    }

    private void SetZoomViewportAtPointer(double zoomScale, Point pointer)
    {
        if (Vm is null)
        {
            return;
        }

        var oldZoom = Vm.Canvas.ZoomScale;
        var newZoom = Math.Clamp(zoomScale, 0.25, 2);
        if (Math.Abs(newZoom - oldZoom) < double.Epsilon)
        {
            return;
        }

        var documentPoint = new Point(
            (DesignScrollViewer.Offset.X + pointer.X) / oldZoom,
            (DesignScrollViewer.Offset.Y + pointer.Y) / oldZoom);

        Vm.Canvas.SetZoomScale(newZoom);

        RestoreViewportDocumentPoint(documentPoint, pointer);
    }

    private bool TryGetViewportDocumentPointAtCenter(
        out Point documentPoint,
        out Point viewportPoint)
    {
        documentPoint = default;
        viewportPoint = default;
        if (Vm is null || !double.IsFinite(Vm.Canvas.ZoomScale) || Vm.Canvas.ZoomScale <= 0)
        {
            return false;
        }

        var viewport = DesignScrollViewer.Viewport;
        var width = viewport.Width > 0 ? viewport.Width : DesignViewport.Bounds.Width;
        var height = viewport.Height > 0 ? viewport.Height : DesignViewport.Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        viewportPoint = new Point(width / 2, height / 2);
        documentPoint = new Point(
            (DesignScrollViewer.Offset.X + viewportPoint.X) / Vm.Canvas.ZoomScale,
            (DesignScrollViewer.Offset.Y + viewportPoint.Y) / Vm.Canvas.ZoomScale);
        return true;
    }

    private void RestoreViewportDocumentPoint(Point documentPoint, Point viewportPoint)
    {
        if (Vm is null || !double.IsFinite(Vm.Canvas.ZoomScale) || Vm.Canvas.ZoomScale <= 0)
        {
            return;
        }

        void ApplyViewportOffset()
        {
            var viewport = DesignScrollViewer.Viewport;
            var width = viewport.Width > 0 ? viewport.Width : DesignViewport.Bounds.Width;
            var height = viewport.Height > 0 ? viewport.Height : DesignViewport.Bounds.Height;
            var maxX = Math.Max(0, DesignScrollViewer.Extent.Width - width);
            var maxY = Math.Max(0, DesignScrollViewer.Extent.Height - height);
            DesignScrollViewer.Offset = new Vector(
                Math.Clamp(documentPoint.X * Vm.Canvas.ZoomScale - viewportPoint.X, 0, maxX),
                Math.Clamp(documentPoint.Y * Vm.Canvas.ZoomScale - viewportPoint.Y, 0, maxY));
        }

        ApplyViewportOffset();
        EventHandler? layoutUpdated = null;
        layoutUpdated = (_, _) =>
        {
            DesignScrollViewer.LayoutUpdated -= layoutUpdated;
            ApplyViewportOffset();
        };
        DesignScrollViewer.LayoutUpdated += layoutUpdated;
        Dispatcher.UIThread.Post(ApplyViewportOffset, DispatcherPriority.Normal);
    }

    private void UpdateViewportPan(Point current)
    {
        var delta = current - _panStart;
        DesignScrollViewer.Offset = new Vector(
            _panStartOffset.X - delta.X,
            _panStartOffset.Y - delta.Y);
    }

    private void PanViewportBy(Vector delta)
    {
        var viewport = DesignScrollViewer.Viewport;
        var extent = DesignScrollViewer.Extent;
        var maxX = Math.Max(0, extent.Width - viewport.Width);
        var maxY = Math.Max(0, extent.Height - viewport.Height);
        DesignScrollViewer.Offset = new Vector(
            Math.Clamp(DesignScrollViewer.Offset.X + delta.X, 0, maxX),
            Math.Clamp(DesignScrollViewer.Offset.Y + delta.Y, 0, maxY));
    }

    private bool TryEndReleasedSpacePan(PointerEventArgs e)
    {
        if (!_isPanningViewport || !_isSpacePanGesture || _isSpacePanModifier)
        {
            return false;
        }

        _isPanningViewport = false;
        _isSpacePanGesture = false;
        UpdatePanCursor();
        e.Pointer.Capture(null);
        e.Handled = true;
        return true;
    }

    private void OnDragPointerMoved(object? sender, PointerEventArgs e)
    {
        if (TryEndReleasedSpacePan(e))
        {
            return;
        }

        if (_isPanningViewport)
        {
            HideToolboxPlacementPreview();
            UpdateViewportPan(e.GetPosition(DesignScrollViewer));
            e.Handled = true;
            return;
        }

        if (_isMarqueeSelecting)
        {
            HideToolboxPlacementPreview();
            TryAutoPanDesignViewport(e.GetPosition(DesignViewport));
            UpdateMarquee(e.GetPosition(DesignHost));
            return;
        }

        if (_dragMode == DragMode.None || _dragTarget is null)
        {
            UpdateToolboxPlacementPreview(e.GetPosition(DesignHost));
            return;
        }

        HideToolboxPlacementPreview();
        TryAutoPanDesignViewport(e.GetPosition(DesignViewport));
        var p = e.GetPosition(DesignHost);
        var dx = p.X - _dragStart.X;
        var dy = p.Y - _dragStart.Y;

        ApplyDrag(dx, dy, e.KeyModifiers);
    }

    private void ApplyDrag(double dx, double dy)
        => ApplyDrag(dx, dy, KeyModifiers.None);

    private void ApplyDrag(double dx, double dy, KeyModifiers modifiers)
    {
        if (_dragTarget is null)
        {
            return;
        }

        if (_isSelectionResize)
        {
            ApplySelectionResize(dx, dy, modifiers);
            return;
        }

        switch (_dragMode)
        {
            case DragMode.Move:
                var origin = _dragOrigins.TryGetValue(_dragTarget, out var dragOrigin)
                    ? dragOrigin
                    : new Point(_origX, _origY);
                var position = GetSmartSnappedPosition(
                    Math.Max(0, SnapPosition(origin.X + dx)),
                    Math.Max(0, SnapPosition(origin.Y + dy)));
                foreach (var (element, elementOrigin) in _dragOrigins)
                {
                    element.X = Math.Max(0, position.X + (elementOrigin.X - dragOrigin.X));
                    element.Y = Math.Max(0, position.Y + (elementOrigin.Y - dragOrigin.Y));
                }
                break;
            case DragMode.E:
                _dragTarget.Width = SnapSize(_origW + dx);
                break;
            case DragMode.S:
                _dragTarget.Height = SnapSize(_origH + dy);
                break;
            case DragMode.W:
                ResizeLeft(dx);
                break;
            case DragMode.N:
                ResizeTop(dy);
                break;
            case DragMode.SE:
                _dragTarget.Width = SnapSize(_origW + dx);
                _dragTarget.Height = SnapSize(_origH + dy);
                break;
            case DragMode.NE:
                _dragTarget.Width = SnapSize(_origW + dx);
                ResizeTop(dy);
                break;
            case DragMode.SW:
                ResizeLeft(dx);
                _dragTarget.Height = SnapSize(_origH + dy);
                break;
            case DragMode.NW:
                ResizeLeft(dx);
                ResizeTop(dy);
                break;
        }

        if (_dragMode != DragMode.Move)
        {
            ApplySmartResizeSnap(modifiers);
        }
    }

    private void ApplySelectionResize(double dx, double dy)
        => ApplySelectionResize(dx, dy, KeyModifiers.None);

    private void ApplySelectionResize(double dx, double dy, KeyModifiers modifiers)
    {
        if (Vm is null || _selectionResizeOrigins.Count == 0)
        {
            return;
        }

        var requested = GetGridResizeBounds(_selectionResizeBounds, _dragMode, dx, dy);
        var aspectLocked = modifiers.HasFlag(KeyModifiers.Shift) && IsCornerResizeMode(_dragMode);
        if (aspectLocked)
        {
            requested = GetAspectLockedResizeBounds(_selectionResizeBounds, requested, _dragMode);
        }

        var snapped = GetSmartResizeBounds(requested, _dragMode, out var guideX, out var guideY);
        if (aspectLocked)
        {
            snapped = GetAspectLockedResizeBounds(_selectionResizeBounds, snapped, _dragMode);
            guideX = null;
            guideY = null;
        }
        var scaleX = snapped.Width / Math.Max(MinSize, _selectionResizeBounds.Width);
        var scaleY = snapped.Height / Math.Max(MinSize, _selectionResizeBounds.Height);
        foreach (var (element, origin) in _selectionResizeOrigins)
        {
            element.X = Math.Max(0, snapped.X + ((origin.X - _selectionResizeBounds.X) * scaleX));
            element.Y = Math.Max(0, snapped.Y + ((origin.Y - _selectionResizeBounds.Y) * scaleY));
            element.Width = Math.Max(MinSize, origin.Width * scaleX);
            element.Height = Math.Max(MinSize, origin.Height * scaleY);
        }

        UpdateSmartSnapGuides(guideX, guideY);
    }

    private void ApplySelectionBounds(Rect original, Rect target)
    {
        if (Vm is null
            || Vm.Canvas.SelectedElements.Count < 2
            || original.Width <= 0
            || original.Height <= 0
            || (original.Position == target.Position && original.Size == target.Size))
        {
            return;
        }

        var selected = Vm.Canvas.SelectedElements.ToList();
        Vm.BeginCanvasMutation(
            MainWindowViewModel.HistoryActionType.TransformElement,
            "Updated selection bounds.");

        var scaleX = target.Width / original.Width;
        var scaleY = target.Height / original.Height;
        foreach (var element in selected)
        {
            element.X = Math.Max(0, target.X + ((element.X - original.X) * scaleX));
            element.Y = Math.Max(0, target.Y + ((element.Y - original.Y) * scaleY));
            element.Width = Math.Max(MinSize, element.Width * scaleX);
            element.Height = Math.Max(MinSize, element.Height * scaleY);
        }

        Vm.CommitCanvasMutation();
        Vm.StatusText = $"Updated bounds for {selected.Count} controls to {FormatSelectionBounds(target)}.";
    }

    private static string FormatSelectionBounds(Rect bounds)
        => $"X:{bounds.X.ToString("0.###", CultureInfo.InvariantCulture)} "
            + $"Y:{bounds.Y.ToString("0.###", CultureInfo.InvariantCulture)} "
            + $"W:{bounds.Width.ToString("0.###", CultureInfo.InvariantCulture)} "
            + $"H:{bounds.Height.ToString("0.###", CultureInfo.InvariantCulture)}";

    private Rect GetGridResizeBounds(Rect original, DragMode mode, double dx, double dy)
    {
        var left = original.X;
        var top = original.Y;
        var right = original.Right;
        var bottom = original.Bottom;

        if (mode is DragMode.W or DragMode.NW or DragMode.SW)
        {
            var width = SnapSize(original.Width - dx);
            left = Math.Max(0, SnapPosition(original.Right - width));
        }
        else if (mode is DragMode.E or DragMode.NE or DragMode.SE)
        {
            right = original.X + SnapSize(original.Width + dx);
        }

        if (mode is DragMode.N or DragMode.NW or DragMode.NE)
        {
            var height = SnapSize(original.Height - dy);
            top = Math.Max(0, SnapPosition(original.Bottom - height));
        }
        else if (mode is DragMode.S or DragMode.SW or DragMode.SE)
        {
            bottom = original.Y + SnapSize(original.Height + dy);
        }

        return new Rect(
            left,
            top,
            Math.Max(MinSize, right - left),
            Math.Max(MinSize, bottom - top));
    }

    private static bool IsCornerResizeMode(DragMode mode)
        => mode is DragMode.NW or DragMode.NE or DragMode.SE or DragMode.SW;

    private static Rect GetAspectLockedResizeBounds(Rect original, Rect requested, DragMode mode)
    {
        if (!IsCornerResizeMode(mode)
            || original.Width <= 0
            || original.Height <= 0)
        {
            return requested;
        }

        var widthScale = requested.Width / original.Width;
        var heightScale = requested.Height / original.Height;
        var scale = Math.Abs(widthScale - 1) >= Math.Abs(heightScale - 1)
            ? widthScale
            : heightScale;
        scale = Math.Max(scale, Math.Max(MinSize / original.Width, MinSize / original.Height));
        var width = Math.Max(MinSize, original.Width * scale);
        var height = Math.Max(MinSize, original.Height * scale);
        var left = mode is DragMode.NW or DragMode.SW
            ? original.Right - width
            : original.X;
        var top = mode is DragMode.NW or DragMode.NE
            ? original.Bottom - height
            : original.Y;
        return new Rect(left, top, width, height);
    }

    private void ResizeLeft(double dx)
    {
        if (_dragTarget is null)
        {
            return;
        }

        var newW = _origW - dx;
        if (newW < MinSize)
        {
            _dragTarget.Width = MinSize;
            _dragTarget.X = Math.Max(0, SnapPosition(_origX + (_origW - MinSize)));
        }
        else
        {
            _dragTarget.Width = SnapSize(newW);
            _dragTarget.X = Math.Max(0, SnapPosition(_origX + (_origW - _dragTarget.Width)));
        }
    }

    private void ResizeTop(double dy)
    {
        if (_dragTarget is null)
        {
            return;
        }

        var newH = _origH - dy;
        if (newH < MinSize)
        {
            _dragTarget.Height = MinSize;
            _dragTarget.Y = Math.Max(0, SnapPosition(_origY + (_origH - MinSize)));
        }
        else
        {
            _dragTarget.Height = SnapSize(newH);
            _dragTarget.Y = Math.Max(0, SnapPosition(_origY + (_origH - _dragTarget.Height)));
        }
    }

    private double SnapPosition(double value) => Vm?.Canvas.SnapPosition(value) ?? value;

    private Point GetSmartSnappedPosition(double x, double y)
    {
        if (_dragTarget is null || Vm is null)
        {
            HideSmartSnapGuides();
            return new Point(x, y);
        }

        double? guideX = null;
        double? guideY = null;
        var bestX = SmartSnapThreshold + 1;
        var bestY = SmartSnapThreshold + 1;
        var snappedX = x;
        var snappedY = y;
        var movingX = new[] { x, x + _dragTarget.Width / 2, x + _dragTarget.Width };
        var movingY = new[] { y, y + _dragTarget.Height / 2, y + _dragTarget.Height };
        var candidatesX = GetSmartSnapCandidates(horizontal: true);
        var candidatesY = GetSmartSnapCandidates(horizontal: false);

        foreach (var candidate in candidatesX)
        {
            foreach (var moving in movingX)
            {
                var distance = Math.Abs(candidate - moving);
                if (distance < bestX)
                {
                    bestX = distance;
                    snappedX = x + candidate - moving;
                    guideX = candidate;
                }
            }
        }

        foreach (var candidate in candidatesY)
        {
            foreach (var moving in movingY)
            {
                var distance = Math.Abs(candidate - moving);
                if (distance < bestY)
                {
                    bestY = distance;
                    snappedY = y + candidate - moving;
                    guideY = candidate;
                }
            }
        }

        UpdateSmartSnapGuides(guideX, guideY);
        return new Point(snappedX, snappedY);
    }

    private List<double> GetSmartSnapCandidates(bool horizontal)
    {
        if (Vm is null)
        {
            return new List<double>();
        }

        var extent = horizontal ? Vm.Canvas.ArtboardWidth : Vm.Canvas.ArtboardHeight;
        var candidates = new List<double> { 0, extent / 2, extent };
        if (_snapToGuides)
        {
            candidates.AddRange(horizontal ? _verticalGuides : _horizontalGuides);
        }

        foreach (var element in Vm.Canvas.Elements)
        {
            if (ReferenceEquals(element, _dragTarget)
                || _dragOrigins.ContainsKey(element)
                || _selectionResizeOrigins.ContainsKey(element))
            {
                continue;
            }

            if (horizontal)
            {
                candidates.Add(element.X);
                candidates.Add(element.X + element.Width / 2);
                candidates.Add(element.X + element.Width);
            }
            else
            {
                candidates.Add(element.Y);
                candidates.Add(element.Y + element.Height / 2);
                candidates.Add(element.Y + element.Height);
            }
        }

        return candidates;
    }

    private void ApplySmartResizeSnap()
        => ApplySmartResizeSnap(KeyModifiers.None);

    private void ApplySmartResizeSnap(KeyModifiers modifiers)
    {
        if (_dragTarget is null || Vm is null)
        {
            HideSmartSnapGuides();
            return;
        }

        var requested = new Rect(
            _dragTarget.X,
            _dragTarget.Y,
            _dragTarget.Width,
            _dragTarget.Height);
        var original = new Rect(_origX, _origY, _origW, _origH);
        var aspectLocked = modifiers.HasFlag(KeyModifiers.Shift) && IsCornerResizeMode(_dragMode);
        if (aspectLocked)
        {
            requested = GetAspectLockedResizeBounds(original, requested, _dragMode);
        }

        var snapped = GetSmartResizeBounds(requested, _dragMode, out var guideX, out var guideY);
        if (aspectLocked)
        {
            snapped = GetAspectLockedResizeBounds(original, snapped, _dragMode);
            guideX = null;
            guideY = null;
        }
        if (_dragMode is DragMode.W or DragMode.NW or DragMode.SW)
        {
            _dragTarget.X = snapped.X;
        }

        if (_dragMode is DragMode.N or DragMode.NW or DragMode.NE)
        {
            _dragTarget.Y = snapped.Y;
        }

        _dragTarget.Width = snapped.Width;
        _dragTarget.Height = snapped.Height;
        UpdateSmartSnapGuides(guideX, guideY);
    }

    private Rect GetSmartResizeBounds(
        Rect requested,
        DragMode mode,
        out double? guideX,
        out double? guideY)
    {
        var left = requested.X;
        var top = requested.Y;
        var right = requested.Right;
        var bottom = requested.Bottom;
        guideX = null;
        guideY = null;

        if (mode is DragMode.W or DragMode.NW or DragMode.SW)
        {
            left = SnapResizeEdge(left, horizontal: true, out guideX);
        }
        else if (mode is DragMode.E or DragMode.NE or DragMode.SE)
        {
            right = SnapResizeEdge(right, horizontal: true, out guideX);
        }

        if (mode is DragMode.N or DragMode.NW or DragMode.NE)
        {
            top = SnapResizeEdge(top, horizontal: false, out guideY);
        }
        else if (mode is DragMode.S or DragMode.SW or DragMode.SE)
        {
            bottom = SnapResizeEdge(bottom, horizontal: false, out guideY);
        }

        if (mode is DragMode.W or DragMode.NW or DragMode.SW)
        {
            left = Math.Max(0, left);
            if (right - left < MinSize)
            {
                left = Math.Max(0, right - MinSize);
                right = Math.Max(right, left + MinSize);
            }
        }
        else if (mode is DragMode.E or DragMode.NE or DragMode.SE)
        {
            right = Math.Max(left + MinSize, right);
        }

        if (mode is DragMode.N or DragMode.NW or DragMode.NE)
        {
            top = Math.Max(0, top);
            if (bottom - top < MinSize)
            {
                top = Math.Max(0, bottom - MinSize);
                bottom = Math.Max(bottom, top + MinSize);
            }
        }
        else if (mode is DragMode.S or DragMode.SW or DragMode.SE)
        {
            bottom = Math.Max(top + MinSize, bottom);
        }

        return new Rect(
            left,
            top,
            Math.Max(MinSize, right - left),
            Math.Max(MinSize, bottom - top));
    }

    private double SnapResizeEdge(double edge, bool horizontal, out double? snappedGuide)
    {
        var bestDistance = SmartSnapThreshold + 1;
        var snappedEdge = edge;
        snappedGuide = null;
        foreach (var candidate in GetSmartSnapCandidates(horizontal))
        {
            var distance = Math.Abs(candidate - edge);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                snappedEdge = candidate;
                snappedGuide = candidate;
            }
        }

        return snappedEdge;
    }

    private void UpdateSmartSnapGuides(double? x, double? y)
    {
        SmartSnapVertical.IsVisible = x.HasValue;
        SmartSnapHorizontal.IsVisible = y.HasValue;
        if (x.HasValue)
        {
            Canvas.SetLeft(SmartSnapVertical, x.Value);
            SmartSnapVertical.Height = Vm?.Canvas.ArtboardHeight ?? 0;
        }

        if (y.HasValue)
        {
            Canvas.SetTop(SmartSnapHorizontal, y.Value);
            SmartSnapHorizontal.Width = Vm?.Canvas.ArtboardWidth ?? 0;
        }
    }

    private void HideSmartSnapGuides()
    {
        SmartSnapVertical.IsVisible = false;
        SmartSnapHorizontal.IsVisible = false;
    }

    private double SnapSize(double value) => Vm?.Canvas.SnapSize(value, MinSize) ?? Math.Max(MinSize, value);

    private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanningViewport)
        {
            _isPanningViewport = false;
            _isSpacePanGesture = false;
            UpdatePanCursor();
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (_isMarqueeSelecting)
        {
            CompleteMarquee(e.GetPosition(DesignHost));
            _isMarqueeSelecting = false;
            MarqueeRectangle.IsVisible = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (_dragMode == DragMode.None)
        {
            e.Pointer.Capture(null);
            return;
        }

        _dragMode = DragMode.None;
        _dragTarget = null;
        _dragOrigins.Clear();
        _selectionResizeOrigins.Clear();
        _selectionResizeBounds = default;
        _isSelectionResize = false;
        HideSmartSnapGuides();
        e.Pointer.Capture(null);
        e.Handled = true;

        Vm?.CommitCanvasMutation();
    }

    private void UpdateMarquee(Point current)
    {
        var area = GetMarqueeArea(current);
        Canvas.SetLeft(MarqueeRectangle, area.X);
        Canvas.SetTop(MarqueeRectangle, area.Y);
        MarqueeRectangle.Width = area.Width;
        MarqueeRectangle.Height = area.Height;
        MarqueeRectangle.IsVisible = area.Width >= MarqueeThreshold || area.Height >= MarqueeThreshold;
    }

    private void CompleteMarquee(Point current)
    {
        if (Vm is null)
        {
            return;
        }

        var area = GetMarqueeArea(current);
        if (area.Width < MarqueeThreshold && area.Height < MarqueeThreshold)
        {
            if (!_marqueeAdditive && !_marqueeSubtractive)
            {
                Vm.SelectElements(Array.Empty<DesignElement>());
            }

            return;
        }

        // Match common design-tool marquee behavior: left-to-right contains, right-to-left crosses.
        var requiresContainment = current.X >= _marqueeStart.X;
        var selected = Vm.Canvas.Elements
            .Where(element => !element.IsLocked && Vm.Canvas.IsElementVisibleOnCanvas(element))
            .Where(element =>
            {
                var bounds = new Rect(element.X, element.Y, element.Width, element.Height);
                return requiresContainment
                    ? area.Contains(bounds)
                    : area.Intersects(bounds);
            })
            .ToList();
        if (_marqueeSubtractive)
        {
            Vm.SelectElements(Vm.Canvas.SelectedElements.Except(selected));
        }
        else
        {
            Vm.SelectElements(selected, _marqueeAdditive);
        }
    }

    private static bool IsAdditiveMarqueeModifier(KeyModifiers modifiers)
        => modifiers.HasFlag(KeyModifiers.Control)
            || modifiers.HasFlag(KeyModifiers.Shift);

    private static bool IsSubtractiveMarqueeModifier(KeyModifiers modifiers)
        => modifiers.HasFlag(KeyModifiers.Alt);

    private Rect GetMarqueeArea(Point current)
    {
        var x = Math.Min(_marqueeStart.X, current.X);
        var y = Math.Min(_marqueeStart.Y, current.Y);
        var width = Math.Abs(current.X - _marqueeStart.X);
        var height = Math.Abs(current.Y - _marqueeStart.Y);
        return new Rect(x, y, width, height);
    }

    private async Task OpenStorageFileAsync(IStorageFile file)
    {
        if (Vm is null)
        {
            return;
        }

        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        var localPath = file.TryGetLocalPath();
        if (!Vm.TryOpenDocumentTab(content, localPath, out var error, out var warning))
        {
            Vm.StatusText = $"Open failed: {error}";
            return;
        }

        ClearDesignGuides();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            Vm.StatusText = BuildOpenStatus(System.IO.Path.GetFileName(localPath), warning);
        }
        else
        {
            Vm.StatusText = BuildOpenStatus(file.Name, warning);
        }
    }

    private static string BuildOpenStatus(string name, string warning)
        => string.IsNullOrEmpty(warning) ? $"Opened {name}" : $"Opened {name}. {warning}";

    private async Task<bool> SaveDocumentAsync(bool forceSaveAs)
    {
        if (Vm is null)
        {
            return false;
        }

        FlushPendingPropertyHistory();

        var axaml = Vm.ExportFullAxaml();
        var targetPath = forceSaveAs ? null : Vm.CurrentDocumentPath;
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save AXAML",
                SuggestedFileName = "design-draft.axaml",
                DefaultExtension = "axaml",
                FileTypeChoices =
                [
                    new FilePickerFileType("AXAML") { Patterns = ["*.axaml"] }
                ]
            });

            if (file is null)
            {
                return false;
            }

            var pickedPath = file.TryGetLocalPath();
            try
            {
                if (!string.IsNullOrWhiteSpace(pickedPath))
                {
                    await AtomicFileWriter.WriteAllTextAsync(
                        pickedPath,
                        axaml,
                        GetDocumentBackupPath(pickedPath));
                    Vm.MarkDocumentSaved(pickedPath);
                    Vm.StatusText = $"Saved {System.IO.Path.GetFileName(pickedPath)}";
                }
                else
                {
                    await using var stream = await file.OpenWriteAsync();
                    stream.SetLength(0);
                    using var writer = new StreamWriter(stream);
                    await writer.WriteAsync(axaml);
                    await writer.FlushAsync();

                    Vm.MarkCurrentStateSaved();
                    Vm.StatusText = $"Saved {file.Name}";
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Vm.StatusText = $"Could not save {file.Name}: {exception.Message}";
                return false;
            }

            return true;
        }

        try
        {
            await AtomicFileWriter.WriteAllTextAsync(
                targetPath,
                axaml,
                GetDocumentBackupPath(targetPath));
            Vm.MarkDocumentSaved(targetPath);
            Vm.StatusText = $"Saved {System.IO.Path.GetFileName(targetPath)}";
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Vm.StatusText = $"Could not save {System.IO.Path.GetFileName(targetPath)}: {exception.Message}";
            return false;
        }
    }

    private async Task<bool> SaveAllDocumentsAsync()
    {
        if (Vm is null)
        {
            return false;
        }

        FlushPendingPropertyHistory();
        var originalTab = Vm.SelectedDocumentTab;
        var dirtyTabs = Vm.DocumentTabs
            .Where(Vm.IsDocumentTabDirty)
            .ToList();
        if (dirtyTabs.Count == 0)
        {
            Vm.StatusText = "All document tabs are already saved.";
            return true;
        }

        foreach (var tab in dirtyTabs)
        {
            if (!ReferenceEquals(Vm.SelectedDocumentTab, tab))
            {
                Vm.ActivateDocumentTab(tab);
            }

            FlushPendingPropertyHistory();
            if (!await SaveDocumentAsync(forceSaveAs: false))
            {
                if (originalTab is not null)
                {
                    RestoreDocumentTabIfPresent(originalTab);
                }

                return false;
            }
        }

        if (originalTab is not null)
        {
            RestoreDocumentTabIfPresent(originalTab);
        }

        Vm.StatusText = $"Saved {dirtyTabs.Count} document tab(s).";
        return true;
    }

    private async Task<bool> EnsureCanContinueWithUnsavedChangesAsync()
    {
        if (Vm is null || !Vm.IsDirty)
        {
            return true;
        }

        var choice = await ShowUnsavedChangesDialogAsync();
        return choice switch
        {
            UnsavedChoice.Discard => true,
            UnsavedChoice.Save => await SaveDocumentAsync(forceSaveAs: false),
            _ => false,
        };
    }

    private async Task<UnsavedChoice> ShowUnsavedChangesDialogAsync()
    {
        var dialog = new Window
        {
            Title = "Unsaved Changes",
            Width = 420,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var message = new TextBlock
        {
            Text = "You have unsaved changes. Save before continuing?",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
        };

        var saveButton = new Button { Content = "Save", MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
        var discardButton = new Button { Content = "Discard", MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 80 };

        saveButton.Click += (_, _) => dialog.Close(UnsavedChoice.Save);
        discardButton.Click += (_, _) => dialog.Close(UnsavedChoice.Discard);
        cancelButton.Click += (_, _) => dialog.Close(UnsavedChoice.Cancel);

        dialog.Content = new DockPanel
        {
            Margin = new Thickness(16),
            Children =
            {
                new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        message,
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Children = { saveButton, discardButton, cancelButton }
                        }
                    }
                }
            }
        };

        return await dialog.ShowDialog<UnsavedChoice>(this);
    }

    private async Task<IReadOnlyList<string>?> ShowItemsEditorDialogAsync(ItemsEditorState state)
    {
        var editor = new TextBox
        {
            Text = string.Join(Environment.NewLine, state.Items),
            AcceptsReturn = true,
            MinHeight = 200,
        };

        var dialog = new Window
        {
            Title = state.Mode == ItemsEditorMode.DataGrid
                ? $"Edit DataGrid Columns - {state.ControlName}"
                : $"Edit Items - {state.ControlName}",
            Width = state.Mode is ItemsEditorMode.Menu or ItemsEditorMode.DataGrid ? 680 : 460,
            Height = state.Mode is ItemsEditorMode.Menu or ItemsEditorMode.DataGrid ? 430 : 380,
            MinWidth = 360,
            MinHeight = 260,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        void ApplyItems()
        {
            var updatedItems = (editor.Text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', state.Mode != ItemsEditorMode.Flat
                    ? StringSplitOptions.None
                    : StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            if (state.Mode == ItemsEditorMode.TreeView
                && !DesignerTreeItemRuntime.TryParseEditorLines(updatedItems, out _, out var error))
            {
                errorText.Text = error;
                return;
            }

            if (state.Mode == ItemsEditorMode.Menu
                && !DesignerMenuItemRuntime.TryParseEditorLines(updatedItems, out _, out var menuError))
            {
                errorText.Text = menuError;
                return;
            }

            if (state.Mode == ItemsEditorMode.DataGrid
                && !DesignerDataGridRuntime.TryParseEditorLines(updatedItems, out _, out var dataGridError))
            {
                errorText.Text = dataGridError;
                return;
            }

            dialog.Close(updatedItems);
        }
        applyButton.Click += (_, _) => ApplyItems();
        editor.KeyDown += (_, e) =>
        {
            HandleTextEditorShortcut(
                e,
                () => dialog.Close(null),
                ApplyItems);
        };

        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = state.Mode switch
                    {
                        ItemsEditorMode.TreeView =>
                            "Use [-] for expanded, [+] for collapsed, and two spaces per child level.",
                        ItemsEditorMode.Menu =>
                            "Use two spaces per child level; --- separator; [x]/[ ] check; (x)/( ) radio with {Group}; | Ctrl+N shortcut.",
                        ItemsEditorMode.DataGrid =>
                            "One column per line: Type | Header | Binding | Width | ReadOnly. Type is Text or CheckBox; Width supports Auto, pixels, *, and N*.",
                        _ => "Enter one item per line. Empty lines are ignored.",
                    },
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                editor,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(editor, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;

        return await dialog.ShowDialog<IReadOnlyList<string>?>(this);
    }

    private async Task ShowDataGridBehaviorPropertiesDialogAsync(
        DataGridBehaviorEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var autoGenerateEditor = new CheckBox
        {
            Content = "Auto-generate columns",
            IsChecked = state.AutoGenerateColumns,
        };
        var readOnlyEditor = new CheckBox
        {
            Content = "Read-only cells",
            IsChecked = state.IsReadOnly,
        };
        var reorderEditor = new CheckBox
        {
            Content = "Allow column reordering",
            IsChecked = state.CanUserReorderColumns,
        };
        var resizeEditor = new CheckBox
        {
            Content = "Allow column resizing",
            IsChecked = state.CanUserResizeColumns,
        };
        var sortEditor = new CheckBox
        {
            Content = "Allow column sorting",
            IsChecked = state.CanUserSortColumns,
        };
        var rowDetailsFrozenEditor = new CheckBox
        {
            Content = "Freeze row details",
            IsChecked = state.AreRowDetailsFrozen,
        };
        var rowGroupsFrozenEditor = new CheckBox
        {
            Content = "Freeze row group headers",
            IsChecked = state.AreRowGroupHeadersFrozen,
        };
        var inertiaEditor = new CheckBox
        {
            Content = "Use scroll inertia",
            IsChecked = state.IsScrollInertiaEnabled,
        };
        var headersEditor = CreateComboBox(
            DesignerDataGridBehaviorRuntime.HeadersVisibilityNames,
            state.HeadersVisibility);
        var gridLinesEditor = CreateComboBox(
            DesignerDataGridBehaviorRuntime.GridLinesVisibilityNames,
            state.GridLinesVisibility);
        var selectionEditor = CreateComboBox(
            DesignerDataGridBehaviorRuntime.SelectionModeNames,
            state.SelectionMode);
        var clipboardEditor = CreateComboBox(
            DesignerDataGridBehaviorRuntime.ClipboardCopyModeNames,
            state.ClipboardCopyMode);
        var horizontalScrollEditor = CreateComboBox(
            DesignerDataGridBehaviorRuntime.ScrollBarVisibilityNames,
            state.HorizontalScrollBarVisibility);
        var verticalScrollEditor = CreateComboBox(
            DesignerDataGridBehaviorRuntime.ScrollBarVisibilityNames,
            state.VerticalScrollBarVisibility);
        var frozenColumnsEditor = new TextBox
        {
            Text = state.FrozenColumnCount,
            Watermark = "0 or greater",
        };
        var rowHeightEditor = new TextBox
        {
            Text = state.RowHeight,
            Watermark = "Auto or non-negative number",
        };
        var rowHeaderWidthEditor = new TextBox
        {
            Text = state.RowHeaderWidth,
            Watermark = "Auto or non-negative number",
        };
        var columnHeaderHeightEditor = new TextBox
        {
            Text = state.ColumnHeaderHeight,
            Watermark = "Auto or non-negative number",
        };
        var minColumnWidthEditor = new TextBox
        {
            Text = state.MinColumnWidth,
            Watermark = "Finite non-negative number",
        };
        var maxColumnWidthEditor = new TextBox
        {
            Text = state.MaxColumnWidth,
            Watermark = "Finite number or Infinity",
        };
        var columnWidthEditor = new TextBox
        {
            Text = state.ColumnWidth,
            Watermark = "Auto, SizeToCells, SizeToHeader, 120, or 2*",
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
            Children =
            {
                CreateField("Headers visibility", headersEditor, 0, 0),
                CreateField("Grid lines", gridLinesEditor, 0, 1),
                CreateField("Selection mode", selectionEditor, 0, 2),
                CreateField("Clipboard copy", clipboardEditor, 1, 0),
                CreateField("Horizontal scrollbar", horizontalScrollEditor, 1, 1),
                CreateField("Vertical scrollbar", verticalScrollEditor, 1, 2),
                CreateField("Frozen column count", frozenColumnsEditor, 2, 0),
                CreateField("Row height", rowHeightEditor, 2, 1),
                CreateField("Row header width", rowHeaderWidthEditor, 2, 2),
                CreateField("Column header height", columnHeaderHeightEditor, 3, 0),
                CreateField("Minimum column width", minColumnWidthEditor, 3, 1),
                CreateField("Maximum column width", maxColumnWidthEditor, 3, 2),
                CreateField("Default column width", columnWidthEditor, 4, 0),
            },
        };
        Grid.SetColumnSpan(fields.Children[^1], 3);
        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children =
            {
                autoGenerateEditor,
                readOnlyEditor,
                reorderEditor,
                resizeEditor,
                sortEditor,
                rowDetailsFrozenEditor,
                rowGroupsFrozenEditor,
                inertiaEditor,
            },
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit DataGrid Behavior - {state.ControlName}",
            Width = 920,
            Height = 760,
            MinWidth = 760,
            MinHeight = 620,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        void ApplyDataGridBehavior()
        {
            var input = new DesignerDataGridBehaviorEditorInput(
                autoGenerateEditor.IsChecked == true,
                readOnlyEditor.IsChecked == true,
                reorderEditor.IsChecked == true,
                resizeEditor.IsChecked == true,
                sortEditor.IsChecked == true,
                headersEditor.SelectedItem?.ToString() ?? string.Empty,
                gridLinesEditor.SelectedItem?.ToString() ?? string.Empty,
                selectionEditor.SelectedItem?.ToString() ?? string.Empty,
                clipboardEditor.SelectedItem?.ToString() ?? string.Empty,
                rowDetailsFrozenEditor.IsChecked == true,
                rowGroupsFrozenEditor.IsChecked == true,
                inertiaEditor.IsChecked == true,
                frozenColumnsEditor.Text ?? string.Empty,
                rowHeightEditor.Text ?? string.Empty,
                rowHeaderWidthEditor.Text ?? string.Empty,
                columnHeaderHeightEditor.Text ?? string.Empty,
                minColumnWidthEditor.Text ?? string.Empty,
                maxColumnWidthEditor.Text ?? string.Empty,
                columnWidthEditor.Text ?? string.Empty,
                horizontalScrollEditor.SelectedItem?.ToString() ?? string.Empty,
                verticalScrollEditor.SelectedItem?.ToString() ?? string.Empty);
            if (!Vm.SetSelectedDataGridBehaviorProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        }
        applyButton.Click += (_, _) => ApplyDataGridBehavior();
        WireEditorDialogShortcuts(dialog, dialog.Close, ApplyDataGridBehavior);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure DataGrid presentation, interaction, selection, scrolling, and shared column sizing. Column definitions remain editable separately from this behavior workflow.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                switches,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(switches, 2);
        Grid.SetRow(errorText, 4);
        Grid.SetRow(buttons, 5);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static ComboBox CreateComboBox(
            IReadOnlyList<string> items,
            string selectedItem)
            => new()
            {
                ItemsSource = items,
                SelectedItem = selectedItem,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

        static Control CreateField(
            string label,
            Control editor,
            int row,
            int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            return field;
        }
    }

    private async Task ShowTypographyPropertiesDialogAsync(TypographyEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var fontFamilyEditor = new TextBox
        {
            Text = state.FontFamily,
            Watermark = "Font family or avares URI",
        };
        var fontSizeEditor = new TextBox { Text = state.FontSize };
        var fontStyleEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Media.FontStyle>(),
            SelectedItem = state.FontStyle,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var fontWeightEditor = new ComboBox
        {
            ItemsSource = DesignerTypographyRuntime.FontWeightNames,
            SelectedItem = state.FontWeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var textAlignmentEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Media.TextAlignment>(),
            SelectedItem = state.TextAlignment,
            IsEnabled = state.SupportsTextAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var textWrappingEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Media.TextWrapping>(),
            SelectedItem = state.TextWrapping,
            IsEnabled = state.SupportsTextWrapping,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Typography Properties - {state.ControlName}",
            Width = 620,
            Height = 440,
            MinWidth = 520,
            MinHeight = 390,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        void ApplyTypography()
        {
            if (!Vm.SetSelectedTypographyProperties(
                    fontFamilyEditor.Text ?? string.Empty,
                    fontSizeEditor.Text ?? string.Empty,
                    fontStyleEditor.SelectedItem?.ToString() ?? string.Empty,
                    fontWeightEditor.SelectedItem?.ToString() ?? string.Empty,
                    textAlignmentEditor.SelectedItem?.ToString() ?? state.TextAlignment,
                    textWrappingEditor.SelectedItem?.ToString() ?? state.TextWrapping))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        }
        applyButton.Click += (_, _) => ApplyTypography();
        WireEditorDialogShortcuts(dialog, dialog.Close, ApplyTypography);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Font family", fontFamilyEditor, 0, 0);
        AddField("Font size", fontSizeEditor, 0, 1);
        AddField("Font style", fontStyleEditor, 1, 0);
        AddField("Font weight", fontWeightEditor, 1, 1);
        AddField("Text alignment", textAlignmentEditor, 2, 0);
        AddField("Text wrapping", textWrappingEditor, 2, 1);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Font settings apply to text and font-aware controls. Alignment and wrapping are available for TextBlock and TextBox.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);
        return;

        void AddField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            fields.Children.Add(field);
        }
    }

    private async Task ShowTransformPropertiesDialogAsync(TransformEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var translateXEditor = new TextBox { Text = state.TranslateX };
        var translateYEditor = new TextBox { Text = state.TranslateY };
        var rotationEditor = new TextBox { Text = state.Rotation };
        var scaleXEditor = new TextBox { Text = state.ScaleX };
        var scaleYEditor = new TextBox { Text = state.ScaleY };
        var skewXEditor = new TextBox { Text = state.SkewX };
        var skewYEditor = new TextBox { Text = state.SkewY };
        var originXEditor = new TextBox { Text = state.OriginX };
        var originYEditor = new TextBox { Text = state.OriginY };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Transform Properties - {state.ControlName}",
            Width = 620,
            Height = 560,
            MinWidth = 520,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        void ApplyTransform()
        {
            if (!Vm.SetSelectedTransformProperties(
                    translateXEditor.Text ?? string.Empty,
                    translateYEditor.Text ?? string.Empty,
                    rotationEditor.Text ?? string.Empty,
                    scaleXEditor.Text ?? string.Empty,
                    scaleYEditor.Text ?? string.Empty,
                    skewXEditor.Text ?? string.Empty,
                    skewYEditor.Text ?? string.Empty,
                    originXEditor.Text ?? string.Empty,
                    originYEditor.Text ?? string.Empty))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        }
        applyButton.Click += (_, _) => ApplyTransform();
        WireEditorDialogShortcuts(dialog, dialog.Close, ApplyTransform);
        var resetButton = new Button { Content = "Reset", MinWidth = 84 };
        resetButton.Click += (_, _) =>
        {
            translateXEditor.Text = "0";
            translateYEditor.Text = "0";
            rotationEditor.Text = "0";
            scaleXEditor.Text = "1";
            scaleYEditor.Text = "1";
            skewXEditor.Text = "0";
            skewYEditor.Text = "0";
            originXEditor.Text = "50";
            originYEditor.Text = "50";
            errorText.Text = string.Empty;
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            ColumnSpacing = 8,
            Children = { resetButton, cancelButton, applyButton },
        };
        Grid.SetColumn(cancelButton, 2);
        Grid.SetColumn(applyButton, 3);

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Translate X (px)", translateXEditor, 0, 0);
        AddField("Translate Y (px)", translateYEditor, 0, 1);
        AddField("Rotation (degrees)", rotationEditor, 1, 0);
        AddField("Scale X", scaleXEditor, 1, 1);
        AddField("Scale Y", scaleYEditor, 2, 0);
        AddField("Skew X (degrees)", skewXEditor, 2, 1);
        AddField("Skew Y (degrees)", skewYEditor, 3, 0);
        AddField("Origin X (%)", originXEditor, 3, 1);
        AddField("Origin Y (%)", originYEditor, 4, 0);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Transform the rendered control without changing its layout slot. Origin values are percentages from 0 to 100.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);
        return;

        void AddField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            fields.Children.Add(field);
        }
    }

    private async Task ShowAccessibilityPropertiesDialogAsync(AccessibilityEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var accessibleNameEditor = new TextBox
        {
            Text = state.AccessibleName,
            Watermark = "Name announced by assistive technology",
        };
        var automationIdEditor = new TextBox
        {
            Text = state.AutomationId,
            Watermark = "Stable UI automation identifier",
        };
        var helpTextEditor = new TextBox
        {
            Text = state.HelpText,
            AcceptsReturn = true,
            Height = 70,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Watermark = "Additional instructions for assistive technology",
        };
        var toolTipEditor = new TextBox
        {
            Text = state.ToolTip,
            AcceptsReturn = true,
            Height = 70,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Watermark = "Pointer tooltip",
        };
        var accessibilityViewEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Automation.AccessibilityView>(),
            SelectedItem = state.AccessibilityView,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var headingLevelEditor = new TextBox
        {
            Text = state.HeadingLevel,
            Watermark = "0 means not a heading",
        };
        var liveSettingEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Automation.AutomationLiveSetting>(),
            SelectedItem = state.LiveSetting,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var tabIndexEditor = new TextBox { Text = state.TabIndex };
        var requiredEditor = new CheckBox
        {
            Content = "Required for form",
            IsChecked = state.IsRequiredForForm,
        };
        var tabStopEditor = new CheckBox
        {
            Content = "Include in tab navigation",
            IsChecked = state.IsTabStop,
        };
        var focusableEditor = new CheckBox
        {
            Content = "Focusable",
            IsChecked = state.Focusable,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Accessibility & Navigation - {state.ControlName}",
            Width = 720,
            Height = 650,
            MinWidth = 600,
            MinHeight = 580,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetSelectedAccessibilityProperties(
                    toolTipEditor.Text ?? string.Empty,
                    accessibleNameEditor.Text ?? string.Empty,
                    automationIdEditor.Text ?? string.Empty,
                    helpTextEditor.Text ?? string.Empty,
                    accessibilityViewEditor.SelectedItem?.ToString() ?? string.Empty,
                    headingLevelEditor.Text ?? string.Empty,
                    liveSettingEditor.SelectedItem?.ToString() ?? string.Empty,
                    requiredEditor.IsChecked == true,
                    tabIndexEditor.Text ?? string.Empty,
                    tabStopEditor.IsChecked == true,
                    focusableEditor.IsChecked == true))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Accessible name", accessibleNameEditor, 0, 0);
        AddField("Automation ID", automationIdEditor, 0, 1);
        AddField("Help text", helpTextEditor, 1, 0);
        AddField("Tooltip", toolTipEditor, 1, 1);
        AddField("Accessibility view", accessibilityViewEditor, 2, 0);
        AddField("Heading level (0-9)", headingLevelEditor, 2, 1);
        AddField("Live setting", liveSettingEditor, 3, 0);
        AddField("Tab index", tabIndexEditor, 3, 1);
        var switches = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 20,
            Margin = new Thickness(0, 4, 0, 0),
            Children = { requiredEditor, tabStopEditor, focusableEditor },
        };
        Grid.SetRow(switches, 4);
        Grid.SetColumnSpan(switches, 2);
        fields.Children.Add(switches);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure assistive-technology metadata and keyboard focus behavior. Heading level 0 disables heading semantics.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);
        return;

        void AddField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            fields.Children.Add(field);
        }
    }

    private async Task ShowInteractionPropertiesDialogAsync(InteractionEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var opacityEditor = new TextBox
        {
            Text = state.Opacity,
            Watermark = "0 to 1",
        };
        var flowDirectionEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<Avalonia.Media.FlowDirection>(),
            SelectedItem = state.FlowDirection,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var cursorEditor = new ComboBox
        {
            ItemsSource = DesignerInteractionRuntime.CursorNames,
            SelectedItem = state.Cursor,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxDropDownHeight = 300,
        };
        var enabledEditor = new CheckBox
        {
            Content = "Enabled",
            IsChecked = state.IsEnabled,
        };
        var visibleEditor = new CheckBox
        {
            Content = "Visible",
            IsChecked = state.IsVisible,
        };
        var hitTestEditor = new CheckBox
        {
            Content = "Hit test visible",
            IsChecked = state.IsHitTestVisible,
        };
        var clipEditor = new CheckBox
        {
            Content = "Clip to bounds",
            IsChecked = state.ClipToBounds,
        };
        var layoutRoundingEditor = new CheckBox
        {
            Content = "Use layout rounding",
            IsChecked = state.UseLayoutRounding,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Interaction & Rendering - {state.ControlName}",
            Width = 640,
            Height = 480,
            MinWidth = 540,
            MinHeight = 430,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetSelectedInteractionProperties(
                    opacityEditor.Text ?? string.Empty,
                    enabledEditor.IsChecked == true,
                    visibleEditor.IsChecked == true,
                    hitTestEditor.IsChecked == true,
                    clipEditor.IsChecked == true,
                    layoutRoundingEditor.IsChecked == true,
                    flowDirectionEditor.SelectedItem?.ToString() ?? string.Empty,
                    cursorEditor.SelectedItem?.ToString() ?? string.Empty))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Opacity", opacityEditor, 0, 0);
        AddField("Flow direction", flowDirectionEditor, 0, 1);
        AddField("Cursor", cursorEditor, 1, 0);
        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 16,
            LineSpacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
            Children =
            {
                enabledEditor,
                visibleEditor,
                hitTestEditor,
                clipEditor,
                layoutRoundingEditor,
            },
        };

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure rendering visibility, input participation, clipping, right-to-left flow, and the pointer cursor.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                switches,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(switches, 2);
        Grid.SetRow(errorText, 4);
        Grid.SetRow(buttons, 5);
        dialog.Content = content;
        await dialog.ShowDialog(this);
        return;

        void AddField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            fields.Children.Add(field);
        }
    }

    private async Task ShowEffectPropertiesDialogAsync(EffectEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var kindEditor = new ComboBox
        {
            ItemsSource = DesignerEffectRuntime.EffectKinds,
            SelectedItem = state.Kind,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var blurEditor = new TextBox { Text = state.BlurRadius, Watermark = "0 to 1000" };
        var offsetXEditor = new TextBox { Text = state.OffsetX, Watermark = "-10000 to 10000" };
        var offsetYEditor = new TextBox { Text = state.OffsetY, Watermark = "-10000 to 10000" };
        var shadowBlurEditor = new TextBox { Text = state.ShadowBlurRadius, Watermark = "0 to 1000" };
        var colorEditor = new TextBox { Text = state.ShadowColor, Watermark = "#000000" };
        var shadowOpacityEditor = new TextBox { Text = state.ShadowOpacity, Watermark = "0 to 1" };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Visual Effects - {state.ControlName}",
            Width = 680,
            Height = 540,
            MinWidth = 560,
            MinHeight = 480,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetSelectedEffectProperties(
                    kindEditor.SelectedItem?.ToString() ?? string.Empty,
                    blurEditor.Text ?? string.Empty,
                    offsetXEditor.Text ?? string.Empty,
                    offsetYEditor.Text ?? string.Empty,
                    shadowBlurEditor.Text ?? string.Empty,
                    colorEditor.Text ?? string.Empty,
                    shadowOpacityEditor.Text ?? string.Empty))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var blurFields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*"),
            RowDefinitions = new RowDefinitions("Auto"),
        };
        var shadowFields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(blurFields, "Blur radius", blurEditor, 0, 0);
        AddField(shadowFields, "Horizontal offset", offsetXEditor, 0, 0);
        AddField(shadowFields, "Vertical offset", offsetYEditor, 0, 1);
        AddField(shadowFields, "Shadow blur radius", shadowBlurEditor, 1, 0);
        var colorAndOpacity = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,*"),
            ColumnSpacing = 8,
        };
        colorAndOpacity.Children.Add(colorEditor);
        Grid.SetColumn(shadowOpacityEditor, 1);
        colorAndOpacity.Children.Add(shadowOpacityEditor);
        AddField(shadowFields, "Color / opacity", colorAndOpacity, 1, 1);

        void RefreshMode()
        {
            var kind = kindEditor.SelectedItem?.ToString();
            blurFields.IsVisible = string.Equals(kind, "Blur", StringComparison.Ordinal);
            shadowFields.IsVisible = string.Equals(kind, "Drop Shadow", StringComparison.Ordinal);
        }

        kindEditor.SelectionChanged += (_, _) => RefreshMode();
        RefreshMode();
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Apply a blur or drop shadow without changing the control's layout size. Shadow opacity is exported through the AXAML color alpha channel.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                kindEditor,
                blurFields,
                shadowFields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(kindEditor, 1);
        Grid.SetRow(blurFields, 2);
        Grid.SetRow(shadowFields, 3);
        Grid.SetRow(errorText, 5);
        Grid.SetRow(buttons, 6);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowRangePropertiesDialogAsync(RangeEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var minimumEditor = new TextBox { Text = state.Minimum };
        var maximumEditor = new TextBox { Text = state.Maximum };
        var valueEditor = new TextBox
        {
            Text = state.Value,
            Watermark = state.ControlKind == "NumericUpDown" ? "Blank is allowed" : null,
        };
        var smallChangeEditor = new TextBox { Text = state.SmallChange };
        var largeChangeEditor = new TextBox { Text = state.LargeChange };
        var sliderOrientationEditor = CreateCombo(
            DesignerRangeRuntime.OrientationNames,
            state.Orientation);
        var directionReversedEditor = new CheckBox
        {
            Content = "Reverse direction",
            IsChecked = state.IsDirectionReversed,
        };
        var tickFrequencyEditor = new TextBox { Text = state.TickFrequency };
        var tickPlacementEditor = CreateCombo(
            DesignerRangeRuntime.TickPlacementNames,
            state.TickPlacement);
        var snapToTickEditor = new CheckBox
        {
            Content = "Snap value to ticks",
            IsChecked = state.IsSnapToTickEnabled,
        };
        var progressOrientationEditor = CreateCombo(
            DesignerRangeRuntime.OrientationNames,
            state.Orientation);
        var indeterminateEditor = new CheckBox
        {
            Content = "Indeterminate",
            IsChecked = state.IsIndeterminate,
        };
        var showProgressTextEditor = new CheckBox
        {
            Content = "Show progress text",
            IsChecked = state.ShowProgressText,
        };
        var progressTextFormatEditor = new TextBox
        {
            Text = state.ProgressTextFormat,
            Watermark = "{1:0}%",
        };
        var incrementEditor = new TextBox { Text = state.Increment };
        var formatStringEditor = new TextBox
        {
            Text = state.FormatString,
            Watermark = "Blank or N2",
        };
        var clipValueEditor = new CheckBox
        {
            Content = "Clip value to range",
            IsChecked = state.ClipValueToMinMax,
        };
        var allowSpinEditor = new CheckBox
        {
            Content = "Allow spin",
            IsChecked = state.AllowSpin,
        };
        var showSpinnerEditor = new CheckBox
        {
            Content = "Show spinner buttons",
            IsChecked = state.ShowButtonSpinner,
        };
        var spinnerLocationEditor = CreateCombo(
            DesignerRangeRuntime.SpinnerLocationNames,
            state.ButtonSpinnerLocation);

        var commonFields = CreateFieldGrid("*,*,*");
        AddField(commonFields, "Minimum", minimumEditor, 0, 0);
        AddField(commonFields, "Maximum", maximumEditor, 0, 1);
        AddField(commonFields, "Value", valueEditor, 0, 2);

        var sliderFields = CreateFieldGrid("*,*");
        sliderFields.RowDefinitions = new RowDefinitions("Auto,Auto,Auto");
        AddField(sliderFields, "Small change", smallChangeEditor, 0, 0);
        AddField(sliderFields, "Large change", largeChangeEditor, 0, 1);
        AddField(sliderFields, "Orientation", sliderOrientationEditor, 1, 0);
        AddField(sliderFields, "Tick frequency", tickFrequencyEditor, 1, 1);
        AddField(sliderFields, "Tick placement", tickPlacementEditor, 2, 0);
        var sliderSwitches = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { directionReversedEditor, snapToTickEditor },
        };
        Grid.SetRow(sliderSwitches, 2);
        Grid.SetColumn(sliderSwitches, 1);
        sliderFields.Children.Add(sliderSwitches);

        var progressFields = CreateFieldGrid("*,*");
        progressFields.RowDefinitions = new RowDefinitions("Auto,Auto");
        AddField(progressFields, "Orientation", progressOrientationEditor, 0, 0);
        AddField(progressFields, "Progress text format", progressTextFormatEditor, 0, 1);
        var progressSwitches = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Children = { indeterminateEditor, showProgressTextEditor },
        };
        Grid.SetRow(progressSwitches, 1);
        Grid.SetColumnSpan(progressSwitches, 2);
        progressFields.Children.Add(progressSwitches);

        var numericFields = CreateFieldGrid("*,*");
        numericFields.RowDefinitions = new RowDefinitions("Auto,Auto");
        AddField(numericFields, "Increment", incrementEditor, 0, 0);
        AddField(numericFields, "Number format", formatStringEditor, 0, 1);
        AddField(numericFields, "Spinner location", spinnerLocationEditor, 1, 0);
        var numericSwitches = new WrapPanel
        {
            ItemSpacing = 16,
            LineSpacing = 8,
            Children = { clipValueEditor, allowSpinEditor, showSpinnerEditor },
        };
        Grid.SetRow(numericSwitches, 1);
        Grid.SetColumn(numericSwitches, 1);
        numericFields.Children.Add(numericSwitches);

        sliderFields.IsVisible = state.ControlKind == "Slider";
        progressFields.IsVisible = state.ControlKind == "ProgressBar";
        numericFields.IsVisible = state.ControlKind == "NumericUpDown";
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Range & Value - {state.ControlName}",
            Width = 720,
            Height = 570,
            MinWidth = 600,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerRangeEditorInput(
                minimumEditor.Text ?? string.Empty,
                maximumEditor.Text ?? string.Empty,
                valueEditor.Text ?? string.Empty,
                smallChangeEditor.Text ?? string.Empty,
                largeChangeEditor.Text ?? string.Empty,
                state.ControlKind == "ProgressBar"
                    ? progressOrientationEditor.SelectedItem?.ToString() ?? string.Empty
                    : sliderOrientationEditor.SelectedItem?.ToString() ?? string.Empty,
                directionReversedEditor.IsChecked == true,
                tickFrequencyEditor.Text ?? string.Empty,
                tickPlacementEditor.SelectedItem?.ToString() ?? string.Empty,
                snapToTickEditor.IsChecked == true,
                indeterminateEditor.IsChecked == true,
                showProgressTextEditor.IsChecked == true,
                progressTextFormatEditor.Text ?? string.Empty,
                incrementEditor.Text ?? string.Empty,
                formatStringEditor.Text ?? string.Empty,
                clipValueEditor.IsChecked == true,
                allowSpinEditor.IsChecked == true,
                showSpinnerEditor.IsChecked == true,
                spinnerLocationEditor.SelectedItem?.ToString() ?? string.Empty);
            if (!Vm.SetSelectedRangeProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = $"Configure validated range behavior for {state.ControlKind}. Minimum must be less than Maximum, and Value must stay inside the range.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                commonFields,
                sliderFields,
                progressFields,
                numericFields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(commonFields, 1);
        Grid.SetRow(sliderFields, 2);
        Grid.SetRow(progressFields, 3);
        Grid.SetRow(numericFields, 4);
        Grid.SetRow(errorText, 6);
        Grid.SetRow(buttons, 7);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static ComboBox CreateCombo(IEnumerable<string> items, string selected)
            => new()
            {
                ItemsSource = items,
                SelectedItem = selected,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

        static Grid CreateFieldGrid(string columns)
            => new()
            {
                ColumnDefinitions = new ColumnDefinitions(columns),
                RowDefinitions = new RowDefinitions("Auto"),
                ColumnSpacing = 12,
                RowSpacing = 10,
            };

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowTextInputPropertiesDialogAsync(TextInputEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var textEditor = new TextBox
        {
            Text = state.Text,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MinHeight = 82,
            Watermark = "Design-time text",
        };
        var watermarkEditor = new TextBox { Text = state.Watermark };
        var wrappingEditor = new ComboBox
        {
            ItemsSource = DesignerTextInputRuntime.TextWrappingNames,
            SelectedItem = state.TextWrapping,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var alignmentEditor = new ComboBox
        {
            ItemsSource = DesignerTextInputRuntime.TextAlignmentNames,
            SelectedItem = state.TextAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var maxLengthEditor = new TextBox { Text = state.MaxLength, Watermark = "0 = unlimited" };
        var minLinesEditor = new TextBox { Text = state.MinLines, Watermark = "0 = automatic" };
        var maxLinesEditor = new TextBox { Text = state.MaxLines, Watermark = "0 = automatic" };
        var passwordCharEditor = new TextBox
        {
            Text = state.PasswordChar,
            MaxLength = 1,
            Watermark = "Blank or one character",
        };
        var undoLimitEditor = new TextBox { Text = state.UndoLimit };
        var acceptsReturnEditor = new CheckBox
        {
            Content = "Accept Return",
            IsChecked = state.AcceptsReturn,
        };
        var acceptsTabEditor = new CheckBox
        {
            Content = "Accept Tab",
            IsChecked = state.AcceptsTab,
        };
        var readOnlyEditor = new CheckBox
        {
            Content = "Read only",
            IsChecked = state.IsReadOnly,
        };
        var revealPasswordEditor = new CheckBox
        {
            Content = "Reveal password",
            IsChecked = state.RevealPassword,
        };
        var floatingWatermarkEditor = new CheckBox
        {
            Content = "Floating watermark",
            IsChecked = state.UseFloatingWatermark,
        };
        var undoEnabledEditor = new CheckBox
        {
            Content = "Enable undo",
            IsChecked = state.IsUndoEnabled,
        };
        var clearSelectionEditor = new CheckBox
        {
            Content = "Clear selection on lost focus",
            IsChecked = state.ClearSelectionOnLostFocus,
        };
        var inactiveHighlightEditor = new CheckBox
        {
            Content = "Highlight inactive selection",
            IsChecked = state.IsInactiveSelectionHighlightEnabled,
        };

        void RefreshPasswordMode()
        {
            var isPassword = !string.IsNullOrEmpty(passwordCharEditor.Text);
            textEditor.IsEnabled = !isPassword;
            revealPasswordEditor.IsEnabled = isPassword;
            if (isPassword)
            {
                textEditor.Watermark = "Password text is not stored by the designer";
            }
            else
            {
                textEditor.Watermark = "Design-time text";
            }
        }

        passwordCharEditor.TextChanged += (_, _) => RefreshPasswordMode();
        RefreshPasswordMode();

        var primaryFields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(primaryFields, "Design text", textEditor, 0, 0);
        AddField(primaryFields, "Watermark", watermarkEditor, 0, 1);
        AddField(primaryFields, "Text wrapping", wrappingEditor, 1, 0);
        AddField(primaryFields, "Text alignment", alignmentEditor, 1, 1);

        var limitsFields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(limitsFields, "Max length", maxLengthEditor, 0, 0);
        AddField(limitsFields, "Min lines", minLinesEditor, 0, 1);
        AddField(limitsFields, "Max lines", maxLinesEditor, 0, 2);
        AddField(limitsFields, "Password character", passwordCharEditor, 1, 0);
        AddField(limitsFields, "Undo limit", undoLimitEditor, 1, 1);

        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 16,
            LineSpacing = 8,
            Children =
            {
                acceptsReturnEditor,
                acceptsTabEditor,
                readOnlyEditor,
                revealPasswordEditor,
                floatingWatermarkEditor,
                undoEnabledEditor,
                clearSelectionEditor,
                inactiveHighlightEditor,
            },
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Text Input - {state.ControlName}",
            Width = 780,
            Height = 690,
            MinWidth = 640,
            MinHeight = 580,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerTextInputEditorInput(
                textEditor.Text ?? string.Empty,
                watermarkEditor.Text ?? string.Empty,
                acceptsReturnEditor.IsChecked == true,
                acceptsTabEditor.IsChecked == true,
                wrappingEditor.SelectedItem?.ToString() ?? string.Empty,
                alignmentEditor.SelectedItem?.ToString() ?? string.Empty,
                readOnlyEditor.IsChecked == true,
                maxLengthEditor.Text ?? string.Empty,
                minLinesEditor.Text ?? string.Empty,
                maxLinesEditor.Text ?? string.Empty,
                passwordCharEditor.Text ?? string.Empty,
                revealPasswordEditor.IsChecked == true,
                floatingWatermarkEditor.IsChecked == true,
                undoEnabledEditor.IsChecked == true,
                undoLimitEditor.Text ?? string.Empty,
                clearSelectionEditor.IsChecked == true,
                inactiveHighlightEditor.IsChecked == true);
            if (!Vm.SetSelectedTextInputProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure TextBox input, line limits, password behavior, undo, and selection policies. A password character suppresses design text from snapshots and AXAML.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                primaryFields,
                limitsFields,
                switches,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(primaryFields, 1);
        Grid.SetRow(limitsFields, 2);
        Grid.SetRow(switches, 3);
        Grid.SetRow(errorText, 5);
        Grid.SetRow(buttons, 6);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowSelectableTextBlockPropertiesDialogAsync(
        SelectableTextBlockEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var textEditor = new TextBox
        {
            Text = state.Text,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MinHeight = 72,
        };
        var selectionBrushEditor = new TextBox
        {
            Text = state.SelectionBrush,
            Watermark = "Example: #663B82F6 or Transparent",
        };
        var selectionForegroundEditor = new TextBox
        {
            Text = state.SelectionForegroundBrush,
            Watermark = "Example: #FFFFFFFF or Transparent",
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var fields = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            RowSpacing = 10,
            Children =
            {
                CreateField("Text", textEditor, 0),
                CreateField("Selection brush", selectionBrushEditor, 1),
                CreateField("Selection foreground", selectionForegroundEditor, 2),
            },
        };
        var dialog = new Window
        {
            Title = $"Edit SelectableTextBlock - {state.ControlName}",
            Width = 680,
            Height = 460,
            MinWidth = 560,
            MinHeight = 380,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerSelectableTextBlockEditorInput(
                textEditor.Text ?? string.Empty,
                selectionBrushEditor.Text ?? string.Empty,
                selectionForegroundEditor.Text ?? string.Empty);
            if (!Vm.SetSelectedSelectableTextBlockProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure selectable text content and solid selection colors. SelectionStart and SelectionEnd remain runtime interaction state and are not persisted in the document.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static Control CreateField(string label, Control editor, int row)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            return field;
        }
    }

    private async Task ShowSplitViewPropertiesDialogAsync(SplitViewEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var displayModeEditor = new ComboBox
        {
            ItemsSource = DesignerSplitViewRuntime.DisplayModeNames,
            SelectedItem = state.DisplayMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var panePlacementEditor = new ComboBox
        {
            ItemsSource = DesignerSplitViewRuntime.PanePlacementNames,
            SelectedItem = state.PanePlacement,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var openPaneLengthEditor = new TextBox
        {
            Text = state.OpenPaneLength,
            Watermark = "Finite non-negative number",
        };
        var compactPaneLengthEditor = new TextBox
        {
            Text = state.CompactPaneLength,
            Watermark = "Finite non-negative number",
        };
        var paneBackgroundEditor = new TextBox
        {
            Text = state.PaneBackground,
            Watermark = "Example: #E2E8F0 or Transparent; blank clears",
        };
        var paneOpenEditor = new CheckBox
        {
            Content = "Pane open",
            IsChecked = state.IsPaneOpen,
        };
        var lightDismissEditor = new CheckBox
        {
            Content = "Use light-dismiss overlay",
            IsChecked = state.UseLightDismissOverlayMode,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
            Children =
            {
                CreateField("Display mode", displayModeEditor, 0, 0),
                CreateField("Pane placement", panePlacementEditor, 0, 1),
                CreateField("Open pane length", openPaneLengthEditor, 1, 0),
                CreateField("Compact pane length", compactPaneLengthEditor, 1, 1),
                CreateField("Pane background", paneBackgroundEditor, 2, 0),
            },
        };
        Grid.SetColumnSpan(fields.Children[^1], 2);
        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children = { paneOpenEditor, lightDismissEditor },
        };
        var dialog = new Window
        {
            Title = $"Edit SplitView Pane Behavior - {state.ControlName}",
            Width = 720,
            Height = 500,
            MinWidth = 600,
            MinHeight = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerSplitViewEditorInput(
                displayModeEditor.SelectedItem?.ToString() ?? string.Empty,
                paneOpenEditor.IsChecked == true,
                openPaneLengthEditor.Text ?? string.Empty,
                compactPaneLengthEditor.Text ?? string.Empty,
                panePlacementEditor.SelectedItem?.ToString() ?? string.Empty,
                lightDismissEditor.IsChecked == true,
                paneBackgroundEditor.Text ?? string.Empty);
            if (!Vm.SetSelectedSplitViewProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure SplitView pane presentation. Pane background accepts a solid color or Transparent; Pane and Content children are edited separately with Assign to SplitView.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                switches,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(switches, 2);
        Grid.SetRow(errorText, 4);
        Grid.SetRow(buttons, 5);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static Control CreateField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            return field;
        }
    }

    private async Task ShowTabControlBehaviorPropertiesDialogAsync(
        TabControlBehaviorEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var tabStripPlacementEditor = new ComboBox
        {
            ItemsSource = DesignerTabControlRuntime.TabStripPlacementNames,
            SelectedItem = state.TabStripPlacement,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var horizontalContentAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerTabControlRuntime.HorizontalAlignmentNames,
            SelectedItem = state.HorizontalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalContentAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerTabControlRuntime.VerticalAlignmentNames,
            SelectedItem = state.VerticalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
            Children =
            {
                CreateField("Tab strip placement", tabStripPlacementEditor, 0, 0),
                CreateField(
                    "Horizontal content alignment",
                    horizontalContentAlignmentEditor,
                    0,
                    1),
                CreateField(
                    "Vertical content alignment",
                    verticalContentAlignmentEditor,
                    1,
                    0),
            },
        };
        Grid.SetColumnSpan(fields.Children[^1], 2);
        var dialog = new Window
        {
            Title = $"Edit TabControl Behavior - {state.ControlName}",
            Width = 700,
            Height = 390,
            MinWidth = 580,
            MinHeight = 340,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerTabControlEditorInput(
                tabStripPlacementEditor.SelectedItem?.ToString() ?? string.Empty,
                horizontalContentAlignmentEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                verticalContentAlignmentEditor.SelectedItem?.ToString()
                    ?? string.Empty);
            if (!Vm.SetSelectedTabControlBehaviorProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure the tab strip edge and the selected tab content alignment without changing tab headers or assigned children.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static Control CreateField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            return field;
        }
    }

    private async Task ShowMaskedTextBoxPropertiesDialogAsync(MaskedTextBoxEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var maskEditor = new TextBox
        {
            Text = state.Mask,
            Watermark = "Example: 000-0000 or 00/00/0000",
        };
        var promptCharEditor = new TextBox
        {
            Text = state.PromptChar,
            Watermark = "Exactly one character",
        };
        var hidePromptEditor = new CheckBox
        {
            Content = "Hide prompt characters when leaving the field",
            IsChecked = state.HidePromptOnLeave,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto"),
            ColumnSpacing = 12,
            Children =
            {
                CreateField("Mask", maskEditor, 0),
                CreateField("Prompt character", promptCharEditor, 1),
            },
        };
        var dialog = new Window
        {
            Title = $"Edit MaskedTextBox - {state.ControlName}",
            Width = 680,
            Height = 340,
            MinWidth = 560,
            MinHeight = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerMaskedTextBoxEditorInput(
                maskEditor.Text ?? string.Empty,
                promptCharEditor.Text ?? string.Empty,
                hidePromptEditor.IsChecked == true);
            if (!Vm.SetSelectedMaskedTextBoxProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Mask syntax follows .NET MaskedTextProvider. Common tokens: 0 required digit, 9 optional digit, L required letter, ? optional letter, and literals such as '-' or '/'.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                hidePromptEditor,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(hidePromptEditor, 2);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static Control CreateField(string label, Control editor, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetColumn(field, column);
            return field;
        }
    }

    private async Task ShowSelectionPropertiesDialogAsync(SelectionEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var isComboBox = state.ControlKind == nameof(DesignerSelectionControlKind.ComboBox);
        var isListBox = state.ControlKind == nameof(DesignerSelectionControlKind.ListBox);
        var supportsIndex = isComboBox || isListBox;
        var supportsSelectionMode = !isComboBox;
        var selectedIndexEditor = new TextBox
        {
            Text = state.SelectedIndex,
            IsEnabled = supportsIndex,
            Watermark = supportsIndex ? "-1 = no selection" : "Not used by TreeView",
        };
        var textEditor = new TextBox
        {
            Text = state.Text,
            IsEnabled = isComboBox && state.IsEditable,
            Watermark = "Editable ComboBox text",
        };
        var placeholderEditor = new TextBox
        {
            Text = state.PlaceholderText,
            IsEnabled = isComboBox,
        };
        var maxDropDownHeightEditor = new TextBox
        {
            Text = state.MaxDropDownHeight,
            IsEnabled = isComboBox,
        };
        var horizontalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerSelectionRuntime.HorizontalAlignmentNames,
            SelectedItem = state.HorizontalContentAlignment,
            IsEnabled = isComboBox,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerSelectionRuntime.VerticalAlignmentNames,
            SelectedItem = state.VerticalContentAlignment,
            IsEnabled = isComboBox,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var textSearchEditor = new CheckBox
        {
            Content = "Type-to-search",
            IsChecked = state.IsTextSearchEnabled,
            IsEnabled = supportsIndex,
        };
        var autoScrollEditor = new CheckBox
        {
            Content = "Auto-scroll to selection",
            IsChecked = state.AutoScrollToSelectedItem,
        };
        var wrapSelectionEditor = new CheckBox
        {
            Content = "Wrap keyboard selection",
            IsChecked = state.WrapSelection,
            IsEnabled = supportsIndex,
        };
        var multipleEditor = new CheckBox
        {
            Content = "Allow multiple selection",
            IsChecked = state.AllowMultiple,
            IsEnabled = supportsSelectionMode,
        };
        var toggleEditor = new CheckBox
        {
            Content = "Toggle selection on tap / Space",
            IsChecked = state.ToggleSelection,
            IsEnabled = supportsSelectionMode,
        };
        var alwaysSelectedEditor = new CheckBox
        {
            Content = "Always keep an item selected",
            IsChecked = state.AlwaysSelected,
            IsEnabled = supportsSelectionMode,
        };
        var editableEditor = new CheckBox
        {
            Content = "Editable ComboBox",
            IsChecked = state.IsEditable,
            IsEnabled = isComboBox,
        };

        void RefreshEditableText()
        {
            var hasSelection = int.TryParse(
                    selectedIndexEditor.Text,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var selectedIndex)
                && selectedIndex >= 0;
            textEditor.IsEnabled = isComboBox
                && editableEditor.IsChecked == true
                && !hasSelection;
            textEditor.Watermark = hasSelection
                ? "Text is supplied by the selected item"
                : "Editable ComboBox text";
        }

        editableEditor.IsCheckedChanged += (_, _) => RefreshEditableText();
        selectedIndexEditor.TextChanged += (_, _) => RefreshEditableText();
        RefreshEditableText();

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Selected index", selectedIndexEditor, 0, 0);
        AddField(fields, "Editable text", textEditor, 0, 1);
        AddField(fields, "Placeholder text", placeholderEditor, 1, 0);
        AddField(fields, "Maximum drop-down height", maxDropDownHeightEditor, 1, 1);
        AddField(fields, "Horizontal content alignment", horizontalAlignmentEditor, 2, 0);
        AddField(fields, "Vertical content alignment", verticalAlignmentEditor, 2, 1);

        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 16,
            LineSpacing = 8,
            Children =
            {
                textSearchEditor,
                autoScrollEditor,
                wrapSelectionEditor,
                multipleEditor,
                toggleEditor,
                alwaysSelectedEditor,
                editableEditor,
            },
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Selection Behavior - {state.ControlName}",
            Width = 720,
            Height = 530,
            MinWidth = 620,
            MinHeight = 470,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerSelectionEditorInput(
                selectedIndexEditor.Text ?? string.Empty,
                textSearchEditor.IsChecked == true,
                autoScrollEditor.IsChecked == true,
                wrapSelectionEditor.IsChecked == true,
                multipleEditor.IsChecked == true,
                toggleEditor.IsChecked == true,
                alwaysSelectedEditor.IsChecked == true,
                editableEditor.IsChecked == true,
                textEditor.Text ?? string.Empty,
                placeholderEditor.Text ?? string.Empty,
                maxDropDownHeightEditor.Text ?? string.Empty,
                horizontalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty,
                verticalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty);
            if (!Vm.SetSelectedSelectionProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = $"Configure {state.ControlKind} selection, keyboard navigation, and type-specific presentation behavior.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                switches,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(switches, 2);
        Grid.SetRow(errorText, 4);
        Grid.SetRow(buttons, 5);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowDateTimePropertiesDialogAsync(DateTimeEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var isDatePicker = state.ControlKind == nameof(DesignerDateTimeControlKind.DatePicker);
        var isCalendarDatePicker =
            state.ControlKind
            == nameof(DesignerDateTimeControlKind.CalendarDatePicker);
        var isCalendarControl =
            state.ControlKind == nameof(DesignerDateTimeControlKind.Calendar);
        var selectedDateEditor = new TextBox
        {
            Text = state.SelectedDate,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var minYearEditor = new TextBox { Text = state.MinYear, Watermark = "yyyy-MM-dd" };
        var maxYearEditor = new TextBox { Text = state.MaxYear, Watermark = "yyyy-MM-dd" };
        var dayVisibleEditor = new CheckBox { Content = "Show day", IsChecked = state.DayVisible };
        var monthVisibleEditor = new CheckBox { Content = "Show month", IsChecked = state.MonthVisible };
        var yearVisibleEditor = new CheckBox { Content = "Show year", IsChecked = state.YearVisible };
        var dayFormatEditor = new TextBox { Text = state.DayFormat };
        var monthFormatEditor = new TextBox { Text = state.MonthFormat };
        var yearFormatEditor = new TextBox { Text = state.YearFormat };

        var displayDateEditor = new TextBox { Text = state.DisplayDate, Watermark = "yyyy-MM-dd" };
        var displayDateStartEditor = new TextBox
        {
            Text = state.DisplayDateStart,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var displayDateEndEditor = new TextBox
        {
            Text = state.DisplayDateEnd,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var firstDayEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.DayOfWeekNames,
            SelectedItem = state.FirstDayOfWeek,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var selectedDateFormatEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.CalendarDateFormatNames,
            SelectedItem = state.SelectedDateFormat,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var customDateFormatEditor = new TextBox { Text = state.CustomDateFormatString };
        var todayHighlightedEditor = new CheckBox
        {
            Content = "Highlight today",
            IsChecked = state.IsTodayHighlighted,
        };
        var calendarSelectionModeEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.CalendarSelectionModeNames,
            SelectedItem = state.CalendarSelectionMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var calendarDisplayModeEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.CalendarDisplayModeNames,
            SelectedItem = state.CalendarDisplayMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var allowTapRangeEditor = new CheckBox
        {
            Content = "Allow tap range selection",
            IsChecked = state.AllowTapRangeSelection,
        };
        var calendarControlSelectedDateEditor = new TextBox
        {
            Text = state.SelectedDate,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var calendarControlDisplayDateEditor = new TextBox
        {
            Text = state.DisplayDate,
            Watermark = "yyyy-MM-dd",
        };
        var calendarControlDisplayStartEditor = new TextBox
        {
            Text = state.DisplayDateStart,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var calendarControlDisplayEndEditor = new TextBox
        {
            Text = state.DisplayDateEnd,
            Watermark = "Optional: yyyy-MM-dd",
        };
        var calendarControlFirstDayEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.DayOfWeekNames,
            SelectedItem = state.FirstDayOfWeek,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var calendarControlTodayHighlightedEditor = new CheckBox
        {
            Content = "Highlight today",
            IsChecked = state.IsTodayHighlighted,
        };
        var watermarkEditor = new TextBox { Text = state.Watermark };
        var floatingWatermarkEditor = new CheckBox
        {
            Content = "Use floating watermark",
            IsChecked = state.UseFloatingWatermark,
        };
        var horizontalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.HorizontalAlignmentNames,
            SelectedItem = state.HorizontalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.VerticalAlignmentNames,
            SelectedItem = state.VerticalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var selectedTimeEditor = new TextBox
        {
            Text = state.SelectedTime,
            Watermark = "Optional: HH:mm or HH:mm:ss",
        };
        var minuteIncrementEditor = new TextBox
        {
            Text = state.MinuteIncrement,
            Watermark = "1-59",
        };
        var secondIncrementEditor = new TextBox
        {
            Text = state.SecondIncrement,
            Watermark = "1-59",
        };
        var clockIdentifierEditor = new ComboBox
        {
            ItemsSource = DesignerDateTimeRuntime.ClockIdentifiers,
            SelectedItem = state.ClockIdentifier,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var useSecondsEditor = new CheckBox
        {
            Content = "Show seconds",
            IsChecked = state.UseSeconds,
        };

        void RefreshCustomFormat()
            => customDateFormatEditor.IsEnabled =
                selectedDateFormatEditor.SelectedItem?.ToString() ==
                nameof(CalendarDatePickerFormat.Custom);

        selectedDateFormatEditor.SelectionChanged += (_, _) => RefreshCustomFormat();
        RefreshCustomFormat();

        var datePickerFields = CreateFieldGrid(3);
        if (isDatePicker)
        {
            AddField(datePickerFields, "Selected date", selectedDateEditor, 0, 0);
        }

        AddField(datePickerFields, "Minimum year", minYearEditor, 0, 1);
        AddField(datePickerFields, "Maximum year", maxYearEditor, 1, 0);
        AddField(datePickerFields, "Day format", dayFormatEditor, 1, 1);
        AddField(datePickerFields, "Month format", monthFormatEditor, 2, 0);
        AddField(datePickerFields, "Year format", yearFormatEditor, 2, 1);
        var datePickerSwitches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children = { dayVisibleEditor, monthVisibleEditor, yearVisibleEditor },
        };

        var calendarFields = CreateFieldGrid(6);
        if (isCalendarDatePicker)
        {
            AddField(calendarFields, "Selected date", selectedDateEditor, 0, 0);
        }

        AddField(calendarFields, "Displayed month", displayDateEditor, 0, 1);
        AddField(calendarFields, "Display range start", displayDateStartEditor, 1, 0);
        AddField(calendarFields, "Display range end", displayDateEndEditor, 1, 1);
        AddField(calendarFields, "First day of week", firstDayEditor, 2, 0);
        AddField(calendarFields, "Selected date format", selectedDateFormatEditor, 2, 1);
        AddField(calendarFields, "Custom date format", customDateFormatEditor, 3, 0);
        AddField(calendarFields, "Watermark", watermarkEditor, 3, 1);
        AddField(calendarFields, "Horizontal content alignment", horizontalAlignmentEditor, 4, 0);
        AddField(calendarFields, "Vertical content alignment", verticalAlignmentEditor, 4, 1);
        var calendarSwitches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children = { todayHighlightedEditor, floatingWatermarkEditor },
        };
        Grid.SetRow(calendarSwitches, 5);
        Grid.SetColumnSpan(calendarSwitches, 2);
        calendarFields.Children.Add(calendarSwitches);

        var calendarControlFields = CreateFieldGrid(5);
        AddField(
            calendarControlFields,
            "Selected date",
            calendarControlSelectedDateEditor,
            0,
            0);
        AddField(
            calendarControlFields,
            "Displayed date",
            calendarControlDisplayDateEditor,
            0,
            1);
        AddField(
            calendarControlFields,
            "Display range start",
            calendarControlDisplayStartEditor,
            1,
            0);
        AddField(
            calendarControlFields,
            "Display range end",
            calendarControlDisplayEndEditor,
            1,
            1);
        AddField(
            calendarControlFields,
            "First day of week",
            calendarControlFirstDayEditor,
            2,
            0);
        AddField(
            calendarControlFields,
            "Selection mode",
            calendarSelectionModeEditor,
            2,
            1);
        AddField(
            calendarControlFields,
            "Display mode",
            calendarDisplayModeEditor,
            3,
            0);
        var calendarControlSwitches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children =
            {
                calendarControlTodayHighlightedEditor,
                allowTapRangeEditor,
            },
        };
        Grid.SetRow(calendarControlSwitches, 4);
        Grid.SetColumnSpan(calendarControlSwitches, 2);
        calendarControlFields.Children.Add(calendarControlSwitches);

        var timePickerFields = CreateFieldGrid(3);
        AddField(timePickerFields, "Selected time", selectedTimeEditor, 0, 0);
        AddField(timePickerFields, "Clock", clockIdentifierEditor, 0, 1);
        AddField(timePickerFields, "Minute increment", minuteIncrementEditor, 1, 0);
        AddField(timePickerFields, "Second increment", secondIncrementEditor, 1, 1);
        Grid.SetRow(useSecondsEditor, 2);
        Grid.SetColumnSpan(useSecondsEditor, 2);
        timePickerFields.Children.Add(useSecondsEditor);

        Control editorContent;
        if (isDatePicker)
        {
            editorContent = new StackPanel
            {
                Spacing = 12,
                Children = { datePickerFields, datePickerSwitches },
            };
        }
        else if (isCalendarDatePicker)
        {
            editorContent = calendarFields;
        }
        else if (isCalendarControl)
        {
            editorContent = calendarControlFields;
        }
        else
        {
            editorContent = timePickerFields;
        }

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Date & Time Input - {state.ControlName}",
            Width = 760,
            Height = isCalendarDatePicker ? 690
                : isCalendarControl ? 600
                : 520,
            MinWidth = 640,
            MinHeight = 460,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerDateTimeEditorInput(
                isCalendarControl
                    ? calendarControlSelectedDateEditor.Text ?? string.Empty
                    : selectedDateEditor.Text ?? string.Empty,
                minYearEditor.Text ?? string.Empty,
                maxYearEditor.Text ?? string.Empty,
                dayVisibleEditor.IsChecked == true,
                monthVisibleEditor.IsChecked == true,
                yearVisibleEditor.IsChecked == true,
                dayFormatEditor.Text ?? string.Empty,
                monthFormatEditor.Text ?? string.Empty,
                yearFormatEditor.Text ?? string.Empty,
                isCalendarControl
                    ? calendarControlDisplayDateEditor.Text ?? string.Empty
                    : displayDateEditor.Text ?? string.Empty,
                isCalendarControl
                    ? calendarControlDisplayStartEditor.Text ?? string.Empty
                    : displayDateStartEditor.Text ?? string.Empty,
                isCalendarControl
                    ? calendarControlDisplayEndEditor.Text ?? string.Empty
                    : displayDateEndEditor.Text ?? string.Empty,
                isCalendarControl
                    ? calendarControlFirstDayEditor.SelectedItem?.ToString()
                        ?? string.Empty
                    : firstDayEditor.SelectedItem?.ToString() ?? string.Empty,
                isCalendarControl
                    ? calendarControlTodayHighlightedEditor.IsChecked == true
                    : todayHighlightedEditor.IsChecked == true,
                selectedDateFormatEditor.SelectedItem?.ToString() ?? string.Empty,
                customDateFormatEditor.Text ?? string.Empty,
                watermarkEditor.Text ?? string.Empty,
                floatingWatermarkEditor.IsChecked == true,
                horizontalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty,
                verticalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty,
                selectedTimeEditor.Text ?? string.Empty,
                minuteIncrementEditor.Text ?? string.Empty,
                secondIncrementEditor.Text ?? string.Empty,
                clockIdentifierEditor.SelectedItem?.ToString() ?? string.Empty,
                useSecondsEditor.IsChecked == true,
                calendarSelectionModeEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                calendarDisplayModeEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                allowTapRangeEditor.IsChecked == true);
            if (!Vm.SetSelectedDateTimeProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = $"Configure {state.ControlKind} values, limits, and presentation behavior.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                new ScrollViewer { Content = editorContent },
                errorText,
                buttons,
            },
        };
        Grid.SetRow(content.Children[1], 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static Grid CreateFieldGrid(int rowCount)
            => new()
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                RowDefinitions = new RowDefinitions(
                    string.Join(",", Enumerable.Repeat("Auto", rowCount))),
                ColumnSpacing = 12,
                RowSpacing = 10,
            };

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowColorPickerPropertiesDialogAsync(ColorPickerEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var colorEditor = new TextBox
        {
            Text = state.Color,
            Watermark = "#AARRGGBB or named color",
        };
        var colorModelEditor = CreateCombo(
            DesignerColorPickerRuntime.ColorModelNames,
            state.ColorModel);
        var spectrumComponentsEditor = CreateCombo(
            DesignerColorPickerRuntime.ColorSpectrumComponentsNames,
            state.ColorSpectrumComponents);
        var spectrumShapeEditor = CreateCombo(
            DesignerColorPickerRuntime.ColorSpectrumShapeNames,
            state.ColorSpectrumShape);
        var alphaPositionEditor = CreateCombo(
            DesignerColorPickerRuntime.AlphaComponentPositionNames,
            state.HexInputAlphaPosition);
        var paletteColumnCountEditor = new TextBox
        {
            Text = state.PaletteColumnCount,
            Watermark = "1-32",
        };

        var accentColorsEditor = new CheckBox
        {
            Content = "Show accent colors",
            IsChecked = state.IsAccentColorsVisible,
        };
        var alphaEnabledEditor = new CheckBox
        {
            Content = "Enable alpha",
            IsChecked = state.IsAlphaEnabled,
        };
        var alphaVisibleEditor = new CheckBox
        {
            Content = "Show alpha",
            IsChecked = state.IsAlphaVisible,
        };
        var previewVisibleEditor = new CheckBox
        {
            Content = "Show color preview",
            IsChecked = state.IsColorPreviewVisible,
        };
        var paletteVisibleEditor = new CheckBox
        {
            Content = "Show palette",
            IsChecked = state.IsColorPaletteVisible,
        };
        var spectrumVisibleEditor = new CheckBox
        {
            Content = "Show spectrum",
            IsChecked = state.IsColorSpectrumVisible,
        };
        var spectrumSliderVisibleEditor = new CheckBox
        {
            Content = "Show spectrum slider",
            IsChecked = state.IsColorSpectrumSliderVisible,
        };
        var componentSliderVisibleEditor = new CheckBox
        {
            Content = "Show component slider",
            IsChecked = state.IsComponentSliderVisible,
        };
        var componentsVisibleEditor = new CheckBox
        {
            Content = "Show color components",
            IsChecked = state.IsColorComponentsVisible,
        };
        var modelVisibleEditor = new CheckBox
        {
            Content = "Show color model",
            IsChecked = state.IsColorModelVisible,
        };
        var componentTextVisibleEditor = new CheckBox
        {
            Content = "Show component inputs",
            IsChecked = state.IsComponentTextInputVisible,
        };
        var hexVisibleEditor = new CheckBox
        {
            Content = "Show hex input",
            IsChecked = state.IsHexInputVisible,
        };

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Color", colorEditor, 0, 0);
        AddField(fields, "Color model", colorModelEditor, 0, 1);
        AddField(fields, "Hex alpha position", alphaPositionEditor, 0, 2);
        AddField(fields, "Spectrum components", spectrumComponentsEditor, 1, 0);
        AddField(fields, "Spectrum shape", spectrumShapeEditor, 1, 1);
        AddField(fields, "Palette columns", paletteColumnCountEditor, 1, 2);

        var appearanceOptions = CreateOptionPanel(
            accentColorsEditor,
            alphaEnabledEditor,
            alphaVisibleEditor,
            previewVisibleEditor);
        var spectrumOptions = CreateOptionPanel(
            paletteVisibleEditor,
            spectrumVisibleEditor,
            spectrumSliderVisibleEditor,
            componentSliderVisibleEditor);
        var inputOptions = CreateOptionPanel(
            componentsVisibleEditor,
            modelVisibleEditor,
            componentTextVisibleEditor,
            hexVisibleEditor);

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit ColorPicker - {state.ControlName}",
            Width = 860,
            Height = 650,
            MinWidth = 720,
            MinHeight = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerColorPickerEditorInput(
                colorEditor.Text ?? string.Empty,
                colorModelEditor.SelectedItem?.ToString() ?? string.Empty,
                spectrumComponentsEditor.SelectedItem?.ToString() ?? string.Empty,
                spectrumShapeEditor.SelectedItem?.ToString() ?? string.Empty,
                alphaPositionEditor.SelectedItem?.ToString() ?? string.Empty,
                accentColorsEditor.IsChecked == true,
                alphaEnabledEditor.IsChecked == true,
                alphaVisibleEditor.IsChecked == true,
                componentsVisibleEditor.IsChecked == true,
                modelVisibleEditor.IsChecked == true,
                paletteVisibleEditor.IsChecked == true,
                previewVisibleEditor.IsChecked == true,
                spectrumVisibleEditor.IsChecked == true,
                spectrumSliderVisibleEditor.IsChecked == true,
                componentSliderVisibleEditor.IsChecked == true,
                componentTextVisibleEditor.IsChecked == true,
                hexVisibleEditor.IsChecked == true,
                paletteColumnCountEditor.Text ?? string.Empty);
            if (!Vm.SetSelectedColorPickerProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Edit the color value and the ColorPicker flyout sections. PaletteColors is kept as a collection and is not changed by this editor.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                new TextBlock { Text = "Preview and alpha" },
                appearanceOptions,
                spectrumOptions,
                inputOptions,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(content.Children[2], 2);
        Grid.SetRow(appearanceOptions, 3);
        Grid.SetRow(spectrumOptions, 4);
        Grid.SetRow(inputOptions, 5);
        Grid.SetRow(errorText, 6);
        Grid.SetRow(buttons, 7);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static ComboBox CreateCombo(IEnumerable<string> items, string selected)
            => new()
            {
                ItemsSource = items,
                SelectedItem = selected,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

        static WrapPanel CreateOptionPanel(params Control[] options)
        {
            var panel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                ItemSpacing = 18,
                LineSpacing = 8,
            };
            foreach (var option in options)
            {
                panel.Children.Add(option);
            }

            return panel;
        }

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowAutoCompleteBoxPropertiesDialogAsync(AutoCompleteBoxEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var textEditor = new TextBox { Text = state.Text };
        var watermarkEditor = new TextBox { Text = state.Watermark, Watermark = "Search hint" };
        var prefixEditor = new TextBox { Text = state.MinimumPrefixLength, Watermark = "0 or greater" };
        var delayEditor = new TextBox { Text = state.MinimumPopulateDelay, Watermark = "milliseconds or hh:mm:ss" };
        var filterModeEditor = new ComboBox
        {
            ItemsSource = DesignerAutoCompleteBoxRuntime.FilterModeNames,
            SelectedItem = state.FilterMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var maxHeightEditor = new TextBox { Text = state.MaxDropDownHeight, Watermark = "Infinity or pixels" };
        var completionEditor = new CheckBox
        {
            Content = "Enable text completion",
            IsChecked = state.IsTextCompletionEnabled,
        };
        var openEditor = new CheckBox
        {
            Content = "Open drop-down initially",
            IsChecked = state.IsDropDownOpen,
        };

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Text", textEditor, 0, 0);
        AddField(fields, "Watermark", watermarkEditor, 0, 1);
        AddField(fields, "Filter mode", filterModeEditor, 0, 2);
        AddField(fields, "Minimum prefix", prefixEditor, 1, 0);
        AddField(fields, "Populate delay", delayEditor, 1, 1);
        AddField(fields, "Max drop-down height", maxHeightEditor, 1, 2);

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit AutoCompleteBox - {state.ControlName}",
            Width = 760,
            Height = 430,
            MinWidth = 640,
            MinHeight = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerAutoCompleteBoxEditorInput(
                textEditor.Text ?? string.Empty,
                watermarkEditor.Text ?? string.Empty,
                completionEditor.IsChecked == true,
                prefixEditor.Text ?? string.Empty,
                delayEditor.Text ?? string.Empty,
                filterModeEditor.SelectedItem?.ToString() ?? string.Empty,
                maxHeightEditor.Text ?? string.Empty,
                openEditor.IsChecked == true);
            if (!Vm.SetSelectedAutoCompleteBoxProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var options = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children = { completionEditor, openEditor },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure static autocomplete behavior. AsyncPopulator and selector delegates remain code-owned and are not synthesized by the designer.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                options,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(options, 2);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowTogglePropertiesDialogAsync(ToggleEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var isRadioButton = state.ControlKind == nameof(DesignerToggleControlKind.RadioButton);
        var isToggleSwitch = state.ControlKind == nameof(DesignerToggleControlKind.ToggleSwitch);
        var contentEditor = new TextBox { Text = state.Content };
        var stateEditor = new ComboBox
        {
            ItemsSource = DesignerToggleRuntime.StateNames,
            SelectedItem = state.State,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var threeStateEditor = new CheckBox
        {
            Content = "Enable indeterminate state",
            IsChecked = state.IsThreeState,
        };
        var clickModeEditor = new ComboBox
        {
            ItemsSource = DesignerToggleRuntime.ClickModeNames,
            SelectedItem = state.ClickMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var groupNameEditor = new TextBox
        {
            Text = state.GroupName,
            IsEnabled = isRadioButton,
            Watermark = isRadioButton ? "Radio group name" : "RadioButton only",
        };
        var onContentEditor = new TextBox
        {
            Text = state.OnContent,
            IsEnabled = isToggleSwitch,
            Watermark = isToggleSwitch ? "Checked label" : "ToggleSwitch only",
        };
        var offContentEditor = new TextBox
        {
            Text = state.OffContent,
            IsEnabled = isToggleSwitch,
            Watermark = isToggleSwitch ? "Unchecked label" : "ToggleSwitch only",
        };
        var horizontalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerToggleRuntime.HorizontalAlignmentNames,
            SelectedItem = state.HorizontalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalAlignmentEditor = new ComboBox
        {
            ItemsSource = DesignerToggleRuntime.VerticalAlignmentNames,
            SelectedItem = state.VerticalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Content", contentEditor, 0, 0);
        AddField(fields, "State", stateEditor, 0, 1);
        AddField(fields, "Click mode", clickModeEditor, 1, 0);
        AddField(fields, "Radio group", groupNameEditor, 1, 1);
        AddField(fields, "On content", onContentEditor, 2, 0);
        AddField(fields, "Off content", offContentEditor, 2, 1);
        AddField(fields, "Horizontal content alignment", horizontalAlignmentEditor, 3, 0);
        AddField(fields, "Vertical content alignment", verticalAlignmentEditor, 3, 1);

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Toggle & Choice Behavior - {state.ControlName}",
            Width = 720,
            Height = 590,
            MinWidth = 620,
            MinHeight = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerToggleEditorInput(
                contentEditor.Text ?? string.Empty,
                stateEditor.SelectedItem?.ToString() ?? string.Empty,
                threeStateEditor.IsChecked == true,
                clickModeEditor.SelectedItem?.ToString() ?? string.Empty,
                groupNameEditor.Text ?? string.Empty,
                onContentEditor.Text ?? string.Empty,
                offContentEditor.Text ?? string.Empty,
                horizontalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty,
                verticalAlignmentEditor.SelectedItem?.ToString() ?? string.Empty);
            if (!Vm.SetSelectedToggleProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = $"Configure {state.ControlKind} state, activation, and type-specific content.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                threeStateEditor,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(threeStateEditor, 2);
        Grid.SetRow(errorText, 4);
        Grid.SetRow(buttons, 5);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(Grid owner, string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowContainerBehaviorPropertiesDialogAsync(
        ContainerBehaviorEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var isExpander =
            state.ControlKind == nameof(DesignerContainerBehaviorKind.Expander);
        var headerEditor = new TextBox { Text = state.Header };
        var expandedEditor = new CheckBox
        {
            Content = "Expanded in the design and at startup",
            IsChecked = state.IsExpanded,
        };
        var expandDirectionEditor = new ComboBox
        {
            ItemsSource = DesignerContainerBehaviorRuntime.ExpandDirectionNames,
            SelectedItem = state.ExpandDirection,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var horizontalContentAlignmentEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.HorizontalAlignmentNames,
            SelectedItem = state.HorizontalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalContentAlignmentEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.VerticalAlignmentNames,
            SelectedItem = state.VerticalContentAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var horizontalScrollBarEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.ScrollBarVisibilityNames,
            SelectedItem = state.HorizontalScrollBarVisibility,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalScrollBarEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.ScrollBarVisibilityNames,
            SelectedItem = state.VerticalScrollBarVisibility,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var horizontalSnapTypeEditor = new ComboBox
        {
            ItemsSource = DesignerContainerBehaviorRuntime.SnapPointsTypeNames,
            SelectedItem = state.HorizontalSnapPointsType,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalSnapTypeEditor = new ComboBox
        {
            ItemsSource = DesignerContainerBehaviorRuntime.SnapPointsTypeNames,
            SelectedItem = state.VerticalSnapPointsType,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var horizontalSnapAlignmentEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.SnapPointsAlignmentNames,
            SelectedItem = state.HorizontalSnapPointsAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalSnapAlignmentEditor = new ComboBox
        {
            ItemsSource =
                DesignerContainerBehaviorRuntime.SnapPointsAlignmentNames,
            SelectedItem = state.VerticalSnapPointsAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var autoHideEditor = new CheckBox
        {
            Content = "Auto-hide scrollbars",
            IsChecked = state.AllowAutoHide,
        };
        var chainingEditor = new CheckBox
        {
            Content = "Chain scrolling to parent",
            IsChecked = state.IsScrollChainingEnabled,
        };
        var deferredEditor = new CheckBox
        {
            Content = "Use deferred scrolling",
            IsChecked = state.IsDeferredScrollingEnabled,
        };
        var bringIntoViewEditor = new CheckBox
        {
            Content = "Bring focused child into view",
            IsChecked = state.BringIntoViewOnFocusChange,
        };

        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
        };
        if (isExpander)
        {
            AddField(fields, "Header", headerEditor, 0, 0);
            AddField(fields, "Expand direction", expandDirectionEditor, 0, 1);
            AddField(
                fields,
                "Horizontal content alignment",
                horizontalContentAlignmentEditor,
                1,
                0);
            AddField(
                fields,
                "Vertical content alignment",
                verticalContentAlignmentEditor,
                1,
                1);
            switches.Children.Add(expandedEditor);
        }
        else
        {
            AddField(
                fields,
                "Horizontal scrollbar",
                horizontalScrollBarEditor,
                0,
                0);
            AddField(
                fields,
                "Vertical scrollbar",
                verticalScrollBarEditor,
                0,
                1);
            AddField(
                fields,
                "Horizontal snap type",
                horizontalSnapTypeEditor,
                1,
                0);
            AddField(
                fields,
                "Vertical snap type",
                verticalSnapTypeEditor,
                1,
                1);
            AddField(
                fields,
                "Horizontal snap alignment",
                horizontalSnapAlignmentEditor,
                2,
                0);
            AddField(
                fields,
                "Vertical snap alignment",
                verticalSnapAlignmentEditor,
                2,
                1);
            switches.Children.Add(autoHideEditor);
            switches.Children.Add(chainingEditor);
            switches.Children.Add(deferredEditor);
            switches.Children.Add(bringIntoViewEditor);
        }

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Disclosure & Scrolling - {state.ControlName}",
            Width = 740,
            Height = 590,
            MinWidth = 640,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerContainerBehaviorEditorInput(
                headerEditor.Text ?? string.Empty,
                expandedEditor.IsChecked == true,
                expandDirectionEditor.SelectedItem?.ToString() ?? string.Empty,
                horizontalContentAlignmentEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                verticalContentAlignmentEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                horizontalScrollBarEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                verticalScrollBarEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                autoHideEditor.IsChecked == true,
                chainingEditor.IsChecked == true,
                deferredEditor.IsChecked == true,
                bringIntoViewEditor.IsChecked == true,
                horizontalSnapTypeEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                verticalSnapTypeEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                horizontalSnapAlignmentEditor.SelectedItem?.ToString()
                    ?? string.Empty,
                verticalSnapAlignmentEditor.SelectedItem?.ToString()
                    ?? string.Empty);
            if (!Vm.SetSelectedContainerBehaviorProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = isExpander
                        ? "Configure disclosure state, direction, and content alignment without changing the assigned child."
                        : "Configure scrollbar, chaining, focus, and snap behavior without changing the assigned child.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                switches,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(switches, 2);
        Grid.SetRow(errorText, 4);
        Grid.SetRow(buttons, 5);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(
            Grid owner,
            string label,
            Control editor,
            int row,
            int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            owner.Children.Add(field);
        }
    }

    private async Task ShowImagePropertiesDialogAsync(ImageEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var sourceEditor = new TextBox
        {
            Text = state.Source,
            Watermark = "Local file path or file URI",
        };
        var browseButton = new Button { Content = "Browse...", MinWidth = 88 };
        browseButton.Click += async (_, _) =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose image",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Image files")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp"]
                    }
                ]
            });
            if (files.Count > 0 && files[0].Path.IsFile)
            {
                sourceEditor.Text = files[0].Path.AbsoluteUri;
            }
        };
        var clearButton = new Button { Content = "Clear", MinWidth = 72 };
        clearButton.Click += (_, _) => sourceEditor.Text = string.Empty;
        var sourceButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { browseButton, clearButton },
        };
        var sourceField = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 8,
            Children = { sourceEditor, sourceButtons },
        };
        Grid.SetColumn(sourceButtons, 1);

        var stretchEditor = CreateComboBox(
            DesignerImageRuntime.StretchNames,
            state.Stretch);
        var stretchDirectionEditor = CreateComboBox(
            DesignerImageRuntime.StretchDirectionNames,
            state.StretchDirection);
        var interpolationEditor = CreateComboBox(
            DesignerImageRuntime.BitmapInterpolationModeNames,
            state.BitmapInterpolationMode);
        var edgeModeEditor = CreateComboBox(
            DesignerImageRuntime.EdgeModeNames,
            state.EdgeMode);
        var blendingEditor = CreateComboBox(
            DesignerImageRuntime.BitmapBlendingModeNames,
            state.BitmapBlendingMode);
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Source", sourceField, 0, 0, 2);
        AddField(fields, "Stretch", stretchEditor, 1, 0);
        AddField(fields, "Stretch direction", stretchDirectionEditor, 1, 1);
        AddField(fields, "Bitmap interpolation", interpolationEditor, 2, 0);
        AddField(fields, "Edge mode", edgeModeEditor, 2, 1);
        AddField(fields, "Bitmap blending", blendingEditor, 3, 0, 2);

        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Image Source & Rendering - {state.ControlName}",
            Width = 740,
            Height = 520,
            MinWidth = 640,
            MinHeight = 460,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerImageEditorInput(
                sourceEditor.Text ?? string.Empty,
                stretchEditor.SelectedItem?.ToString() ?? string.Empty,
                stretchDirectionEditor.SelectedItem?.ToString() ?? string.Empty,
                interpolationEditor.SelectedItem?.ToString() ?? string.Empty,
                edgeModeEditor.SelectedItem?.ToString() ?? string.Empty,
                blendingEditor.SelectedItem?.ToString() ?? string.Empty);
            if (!Vm.SetSelectedImageProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Choose a local image and configure scaling, interpolation, edge, and blending behavior.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static ComboBox CreateComboBox(
            IReadOnlyList<string> items,
            string selected)
            => new()
            {
                ItemsSource = items,
                SelectedItem = selected,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

        static void AddField(
            Grid owner,
            string label,
            Control editor,
            int row,
            int column,
            int columnSpan = 1)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            Grid.SetColumnSpan(field, columnSpan);
            owner.Children.Add(field);
        }
    }

    private async Task ShowButtonPropertiesDialogAsync(ButtonEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var contentEditor = new TextBox { Text = state.Content };
        var clickModeEditor = new ComboBox
        {
            ItemsSource = DesignerButtonRuntime.ClickModeNames,
            SelectedItem = state.ClickMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var hotKeyEditor = new TextBox
        {
            Text = state.HotKey,
            Watermark = "For example Ctrl+S",
        };
        var commandParameterEditor = new TextBox
        {
            Text = state.CommandParameter,
            Watermark = "Optional static parameter",
        };
        var clickHandlerEditor = new TextBox
        {
            Text = state.ClickHandler,
            Watermark = "For example SaveButton_Click",
        };
        var defaultEditor = new CheckBox
        {
            Content = "Default action for the host Window",
            IsChecked = state.IsDefault,
        };
        var cancelEditor = new CheckBox
        {
            Content = "Cancel action for the host Window",
            IsChecked = state.IsCancel,
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField(fields, "Content", contentEditor, 0, 0);
        AddField(fields, "Click mode", clickModeEditor, 0, 1);
        AddField(fields, "HotKey", hotKeyEditor, 1, 0);
        AddField(
            fields,
            "Command parameter",
            commandParameterEditor,
            1,
            1);
        AddField(fields, "Click handler", clickHandlerEditor, 2, 0, 2);

        var switches = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 18,
            LineSpacing = 8,
            Children = { defaultEditor, cancelEditor },
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Button Actions & Commands - {state.ControlName}",
            Width = 740,
            Height = 580,
            MinWidth = 640,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerButtonEditorInput(
                contentEditor.Text ?? string.Empty,
                clickModeEditor.SelectedItem?.ToString() ?? string.Empty,
                hotKeyEditor.Text ?? string.Empty,
                defaultEditor.IsChecked == true,
                cancelEditor.IsChecked == true,
                commandParameterEditor.Text ?? string.Empty,
                clickHandlerEditor.Text ?? string.Empty);
            if (!Vm.SetSelectedButtonProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var bindingHint = new TextBlock
        {
            Text = "Use Edit Bindings... to connect Command or CommandParameter to the application ViewModel. A Click handler and Command may be declared together.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Opacity = 0.72,
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions =
                new RowDefinitions("Auto,Auto,Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure pointer activation, keyboard activation, Window default/cancel behavior, command data, and the generated Click event declaration.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                switches,
                bindingHint,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(switches, 2);
        Grid.SetRow(bindingHint, 3);
        Grid.SetRow(errorText, 5);
        Grid.SetRow(buttons, 6);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static void AddField(
            Grid owner,
            string label,
            Control editor,
            int row,
            int column,
            int columnSpan = 1)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            Grid.SetColumnSpan(field, columnSpan);
            owner.Children.Add(field);
        }
    }

    private async Task<CommonPropertiesDialogResult?> ShowCommonPropertiesDialogAsync(
        CommonPropertiesEditorState state)
    {
        if (Vm is null)
        {
            return null;
        }

        var marginEditor = new TextBox
        {
            Text = state.Margin,
            Watermark = "Leave unchanged if blank",
        };
        var horizontalEditor = new ComboBox
        {
            ItemsSource = new[] { string.Empty }.Concat(Enum.GetNames<HorizontalAlignment>()),
            SelectedItem = state.HorizontalAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalEditor = new ComboBox
        {
            ItemsSource = new[] { string.Empty }.Concat(Enum.GetNames<VerticalAlignment>()),
            SelectedItem = state.VerticalAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var opacityEditor = new TextBox
        {
            Text = state.Opacity,
            Watermark = "0 to 1; blank keeps each value",
        };
        var enabledEditor = new CheckBox
        {
            Content = "Enabled",
            IsThreeState = true,
            IsChecked = state.IsEnabled,
        };
        var visibleEditor = new CheckBox
        {
            Content = "Visible",
            IsThreeState = true,
            IsChecked = state.IsVisible,
        };
        var hitTestEditor = new CheckBox
        {
            Content = "Hit test visible",
            IsThreeState = true,
            IsChecked = state.IsHitTestVisible,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = "Edit Common Properties",
            Width = 620,
            Height = 470,
            MinWidth = 520,
            MinHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var updated = new CommonPropertiesDialogResult(
                marginEditor.Text ?? string.Empty,
                horizontalEditor.SelectedItem?.ToString() ?? string.Empty,
                verticalEditor.SelectedItem?.ToString() ?? string.Empty,
                opacityEditor.Text ?? string.Empty,
                enabledEditor.IsChecked,
                visibleEditor.IsChecked,
                hitTestEditor.IsChecked);
            if (!Vm.SetSelectedCommonProperties(
                    updated.Margin,
                    updated.HorizontalAlignment,
                    updated.VerticalAlignment,
                    updated.Opacity,
                    updated.IsEnabled,
                    updated.IsVisible,
                    updated.IsHitTestVisible))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close(updated);
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Margin", marginEditor, 0, 0);
        AddField("Horizontal alignment", horizontalEditor, 0, 1);
        AddField("Vertical alignment", verticalEditor, 1, 0);
        AddField("Opacity", opacityEditor, 1, 1);
        AddField("Boolean values (three-state)", new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children = { enabledEditor, visibleEditor, hitTestEditor },
        }, 2, 0, 2);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = state.SelectionLabel,
                    FontWeight = FontWeight.SemiBold,
                },
                new TextBlock
                {
                    Text = "Mixed values are blank. Blank fields keep each selected control's current value; tri-state checkboxes leave mixed values unchanged.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(content.Children[1], 1);
        Grid.SetRow(fields, 2);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;
        return await dialog.ShowDialog<CommonPropertiesDialogResult?>(this);

        void AddField(string label, Control editor, int row, int column, int columnSpan = 1)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            Grid.SetColumnSpan(field, columnSpan);
            fields.Children.Add(field);
        }
    }

    private async Task<Rect?> ShowSelectionBoundsDialogAsync(Rect bounds)
    {
        var xEditor = new TextBox
        {
            Text = bounds.X.ToString("0.###", CultureInfo.InvariantCulture),
            Watermark = "0 or greater",
        };
        var yEditor = new TextBox
        {
            Text = bounds.Y.ToString("0.###", CultureInfo.InvariantCulture),
            Watermark = "0 or greater",
        };
        var widthEditor = new TextBox
        {
            Text = bounds.Width.ToString("0.###", CultureInfo.InvariantCulture),
            Watermark = $"At least {MinSize:0.#}",
        };
        var heightEditor = new TextBox
        {
            Text = bounds.Height.ToString("0.###", CultureInfo.InvariantCulture),
            Watermark = $"At least {MinSize:0.#}",
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = "Edit Selection Bounds",
            Width = 460,
            Height = 360,
            MinWidth = 420,
            MinHeight = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void ApplyBounds()
        {
            if (!TryParse(xEditor, "X", out var x)
                || !TryParse(yEditor, "Y", out var y)
                || !TryParse(widthEditor, "W", out var width)
                || !TryParse(heightEditor, "H", out var height))
            {
                return;
            }

            if (x < 0 || y < 0)
            {
                errorText.Text = "X and Y must be 0 or greater.";
                return;
            }

            if (width < MinSize || height < MinSize)
            {
                errorText.Text = $"W and H must be at least {MinSize:0.#} px.";
                return;
            }

            dialog.Close(new Rect(x, y, width, height));
        }

        bool TryParse(TextBox editor, string label, out double value)
        {
            if (double.TryParse(
                    editor.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value)
                && double.IsFinite(value))
            {
                return true;
            }

            errorText.Text = $"{label} must be a finite number using invariant decimal notation.";
            return false;
        }

        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) => ApplyBounds();
        WireEditorDialogShortcuts(dialog, dialog.Close, ApplyBounds);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("X (px)", xEditor, 0, 0);
        AddField("Y (px)", yEditor, 0, 1);
        AddField("W (px)", widthEditor, 1, 0);
        AddField("H (px)", heightEditor, 1, 1);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Set the union bounds for the selected controls. Each control keeps its relative position and scales with the shared bounds.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        return await dialog.ShowDialog<Rect?>(this);

        void AddField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            fields.Children.Add(field);
        }
    }

    private async Task ShowLayoutPropertiesDialogAsync(LayoutEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var marginEditor = new TextBox { Text = state.Margin };
        var paddingEditor = new TextBox
        {
            Text = state.Padding,
            IsEnabled = state.SupportsPadding,
            Watermark = state.SupportsPadding ? null : "Not supported by this control",
        };
        var horizontalEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<HorizontalAlignment>(),
            SelectedItem = state.HorizontalAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var verticalEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<VerticalAlignment>(),
            SelectedItem = state.VerticalAlignment,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var minWidthEditor = new TextBox { Text = state.MinWidth };
        var minHeightEditor = new TextBox { Text = state.MinHeight };
        var maxWidthEditor = new TextBox
        {
            Text = state.MaxWidth,
            Watermark = "No maximum",
        };
        var maxHeightEditor = new TextBox
        {
            Text = state.MaxHeight,
            Watermark = "No maximum",
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Layout Properties - {state.ControlName}",
            Width = 620,
            Height = 500,
            MinWidth = 520,
            MinHeight = 430,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetSelectedLayoutProperties(
                    marginEditor.Text ?? string.Empty,
                    paddingEditor.Text ?? string.Empty,
                    horizontalEditor.SelectedItem?.ToString() ?? string.Empty,
                    verticalEditor.SelectedItem?.ToString() ?? string.Empty,
                    minWidthEditor.Text ?? string.Empty,
                    minHeightEditor.Text ?? string.Empty,
                    maxWidthEditor.Text ?? string.Empty,
                    maxHeightEditor.Text ?? string.Empty))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Margin", marginEditor, 0, 0);
        AddField("Padding", paddingEditor, 0, 1);
        AddField("Horizontal alignment", horizontalEditor, 1, 0);
        AddField("Vertical alignment", verticalEditor, 1, 1);
        AddField("Minimum width", minWidthEditor, 2, 0);
        AddField("Minimum height", minHeightEditor, 2, 1);
        AddField("Maximum width", maxWidthEditor, 3, 0);
        AddField("Maximum height", maxHeightEditor, 3, 1);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Thickness accepts 1, 2, or 4 comma-separated values. Leave a maximum blank for no limit.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(errorText, 2);
        Grid.SetRow(buttons, 3);
        dialog.Content = content;
        await dialog.ShowDialog(this);
        return;

        void AddField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            fields.Children.Add(field);
        }
    }

    private async Task ShowRootPropertiesDialogAsync(RootEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var rootKindEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<DesignerRootKind>(),
            SelectedItem = state.RootKind,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var titleEditor = new TextBox
        {
            Text = state.Title,
            Watermark = "Application window title",
        };
        var canResizeEditor = new CheckBox
        {
            Content = "Allow the user to resize the window",
            IsChecked = state.CanResize,
        };
        var startupEditor = new ComboBox
        {
            ItemsSource = Enum.GetNames<DesignerWindowStartupLocation>(),
            SelectedItem = state.StartupLocation,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var minWidthEditor = new TextBox { Text = state.MinWidth };
        var minHeightEditor = new TextBox { Text = state.MinHeight };
        var maxWidthEditor = new TextBox
        {
            Text = state.MaxWidth,
            Watermark = "No maximum",
        };
        var maxHeightEditor = new TextBox
        {
            Text = state.MaxHeight,
            Watermark = "No maximum",
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = "Edit Document Root Properties",
            Width = 620,
            Height = 520,
            MinWidth = 520,
            MinHeight = 460,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (!Vm.SetRootProperties(
                    rootKindEditor.SelectedItem?.ToString() ?? string.Empty,
                    titleEditor.Text ?? string.Empty,
                    canResizeEditor.IsChecked == true,
                    startupEditor.SelectedItem?.ToString() ?? string.Empty,
                    minWidthEditor.Text ?? string.Empty,
                    minHeightEditor.Text ?? string.Empty,
                    maxWidthEditor.Text ?? string.Empty,
                    maxHeightEditor.Text ?? string.Empty))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
        };
        AddField("Root type", rootKindEditor, 0, 0);
        AddField("Window startup location", startupEditor, 0, 1);
        AddField("Window title", titleEditor, 1, 0, 2);
        AddField("Minimum width", minWidthEditor, 2, 0);
        AddField("Minimum height", minHeightEditor, 2, 1);
        AddField("Maximum width", maxWidthEditor, 3, 0);
        AddField("Maximum height", maxHeightEditor, 3, 1);

        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Full AXAML saves with the selected root type. Window-only settings are disabled for UserControl.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                canResizeEditor,
                fields,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(canResizeEditor, 1);
        Grid.SetRow(fields, 2);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;

        rootKindEditor.SelectionChanged += (_, _) => UpdateWindowFields();
        UpdateWindowFields();
        await dialog.ShowDialog(this);
        return;

        void UpdateWindowFields()
        {
            var isWindow = string.Equals(
                rootKindEditor.SelectedItem?.ToString(),
                nameof(DesignerRootKind.Window),
                StringComparison.Ordinal);
            titleEditor.IsEnabled = isWindow;
            canResizeEditor.IsEnabled = isWindow;
            startupEditor.IsEnabled = isWindow;
        }

        void AddField(string label, Control editor, int row, int column, int columnSpan = 1)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            Grid.SetColumnSpan(field, columnSpan);
            fields.Children.Add(field);
        }
    }

    private async Task ShowSampleDataEditorDialogAsync(string source)
    {
        if (Vm is null)
        {
            return;
        }

        var editor = new TextBox
        {
            Text = source,
            AcceptsReturn = true,
            AcceptsTab = true,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            FontSize = 13,
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(editor, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(editor, ScrollBarVisibility.Auto);
        var resultText = new TextBlock
        {
            Text = "JSON properties resolve binding paths such as User.Name. Arrays can preview ItemsSource bindings.",
            Foreground = Avalonia.Media.Brushes.SlateGray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = "Edit Sample DataContext",
            Width = 760,
            Height = 620,
            MinWidth = 560,
            MinHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var validateButton = new Button { Content = "Validate", MinWidth = 84 };
        validateButton.Click += (_, _) =>
        {
            var isValid = Vm.TryValidateSampleDataJson(editor.Text ?? string.Empty, out var result);
            resultText.Foreground = isValid
                ? Avalonia.Media.Brushes.SeaGreen
                : Avalonia.Media.Brushes.IndianRed;
            resultText.Text = result;
        };
        var clearButton = new Button
        {
            Content = "Clear Sample",
            MinWidth = 100,
            IsEnabled = Vm.HasSampleData,
        };
        clearButton.Click += (_, _) =>
        {
            if (!Vm.TrySetSampleDataJson(string.Empty, out var result))
            {
                resultText.Foreground = Avalonia.Media.Brushes.IndianRed;
                resultText.Text = result;
                return;
            }

            dialog.Close();
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        void ApplySampleData()
        {
            if (!Vm.TrySetSampleDataJson(editor.Text ?? string.Empty, out var result))
            {
                resultText.Foreground = Avalonia.Media.Brushes.IndianRed;
                resultText.Text = result;
                return;
            }

            dialog.Close();
        }
        applyButton.Click += (_, _) => ApplySampleData();
        editor.KeyDown += (_, e) =>
        {
            HandleTextEditorShortcut(
                e,
                dialog.Close,
                ApplySampleData);
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, clearButton, validateButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 10,
            Children = { resultText, editor, buttons },
        };
        Grid.SetRow(editor, 1);
        Grid.SetRow(buttons, 2);
        dialog.Content = content;
        await dialog.ShowDialog(this);
    }

    private static bool HandleTextEditorShortcut(
        KeyEventArgs e,
        Action close,
        Action apply)
    {
        if (e.Key == Key.Escape)
        {
            close();
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Enter
            && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            apply();
            e.Handled = true;
            return true;
        }

        return false;
    }

    private static void WireEditorDialogShortcuts(
        Window dialog,
        Action close,
        Action apply)
    {
        dialog.KeyDown += (_, e) =>
        {
            HandleTextEditorShortcut(e, close, apply);
        };
    }

    private static void WireEditorDialogShortcuts(Window dialog, Button applyButton)
    {
        WireEditorDialogShortcuts(
            dialog,
            dialog.Close,
            () => applyButton.RaiseEvent(
                new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)));
    }

    private async Task ShowAxamlSourceEditorDialogAsync(string source)
    {
        if (Vm is null)
        {
            return;
        }

        var editor = new TextBox
        {
            Text = source,
            AcceptsReturn = true,
            AcceptsTab = true,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            FontSize = 13,
            TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(editor, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(editor, ScrollBarVisibility.Auto);
        var resultText = new TextBlock
        {
            Text = "Edit the complete Window or UserControl AXAML, then validate or preview before applying.",
            Foreground = Avalonia.Media.Brushes.SlateGray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = "Edit AXAML Source",
            Width = 960,
            Height = 720,
            MinWidth = 640,
            MinHeight = 440,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var validateButton = new Button { Content = "Validate", MinWidth = 84 };
        validateButton.Click += (_, _) =>
        {
            var isValid = Vm.TryValidateAxamlSource(editor.Text ?? string.Empty, out var result);
            resultText.Foreground = isValid
                ? Avalonia.Media.Brushes.SeaGreen
                : Avalonia.Media.Brushes.IndianRed;
            resultText.Text = result;
        };
        var previewButton = new Button { Content = "Preview", MinWidth = 84 };
        previewButton.Click += (_, _) =>
        {
            if (!Vm.TryCreatePreviewDocumentFromAxaml(
                    editor.Text ?? string.Empty,
                    out var document,
                    out var result))
            {
                resultText.Foreground = Avalonia.Media.Brushes.IndianRed;
                resultText.Text = result;
                return;
            }

            resultText.Foreground = Avalonia.Media.Brushes.SeaGreen;
            resultText.Text = result;
            try
            {
                var preview = new PreviewWindow(document);
                preview.Show(dialog);
            }
            catch (Exception ex)
            {
                resultText.Foreground = Avalonia.Media.Brushes.IndianRed;
                resultText.Text = $"AXAML preview failed: {ex.Message}";
            }
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        void ApplySource()
        {
            if (!Vm.TryApplyAxamlSource(editor.Text ?? string.Empty, out var result))
            {
                resultText.Foreground = Avalonia.Media.Brushes.IndianRed;
                resultText.Text = result;
                return;
            }

            ClearDesignGuides();
            dialog.Close();
        }
        applyButton.Click += (_, _) => ApplySource();
        editor.KeyDown += (_, e) =>
        {
            HandleTextEditorShortcut(e, dialog.Close, ApplySource);
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, validateButton, previewButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 10,
            Children = { resultText, editor, buttons },
        };
        Grid.SetRow(editor, 1);
        Grid.SetRow(buttons, 2);
        dialog.Content = content;
        await dialog.ShowDialog(this);
    }

    private async Task<IReadOnlyList<string>?> ShowBindingEditorDialogAsync(BindingEditorState state)
    {
        var editor = new TextBox
        {
            Text = string.Join(Environment.NewLine, state.Lines),
            AcceptsReturn = true,
            MinHeight = 220,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Bindings - {state.ControlName}",
            Width = 720,
            Height = 450,
            MinWidth = 520,
            MinHeight = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        void ApplyBindings()
        {
            var lines = (editor.Text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.None)
                .ToList();
            if (!DesignerBindingRuntime.TryParseEditorLines(
                    state.TargetType,
                    lines,
                    out _,
                    out var error))
            {
                errorText.Text = error;
                return;
            }

            dialog.Close(lines);
        }
        applyButton.Click += (_, _) => ApplyBindings();
        editor.KeyDown += (_, e) =>
        {
            HandleTextEditorShortcut(
                e,
                () => dialog.Close(null),
                ApplyBindings);
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"),
            RowSpacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "One binding per line: Property | Path | Mode | Fallback. Mode and Fallback are optional.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                new TextBlock
                {
                    Text = $"Supported: {string.Join(", ", state.SupportedProperties)}",
                    Foreground = Avalonia.Media.Brushes.SlateGray,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                editor,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(content.Children[1], 1);
        Grid.SetRow(editor, 2);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;
        return await dialog.ShowDialog<IReadOnlyList<string>?>(this);
    }

    private async Task<GridDefinitionOptions?> ShowGridDefinitionsDialogAsync(GridDefinitionEditorState state)
    {
        var rowsEditor = new TextBox { Text = state.RowDefinitions };
        var columnsEditor = new TextBox { Text = state.ColumnDefinitions };
        var showLinesEditor = new CheckBox
        {
            Content = "Show grid lines on the design surface and preview",
            IsChecked = state.ShowGridLines,
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit Grid Definitions - {state.ControlName}",
            Width = 500,
            Height = 350,
            MinWidth = 400,
            MinHeight = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        void ApplyGridDefinitions()
        {
            var rowDefinitions = rowsEditor.Text ?? string.Empty;
            var columnDefinitions = columnsEditor.Text ?? string.Empty;
            if (!DesignerGridDefinitionRuntime.TryParse(
                    rowDefinitions,
                    columnDefinitions,
                    out _,
                    out _,
                    out var error))
            {
                errorText.Text = error;
                return;
            }

            dialog.Close(new GridDefinitionOptions(
                rowDefinitions,
                columnDefinitions,
                showLinesEditor.IsChecked == true));
        }
        applyButton.Click += (_, _) => ApplyGridDefinitions();
        WireEditorDialogShortcuts(dialog, () => dialog.Close(null), ApplyGridDefinitions);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Rows" },
                rowsEditor,
                new TextBlock { Text = "Columns" },
                columnsEditor,
                showLinesEditor,
                errorText,
            },
        };
        var help = new TextBlock
        {
            Text = "Use comma-separated Avalonia GridLength values such as Auto,*,2*,96. Leave a field empty for one implicit cell.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 12,
            Children = { help, fields, buttons },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(buttons, 2);
        dialog.Content = content;

        return await dialog.ShowDialog<GridDefinitionOptions?>(this);
    }

    private async Task ShowGridSplitterPropertiesDialogAsync(
        GridSplitterEditorState state)
    {
        if (Vm is null)
        {
            return;
        }

        var resizeDirectionEditor = new ComboBox
        {
            ItemsSource = DesignerGridSplitterRuntime.ResizeDirectionNames,
            SelectedItem = state.ResizeDirection,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var resizeBehaviorEditor = new ComboBox
        {
            ItemsSource = DesignerGridSplitterRuntime.ResizeBehaviorNames,
            SelectedItem = state.ResizeBehavior,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var keyboardIncrementEditor = new TextBox
        {
            Text = state.KeyboardIncrement,
            Watermark = "Finite non-negative number",
        };
        var dragIncrementEditor = new TextBox
        {
            Text = state.DragIncrement,
            Watermark = "Finite non-negative number",
        };
        var showsPreviewEditor = new CheckBox
        {
            Content = "Show the resize preview adorner",
            IsChecked = state.ShowsPreview,
        };
        var fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 10,
            Children =
            {
                CreateField("Resize direction", resizeDirectionEditor, 0, 0),
                CreateField("Resize behavior", resizeBehaviorEditor, 0, 1),
                CreateField("Keyboard increment", keyboardIncrementEditor, 1, 0),
                CreateField("Drag increment", dragIncrementEditor, 1, 1),
            },
        };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Edit GridSplitter Behavior - {state.ControlName}",
            Width = 680,
            Height = 470,
            MinWidth = 560,
            MinHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            var input = new DesignerGridSplitterEditorInput(
                resizeDirectionEditor.SelectedItem?.ToString() ?? string.Empty,
                resizeBehaviorEditor.SelectedItem?.ToString() ?? string.Empty,
                showsPreviewEditor.IsChecked == true,
                keyboardIncrementEditor.Text ?? string.Empty,
                dragIncrementEditor.Text ?? string.Empty);
            if (!Vm.SetSelectedGridSplitterProperties(input))
            {
                errorText.Text = Vm.StatusText;
                return;
            }

            dialog.Close();
        };
        WireEditorDialogShortcuts(dialog, applyButton);
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close();
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Configure which Grid rows or columns are resized and how keyboard, drag, and preview increments behave. Assign the splitter to a Grid cell separately.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
                fields,
                showsPreviewEditor,
                errorText,
                buttons,
            },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(showsPreviewEditor, 2);
        Grid.SetRow(errorText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;
        await dialog.ShowDialog(this);

        static Control CreateField(string label, Control editor, int row, int column)
        {
            var field = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = label },
                    editor,
                },
            };
            Grid.SetRow(field, row);
            Grid.SetColumn(field, column);
            return field;
        }
    }

    private async Task<GridCellAssignmentOptions?> ShowGridCellAssignmentDialogAsync(
        GridCellAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var rowEditor = new NumericUpDown { Minimum = 1, Value = state.GridRow + 1 };
        var columnEditor = new NumericUpDown { Minimum = 1, Value = state.GridColumn + 1 };
        var rowSpanEditor = new NumericUpDown { Minimum = 1, Value = state.GridRowSpan };
        var columnSpanEditor = new NumericUpDown { Minimum = 1, Value = state.GridColumnSpan };
        var errorText = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.IndianRed,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Assign to Grid Cell - {state.ControlName}",
            Width = 460,
            Height = 440,
            MinWidth = 380,
            MinHeight = 380,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateLimits()
        {
            if (parentSelector.SelectedItem is not GridCellParentOption parent)
            {
                return;
            }

            rowEditor.Maximum = parent.RowCount;
            columnEditor.Maximum = parent.ColumnCount;
            rowSpanEditor.Maximum = parent.RowCount;
            columnSpanEditor.Maximum = parent.ColumnCount;
        }

        parentSelector.SelectionChanged += (_, _) => UpdateLimits();
        UpdateLimits();

        var applyButton = new Button { Content = "Assign", MinWidth = 84 };
        applyButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not GridCellParentOption parent)
            {
                errorText.Text = "Choose a Grid.";
                return;
            }

            var row = (int)(rowEditor.Value ?? 1) - 1;
            var column = (int)(columnEditor.Value ?? 1) - 1;
            var rowSpan = (int)(rowSpanEditor.Value ?? 1);
            var columnSpan = (int)(columnSpanEditor.Value ?? 1);
            if (row + rowSpan > parent.RowCount || column + columnSpan > parent.ColumnCount)
            {
                errorText.Text = $"The cell span must fit within {parent.RowCount} row(s) and {parent.ColumnCount} column(s).";
                return;
            }

            dialog.Close(new GridCellAssignmentOptions(
                parent.DisplayName,
                row,
                column,
                rowSpan,
                columnSpan));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent Grid" },
                parentSelector,
                new TextBlock { Text = "Row (1-based)" },
                rowEditor,
                new TextBlock { Text = "Column (1-based)" },
                columnEditor,
                new TextBlock { Text = "Row span" },
                rowSpanEditor,
                new TextBlock { Text = "Column span" },
                columnSpanEditor,
                errorText,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<GridCellAssignmentOptions?>(this);
    }

    private async Task<StackPanelAssignmentOptions?> ShowStackPanelAssignmentDialogAsync(
        StackPanelAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var positionEditor = new NumericUpDown
        {
            Minimum = 1,
            Value = state.ItemIndex + 1,
        };
        var sizeEditor = new NumericUpDown
        {
            Minimum = 10,
            Maximum = 2000,
            Increment = 4,
            Value = (decimal)state.ItemSize,
        };
        var orientationHint = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Assign to StackPanel - {state.ControlName}",
            Width = 440,
            Height = 350,
            MinWidth = 380,
            MinHeight = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateParentState()
        {
            if (parentSelector.SelectedItem is not StackPanelParentOption parent)
            {
                return;
            }

            positionEditor.Maximum = parent.ChildCount + 1;
            orientationHint.Text = parent.Orientation == Orientation.Vertical
                ? "Item size controls Height. Width stretches to the StackPanel."
                : "Item size controls Width. Height stretches to the StackPanel.";
        }

        parentSelector.SelectionChanged += (_, _) => UpdateParentState();
        UpdateParentState();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not StackPanelParentOption parent)
            {
                return;
            }

            dialog.Close(new StackPanelAssignmentOptions(
                parent.DisplayName,
                (int)(positionEditor.Value ?? 1) - 1,
                (double)(sizeEditor.Value ?? 40)));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent StackPanel" },
                parentSelector,
                new TextBlock { Text = "Position (1-based)" },
                positionEditor,
                new TextBlock { Text = "Item size" },
                sizeEditor,
                orientationHint,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<StackPanelAssignmentOptions?>(this);
    }

    private async Task<DockPanelAssignmentOptions?> ShowDockPanelAssignmentDialogAsync(
        DockPanelAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var positionEditor = new NumericUpDown
        {
            Minimum = 1,
            Value = state.ItemIndex + 1,
        };
        var dockSelector = new ComboBox
        {
            ItemsSource = Enum.GetValues<DesignerDockSide>(),
            SelectedItem = state.Dock,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var sizeEditor = new NumericUpDown
        {
            Minimum = 10,
            Maximum = 2000,
            Increment = 4,
            Value = (decimal)state.ItemSize,
        };
        var lastChildFillCheck = new CheckBox
        {
            Content = "Last child fills remaining space",
            IsChecked = state.LastChildFill,
        };
        var sizeHint = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var dialog = new Window
        {
            Title = $"Assign to DockPanel - {state.ControlName}",
            Width = 460,
            Height = 440,
            MinWidth = 400,
            MinHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateParentState()
        {
            if (parentSelector.SelectedItem is not DockPanelParentOption parent)
            {
                return;
            }

            positionEditor.Maximum = parent.ChildCount + 1;
            lastChildFillCheck.IsChecked = parent.LastChildFill;
        }

        void UpdateSizeHint()
        {
            sizeHint.Text = dockSelector.SelectedItem is DesignerDockSide.Top or DesignerDockSide.Bottom
                ? "Item size controls Height unless this is the LastChildFill item."
                : "Item size controls Width unless this is the LastChildFill item.";
        }

        parentSelector.SelectionChanged += (_, _) => UpdateParentState();
        dockSelector.SelectionChanged += (_, _) => UpdateSizeHint();
        UpdateParentState();
        UpdateSizeHint();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not DockPanelParentOption parent
                || dockSelector.SelectedItem is not DesignerDockSide dock)
            {
                return;
            }

            dialog.Close(new DockPanelAssignmentOptions(
                parent.DisplayName,
                (int)(positionEditor.Value ?? 1) - 1,
                dock,
                (double)(sizeEditor.Value ?? 40),
                lastChildFillCheck.IsChecked == true));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent DockPanel" },
                parentSelector,
                new TextBlock { Text = "Position (1-based)" },
                positionEditor,
                new TextBlock { Text = "Dock side" },
                dockSelector,
                new TextBlock { Text = "Item size" },
                sizeEditor,
                lastChildFillCheck,
                sizeHint,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<DockPanelAssignmentOptions?>(this);
    }

    private async Task<WrapPanelAssignmentOptions?> ShowWrapPanelAssignmentDialogAsync(
        WrapPanelAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var positionEditor = new NumericUpDown
        {
            Minimum = 1,
            Value = state.ItemIndex + 1,
        };
        var layoutHint = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Text = "Item size, spacing, orientation, and alignment are edited on the parent WrapPanel in the Property Inspector.",
        };
        var dialog = new Window
        {
            Title = $"Assign to WrapPanel - {state.ControlName}",
            Width = 460,
            Height = 320,
            MinWidth = 400,
            MinHeight = 290,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateParentState()
        {
            if (parentSelector.SelectedItem is WrapPanelParentOption parent)
            {
                positionEditor.Maximum = parent.ChildCount + 1;
            }
        }

        parentSelector.SelectionChanged += (_, _) => UpdateParentState();
        UpdateParentState();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not WrapPanelParentOption parent)
            {
                return;
            }

            dialog.Close(new WrapPanelAssignmentOptions(
                parent.DisplayName,
                (int)(positionEditor.Value ?? 1) - 1));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent WrapPanel" },
                parentSelector,
                new TextBlock { Text = "Position (1-based)" },
                positionEditor,
                layoutHint,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<WrapPanelAssignmentOptions?>(this);
    }

    private async Task<UniformGridAssignmentOptions?> ShowUniformGridAssignmentDialogAsync(
        UniformGridAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var positionEditor = new NumericUpDown
        {
            Minimum = 1,
            Value = state.ItemIndex + 1,
        };
        var layoutHint = new TextBlock
        {
            Foreground = Avalonia.Media.Brushes.Gray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Text = "Rows, columns, first column, and spacing are edited on the parent UniformGrid in the Property Inspector.",
        };
        var dialog = new Window
        {
            Title = $"Assign to UniformGrid - {state.ControlName}",
            Width = 460,
            Height = 320,
            MinWidth = 400,
            MinHeight = 290,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateParentState()
        {
            if (parentSelector.SelectedItem is UniformGridParentOption parent)
            {
                positionEditor.Maximum = parent.ChildCount + 1;
            }
        }

        parentSelector.SelectionChanged += (_, _) => UpdateParentState();
        UpdateParentState();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not UniformGridParentOption parent)
            {
                return;
            }

            dialog.Close(new UniformGridAssignmentOptions(
                parent.DisplayName,
                (int)(positionEditor.Value ?? 1) - 1));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent UniformGrid" },
                parentSelector,
                new TextBlock { Text = "Position (1-based)" },
                positionEditor,
                layoutHint,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<UniformGridAssignmentOptions?>(this);
    }

    private async Task<CanvasAssignmentOptions?> ShowCanvasAssignmentDialogAsync(
        CanvasAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var positionEditor = new NumericUpDown { Minimum = 1, Value = state.ItemIndex + 1 };
        var leftEditor = new NumericUpDown
        {
            Minimum = -5000,
            Maximum = 5000,
            Increment = 4,
            Value = (decimal)state.Left,
        };
        var topEditor = new NumericUpDown
        {
            Minimum = -5000,
            Maximum = 5000,
            Increment = 4,
            Value = (decimal)state.Top,
        };
        var dialog = new Window
        {
            Title = $"Assign to Canvas - {state.ControlName}",
            Width = 460,
            Height = 390,
            MinWidth = 400,
            MinHeight = 350,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateParentState()
        {
            if (parentSelector.SelectedItem is CanvasParentOption parent)
            {
                positionEditor.Maximum = parent.ChildCount + 1;
            }
        }

        parentSelector.SelectionChanged += (_, _) => UpdateParentState();
        UpdateParentState();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is not CanvasParentOption parent)
            {
                return;
            }

            dialog.Close(new CanvasAssignmentOptions(
                parent.DisplayName,
                (int)(positionEditor.Value ?? 1) - 1,
                (double)(leftEditor.Value ?? 0),
                (double)(topEditor.Value ?? 0)));
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent Canvas" },
                parentSelector,
                new TextBlock { Text = "Z-order position (1-based)" },
                positionEditor,
                new TextBlock { Text = "Local left" },
                leftEditor,
                new TextBlock { Text = "Local top" },
                topEditor,
                new TextBlock
                {
                    Text = "After assignment, drag or resize the child directly inside the Canvas.",
                    Foreground = Avalonia.Media.Brushes.Gray,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<CanvasAssignmentOptions?>(this);
    }

    private async Task<TabControlAssignmentOptions?> ShowTabControlAssignmentDialogAsync(
        TabControlAssignmentEditorState state)
    {
        var initialParent = state.Parents.First(parent => string.Equals(
            parent.DisplayName,
            state.SelectedParentName,
            StringComparison.OrdinalIgnoreCase));
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = initialParent,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var tabSelector = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var dialog = new Window
        {
            Title = $"Assign to TabControl - {state.ControlName}",
            Width = 460,
            Height = 300,
            MinWidth = 400,
            MinHeight = 270,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateTabs()
        {
            if (parentSelector.SelectedItem is not TabControlParentOption parent)
            {
                tabSelector.ItemsSource = null;
                return;
            }

            tabSelector.ItemsSource = parent.Tabs;
            var preferredIndex = ReferenceEquals(parent, initialParent)
                ? state.TabIndex
                : parent.Tabs.FirstOrDefault(tab => tab.ChildName is null)?.Index ?? 0;
            tabSelector.SelectedItem = parent.Tabs.FirstOrDefault(tab => tab.Index == preferredIndex)
                ?? parent.Tabs[0];
        }

        parentSelector.SelectionChanged += (_, _) => UpdateTabs();
        UpdateTabs();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is TabControlParentOption parent
                && tabSelector.SelectedItem is TabSlotOption tab)
            {
                dialog.Close(new TabControlAssignmentOptions(parent.DisplayName, tab.Index));
            }
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent TabControl" },
                parentSelector,
                new TextBlock { Text = "Tab slot" },
                tabSelector,
                new TextBlock
                {
                    Text = "Each tab accepts one designer child. Assigning to an occupied tab moves its current child back to the Canvas root.",
                    Foreground = Avalonia.Media.Brushes.Gray,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<TabControlAssignmentOptions?>(this);
    }

    private async Task<SplitViewAssignmentOptions?> ShowSplitViewAssignmentDialogAsync(
        SplitViewAssignmentEditorState state)
    {
        var initialParent = state.Parents.First(parent => string.Equals(
            parent.DisplayName,
            state.SelectedParentName,
            StringComparison.OrdinalIgnoreCase));
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = initialParent,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var slotSelector = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var dialog = new Window
        {
            Title = $"Assign to SplitView - {state.ControlName}",
            Width = 460,
            Height = 300,
            MinWidth = 400,
            MinHeight = 270,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateSlots()
        {
            if (parentSelector.SelectedItem is not SplitViewParentOption parent)
            {
                slotSelector.ItemsSource = null;
                return;
            }

            var slots = new[]
            {
                new SplitViewSlotChoice(DesignerSplitViewSlot.Pane, parent.PaneChildName),
                new SplitViewSlotChoice(DesignerSplitViewSlot.Content, parent.ContentChildName),
            };
            slotSelector.ItemsSource = slots;
            var preferredSlot = ReferenceEquals(parent, initialParent)
                ? state.Slot
                : parent.ContentChildName is null
                    ? DesignerSplitViewSlot.Content
                    : DesignerSplitViewSlot.Pane;
            slotSelector.SelectedItem = slots.First(slot => slot.Slot == preferredSlot);
        }

        parentSelector.SelectionChanged += (_, _) => UpdateSlots();
        UpdateSlots();

        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is SplitViewParentOption parent
                && slotSelector.SelectedItem is SplitViewSlotChoice slot)
            {
                dialog.Close(new SplitViewAssignmentOptions(parent.DisplayName, slot.Slot));
            }
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Parent SplitView" },
                parentSelector,
                new TextBlock { Text = "Slot" },
                slotSelector,
                new TextBlock
                {
                    Text = "Pane and Content each accept one designer child. Assigning to an occupied slot moves its current child back to the Canvas root.",
                    Foreground = Avalonia.Media.Brushes.Gray,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<SplitViewAssignmentOptions?>(this);
    }

    private async Task<ContentAssignmentOptions?> ShowContentAssignmentDialogAsync(
        ContentAssignmentEditorState state)
    {
        var parentSelector = new ComboBox
        {
            ItemsSource = state.Parents,
            SelectedItem = state.Parents.First(parent => string.Equals(
                parent.DisplayName,
                state.SelectedParentName,
                StringComparison.OrdinalIgnoreCase)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var dialog = new Window
        {
            Title = $"Assign as Container Content - {state.ControlName}",
            Width = 440,
            Height = 230,
            MinWidth = 380,
            MinHeight = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var assignButton = new Button { Content = "Assign", MinWidth = 84 };
        assignButton.Click += (_, _) =>
        {
            if (parentSelector.SelectedItem is ContentParentOption parent)
            {
                dialog.Close(new ContentAssignmentOptions(parent.DisplayName));
            }
        };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, assignButton },
        };
        var fields = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Parent content container" },
                parentSelector,
                new TextBlock
                {
                    Text = "Border, ScrollViewer, and Expander accept one designer child. Existing designer content is moved back to the Canvas root.",
                    Foreground = Avalonia.Media.Brushes.Gray,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<ContentAssignmentOptions?>(this);
    }

    private async Task<ComponentPackExportOptions?> ShowComponentPackExportDialogAsync(
        string packName,
        string displayName,
        string namePrefix)
    {
        var packNameEditor = new TextBox { Text = packName };
        var displayNameEditor = new TextBox { Text = displayName };
        var namePrefixEditor = new TextBox { Text = namePrefix };
        var dialog = new Window
        {
            Title = "Export Component Pack",
            Width = 480,
            Height = 340,
            MinWidth = 400,
            MinHeight = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "Export", MinWidth = 84 };
        applyButton.Click += (_, _) => dialog.Close(new ComponentPackExportOptions(
            packNameEditor.Text ?? string.Empty,
            displayNameEditor.Text ?? string.Empty,
            namePrefixEditor.Text ?? string.Empty));
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);

        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Pack name" },
                packNameEditor,
                new TextBlock { Text = "Toolbox display name" },
                displayNameEditor,
                new TextBlock { Text = "Name prefix (letters, numbers, underscores)" },
                namePrefixEditor,
            },
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var layout = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 16,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = layout;

        return await dialog.ShowDialog<ComponentPackExportOptions?>(this);
    }

    private async Task<ComponentPackManagementAction?> ShowComponentPackManagerDialogAsync(
        IReadOnlyList<ComponentPackInfo> packs)
    {
        var packSelector = new ListBox
        {
            ItemsSource = packs,
            SelectedIndex = packs.Count > 0 ? 0 : -1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = 150,
        };
        var sourceText = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var componentText = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var dialog = new Window
        {
            Title = "Manage Component Packs",
            Width = 680,
            Height = 460,
            MinWidth = 520,
            MinHeight = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        void UpdateDetails()
        {
            if (packSelector.SelectedItem is not ComponentPackInfo pack)
            {
                sourceText.Text = "No external component packs are loaded.";
                componentText.Text = string.Empty;
                return;
            }

            sourceText.Text = $"{pack.SourceKindLabel}: {pack.SourceLabel}";
            componentText.Text = $"Components: {pack.ComponentSummary}";
        }

        packSelector.SelectionChanged += (_, _) => UpdateDetails();
        UpdateDetails();

        var removeButton = new Button
        {
            Content = "Remove Pack",
            MinWidth = 108,
            IsEnabled = packs.Count > 0,
        };
        removeButton.Click += (_, _) =>
        {
            if (packSelector.SelectedItem is ComponentPackInfo pack)
            {
                dialog.Close(new ComponentPackManagementAction(pack.SourceId));
            }
        };
        var closeButton = new Button { Content = "Close", MinWidth = 84 };
        closeButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { closeButton, removeButton },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto,Auto"),
            RowSpacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "Select a JSON or plugin pack to remove its Toolbox entries.",
                    TextWrapping = TextWrapping.Wrap,
                },
                packSelector,
                sourceText,
                componentText,
                buttons,
            },
        };
        Grid.SetRow(packSelector, 1);
        Grid.SetRow(sourceText, 2);
        Grid.SetRow(componentText, 3);
        Grid.SetRow(buttons, 4);
        dialog.Content = content;

        return await dialog.ShowDialog<ComponentPackManagementAction?>(this);
    }

    private async Task<IReadOnlyDictionary<string, string>?> ShowAppearanceEditorDialogAsync(
        string controlName,
        IReadOnlyDictionary<string, string> appearance)
    {
        var editors = new Dictionary<string, TextBox>(StringComparer.Ordinal);
        var fields = new StackPanel { Spacing = 6 };
        foreach (var pair in appearance)
        {
            var editor = new TextBox { Text = pair.Value };
            editors[pair.Key] = editor;
            fields.Children.Add(new TextBlock { Text = pair.Key });
            fields.Children.Add(editor);
        }

        var dialog = new Window
        {
            Title = $"Edit Appearance - {controlName}",
            Width = 500,
            Height = Math.Clamp(190 + appearance.Count * 58, 280, 560),
            MinWidth = 400,
            MinHeight = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) => dialog.Close(
            editors
                .Where(pair => !string.Equals(
                    pair.Value.Text ?? string.Empty,
                    appearance[pair.Key],
                    StringComparison.Ordinal))
                .ToDictionary(pair => pair.Key, pair => pair.Value.Text ?? string.Empty, StringComparer.Ordinal));
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var help = new TextBlock
        {
            Text = "Brushes accept Avalonia values such as #2563EB. Leave a brush blank to clear it.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 12,
            Children = { help, fields, buttons },
        };
        Grid.SetRow(fields, 1);
        Grid.SetRow(buttons, 2);
        dialog.Content = content;

        return await dialog.ShowDialog<IReadOnlyDictionary<string, string>?>(this);
    }

    private async Task<ColorResourceApplicationOptions?> ShowColorResourceApplicationDialogAsync(
        string controlName,
        IReadOnlyList<string> resourceNames,
        IReadOnlyList<string> propertyNames)
    {
        var resourceSelector = new ComboBox
        {
            ItemsSource = resourceNames,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var propertySelector = new ComboBox
        {
            ItemsSource = propertyNames,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var dialog = new Window
        {
            Title = $"Apply Color Resource - {controlName}",
            Width = 460,
            Height = 260,
            MinWidth = 380,
            MinHeight = 230,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) => dialog.Close(new ColorResourceApplicationOptions(
            resourceSelector.SelectedItem?.ToString() ?? string.Empty,
            propertySelector.SelectedItem?.ToString() ?? string.Empty));
        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };
        var fields = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Color resource" },
                resourceSelector,
                new TextBlock { Text = "Target property" },
                propertySelector,
            },
        };
        var content = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 12,
            Children = { fields, buttons },
        };
        Grid.SetRow(buttons, 1);
        dialog.Content = content;

        return await dialog.ShowDialog<ColorResourceApplicationOptions?>(this);
    }

    private async Task<string?> ShowTextEditorDialogAsync(string title, string content, string helpText, bool multiline = true)
    {
        var editor = new TextBox
        {
            Text = content,
            AcceptsReturn = multiline,
            MinHeight = multiline ? 160 : 32,
        };

        var dialog = new Window
        {
            Title = title,
            Width = 460,
            Height = multiline ? 330 : 190,
            MinWidth = 360,
            MinHeight = multiline ? 240 : 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var applyButton = new Button { Content = "Apply", MinWidth = 84 };
        applyButton.Click += (_, _) => dialog.Close(editor.Text ?? string.Empty);
        editor.KeyDown += (_, e) =>
        {
            HandleTextEditorShortcut(
                e,
                () => dialog.Close(null),
                () => dialog.Close(editor.Text ?? string.Empty));
        };

        var cancelButton = new Button { Content = "Cancel", MinWidth = 84 };
        cancelButton.Click += (_, _) => dialog.Close(null);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancelButton, applyButton },
        };

        var layout = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 12,
            Children =
            {
                new TextBlock { Text = helpText },
                editor,
                buttons,
            },
        };
        Grid.SetRow(editor, 1);
        Grid.SetRow(buttons, 2);
        dialog.Content = layout;

        return await dialog.ShowDialog<string?>(this);
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowCloseWithoutPrompt)
        {
            DisposeProjectWorkspaceWatcher();
            return;
        }

        e.Cancel = true;
        _ = RequestCloseAsync();
    }

    private async Task RequestCloseAsync()
    {
        FlushPendingPropertyHistory();
        if (Vm is null)
        {
            return;
        }

        var originalTab = Vm.SelectedDocumentTab;
        foreach (var tab in Vm.DocumentTabs.ToList())
        {
            if (!ReferenceEquals(Vm.SelectedDocumentTab, tab))
            {
                Vm.ActivateDocumentTab(tab);
            }

            if (!await EnsureCanContinueWithUnsavedChangesAsync())
            {
                if (originalTab is not null && !ReferenceEquals(Vm.SelectedDocumentTab, originalTab))
                {
                    Vm.ActivateDocumentTab(originalTab);
                }

                return;
            }
        }

        SyncWorkspacePanelState();
        CaptureDesignGuidesForTab(Vm.SelectedDocumentTab);
        CaptureViewportForTab(Vm.SelectedDocumentTab);
        Vm.SaveSession();
        _allowCloseWithoutPrompt = true;
        _previewWindow?.Close();
        _previewWindow = null;
        Close();
    }
}
