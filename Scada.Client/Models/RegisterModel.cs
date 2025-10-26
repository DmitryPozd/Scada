namespace Scada.Client.Models;

/// <summary>
/// Model representing a register on the SCADA system.
/// </summary>
public class RegisterModel
{
    public ushort Address { get; set; }
    public ushort Value { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}
