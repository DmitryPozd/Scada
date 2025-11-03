using System.Threading.Tasks;

namespace Scada.Client.Services;

/// <summary>
/// Interface for Modbus client service.
/// </summary>
public interface IModbusClientService
{
    Task ConnectAsync(string host, int port, byte unitId);
    Task DisconnectAsync();
    Task<ushort> ReadHoldingRegisterAsync(ushort address);
    Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count);
    Task<ushort[]> ReadInputRegistersAsync(ushort startAddress, ushort count);
    Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count);
    Task<bool[]> ReadDiscreteInputsAsync(ushort startAddress, ushort count);
    Task<bool> ReadCoilAsync(ushort address);
    Task WriteHoldingRegisterAsync(ushort address, ushort value);
    Task WriteCoilAsync(ushort address, bool value);
    bool IsConnected { get; }
}
