using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Markup.Xaml;
using AvaloniaUIDesigner.App.ViewModels;
using AvaloniaUIDesigner.App.Views;

namespace AvaloniaUIDesigner.App;

public partial class App : Application
{
    private static bool TryOpenStartupPath(
        MainWindowViewModel viewModel,
        string rawPath,
        out string status)
    {
        status = string.Empty;
        var trimmedPath = rawPath.Trim();
        if (trimmedPath.Length >= 2
            && trimmedPath[0] == '"'
            && trimmedPath[^1] == '"')
        {
            trimmedPath = trimmedPath[1..^1];
        }

        if (string.IsNullOrWhiteSpace(trimmedPath))
        {
            status = "Startup path is empty.";
            return false;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(trimmedPath);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException)
        {
            status = $"Startup path is invalid: {exception.Message}";
            return false;
        }

        if (Directory.Exists(fullPath))
        {
            if (!viewModel.TryOpenProjectWorkspace(fullPath, out var error))
            {
                status = $"Could not open startup project folder: {error}";
                return false;
            }

            status = $"Opened startup project {viewModel.ProjectWorkspaceName} ({viewModel.ProjectFiles.Count} AXAML file(s)).";
            return true;
        }

        if (!File.Exists(fullPath))
        {
            status = $"Startup path not found: {fullPath}";
            return false;
        }

        var extension = Path.GetExtension(fullPath);
        if (string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase))
        {
            var workspacePath = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(workspacePath))
            {
                status = "Could not open startup project file because its directory is unavailable.";
                return false;
            }

            if (!viewModel.TryOpenProjectWorkspace(workspacePath, out var error))
            {
                status = $"Could not open startup project file: {error}";
                return false;
            }

            status = $"Opened startup project {viewModel.ProjectWorkspaceName} from {Path.GetFileName(fullPath)} ({viewModel.ProjectFiles.Count} AXAML file(s)).";
            return true;
        }

        if (!string.Equals(extension, ".axaml", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".xaml", StringComparison.OrdinalIgnoreCase))
        {
            status = "Startup path is not an AXAML document, project, or solution file.";
            return false;
        }

        try
        {
            var content = File.ReadAllText(fullPath);
            if (!viewModel.TryOpenDocumentTab(content, fullPath, out var error, out var warning))
            {
                status = $"Could not open startup AXAML: {error}";
                return false;
            }

            status = string.IsNullOrWhiteSpace(warning)
                ? $"Opened startup AXAML {Path.GetFileName(fullPath)}."
                : $"Opened startup AXAML {Path.GetFileName(fullPath)}. {warning}";
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            status = $"Could not read startup AXAML: {exception.Message}";
            return false;
        }
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel();
            if (!viewModel.TryRestoreSession(out var sessionError)
                && !string.IsNullOrWhiteSpace(sessionError))
            {
                viewModel.StatusText = "Previous session could not be restored. Started a new document.";
            }

            var startupPaths = desktop.Args?
                .Where(argument => !string.IsNullOrWhiteSpace(argument)
                    && !argument.StartsWith("-", StringComparison.Ordinal))
                .ToArray()
                ?? Array.Empty<string>();
            if (startupPaths.Length > 0)
            {
                if (startupPaths.Length == 1)
                {
                    TryOpenStartupPath(viewModel, startupPaths[0], out var startupStatus);
                    viewModel.StatusText = startupStatus;
                }
                else
                {
                    var failures = new List<string>();
                    var openedCount = 0;
                    foreach (var startupPath in startupPaths)
                    {
                        if (TryOpenStartupPath(viewModel, startupPath, out var startupStatus))
                        {
                            openedCount++;
                        }
                        else
                        {
                            failures.Add(startupStatus);
                        }
                    }

                    viewModel.StatusText = BuildStartupPathsStatus(openedCount, failures);
                }
            }

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string BuildStartupPathsStatus(
        int openedCount,
        IReadOnlyList<string> failures)
    {
        var summary = openedCount == 0
            ? "No startup paths were opened."
            : $"Opened {openedCount} startup path(s).";
        return failures.Count == 0
            ? summary
            : $"{summary} {string.Join(" ", failures)}";
    }

}
