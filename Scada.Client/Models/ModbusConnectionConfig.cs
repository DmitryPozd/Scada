using System.Collections.ObjectModel;

namespace Scada.Client.Models;

/// <summary>
/// Configuration model for Modbus connection.
/// </summary>
public class ModbusConnectionConfig
{
    public string Host { get; set; } = "192.168.1.111";
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

    /// <summary>
    /// Размер и позиция главного окна
    /// </summary>
    public double WindowWidth { get; set; } = 1400;
    public double WindowHeight { get; set; } = 900;
    public double WindowX { get; set; } = 100;
    public double WindowY { get; set; } = 100;
    public bool IsMaximized { get; set; } = false;
}
