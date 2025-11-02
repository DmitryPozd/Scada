using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;
using System.IO;

namespace Scada.Client;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Создаём папку Assets в директории запуска приложения, если её нет
        var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
        if (!Directory.Exists(assetsPath))
        {
            Directory.CreateDirectory(assetsPath);
        }

        // Явно устанавливаем следование за системной темой
        RequestedThemeVariant = ThemeVariant.Default;
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = new ViewModels.MainWindowViewModel();
            desktop.MainWindow = new Views.MainWindow
            {
                DataContext = mainVm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}