using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;
using LineShape = Avalonia.Controls.Shapes.Line;
using PathShape = Avalonia.Controls.Shapes.Path;
using RectangleShape = Avalonia.Controls.Shapes.Rectangle;

namespace AvaloniaUIDesigner.App.Views;

public sealed class PreviewWindow : Window
{
    public PreviewWindow(DesignerCanvasDocument document)
    {
        var settings = document.Settings ?? new DesignerCanvasSettings();
        var rootSettings = document.RootSettings ?? new DesignerRootSettings();
        Title = rootSettings.Kind == DesignerRootKind.Window
            && !string.IsNullOrEmpty(rootSettings.Title)
                ? rootSettings.Title
                : "Avalonia UI Designer - Preview";
        CanResize = rootSettings.Kind != DesignerRootKind.Window || rootSettings.CanResize;
        WindowStartupLocation = rootSettings.Kind == DesignerRootKind.UserControl
            ? WindowStartupLocation.CenterOwner
            : rootSettings.StartupLocation switch
            {
                DesignerWindowStartupLocation.CenterScreen => WindowStartupLocation.CenterScreen,
                DesignerWindowStartupLocation.CenterOwner => WindowStartupLocation.CenterOwner,
                _ => WindowStartupLocation.Manual,
            };
        MinWidth = rootSettings.MinWidth;
        MinHeight = rootSettings.MinHeight;
        MaxWidth = rootSettings.MaxWidth;
        MaxHeight = rootSettings.MaxHeight;
        var previewMaxWidth = double.IsPositiveInfinity(MaxWidth)
            ? Math.Max(1280, MinWidth)
            : MaxWidth;
        var previewMaxHeight = double.IsPositiveInfinity(MaxHeight)
            ? Math.Max(960, MinHeight)
            : MaxHeight;
        Width = Math.Clamp(settings.Width + 32, MinWidth, previewMaxWidth);
        Height = Math.Clamp(settings.Height + 72, MinHeight, previewMaxHeight);
        var canvas = CreatePreviewCanvas(document);

        Content = new Border
        {
            Background = Brush.Parse(settings.Background),
            Padding = new Thickness(16),
            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Content = canvas,
            },
        };
    }

    internal static Canvas CreatePreviewCanvas(DesignerCanvasDocument document)
    {
        var settings = document.Settings ?? new DesignerCanvasSettings();
        var colorResources = document.ColorResources ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var styles = document.Styles ?? Array.Empty<DesignerStyleDefinition>();
        var canvas = new Canvas
        {
            Background = Brush.Parse(settings.Background),
            Width = Math.Max(settings.Width, document.Elements.Count == 0 ? 0 : document.Elements.Max(element => element.X + element.Width + 32)),
            Height = Math.Max(settings.Height, document.Elements.Count == 0 ? 0 : document.Elements.Max(element => element.Y + element.Height + 32)),
        };
        foreach (var pair in colorResources)
        {
            canvas.Resources[pair.Key] = Brush.Parse(pair.Value);
        }

        var controlsByName = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
        var containersByName = document.Elements
            .Where(element => string.Equals(element.TypeName, "Avalonia.Controls.Grid", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.StackPanel", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.DockPanel", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.WrapPanel", StringComparison.Ordinal)
                || string.Equals(
                    element.TypeName,
                    "Avalonia.Controls.Primitives.UniformGrid",
                    StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.Canvas", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.TabControl", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.SplitView", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.Border", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.ScrollViewer", StringComparison.Ordinal)
                || string.Equals(element.TypeName, "Avalonia.Controls.Expander", StringComparison.Ordinal))
            .ToDictionary(element => element.DisplayName, StringComparer.OrdinalIgnoreCase);
        var childrenByParent = document.Elements
            .Where(element => element.ParentName is not null && containersByName.ContainsKey(element.ParentName))
            .GroupBy(element => element.ParentName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        foreach (var element in document.Elements.Where(element =>
                     element.ParentName is null || !containersByName.ContainsKey(element.ParentName)))
        {
            var control = CreateControl(element, colorResources, styles);
            control.Width = element.Width;
            control.Height = element.Height;
            Canvas.SetLeft(control, element.X);
            Canvas.SetTop(control, element.Y);
            canvas.Children.Add(control);
            controlsByName[element.DisplayName] = control;
            AddContainerChildren(control, element);
        }

        foreach (var label in controlsByName.Values.OfType<Label>())
        {
            if (label.Tag is string targetName && controlsByName.TryGetValue(targetName, out var target))
            {
                label.Target = target;
            }
        }

        if (DesignerSampleDataRuntime.TryParse(
                document.SampleDataJson ?? string.Empty,
                out var sampleData,
                out _)
            && sampleData is not null)
        {
            canvas.DataContext = sampleData.Root;
            foreach (var control in controlsByName.Values)
            {
                DesignerSampleDataRuntime.Apply(control, sampleData.Root);
            }
        }

        return canvas;

        void AddContainerChildren(Control parent, DesignerElementSnapshot parentSnapshot)
        {
            if (!childrenByParent.TryGetValue(parentSnapshot.DisplayName, out var children))
            {
                return;
            }

            var orderedChildren = parent is StackPanel
                ? children.OrderBy(child => child.StackPanelIndex).ToList()
                : parent is DockPanel
                    ? children.OrderBy(child => child.DockPanelIndex).ToList()
                    : parent is WrapPanel
                        ? children.OrderBy(child => child.WrapPanelIndex).ToList()
                        : parent is UniformGrid
                            ? children.OrderBy(child => child.UniformGridIndex).ToList()
                            : parent is Canvas
                                ? children.OrderBy(child => child.CanvasChildIndex).ToList()
                                : parent is TabControl
                                    ? children.OrderBy(child => child.TabIndex).ToList()
                                    : parent is SplitView
                                        ? children.OrderBy(child => child.SplitViewSlot).ToList()
                : children;
            if (parent is Panel panel)
            {
                panel.Children.Clear();
            }

            for (var index = 0; index < orderedChildren.Count; index++)
            {
                var childSnapshot = orderedChildren[index];
                var child = CreateControl(childSnapshot, colorResources, styles);
                switch (parent)
                {
                    case Grid grid:
                        Grid.SetRow(child, Math.Max(0, childSnapshot.GridRow));
                        Grid.SetColumn(child, Math.Max(0, childSnapshot.GridColumn));
                        Grid.SetRowSpan(child, Math.Max(1, childSnapshot.GridRowSpan));
                        Grid.SetColumnSpan(child, Math.Max(1, childSnapshot.GridColumnSpan));
                        grid.Children.Add(child);
                        break;
                    case StackPanel stack:
                        if (stack.Orientation == Orientation.Vertical)
                        {
                            child.Height = Math.Max(10, childSnapshot.StackPanelItemSize);
                        }
                        else
                        {
                            child.Width = Math.Max(10, childSnapshot.StackPanelItemSize);
                        }

                        stack.Children.Add(child);
                        break;
                    case DockPanel dock:
                        DockPanel.SetDock(child, childSnapshot.DockPanelDock switch
                        {
                            DesignerDockSide.Top => Dock.Top,
                            DesignerDockSide.Right => Dock.Right,
                            DesignerDockSide.Bottom => Dock.Bottom,
                            _ => Dock.Left,
                        });
                        if (!dock.LastChildFill || index != orderedChildren.Count - 1)
                        {
                            if (childSnapshot.DockPanelDock is DesignerDockSide.Top or DesignerDockSide.Bottom)
                            {
                                child.Height = Math.Max(10, childSnapshot.DockPanelItemSize);
                            }
                            else
                            {
                                child.Width = Math.Max(10, childSnapshot.DockPanelItemSize);
                            }
                        }

                        dock.Children.Add(child);
                        break;
                    case WrapPanel wrap:
                        wrap.Children.Add(child);
                        break;
                    case UniformGrid uniformGrid:
                        uniformGrid.Children.Add(child);
                        break;
                    case Canvas nestedCanvas:
                        child.Width = Math.Max(10, childSnapshot.Width);
                        child.Height = Math.Max(10, childSnapshot.Height);
                        Canvas.SetLeft(child, childSnapshot.CanvasChildLeft);
                        Canvas.SetTop(child, childSnapshot.CanvasChildTop);
                        nestedCanvas.Children.Add(child);
                        break;
                    case TabControl tabControl:
                    {
                        var tabs = tabControl.Items.OfType<TabItem>().ToList();
                        if (childSnapshot.TabIndex < 0 || childSnapshot.TabIndex >= tabs.Count)
                        {
                            continue;
                        }

                        tabs[childSnapshot.TabIndex].Content = child;
                        break;
                    }
                    case SplitView splitView:
                        if (childSnapshot.SplitViewSlot == DesignerSplitViewSlot.Pane)
                        {
                            splitView.Pane = child;
                        }
                        else
                        {
                            splitView.Content = child;
                        }

                        break;
                    case Border border:
                        border.Child = child;
                        break;
                    case ScrollViewer scrollViewer:
                        scrollViewer.Content = child;
                        break;
                    case Expander expander:
                        expander.Content = child;
                        break;
                    default:
                        continue;
                }

                controlsByName[childSnapshot.DisplayName] = child;
                AddContainerChildren(child, childSnapshot);
            }
        }
    }

    private static Control CreateControl(
        DesignerElementSnapshot snapshot,
        IReadOnlyDictionary<string, string> colorResources,
        IReadOnlyList<DesignerStyleDefinition> styles)
    {
        Control control = snapshot.TypeName switch
        {
            "Avalonia.Controls.Button" => new Button { Content = "Button" },
            "Avalonia.Controls.TextBox" => new TextBox { Watermark = "Type here" },
            "Avalonia.Controls.MaskedTextBox" => new MaskedTextBox
            {
                Mask = "000-0000",
                Text = "5551234",
                Watermark = "Phone number",
            },
            "Avalonia.Controls.AutoCompleteBox" => new AutoCompleteBox
            {
                Watermark = "Search...",
                ItemsSource = new[] { "Alpha", "Beta", "Gamma" },
            },
            "Avalonia.Controls.SelectableTextBlock" => new SelectableTextBlock
            {
                Text = "Select and copy this text",
                SelectionBrush = Brush.Parse("#663B82F6"),
                SelectionForegroundBrush = Brushes.White,
            },
            "Avalonia.Controls.TextBlock" => new TextBlock { Text = "Text" },
            "Avalonia.Controls.Label" => new Label { Content = "Label" },
            "Avalonia.Controls.Image" => new Image { Stretch = Stretch.Uniform },
            "Avalonia.Controls.Shapes.Rectangle" => new Rectangle(),
            "Avalonia.Controls.Shapes.Ellipse" => new Ellipse(),
            "Avalonia.Controls.Shapes.Line" => new Line(),
            "Avalonia.Controls.Shapes.Path" => new PathShape(),
            "Avalonia.Controls.CheckBox" => new CheckBox { Content = "CheckBox" },
            "Avalonia.Controls.RadioButton" => new RadioButton { Content = "Option", GroupName = "Options" },
            "Avalonia.Controls.ToggleSwitch" => new ToggleSwitch { Content = "Toggle" },
            "Avalonia.Controls.Primitives.ToggleButton" => new Avalonia.Controls.Primitives.ToggleButton { Content = "Toggle" },
            "Avalonia.Controls.ComboBox" => new ComboBox
            {
                SelectedIndex = 0,
                Items = { "Option 1", "Option 2", "Option 3" },
            },
            "Avalonia.Controls.ListBox" => new ListBox
            {
                SelectedIndex = 0,
                Items = { "Item 1", "Item 2", "Item 3" },
            },
            "Avalonia.Controls.ItemsControl" => new ItemsControl
            {
                Items = { "Item 1", "Item 2", "Item 3" },
            },
            "Avalonia.Controls.TreeView" => DesignerTreeItemRuntime.CreateDefaultTreeView(),
            "Avalonia.Controls.Menu" => DesignerMenuItemRuntime.CreateDefaultMenu(),
            "Avalonia.Controls.DataGrid" => DesignerDataGridRuntime.CreateDefaultDataGrid(),
            "Avalonia.Controls.Slider" => new Slider { Minimum = 0, Maximum = 100, Value = 50 },
            "Avalonia.Controls.ProgressBar" => new ProgressBar { Minimum = 0, Maximum = 100, Value = 50 },
            "Avalonia.Controls.DatePicker" => new DatePicker(),
            "Avalonia.Controls.CalendarDatePicker" => new CalendarDatePicker { Watermark = "Select date" },
            "Avalonia.Controls.Calendar" => new Avalonia.Controls.Calendar(),
            "Avalonia.Controls.ColorPicker" => new ColorPicker { Color = Color.Parse("#FF3B82F6") },
            "Avalonia.Controls.TimePicker" => new TimePicker(),
            "Avalonia.Controls.NumericUpDown" => new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Increment = 1,
                Value = 50,
            },
            "Avalonia.Controls.TabControl" => CreateDefaultTabControl(),
            "Avalonia.Controls.SplitView" => new SplitView
            {
                DisplayMode = SplitViewDisplayMode.Inline,
                IsPaneOpen = true,
                OpenPaneLength = 140,
                CompactPaneLength = 48,
                PanePlacement = SplitViewPanePlacement.Left,
                PaneBackground = Brush.Parse("#E2E8F0"),
                Pane = new TextBlock { Text = "Navigation pane", Margin = new Thickness(12) },
                Content = new TextBlock { Text = "Main content", Margin = new Thickness(16) },
            },
            "Avalonia.Controls.Expander" => new Expander
            {
                Header = "Advanced options",
                IsExpanded = true,
                Content = new TextBlock { Text = "Expanded content", Margin = new Thickness(8) },
            },
            "Avalonia.Controls.ScrollViewer" => new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = "Scrollable content\n\nUse Edit Content... to add more text.",
                    Margin = new Thickness(8),
                },
            },
            "Avalonia.Controls.Border" => new Border
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
            },
            "Avalonia.Controls.Grid" => new Grid
            {
                RowDefinitions = new RowDefinitions("*,*"),
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                ShowGridLines = true,
            },
            "Avalonia.Controls.StackPanel" => new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
            },
            "Avalonia.Controls.DockPanel" => new DockPanel
            {
                LastChildFill = true,
            },
            "Avalonia.Controls.WrapPanel" => new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                ItemWidth = 96,
                ItemHeight = 36,
                ItemSpacing = 8,
                LineSpacing = 8,
                ItemsAlignment = WrapPanelItemsAlignment.Start,
            },
            "Avalonia.Controls.Primitives.UniformGrid" => new UniformGrid
            {
                Rows = 2,
                Columns = 3,
                FirstColumn = 0,
                RowSpacing = 8,
                ColumnSpacing = 8,
            },
            "Avalonia.Controls.Canvas" => new Canvas
            {
                Background = Brush.Parse("#F8FAFC"),
            },
            _ => new TextBlock { Text = $"[Unsupported: {snapshot.DisplayName}]" },
        };

        ApplyProperties(control, snapshot.VisualProperties, colorResources);
        DesignerStyleRuntime.ApplyStyles(control, styles, colorResources);
        WireInteractiveStyleStates(control, styles, colorResources);
        return control;
    }

    private static void WireInteractiveStyleStates(
        Control control,
        IReadOnlyList<DesignerStyleDefinition> styles,
        IReadOnlyDictionary<string, string> colorResources)
    {
        if (!styles.Any(style =>
                style.PseudoClass is not null
                && string.Equals(style.TargetType, control.GetType().Name, StringComparison.Ordinal)
                && control.Classes.Contains(style.ClassName)))
        {
            return;
        }

        control.PropertyChanged += (_, args) =>
        {
            if (args.Property.Name is "IsEnabled" or "IsPointerOver" or "IsFocused"
                or "IsPressed" or "IsChecked" or "IsExpanded")
            {
                DesignerStyleRuntime.ApplyStyles(control, styles, colorResources);
            }
        };
    }

    private static void ApplyProperties(
        Control control,
        IReadOnlyDictionary<string, string>? properties,
        IReadOnlyDictionary<string, string> colorResources)
    {
        if (properties is null)
        {
            return;
        }

        if (properties.TryGetValue("__bindings", out var bindingsJson)
            && DesignerBindingRuntime.TryDeserialize(bindingsJson, out var bindings))
        {
            DesignerBindingRuntime.ReplaceBindings(control, bindings);
        }

        if (properties.TryGetValue("Classes", out var classes))
        {
            foreach (var className in classes.Split(
                         [' ', '\t', '\r', '\n'],
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Distinct(StringComparer.Ordinal))
            {
                control.Classes.Add(className);
            }
        }

        DesignerLayoutRuntime.Apply(control, properties);
        DesignerTypographyRuntime.Apply(control, properties);
        DesignerTransformRuntime.Apply(control, properties);
        DesignerAccessibilityRuntime.Apply(control, properties);
        DesignerInteractionRuntime.Apply(control, properties);
        DesignerEffectRuntime.Apply(control, properties);
        DesignerRangeRuntime.Apply(control, properties);
        DesignerTextInputRuntime.Apply(control, properties);
        ApplyTemplatedAppearanceProperties(control, properties, colorResources);

        switch (control)
        {
            case Shape shape:
                ApplyShapeProperties(shape, properties, colorResources);
                break;
            case ToggleButton toggleButton:
                DesignerToggleRuntime.Apply(toggleButton, properties);
                break;
            case Button button:
                DesignerButtonRuntime.Apply(button, properties);
                break;
            case SelectableTextBlock selectableTextBlock when selectableTextBlock.GetType() == typeof(SelectableTextBlock):
                if (properties.TryGetValue("Text", out var selectableText))
                {
                    selectableTextBlock.Text = selectableText;
                }

                DesignerSelectableTextBlockRuntime.Apply(selectableTextBlock, properties);
                break;
            case TextBlock textBlock:
                if (properties.TryGetValue("Text", out var textBlockText))
                {
                    textBlock.Text = textBlockText;
                }

                if (properties.TryGetValue("FontSize", out var fontSize)
                    && double.TryParse(fontSize, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFontSize))
                {
                    textBlock.FontSize = Math.Clamp(parsedFontSize, 8, 96);
                }

                if (properties.TryGetValue("FontWeight", out var fontWeight)
                    && TryParseTextWeight(fontWeight, out var parsedFontWeight))
                {
                    textBlock.FontWeight = parsedFontWeight;
                }

                if (properties.TryGetValue("Foreground", out var foreground))
                {
                    TrySetTextForeground(textBlock, foreground, colorResources);
                }

                if (properties.TryGetValue("Background", out var background))
                {
                    TrySetBorderBrush(value => textBlock.Background = value, background, colorResources);
                }

                break;
            case Label label:
                if (properties.TryGetValue("Content", out var labelContent))
                {
                    label.Content = labelContent;
                }

                label.Tag = properties.TryGetValue("Target", out var targetName)
                    && !string.IsNullOrWhiteSpace(targetName)
                    ? targetName
                    : null;
                break;
            case Image image:
                ApplyImageProperties(image, properties);
                break;
            case ComboBox comboBox:
                ApplyComboBoxProperties(comboBox, properties);
                break;
            case ListBox listBox:
                ApplyListBoxProperties(listBox, properties);
                break;
            case TreeView treeView:
                ApplyTreeViewProperties(treeView, properties);
                break;
            case Menu menu:
                ApplyMenuProperties(menu, properties);
                break;
            case DataGrid dataGrid:
                ApplyDataGridProperties(dataGrid, properties);
                break;
            case DatePicker datePicker:
                ApplyDatePickerProperties(datePicker, properties);
                break;
            case CalendarDatePicker calendarDatePicker:
                ApplyCalendarDatePickerProperties(calendarDatePicker, properties);
                break;
            case Avalonia.Controls.Calendar calendar:
                DesignerDateTimeRuntime.Apply(calendar, properties);
                break;
            case ColorPicker colorPicker:
                DesignerColorPickerRuntime.Apply(colorPicker, properties);
                break;
            case AutoCompleteBox autoCompleteBox:
                ApplyAutoCompleteBoxProperties(autoCompleteBox, properties);
                break;
            case MaskedTextBox maskedTextBox:
                DesignerMaskedTextBoxRuntime.Apply(maskedTextBox, properties);
                break;
            case TimePicker timePicker:
                ApplyTimePickerProperties(timePicker, properties);
                break;
            case TabControl tabControl:
                ApplyTabControlProperties(tabControl, properties);
                break;
            case ItemsControl itemsControl when itemsControl.GetType() == typeof(ItemsControl):
                ApplyItemsControlProperties(itemsControl, properties);
                break;
            case SplitView splitView:
                ApplySplitViewProperties(splitView, properties, colorResources);
                break;
            case Expander expander:
                ApplyExpanderProperties(expander, properties);
                break;
            case ScrollViewer scrollViewer:
                ApplyScrollViewerProperties(scrollViewer, properties);
                break;
            case Border border:
                ApplyBorderProperties(border, properties, colorResources);
                break;
            case Grid grid:
                DesignerGridDefinitionRuntime.TryApply(grid, properties, out _);
                if (properties.TryGetValue("ShowGridLines", out var showGrid)
                    && bool.TryParse(showGrid, out var parsedShowGrid))
                {
                    grid.ShowGridLines = parsedShowGrid;
                }
                break;
            case StackPanel stackPanel:
                ApplyStackPanelProperties(stackPanel, properties);
                break;
            case DockPanel dockPanel:
                if (properties.TryGetValue("LastChildFill", out var lastChildFill)
                    && bool.TryParse(lastChildFill, out var parsedLastChildFill))
                {
                    dockPanel.LastChildFill = parsedLastChildFill;
                }
                break;
            case WrapPanel wrapPanel:
                if (properties.TryGetValue("Orientation", out var orientation)
                    && Enum.TryParse<Orientation>(orientation, ignoreCase: true, out var parsedOrientation))
                {
                    wrapPanel.Orientation = parsedOrientation;
                }

                if (properties.TryGetValue("ItemWidth", out var itemWidth)
                    && double.TryParse(itemWidth, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedItemWidth))
                {
                    wrapPanel.ItemWidth = Math.Max(10, parsedItemWidth);
                }

                if (properties.TryGetValue("ItemHeight", out var itemHeight)
                    && double.TryParse(itemHeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedItemHeight))
                {
                    wrapPanel.ItemHeight = Math.Max(10, parsedItemHeight);
                }

                if (properties.TryGetValue("ItemSpacing", out var itemSpacing)
                    && double.TryParse(itemSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedItemSpacing))
                {
                    wrapPanel.ItemSpacing = Math.Max(0, parsedItemSpacing);
                }

                if (properties.TryGetValue("LineSpacing", out var lineSpacing)
                    && double.TryParse(lineSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedLineSpacing))
                {
                    wrapPanel.LineSpacing = Math.Max(0, parsedLineSpacing);
                }

                if (properties.TryGetValue("ItemsAlignment", out var itemsAlignment)
                    && Enum.TryParse<WrapPanelItemsAlignment>(
                        itemsAlignment,
                        ignoreCase: true,
                        out var parsedItemsAlignment))
                {
                    wrapPanel.ItemsAlignment = parsedItemsAlignment;
                }
                break;
            case UniformGrid uniformGrid:
                if (properties.TryGetValue("Rows", out var rows)
                    && int.TryParse(rows, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRows))
                {
                    uniformGrid.Rows = Math.Max(0, parsedRows);
                }

                if (properties.TryGetValue("Columns", out var columns)
                    && int.TryParse(columns, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedColumns))
                {
                    uniformGrid.Columns = Math.Max(0, parsedColumns);
                }

                if (properties.TryGetValue("FirstColumn", out var firstColumn)
                    && int.TryParse(firstColumn, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedFirstColumn))
                {
                    uniformGrid.FirstColumn = Math.Max(0, parsedFirstColumn);
                }

                if (properties.TryGetValue("RowSpacing", out var rowSpacing)
                    && double.TryParse(rowSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRowSpacing))
                {
                    uniformGrid.RowSpacing = Math.Max(0, parsedRowSpacing);
                }

                if (properties.TryGetValue("ColumnSpacing", out var columnSpacing)
                    && double.TryParse(columnSpacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedColumnSpacing))
                {
                    uniformGrid.ColumnSpacing = Math.Max(0, parsedColumnSpacing);
                }
                break;
            case Canvas canvas:
                if (properties.TryGetValue("Background", out var canvasBackground))
                {
                    TrySetBorderBrush(value => canvas.Background = value, canvasBackground, colorResources);
                }
                break;
        }
    }

    private static void TrySetTextForeground(
        TextBlock textBlock,
        string foreground,
        IReadOnlyDictionary<string, string> colorResources)
    {
        TrySetBorderBrush(value => textBlock.Foreground = value, foreground, colorResources);
    }

    private static bool TryParseTextWeight(string value, out FontWeight fontWeight)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "normal":
            case "regular":
            case "400":
                fontWeight = FontWeight.Normal;
                return true;
            case "semibold":
            case "semi-bold":
            case "600":
                fontWeight = FontWeight.SemiBold;
                return true;
            case "bold":
            case "700":
                fontWeight = FontWeight.Bold;
                return true;
            default:
                fontWeight = FontWeight.Normal;
                return false;
        }
    }

    private static void ApplyImageProperties(Image image, IReadOnlyDictionary<string, string> properties)
        => DesignerImageRuntime.Apply(image, properties);

    private static void ApplyComboBoxProperties(ComboBox comboBox, IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("__items", out var itemsJson))
        {
            List<string>? items;
            try
            {
                items = JsonSerializer.Deserialize<List<string>>(itemsJson);
            }
            catch
            {
                items = null;
            }

            if (items is not null)
            {
                comboBox.Items.Clear();
                foreach (var item in items)
                {
                    comboBox.Items.Add(item);
                }
            }
        }

        DesignerSelectionRuntime.Apply(comboBox, properties);
    }

    private static void ApplyDatePickerProperties(DatePicker datePicker, IReadOnlyDictionary<string, string> properties)
        => DesignerDateTimeRuntime.Apply(datePicker, properties);

    private static void ApplyCalendarDatePickerProperties(CalendarDatePicker calendarDatePicker, IReadOnlyDictionary<string, string> properties)
        => DesignerDateTimeRuntime.Apply(calendarDatePicker, properties);

    private static void ApplyTimePickerProperties(TimePicker timePicker, IReadOnlyDictionary<string, string> properties)
        => DesignerDateTimeRuntime.Apply(timePicker, properties);

    private static void ApplyAutoCompleteBoxProperties(
        AutoCompleteBox autoCompleteBox,
        IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("__items", out var itemsJson))
        {
            List<string>? items;
            try
            {
                items = System.Text.Json.JsonSerializer.Deserialize<List<string>>(itemsJson);
            }
            catch
            {
                items = null;
            }

            if (items is not null)
            {
                autoCompleteBox.ItemsSource = items;
            }
        }

        DesignerAutoCompleteBoxRuntime.Apply(autoCompleteBox, properties);
    }

    private static TabControl CreateDefaultTabControl()
        => new()
        {
            SelectedIndex = 0,
            Items =
            {
                CreateTabItem("Overview"),
                CreateTabItem("Details"),
            },
        };

    private static void ApplyTabControlProperties(TabControl tabControl, IReadOnlyDictionary<string, string> properties)
    {
        DesignerTabControlRuntime.Apply(tabControl, properties);

        if (properties.TryGetValue("__tabs", out var tabsJson))
        {
            List<string>? tabs;
            try
            {
                tabs = JsonSerializer.Deserialize<List<string>>(tabsJson);
            }
            catch
            {
                tabs = null;
            }

            if (tabs is not null)
            {
                tabControl.Items.Clear();
                foreach (var tab in tabs)
                {
                    tabControl.Items.Add(CreateTabItem(tab));
                }
            }
        }

        if (properties.TryGetValue("SelectedIndex", out var selectedIndex)
            && int.TryParse(selectedIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSelectedIndex))
        {
            tabControl.SelectedIndex = Math.Clamp(parsedSelectedIndex, -1, tabControl.Items.Count - 1);
        }
    }

    private static TabItem CreateTabItem(string header)
        => new()
        {
            Header = header,
            Content = new TextBlock { Text = $"{header} content", Margin = new Thickness(12) },
        };

    private static void ApplySplitViewProperties(
        SplitView splitView,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, string> colorResources)
    {
        DesignerSplitViewRuntime.Apply(splitView, properties);

        if (properties.TryGetValue("DisplayMode", out var displayMode)
            && Enum.TryParse<SplitViewDisplayMode>(displayMode, true, out var parsedDisplayMode))
        {
            splitView.DisplayMode = parsedDisplayMode;
        }

        if (properties.TryGetValue("IsPaneOpen", out var isPaneOpen)
            && bool.TryParse(isPaneOpen, out var parsedIsPaneOpen))
        {
            splitView.IsPaneOpen = parsedIsPaneOpen;
        }

        if (properties.TryGetValue("OpenPaneLength", out var openPaneLength)
            && double.TryParse(openPaneLength, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedOpenPaneLength))
        {
            splitView.OpenPaneLength = Math.Max(0, parsedOpenPaneLength);
        }

        if (properties.TryGetValue("CompactPaneLength", out var compactPaneLength)
            && double.TryParse(compactPaneLength, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedCompactPaneLength))
        {
            splitView.CompactPaneLength = Math.Max(0, parsedCompactPaneLength);
        }

        if (properties.TryGetValue("PanePlacement", out var panePlacement)
            && Enum.TryParse<SplitViewPanePlacement>(panePlacement, true, out var parsedPanePlacement))
        {
            splitView.PanePlacement = parsedPanePlacement;
        }

        if (properties.TryGetValue("UseLightDismissOverlayMode", out var useLightDismiss)
            && bool.TryParse(useLightDismiss, out var parsedUseLightDismiss))
        {
            splitView.UseLightDismissOverlayMode = parsedUseLightDismiss;
        }

        if (properties.TryGetValue("PaneBackground", out var paneBackground))
        {
            TrySetBorderBrush(
                value => splitView.PaneBackground = value,
                paneBackground,
                colorResources);
        }

        if (properties.TryGetValue("__paneText", out var paneText))
        {
            splitView.Pane = new TextBlock { Text = paneText, Margin = new Thickness(12) };
        }

        if (properties.TryGetValue("__contentText", out var contentText))
        {
            splitView.Content = new TextBlock { Text = contentText, Margin = new Thickness(16) };
        }
    }

    private static void ApplyExpanderProperties(Expander expander, IReadOnlyDictionary<string, string> properties)
    {
        DesignerContainerBehaviorRuntime.Apply(expander, properties);

        if (properties.TryGetValue("__contentText", out var contentText))
        {
            expander.Content = new TextBlock { Text = contentText, Margin = new Thickness(8) };
        }
    }

    private static void ApplyScrollViewerProperties(ScrollViewer scrollViewer, IReadOnlyDictionary<string, string> properties)
    {
        DesignerContainerBehaviorRuntime.Apply(scrollViewer, properties);
        if (properties.TryGetValue("__contentText", out var contentText))
        {
            scrollViewer.Content = new TextBlock { Text = contentText, Margin = new Thickness(8) };
        }
    }

    private static void ApplyListBoxProperties(ListBox listBox, IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("__items", out var itemsJson))
        {
            List<string>? items;
            try
            {
                items = JsonSerializer.Deserialize<List<string>>(itemsJson);
            }
            catch
            {
                items = null;
            }

            if (items is not null)
            {
                listBox.Items.Clear();
                foreach (var item in items)
                {
                    listBox.Items.Add(item);
                }
            }
        }

        DesignerSelectionRuntime.Apply(listBox, properties);
    }

    private static void ApplyItemsControlProperties(
        ItemsControl itemsControl,
        IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("__items", out var itemsJson))
        {
            List<string>? items;
            try
            {
                items = JsonSerializer.Deserialize<List<string>>(itemsJson);
            }
            catch
            {
                items = null;
            }

            if (items is not null)
            {
                itemsControl.Items.Clear();
                foreach (var item in items)
                {
                    itemsControl.Items.Add(item);
                }
            }
        }
        else if (DesignerBindingRuntime.HasBinding(itemsControl, "ItemsSource"))
        {
            itemsControl.Items.Clear();
        }
    }

    private static void ApplyTreeViewProperties(
        TreeView treeView,
        IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("__treeItems", out var treeItemsJson)
            && DesignerTreeItemRuntime.TryDeserialize(treeItemsJson, out var definitions))
        {
            DesignerTreeItemRuntime.ReplaceItems(treeView, definitions);
        }

        DesignerSelectionRuntime.Apply(treeView, properties);
    }

    private static void ApplyMenuProperties(
        Menu menu,
        IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("__menuItems", out var menuItemsJson)
            && DesignerMenuItemRuntime.TryDeserialize(menuItemsJson, out var definitions))
        {
            DesignerMenuItemRuntime.ReplaceItems(menu, definitions);
        }
    }

    private static void ApplyDataGridProperties(
        DataGrid dataGrid,
        IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("__dataGridColumns", out var columnsJson)
            && DesignerDataGridRuntime.TryDeserialize(columnsJson, out var definitions))
        {
            DesignerDataGridRuntime.ReplaceColumns(dataGrid, definitions);
        }

        if (properties.TryGetValue("GridLinesVisibility", out var gridLinesVisibility)
            && Enum.TryParse<DataGridGridLinesVisibility>(
                gridLinesVisibility,
                ignoreCase: true,
                out var parsedGridLinesVisibility))
        {
            dataGrid.GridLinesVisibility = parsedGridLinesVisibility;
        }

        if (properties.TryGetValue("IsReadOnly", out var isReadOnly)
            && bool.TryParse(isReadOnly, out var parsedIsReadOnly))
        {
            dataGrid.IsReadOnly = parsedIsReadOnly;
        }
    }

    private static void ApplyBorderProperties(
        Border border,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, string> colorResources)
    {
        if (properties.TryGetValue("Background", out var background))
        {
            TrySetBorderBrush(value => border.Background = value, background, colorResources);
        }

        if (properties.TryGetValue("BorderBrush", out var borderBrush))
        {
            TrySetBorderBrush(value => border.BorderBrush = value, borderBrush, colorResources);
        }

        if (properties.TryGetValue("BorderThickness", out var borderThickness))
        {
            try
            {
                border.BorderThickness = Thickness.Parse(borderThickness);
            }
            catch (FormatException)
            {
                // Ignore malformed imported thickness values.
            }
        }

        if (properties.TryGetValue("CornerRadius", out var cornerRadius))
        {
            try
            {
                border.CornerRadius = CornerRadius.Parse(cornerRadius);
            }
            catch (FormatException)
            {
                // Ignore malformed imported corner-radius values.
            }
        }

        if (properties.TryGetValue("__contentText", out var contentText))
        {
            border.Child = new TextBlock
            {
                Text = contentText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }
        else
        {
            border.Child = null;
        }
    }

    private static void ApplyTemplatedAppearanceProperties(
        Control control,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, string> colorResources)
    {
        if (control is not Avalonia.Controls.Primitives.TemplatedControl templated)
        {
            return;
        }

        if (properties.TryGetValue("Background", out var background))
        {
            TrySetBorderBrush(value => templated.Background = value, background, colorResources);
        }

        if (properties.TryGetValue("Foreground", out var foreground))
        {
            TrySetBorderBrush(value => templated.Foreground = value, foreground, colorResources);
        }

        if (properties.TryGetValue("BorderBrush", out var borderBrush))
        {
            TrySetBorderBrush(value => templated.BorderBrush = value, borderBrush, colorResources);
        }

        if (properties.TryGetValue("BorderThickness", out var borderThickness))
        {
            try
            {
                templated.BorderThickness = Thickness.Parse(borderThickness);
            }
            catch (FormatException)
            {
                // Ignore malformed imported thickness values.
            }
        }

        if (properties.TryGetValue("CornerRadius", out var cornerRadius))
        {
            try
            {
                templated.CornerRadius = CornerRadius.Parse(cornerRadius);
            }
            catch (FormatException)
            {
                // Ignore malformed imported corner-radius values.
            }
        }
    }

    private static void ApplyShapeProperties(
        Shape shape,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, string> colorResources)
    {
        if (properties.TryGetValue("Fill", out var fill))
        {
            TrySetBorderBrush(value => shape.Fill = value, fill, colorResources);
        }

        if (properties.TryGetValue("Stroke", out var stroke))
        {
            TrySetBorderBrush(value => shape.Stroke = value, stroke, colorResources);
        }

        if (TryReadFiniteDouble(properties, "StrokeThickness", out var strokeThickness))
        {
            shape.StrokeThickness = Math.Max(0, strokeThickness);
        }

        if (properties.TryGetValue("Stretch", out var stretch)
            && Enum.TryParse<Stretch>(stretch, true, out var parsedStretch))
        {
            shape.Stretch = parsedStretch;
        }

        if (properties.TryGetValue("StrokeDashArray", out var dashArray))
        {
            shape.StrokeDashArray ??= [];
            shape.StrokeDashArray.Clear();
            foreach (var value in ParseNonNegativeDoubleList(dashArray))
            {
                shape.StrokeDashArray.Add(value);
            }
        }

        if (TryReadFiniteDouble(properties, "StrokeDashOffset", out var dashOffset))
        {
            shape.StrokeDashOffset = dashOffset;
        }

        if (properties.TryGetValue("StrokeLineCap", out var lineCap)
            && Enum.TryParse<PenLineCap>(lineCap, true, out var parsedLineCap))
        {
            shape.StrokeLineCap = parsedLineCap;
        }

        if (properties.TryGetValue("StrokeJoin", out var lineJoin)
            && Enum.TryParse<PenLineJoin>(lineJoin, true, out var parsedLineJoin))
        {
            shape.StrokeJoin = parsedLineJoin;
        }

        if (TryReadFiniteDouble(properties, "StrokeMiterLimit", out var miterLimit))
        {
            shape.StrokeMiterLimit = Math.Max(0, miterLimit);
        }

        if (shape is RectangleShape rectangle)
        {
            if (TryReadFiniteDouble(properties, "RadiusX", out var radiusX))
            {
                rectangle.RadiusX = Math.Max(0, radiusX);
            }

            if (TryReadFiniteDouble(properties, "RadiusY", out var radiusY))
            {
                rectangle.RadiusY = Math.Max(0, radiusY);
            }
        }
        else if (shape is LineShape line)
        {
            if (TryReadPoint(properties, "StartPoint", out var startPoint))
            {
                line.StartPoint = startPoint;
            }

            if (TryReadPoint(properties, "EndPoint", out var endPoint))
            {
                line.EndPoint = endPoint;
            }
        }
        else if (shape is PathShape path
                 && properties.TryGetValue("Data", out var data))
        {
            try
            {
                path.Data = string.IsNullOrWhiteSpace(data)
                    ? null
                    : Geometry.Parse(data);
                path.Tag = string.IsNullOrWhiteSpace(data)
                    ? null
                    : new DesignerPathDataMetadata(data);
            }
            catch (Exception exception) when (
                exception is FormatException or ArgumentException or System.IO.InvalidDataException)
            {
                // Keep Preview available when imported Path data is malformed.
            }
        }
    }

    private static bool TryReadFiniteDouble(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        out double value)
    {
        value = 0;
        return properties.TryGetValue(propertyName, out var rawValue)
            && double.TryParse(
                rawValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value)
            && double.IsFinite(value);
    }

    private static IEnumerable<double> ParseNonNegativeDoubleList(string value)
    {
        foreach (var token in value.Split(
                     [',', ' ', '\t'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (double.TryParse(
                    token,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var parsed)
                && double.IsFinite(parsed)
                && parsed >= 0)
            {
                yield return parsed;
            }
        }
    }

    private static bool TryReadPoint(
        IReadOnlyDictionary<string, string> properties,
        string propertyName,
        out Point point)
    {
        point = default;
        if (!properties.TryGetValue(propertyName, out var value))
        {
            return false;
        }

        try
        {
            point = Point.Parse(value);
            return double.IsFinite(point.X) && double.IsFinite(point.Y);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void TrySetBorderBrush(
        Action<IBrush?> applyBrush,
        string value,
        IReadOnlyDictionary<string, string> colorResources)
    {
        if (DesignerResourceReferenceMetadata.TryParseExpression(value, out var resourceKey))
        {
            if (!colorResources.TryGetValue(resourceKey, out var resourceValue)
                || string.IsNullOrWhiteSpace(resourceValue))
            {
                return;
            }

            value = resourceValue;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            applyBrush(null);
            return;
        }

        try
        {
            applyBrush(Brush.Parse(value));
        }
        catch (FormatException)
        {
            // Ignore malformed imported brushes while keeping the preview available.
        }
    }

    private static void ApplyStackPanelProperties(StackPanel stackPanel, IReadOnlyDictionary<string, string> properties)
    {
        if (properties.TryGetValue("Orientation", out var orientation)
            && Enum.TryParse<Orientation>(orientation, ignoreCase: true, out var parsedOrientation))
        {
            stackPanel.Orientation = parsedOrientation;
        }

        if (properties.TryGetValue("Spacing", out var spacing)
            && double.TryParse(spacing, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedSpacing))
        {
            stackPanel.Spacing = parsedSpacing;
        }

        if (!properties.TryGetValue("__children", out var childrenJson))
        {
            return;
        }

        List<StackPanelChildSnapshot>? children;
        try
        {
            children = JsonSerializer.Deserialize<List<StackPanelChildSnapshot>>(childrenJson);
        }
        catch
        {
            return;
        }

        if (children is null)
        {
            return;
        }

        stackPanel.Children.Clear();
        foreach (var child in children)
        {
            Control? control = child.TypeName switch
            {
                "TextBlock" => new TextBlock { Text = child.Text ?? string.Empty },
                "Button" => new Button { Content = child.Content ?? string.Empty },
                "TextBox" => new TextBox
                {
                    Text = string.IsNullOrEmpty(child.PasswordChar) ? child.Text ?? string.Empty : string.Empty,
                    Watermark = child.Watermark,
                    PasswordChar = string.IsNullOrEmpty(child.PasswordChar) ? '\0' : child.PasswordChar[0],
                    RevealPassword = child.RevealPassword ?? false,
                    AcceptsReturn = child.AcceptsReturn ?? false,
                    TextWrapping = Enum.TryParse<TextWrapping>(child.TextWrapping, ignoreCase: true, out var textWrapping)
                        ? textWrapping
                        : TextWrapping.NoWrap,
                },
                _ => null,
            };

            if (control is not null)
            {
                stackPanel.Children.Add(control);
            }
        }
    }

    private sealed record StackPanelChildSnapshot(
        string TypeName,
        string? Text = null,
        string? Content = null,
        string? Watermark = null,
        string? PasswordChar = null,
        bool? RevealPassword = null,
        bool? AcceptsReturn = null,
        string? TextWrapping = null);
}
