using System;
using System.Buffers.Binary;
using System.Threading;
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
    
    // Семафор для предотвращения одновременных записей
    private readonly SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1, 1);
    private DateTime _lastWriteTime = DateTime.MinValue;
    private const int MinWriteDelayMs = 50; // Минимальная задержка между записями

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
        {
            System.Diagnostics.Debug.WriteLine($"WriteHoldingRegister ERROR: client not connected!");
            throw new InvalidOperationException("Modbus client is not connected.");
        }

        System.Diagnostics.Debug.WriteLine($"WriteHoldingRegister: address={address}, value={value}, unitId={_unitId}");
        
        try
        {
            await Task.Run(() => _client.WriteSingleRegister(_unitId, address, value));
            System.Diagnostics.Debug.WriteLine($"WriteHoldingRegister SUCCESS: address={address}, value={value}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WriteHoldingRegister EXCEPTION: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task WriteCoilAsync(ushort address, bool value)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("Modbus client is not connected.");

        await _writeSemaphore.WaitAsync();
        try
        {
            // Проверяем, прошло ли достаточно времени с последней записи
            var timeSinceLastWrite = (DateTime.Now - _lastWriteTime).TotalMilliseconds;
            if (timeSinceLastWrite < MinWriteDelayMs)
            {
                var delayNeeded = (int)(MinWriteDelayMs - timeSinceLastWrite);
                System.Diagnostics.Debug.WriteLine($"WriteCoil: задержка {delayNeeded}мс перед записью адреса {address}");
                await Task.Delay(delayNeeded);
            }

            System.Diagnostics.Debug.WriteLine($"WriteCoil: запись адреса {address} = {value}");
            
            await Task.Run(() => _client.WriteSingleCoil(_unitId, address, value));
            
            _lastWriteTime = DateTime.Now;
            System.Diagnostics.Debug.WriteLine($"WriteCoil: успешно записан адрес {address} = {value}");
        }
        catch (FluentModbus.ModbusException ex)
        {
            System.Diagnostics.Debug.WriteLine($"WriteCoil ERROR: адрес {address}, ошибка: {ex.Message}");
            throw new InvalidOperationException(
                $"Ошибка записи катушки по адресу {address}: {ex.Message}. " +
                $"Проверьте, что адрес существует на контроллере и поддерживает запись.", 
                ex);
        }
        finally
        {
            _writeSemaphore.Release();
        }
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
