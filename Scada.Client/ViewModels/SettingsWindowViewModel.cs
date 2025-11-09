using ReactiveUI;
using Scada.Client.Models;

namespace Scada.Client.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    private string _host = "192.168.1.111";
    private int _port = 502;
    private byte _unitId = 1;
    private int _pollingIntervalMs = 1000;
    private int _themeIndex = 0; // 0 = System, 1 = Light, 2 = Dark

    public string Host
    {
        get => _host;
        set => this.RaiseAndSetIfChanged(ref _host, value);
    }

    public int Port
    {
        get => _port;
        set => this.RaiseAndSetIfChanged(ref _port, value);
    }

    public byte UnitId
    {
        get => _unitId;
        set => this.RaiseAndSetIfChanged(ref _unitId, value);
    }

    public int PollingIntervalMs
    {
        get => _pollingIntervalMs;
        set => this.RaiseAndSetIfChanged(ref _pollingIntervalMs, value);
    }

    public int ThemeIndex
    {
        get => _themeIndex;
        set => this.RaiseAndSetIfChanged(ref _themeIndex, value);
    }

    /// <summary>
    /// Свойство для удобной работы с темой через enum
    /// </summary>
    public ThemePreference Theme
    {
        get => (ThemePreference)_themeIndex;
        set => ThemeIndex = (int)value;
    }

    /// <summary>
    /// Получить ThemePreference из индекса ComboBox
    /// </summary>
    public ThemePreference GetThemePreference()
    {
        return (ThemePreference)_themeIndex;
    }

    /// <summary>
    /// Установить индекс ComboBox из ThemePreference
    /// </summary>
    public void SetThemePreference(ThemePreference theme)
    {
        ThemeIndex = (int)theme;
    }
}
