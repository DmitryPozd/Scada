using Avalonia.Controls;
using Avalonia.Interactivity;
using Scada.Client.ViewModels;

namespace Scada.Client.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        // Теги загружаются из tags.json, валидация не требуется
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private async void OpenTagsEditor_Click(object? sender, RoutedEventArgs e)
    {
        var editorWindow = new TagsEditorWindow
        {
            DataContext = new TagsEditorWindowViewModel()
        };

        var vm = editorWindow.DataContext as TagsEditorWindowViewModel;
        if (vm != null)
        {
            await vm.LoadTagsAsync();
        }

        await editorWindow.ShowDialog(this);
    }
}
