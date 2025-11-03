using System;
using System.Buffers.Binary;
using System.Threading.Tasks;
using FluentModbus;

namespace Scada.Client.Services;

/// <summary>
/// Modbus TCP client service implementation using FluentModbus.
/// </summary>
public class ModbusClientService : IModbusClientService, IDisposable
{
    private ModbusTcpClient? _client;
    private byte _unitId;

    public bool IsConnected => _client != null && _client.IsConnected;

    public async Task ConnectAsync(string host, int port, byte unitId)
    {
        _unitId = unitId;
        _client = new ModbusTcpClient();
        await Task.Run(() => _client.Connect(host, ModbusEndianness.BigEndian));
    }

    public async Task DisconnectAsync()
    {
        await Task.Run(() =>
        {
            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        });
    }

    public async Task<ushort> ReadHoldingRegisterAsync(ushort address)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("Modbus client is not connected.");

        return await Task.Run(() =>
        {
            var result = _client.ReadHoldingRegisters(_unitId, address, 1);
            return BinaryPrimitives.ReadUInt16BigEndian(result);
        });
    }

    public async Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("Modbus client is not connected.");

        return await Task.Run(() =>
        {
            var result = _client.ReadHoldingRegisters(_unitId, startAddress, count);
            var values = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = BinaryPrimitives.ReadUInt16BigEndian(result.Slice(i * 2));
            }
            return values;
        });
    }

    public async Task<ushort[]> ReadInputRegistersAsync(ushort startAddress, ushort count)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("Modbus client is not connected.");

        return await Task.Run(() =>
        {
            var result = _client.ReadInputRegisters(_unitId, startAddress, count);
            var values = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = BinaryPrimitives.ReadUInt16BigEndian(result.Slice(i * 2));
            }
            return values;
        });
    }

    public async Task WriteHoldingRegisterAsync(ushort address, ushort value)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("Modbus client is not connected.");

        await Task.Run(() => _client.WriteSingleRegister(_unitId, address, value));
    }

    public async Task WriteCoilAsync(ushort address, bool value)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("Modbus client is not connected.");

        await Task.Run(() => _client.WriteSingleCoil(_unitId, address, value));
    }

    public async Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("Modbus client is not connected.");

        return await Task.Run(() =>
        {
            var bytes = _client.ReadCoils(_unitId, startAddress, count);
            var result = new bool[count];
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[i / 8];
                int bit = i % 8;
                result[i] = (b & (1 << bit)) != 0;
            }
            return result;
        });
    }

    public async Task<bool[]> ReadDiscreteInputsAsync(ushort startAddress, ushort count)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("Modbus client is not connected.");

        return await Task.Run(() =>
        {
            var bytes = _client.ReadDiscreteInputs(_unitId, startAddress, count);
            var result = new bool[count];
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[i / 8];
                int bit = i % 8;
                result[i] = (b & (1 << bit)) != 0;
            }
            return result;
        });
    }

    public async Task<bool> ReadCoilAsync(ushort address)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("Modbus client is not connected.");

        return await Task.Run(() =>
        {
            var bytes = _client.ReadCoils(_unitId, address, 1);
            return (bytes[0] & 1) != 0;
        });
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
