using ReactiveUI;

namespace Scada.Client.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    private string _host = "192.168.1.111";
    private int _port = 502;
    private byte _unitId = 1;
    private int _pollingIntervalMs = 1000;

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
}
