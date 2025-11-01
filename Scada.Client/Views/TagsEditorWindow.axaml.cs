using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Scada.Client.Views;

public partial class TagsEditorWindow : Window
{
    public TagsEditorWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
