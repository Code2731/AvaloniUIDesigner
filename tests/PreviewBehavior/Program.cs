using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Styling;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Views;

AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
AxamlRoundTripChecks.Run();
var input = new DesignerElementSnapshot("Input", "Avalonia.Controls.TextBox", 10, 10, 120, 32,
    new Dictionary<string, string> { ["Text"] = "Original" });
var toggle = new DesignerElementSnapshot("Choice", "Avalonia.Controls.CheckBox", 10, 60, 120, 32,
    new Dictionary<string, string> { ["IsChecked"] = "False" });
var document = new DesignerCanvasDocument([input, toggle]);
var window = new PreviewWindow(document, ThemeVariant.Dark);
window.Show();
try
{
    var layout = (Grid)window.Content!;
    var surface = layout.Children.OfType<Border>().Single();
    var viewport = (ScrollViewer)surface.Child!;
    var log = layout.Children.OfType<Expander>().Single();
    var toolbar = layout.Children.OfType<StackPanel>().Single();
    var reset = toolbar.Children.OfType<Button>().Single(button => Equals(button.Content, "Reset Preview"));
    var liveUpdates = toolbar.Children.OfType<CheckBox>().Single();
    Canvas Canvas() => (Canvas)viewport.Content!;
    TextBox Input() => Canvas().Children.OfType<TextBox>().Single();
    CheckBox Choice() => Canvas().Children.OfType<CheckBox>().Single();
    void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    Assert(Grid.GetRow(surface) != Grid.GetRow(log), "Log must occupy a separate row from the design surface.");
    Assert(Canvas().Children.Count == 2, "Preview chrome must not enter the document canvas.");
    Assert(reset.Focus() && reset.IsFocused, "Reset must be reachable by keyboard focus.");
    Assert(liveUpdates.Focus() && liveUpdates.IsFocused, "Live updates must be reachable by keyboard focus.");
    var clearLogButton = ((DockPanel)log.Content!).Children.OfType<Button>().Single();
    Assert(clearLogButton.Focus() && clearLogButton.IsFocused, "Clear log must be reachable by keyboard focus.");
    Assert(Input().Focus() && Input().IsFocused, "Focus must be able to return to the form being tested.");
    var previousInput = Input();
    previousInput.Text = "User input";
    Choice().IsChecked = true;
    log.IsExpanded = false;
    var size = (window.Width, window.Height);
    reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Assert(Input().Text == "Original" && Choice().IsChecked == false, "Reset must restore design values.");
    Assert(!ReferenceEquals(previousInput, Input()), "Reset must create fresh runtime controls.");
    Assert(!log.IsExpanded, "Reset must preserve the collapsed log.");
    Assert(size == (window.Width, window.Height) && window.RequestedThemeVariant == ThemeVariant.Dark,
        "Reset must preserve window dimensions and theme.");

    window.RefreshDocument(document with { Elements = [input with
    {
        VisualProperties = new Dictionary<string, string> { ["Text"] = "Latest design" },
    }, toggle] });
    Input().Text = "Another test";
    reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Assert(Input().Text == "Latest design", "Reset must use the latest live design snapshot.");
    window.ShowInteractionLog();
    Assert(log.IsExpanded, "Interaction Log command must reveal a collapsed panel.");
    Assert(input.VisualProperties!["Text"] == "Original", "Preview interactions must not modify source snapshots.");
    var stableCanvas = Canvas();
    var stableTitle = window.Title;
    var stableBackground = surface.Background;
    Input().Text = "Keep this test input";
    var failed = false;
    try
    {
        window.RefreshDocument(document with
        {
            RootSettings = new DesignerRootSettings(Title: "Failed document"),
            Settings = new DesignerCanvasSettings(Background: "#112233"),
            ColorResources = new Dictionary<string, string> { ["Broken"] = "not-a-color" },
        });
    }
    catch (FormatException)
    {
        failed = true;
    }
    Assert(failed, "Invalid resources must fail preview preparation.");
    Assert(ReferenceEquals(Canvas(), stableCanvas) && Input().Text == "Keep this test input",
        "Failed preparation must retain the existing controls and interaction state.");
    Assert(window.Title == stableTitle && Equals(surface.Background, stableBackground),
        "Failed preparation must preserve title and background.");
    window.ResetPreview();
    Assert(Input().Text == "Latest design", "Failed preparation must not replace the reset snapshot.");
    foreach (var invalidRoot in new[]
    {
        new DesignerRootSettings(Title: "Invalid width", MinWidth: -1),
        new DesignerRootSettings(Title: "Invalid height", MinHeight: double.NaN),
        new DesignerRootSettings(MinWidth: double.PositiveInfinity),
        new DesignerRootSettings(MaxWidth: double.NaN),
        new DesignerRootSettings(MinHeight: 200, MaxHeight: 100),
        new DesignerRootSettings(MaxHeight: double.NegativeInfinity),
    })
    {
        var retainedCanvas = Canvas();
        var retainedLimits = (window.MinWidth, window.MinHeight, window.MaxWidth, window.MaxHeight);
        Input().Text = "Retain on invalid limits";
        var rejected = false;
        try { window.RefreshDocument(document with { RootSettings = invalidRoot }); }
        catch (ArgumentException) { rejected = true; }
        Assert(rejected, "Invalid root size limits must be rejected.");
        Assert(ReferenceEquals(Canvas(), retainedCanvas) && Input().Text == "Retain on invalid limits"
            && window.Title == stableTitle
            && retainedLimits == (window.MinWidth, window.MinHeight, window.MaxWidth, window.MaxHeight),
            "Invalid limits must not partially update the preview window or discard input.");
        window.ResetPreview();
        Assert(Input().Text == "Latest design", "Invalid limits must not replace the reset snapshot.");
    }
    liveUpdates.IsChecked = false;
    Input().Text = "Paused interaction";
    var pending = document with { Elements = [input with
    {
        VisualProperties = new Dictionary<string, string> { ["Text"] = "Resumed design" },
    }, toggle] };
    window.UpdateLiveDocument(document);
    window.UpdateLiveDocument(pending);
    Assert(Input().Text == "Paused interaction", "Paused live updates must preserve runtime input.");
    liveUpdates.IsChecked = true;
    Assert(Input().Text == "Resumed design", "Resume must apply only the latest pending design.");
    liveUpdates.IsChecked = false;
    window.UpdateLiveDocument(document);
    window.ResetPreview();
    Assert(Input().Text == "Original" && liveUpdates.IsChecked == false,
        "Explicit reset must apply pending design without resuming automatic updates.");
    var errorViewer = layout.Children.OfType<ScrollViewer>().Single();
    var errorText = (TextBlock)errorViewer.Content!;
    Assert(errorViewer.MaxHeight == 96
        && errorViewer.HorizontalScrollBarVisibility == Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        && Grid.GetRow(errorViewer) != Grid.GetRow(surface),
        "Error messages must have a bounded separate row with horizontal wrapping.");
    Input().Text = "Keep paused input";
    window.UpdateLiveDocument(document with
    {
        ColorResources = new Dictionary<string, string> { ["Invalid"] = "not-a-color" },
    });
    reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Assert(Input().Text == "Keep paused input" && !string.IsNullOrEmpty(errorText.Text),
        "Failed Reset button must retain input and show an error without throwing.");
    liveUpdates.IsChecked = true;
    Assert(liveUpdates.IsChecked == false && !string.IsNullOrEmpty(errorText.Text),
        "Failed resume must remain paused and show the failure.");
    window.RefreshDocument(document);
    Assert(string.IsNullOrEmpty(errorText.Text), "A successful manual refresh must clear stale error messages.");
    window.UpdateLiveDocument(pending);
    reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Assert(Input().Text == "Resumed design" && string.IsNullOrEmpty(errorText.Text),
        "Reset retry must apply corrected data and clear the failure message.");
    liveUpdates.IsChecked = true;
    Input().Text = "Keep live input";
    var liveCanvas = Canvas();
    var liveFailure = false;
    try
    {
        window.UpdateLiveDocument(document with
        {
            ColorResources = new Dictionary<string, string> { ["Invalid"] = "not-a-color" },
        });
    }
    catch (FormatException) { liveFailure = true; }
    Assert(liveFailure && !string.IsNullOrEmpty(errorText.Text),
        "Live failures must be visible in Preview and still propagate to the editor.");
    Assert(ReferenceEquals(Canvas(), liveCanvas) && Input().Text == "Keep live input"
        && liveUpdates.IsChecked == true,
        "Live failure must preserve the test state without disabling future updates.");
    reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Assert(ReferenceEquals(Canvas(), liveCanvas) && Input().Text == "Keep live input"
        && errorText.Text!.StartsWith("Preview could not reset:"),
        "Reset after live failure must retry the failed latest design, not silently restore stale data.");
    window.UpdateLiveDocument(document);
    Assert(Input().Text == "Original" && string.IsNullOrEmpty(errorText.Text),
        "The next valid live update must recover automatically and clear the error.");
    window.ResetPreview();
    Assert(Input().Text == "Original", "Recovery must clear the invalid pending snapshot.");
    var action = new DesignerElementSnapshot("Action", "Avalonia.Controls.Button", 10, 10, 120, 32,
        new Dictionary<string, string> { ["__eventHandlers"] = "{\"Click\":\"OnAction\"}" });
    window.RefreshDocument(new DesignerCanvasDocument([action]));
    var oldButton = Canvas().Children.OfType<Button>().Single();
    var logContent = (DockPanel)log.Content!;
    var logPanel = logContent.Children.OfType<Border>().Single();
    var logText = (TextBlock)((ScrollViewer)logPanel.Child!).Content!;
    oldButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Assert(logText.Text!.Contains("Action.Click"), "Current preview events must be logged.");
    window.ResetPreview();
    var cleanLog = logText.Text;
    oldButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Assert(logText.Text == cleanLog, "Events from replaced controls must not pollute the new preview log.");
    Canvas().Children.OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    Assert(logText.Text!.Contains("Action.Click"), "New controls must keep logging after Reset.");
    Console.WriteLine("Preview behavior checks passed.");
}
finally
{
    window.Close();
}
