using System.Text.Json.Serialization;

namespace Scada.Client.Models;

/// <summary>
/// Defines a Modbus holding register tag with optional linear scaling.
/// </summary>
public class TagDefinition
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public ushort Address { get; set; }
    public RegisterType Register { get; set; } = RegisterType.Holding;
    public DataType Type { get; set; } = DataType.UInt16;
    public WordOrder WordOrder { get; set; } = WordOrder.HighLow;
    public double Scale { get; set; } = 1.0;
    public double Offset { get; set; } = 0.0;

    [JsonIgnore]
    public double Value { get; set; }
}
