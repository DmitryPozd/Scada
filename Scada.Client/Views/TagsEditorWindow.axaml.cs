using Avalonia.Controls;
using Avalonia.Interactivity;
using Scada.Client.ViewModels;

namespace Scada.Client.Views;

public partial class TagsEditorWindow : Window
{
    public TagsEditorWindow()
    {
        InitializeComponent();
        
        // Подписываемся на изменения выделения в DataGrid
        this.Opened += (s, e) =>
        {
            var dataGrid = this.FindControl<DataGrid>("ActiveTagsDataGrid");
            if (dataGrid != null && DataContext is TagsEditorWindowViewModel vm)
            {
                dataGrid.SelectionChanged += (sender, args) =>
                {
                    vm.SelectedActiveTags = dataGrid.SelectedItems;
                };
            }
        };
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
