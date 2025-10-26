using ReactiveUI;
using Scada.Client.Models;

namespace Scada.Client.ViewModels;

/// <summary>
/// ViewModel for Settings window.
/// </summary>
public class SettingsWindowViewModel : ViewModelBase
{
    private ModbusConnectionConfig _connectionConfig = new();

    public ModbusConnectionConfig ConnectionConfig
    {
        get => _connectionConfig;
        set => this.RaiseAndSetIfChanged(ref _connectionConfig, value);
    }

    public string Host
    {
        get => _connectionConfig.Host;
        set
        {
            _connectionConfig.Host = value;
            this.RaisePropertyChanged();
        }
    }

    public int Port
    {
        get => _connectionConfig.Port;
        set
        {
            _connectionConfig.Port = value;
            this.RaisePropertyChanged();
        }
    }

    public byte UnitId
    {
        get => _connectionConfig.UnitId;
        set
        {
            _connectionConfig.UnitId = value;
            this.RaisePropertyChanged();
        }
    }

    public int PollingIntervalMs
    {
        get => _connectionConfig.PollingIntervalMs;
        set
        {
            if (value < 100) value = 100;
            _connectionConfig.PollingIntervalMs = value;
            this.RaisePropertyChanged();
        }
    }

    // For DataGrid ComboBoxes
    public Scada.Client.Models.RegisterType[] RegisterTypes => System.Enum.GetValues<Scada.Client.Models.RegisterType>();
    public Scada.Client.Models.DataType[] DataTypes => System.Enum.GetValues<Scada.Client.Models.DataType>();
    public Scada.Client.Models.WordOrder[] WordOrders => System.Enum.GetValues<Scada.Client.Models.WordOrder>();
}
