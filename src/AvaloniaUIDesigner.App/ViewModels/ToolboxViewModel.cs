using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaUIDesigner.App.Designer.Contracts;
using AvaloniaUIDesigner.App.Designer.Services;
using AvaloniaUIDesigner.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUIDesigner.App.ViewModels;

public partial class ToolboxViewModel : ViewModelBase
{
    public ToolboxViewModel()
        : this(new BuiltInComponentCatalog())
    {
    }

    public ToolboxViewModel(IComponentCatalog componentCatalog)
    {
        Items = new ObservableCollection<ToolboxItem>(
            componentCatalog
                .GetAll()
                .Select(def => new ToolboxItem(def.DisplayName, def.AvaloniaTypeName)));
    }

    public ObservableCollection<ToolboxItem> Items { get; }

    [ObservableProperty]
    private ToolboxItem? _selectedItem;
}
