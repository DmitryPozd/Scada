using System.Collections.ObjectModel;

namespace Scada.Client.Models;

/// <summary>
/// Configuration model for Modbus connection.
/// </summary>
public class ModbusConnectionConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 502;
    public byte UnitId { get; set; } = 1;
    public int PollingIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Tag map (list of Modbus holding register addresses) persisted in settings.
    /// </summary>
    public ObservableCollection<TagDefinition> Tags { get; set; } = new();

    /// <summary>
    /// Mnemoscheme elements (buttons, pumps, valves, sensors) with positions and parameters
    /// </summary>
    public ObservableCollection<MnemoschemeElement> MnemoschemeElements { get; set; } = new();
}
