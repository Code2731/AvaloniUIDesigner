using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Styling;
using AvaloniaUIDesigner.App.Designer.Core;
using AvaloniaUIDesigner.App.Views;

AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
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
    var reset = layout.Children.OfType<Button>().Single();
    Canvas Canvas() => (Canvas)viewport.Content!;
    TextBox Input() => Canvas().Children.OfType<TextBox>().Single();
    CheckBox Choice() => Canvas().Children.OfType<CheckBox>().Single();
    void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    Assert(Grid.GetRow(surface) != Grid.GetRow(log), "Log must occupy a separate row from the design surface.");
    Assert(Canvas().Children.Count == 2, "Preview chrome must not enter the document canvas.");
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
    Console.WriteLine("Preview behavior checks passed.");
}
finally
{
    window.Close();
}
