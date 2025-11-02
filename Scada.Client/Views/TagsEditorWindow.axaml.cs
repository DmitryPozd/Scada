using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Scada.Client.Views;

public partial class TagsEditorWindow : Window
{
    public TagsEditorWindow()
    {
        InitializeComponent();
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
