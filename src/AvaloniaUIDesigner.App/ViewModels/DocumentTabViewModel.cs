using System.Collections.Generic;
using Avalonia.Media;

namespace AvaloniaUIDesigner.App.ViewModels;

public sealed class DocumentTabViewModel : ViewModelBase
{
    private static readonly IBrush ActiveTabBackground = Brush.Parse("#FFFFFF");
    private static readonly IBrush InactiveTabBackground = Brush.Parse("#E2E8F0");
    private static readonly IBrush ActiveTabBorderBrush = Brush.Parse("#2563EB");
    private static readonly IBrush InactiveTabBorderBrush = Brush.Parse("#CBD5E1");
    private static readonly IBrush ActiveTabForeground = Brush.Parse("#0F172A");
    private static readonly IBrush InactiveTabForeground = Brush.Parse("#475569");

    private string? _documentPath;
    private string _displayName;
    private bool _isActive;
    private bool _isDirty;
    private bool _canClose;

    internal DocumentTabViewModel(string displayName)
    {
        _displayName = displayName;
    }

    public string? DocumentPath => _documentPath;

    public string DisplayName => _displayName;

    public string Header => $"{_displayName}{(_isDirty ? " *" : string.Empty)}";

    public bool IsActive => _isActive;

    public bool IsDirty => _isDirty;

    public bool CanClose => _canClose;

    public IBrush TabBackground => _isActive ? ActiveTabBackground : InactiveTabBackground;

    public IBrush TabBorderBrush => _isActive ? ActiveTabBorderBrush : InactiveTabBorderBrush;

    public IBrush TabForeground => _isActive ? ActiveTabForeground : InactiveTabForeground;

    internal void Update(string? documentPath, string displayName, bool isDirty, bool isActive, bool canClose)
    {
        SetField(ref _documentPath, documentPath, nameof(DocumentPath));
        SetField(ref _displayName, displayName, nameof(DisplayName));
        SetField(ref _isDirty, isDirty, nameof(IsDirty));
        SetField(ref _isActive, isActive, nameof(IsActive));
        SetField(ref _canClose, canClose, nameof(CanClose));
        OnPropertyChanged(nameof(Header));
        OnPropertyChanged(nameof(TabBackground));
        OnPropertyChanged(nameof(TabBorderBrush));
        OnPropertyChanged(nameof(TabForeground));
    }

    private void SetField<T>(ref T field, T value, string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName ?? string.Empty);
    }
}
