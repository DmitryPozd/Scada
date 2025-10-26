using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ReactiveUI;
using Scada.Client.Models;
using Scada.Client.Services;

namespace Scada.Client.ViewModels;

/// <summary>
/// ViewModel for the main SCADA window (mnemoscheme).
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly IModbusClientService _modbusService;
    private readonly ISettingsService _settingsService;
    private string _connectionStatus = "Отключен";
    private bool _isConnected;
    private ushort _registerValue;
    private ushort _registerAddress = 0;
    private ModbusConnectionConfig _connectionConfig = new();
    private IDisposable? _pollingSubscription;
    private bool _isPolling;
    private int _pollingIntervalMs = 1000;

    // Mnemoscheme properties
    private bool _pump1Running;
    private bool _pump1Alarm;
    private double _valve1OpenPercent;
    private double _sensor1Value;
    private TagDefinition? _selectedTag;

    // Coil control states
    private bool _coil1Active;
    private bool _coil2Active;
    private bool _coil3Active;
    private bool _motorActive;
    private bool _valveActive;
    private bool _fanActive;

    // Coil addresses (configurable)
    private ushort _coil1Address = 0;
    private ushort _coil2Address = 1;
    private ushort _coil3Address = 2;
    private ushort _motorAddress = 10;
    private ushort _valveAddress = 11;
    private ushort _fanAddress = 12;
    
    private bool _settingsLoaded;
    public bool SettingsLoaded
    {
        get => _settingsLoaded;
        private set => this.RaiseAndSetIfChanged(ref _settingsLoaded, value);
    }
    
    public event EventHandler? SettingsLoadedEvent;

    public MainWindowViewModel()
    {
    _modbusService = new ModbusClientService();
    _settingsService = new SettingsService();

    // Load settings asynchronously on startup
    _ = LoadSettingsAsync();

        ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync);
        DisconnectCommand = ReactiveCommand.CreateFromTask(DisconnectAsync);
            ReadRegisterCommand = ReactiveCommand.CreateFromTask(ReadRegisterAsync, this.WhenAnyValue(vm => vm.IsConnected));
            OpenSettingsCommand = ReactiveCommand.Create(OpenSettings);

            // Coil write commands (enabled only for connected and selected Coils/Bool tag)
            var canWriteCoil = this.WhenAnyValue(vm => vm.IsConnected, vm => vm.SelectedTag,
                (connected, tag) => connected && tag != null && tag.Register == RegisterType.Coils && tag.Type == DataType.Bool);
            CoilOnCommand = ReactiveCommand.CreateFromTask(async () => await WriteSelectedCoilAsync(true), canWriteCoil);
            CoilOffCommand = ReactiveCommand.CreateFromTask(async () => await WriteSelectedCoilAsync(false), canWriteCoil);

            // Direct coil control commands
            var canWrite = this.WhenAnyValue(vm => vm.IsConnected);
            Coil1OnCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _coil1Address, true, v => Coil1Active = v), canWrite);
            Coil1OffCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _coil1Address, false, v => Coil1Active = v), canWrite);
            Coil2OnCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _coil2Address, true, v => Coil2Active = v), canWrite);
            Coil2OffCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _coil2Address, false, v => Coil2Active = v), canWrite);
            Coil3OnCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _coil3Address, true, v => Coil3Active = v), canWrite);
            Coil3OffCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _coil3Address, false, v => Coil3Active = v), canWrite);
            MotorOnCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _motorAddress, true, v => MotorActive = v), canWrite);
            MotorOffCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _motorAddress, false, v => MotorActive = v), canWrite);
            ValveOnCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _valveAddress, true, v => ValveActive = v), canWrite);
            ValveOffCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _valveAddress, false, v => ValveActive = v), canWrite);
            FanOnCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _fanAddress, true, v => FanActive = v), canWrite);
            FanOffCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _fanAddress, false, v => FanActive = v), canWrite);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    public ushort RegisterValue
    {
        get => _registerValue;
        set => this.RaiseAndSetIfChanged(ref _registerValue, value);
    }

    public ushort RegisterAddress
    {
        get => _registerAddress;
        set => this.RaiseAndSetIfChanged(ref _registerAddress, value);
    }

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
    public ReactiveCommand<Unit, Unit> DisconnectCommand { get; }
    public ReactiveCommand<Unit, Unit> ReadRegisterCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit>? CoilOnCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? CoilOffCommand { get; private set; }

    // Direct coil control commands
    public ReactiveCommand<Unit, Unit>? Coil1OnCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? Coil1OffCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? Coil2OnCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? Coil2OffCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? Coil3OnCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? Coil3OffCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? MotorOnCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? MotorOffCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? ValveOnCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? ValveOffCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? FanOnCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? FanOffCommand { get; private set; }

    // Event to request settings window opening (handled in View code-behind)
    public event EventHandler? RequestOpenSettings;

    public ModbusConnectionConfig ConnectionConfig
    {
        get => _connectionConfig;
        set
        {
            this.RaiseAndSetIfChanged(ref _connectionConfig, value);
            // Persist whenever settings are updated from UI
            _ = SaveSettingsAsync();
        }
    }

    public bool IsPolling
    {
        get => _isPolling;
        set => this.RaiseAndSetIfChanged(ref _isPolling, value);
    }

    public int PollingIntervalMs
    {
        get => _pollingIntervalMs;
        set
        {
            if (value < 100) value = 100;
            this.RaiseAndSetIfChanged(ref _pollingIntervalMs, value);
            // keep settings in sync
            ConnectionConfig.PollingIntervalMs = _pollingIntervalMs;
            if (IsPolling)
            {
                RestartPolling();
            }
        }
    }

    private async Task ConnectAsync()
    {
        try
        {
            ConnectionStatus = "Подключение...";
            await _modbusService.ConnectAsync(ConnectionConfig.Host, ConnectionConfig.Port, ConnectionConfig.UnitId);
            IsConnected = true;
            ConnectionStatus = "Подключен";
            // Sync VM polling interval from settings
            PollingIntervalMs = ConnectionConfig.PollingIntervalMs;
            StartPolling();
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Ошибка: {ex.Message}";
            IsConnected = false;
        }
    }

    private async Task DisconnectAsync()
    {
        await _modbusService.DisconnectAsync();
        IsConnected = false;
        ConnectionStatus = "Отключен";
        StopPolling();
    }

    private async Task ReadRegisterAsync()
    {
        try
        {
            var value = await _modbusService.ReadHoldingRegisterAsync(RegisterAddress);
            RegisterValue = value;

            // Demo mapping to mnemoscheme elements
            Pump1Running = IsConnected && (value % 2 == 0);
            Pump1Alarm = value > 50000;
            Valve1OpenPercent = (value % 100);
            Sensor1Value = value;
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Ошибка чтения: {ex.Message}";
        }
    }

    private void OpenSettings()
    {
        RequestOpenSettings?.Invoke(this, EventArgs.Empty);
    }

    private void StartPolling()
    {
        StopPolling();
        IsPolling = true;
        _pollingSubscription = Observable
            .Interval(TimeSpan.FromMilliseconds(PollingIntervalMs), RxApp.TaskpoolScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ =>
            {
                if (IsConnected)
                {
                    await PollTagsOnceAsync();
                }
            });
    }

    private void RestartPolling()
    {
        if (IsPolling)
        {
            StartPolling();
        }
    }

    private void StopPolling()
    {
        _pollingSubscription?.Dispose();
        _pollingSubscription = null;
        IsPolling = false;
    }

    // Mnemoscheme bindable properties
    public bool Pump1Running
    {
        get => _pump1Running;
        set => this.RaiseAndSetIfChanged(ref _pump1Running, value);
    }

    public bool Pump1Alarm
    {
        get => _pump1Alarm;
        set => this.RaiseAndSetIfChanged(ref _pump1Alarm, value);
    }

    public double Valve1OpenPercent
    {
        get => _valve1OpenPercent;
        set => this.RaiseAndSetIfChanged(ref _valve1OpenPercent, value);
    }

    public double Sensor1Value
    {
        get => _sensor1Value;
        set => this.RaiseAndSetIfChanged(ref _sensor1Value, value);
    }

    public TagDefinition? SelectedTag
    {
        get => _selectedTag;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTag, value);
            if (value != null)
            {
                RegisterAddress = value.Address;
            }
        }
    }

    // Coil control properties
    public bool Coil1Active
    {
        get => _coil1Active;
        set => this.RaiseAndSetIfChanged(ref _coil1Active, value);
    }

    public bool Coil2Active
    {
        get => _coil2Active;
        set => this.RaiseAndSetIfChanged(ref _coil2Active, value);
    }

    public bool Coil3Active
    {
        get => _coil3Active;
        set => this.RaiseAndSetIfChanged(ref _coil3Active, value);
    }

    public bool MotorActive
    {
        get => _motorActive;
        set => this.RaiseAndSetIfChanged(ref _motorActive, value);
    }

    public bool ValveActive
    {
        get => _valveActive;
        set => this.RaiseAndSetIfChanged(ref _valveActive, value);
    }

    public bool FanActive
    {
        get => _fanActive;
        set => this.RaiseAndSetIfChanged(ref _fanActive, value);
    }

    // Coil address properties (bindable for UI update)
    public ushort Coil1Address
    {
        get => _coil1Address;
        set => this.RaiseAndSetIfChanged(ref _coil1Address, value);
    }

    public ushort Coil2Address
    {
        get => _coil2Address;
        set => this.RaiseAndSetIfChanged(ref _coil2Address, value);
    }

    public ushort Coil3Address
    {
        get => _coil3Address;
        set => this.RaiseAndSetIfChanged(ref _coil3Address, value);
    }

    public ushort MotorAddress
    {
        get => _motorAddress;
        set => this.RaiseAndSetIfChanged(ref _motorAddress, value);
    }

    public ushort ValveAddress
    {
        get => _valveAddress;
        set => this.RaiseAndSetIfChanged(ref _valveAddress, value);
    }

    public ushort FanAddress
    {
        get => _fanAddress;
        set => this.RaiseAndSetIfChanged(ref _fanAddress, value);
    }

    private async Task PollTagsOnceAsync()
    {
        try
        {
            var enabled = ConnectionConfig.Tags;
            // Process per register type to use appropriate function codes
            await PollGroupAsync(enabled, RegisterType.Holding);
            await PollGroupAsync(enabled, RegisterType.Input);
            await PollGroupCoilsAsync(enabled);
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Ошибка опроса: {ex.Message}";
        }
    }

    private static int GetWordCount(DataType type)
        => type switch
        {
            DataType.UInt16 or DataType.Int16 or DataType.Bool => 1,
            DataType.UInt32 or DataType.Int32 or DataType.Float32 => 2,
            DataType.Int64 or DataType.Double => 4,
            _ => 1
        };

    private async Task PollGroupAsync(System.Collections.ObjectModel.ObservableCollection<TagDefinition> tags, RegisterType regType)
    {
        var group = tags.Where(t => t.Enabled && t.Register == regType).OrderBy(t => t.Address).ToList();
        if (group.Count == 0) return;

        // Compute block span [min, max]
        ushort min = group.Min(t => t.Address);
        int max = group.Max(t => t.Address + GetWordCount(t.Type) - 1);
        int count = max - min + 1;

        ushort[] block = regType == RegisterType.Holding
            ? await _modbusService.ReadHoldingRegistersAsync(min, (ushort)count)
            : await _modbusService.ReadInputRegistersAsync(min, (ushort)count);

        foreach (var tag in group)
        {
            int words = GetWordCount(tag.Type);
            int offset = tag.Address - min;
            var regs = new ushort[words];
            for (int i = 0; i < words; i++) regs[i] = block[offset + i];

            // Apply word order for multi-word types
            if (words > 1 && tag.WordOrder == WordOrder.LowHigh)
            {
                Array.Reverse(regs);
            }

            double val = ParseValue(regs, tag.Type);
            val = val * tag.Scale + tag.Offset;
            tag.Value = val;

            switch (tag.Name)
            {
                case "Pump1Running":
                    Pump1Running = val != 0;
                    break;
                case "Pump1Alarm":
                    Pump1Alarm = val != 0;
                    break;
                case "Valve1Percent":
                    Valve1OpenPercent = val;
                    break;
                case "Sensor1Value":
                    Sensor1Value = val;
                    break;
            }
        }
    }

    private static double ParseValue(ushort[] regs, DataType type)
    {
        switch (type)
        {
            case DataType.Bool:
                return regs[0] != 0 ? 1 : 0;
            case DataType.UInt16:
                return regs[0];
            case DataType.Int16:
                return unchecked((short)regs[0]);
            case DataType.UInt32:
            {
                uint v = ((uint)regs[0] << 16) | regs[1];
                return v;
            }
            case DataType.Int32:
            {
                uint raw = ((uint)regs[0] << 16) | regs[1];
                return unchecked((int)raw);
            }
            case DataType.Float32:
            {
                var bytes = new byte[4]
                {
                    (byte)(regs[0] >> 8), (byte)(regs[0] & 0xFF),
                    (byte)(regs[1] >> 8), (byte)(regs[1] & 0xFF)
                };
                if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
                return BitConverter.ToSingle(bytes, 0);
            }
            case DataType.Int64:
            {
                var bytes = new byte[8]
                {
                    (byte)(regs[0] >> 8), (byte)(regs[0] & 0xFF),
                    (byte)(regs[1] >> 8), (byte)(regs[1] & 0xFF),
                    (byte)(regs[2] >> 8), (byte)(regs[2] & 0xFF),
                    (byte)(regs[3] >> 8), (byte)(regs[3] & 0xFF)
                };
                if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
                long l = BitConverter.ToInt64(bytes, 0);
                return l;
            }
            case DataType.Double:
            {
                var bytes = new byte[8]
                {
                    (byte)(regs[0] >> 8), (byte)(regs[0] & 0xFF),
                    (byte)(regs[1] >> 8), (byte)(regs[1] & 0xFF),
                    (byte)(regs[2] >> 8), (byte)(regs[2] & 0xFF),
                    (byte)(regs[3] >> 8), (byte)(regs[3] & 0xFF)
                };
                if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
                return BitConverter.ToDouble(bytes, 0);
            }
            default:
                return regs[0];
        }
    }

    private async Task PollGroupCoilsAsync(System.Collections.ObjectModel.ObservableCollection<TagDefinition> tags)
    {
        var group = tags.Where(t => t.Enabled && t.Register == RegisterType.Coils).OrderBy(t => t.Address).ToList();
        if (group.Count == 0) return;

        ushort min = group.Min(t => t.Address);
        int max = group.Max(t => t.Address); // coils are 1 bit each
        int count = max - min + 1;

        var block = await _modbusService.ReadCoilsAsync(min, (ushort)count);

        foreach (var tag in group)
        {
            int offset = tag.Address - min;
            bool bit = block[offset];
            double val = bit ? 1.0 : 0.0;
            val = val * tag.Scale + tag.Offset;
            tag.Value = val;

            switch (tag.Name)
            {
                case "Pump1Running":
                    Pump1Running = bit;
                    break;
                case "Pump1Alarm":
                    Pump1Alarm = bit;
                    break;
                case "Valve1Percent":
                    Valve1OpenPercent = val;
                    break;
                case "Sensor1Value":
                    Sensor1Value = val;
                    break;
            }
        }
    }

    private async Task LoadSettingsAsync()
    {
        var loaded = await _settingsService.LoadAsync();
        if (loaded != null)
        {
            ConnectionConfig = loaded;
        }
        
        // Добавляем тестовые теги по умолчанию, если коллекция пустая
        if (ConnectionConfig.Tags.Count == 0)
        {
            InitializeDefaultTags();
        }
        
        // Сигнализируем о завершении загрузки
        SettingsLoaded = true;
        SettingsLoadedEvent?.Invoke(this, EventArgs.Empty);
    }
    
    private void InitializeDefaultTags()
    {
        // Coil теги для кнопок управления
        ConnectionConfig.Tags.Add(new TagDefinition
        {
            Enabled = true,
            Name = "Coil_0",
            Address = 0,
            Register = RegisterType.Coils,
            Type = DataType.Bool
        });
        
        ConnectionConfig.Tags.Add(new TagDefinition
        {
            Enabled = true,
            Name = "Coil_1",
            Address = 1,
            Register = RegisterType.Coils,
            Type = DataType.Bool
        });
        
        ConnectionConfig.Tags.Add(new TagDefinition
        {
            Enabled = true,
            Name = "Coil_2",
            Address = 2,
            Register = RegisterType.Coils,
            Type = DataType.Bool
        });
        
        ConnectionConfig.Tags.Add(new TagDefinition
        {
            Enabled = true,
            Name = "Motor_Coil",
            Address = 10,
            Register = RegisterType.Coils,
            Type = DataType.Bool
        });
        
        ConnectionConfig.Tags.Add(new TagDefinition
        {
            Enabled = true,
            Name = "Valve_Coil",
            Address = 11,
            Register = RegisterType.Coils,
            Type = DataType.Bool
        });
        
        ConnectionConfig.Tags.Add(new TagDefinition
        {
            Enabled = true,
            Name = "Fan_Coil",
            Address = 12,
            Register = RegisterType.Coils,
            Type = DataType.Bool
        });
        
        // Holding register теги для датчиков
        ConnectionConfig.Tags.Add(new TagDefinition
        {
            Enabled = true,
            Name = "Pressure_Sensor",
            Address = 100,
            Register = RegisterType.Holding,
            Type = DataType.UInt16,
            Scale = 0.1,
            Offset = 0
        });
    }

    private Task SaveSettingsAsync()
    {
        return _settingsService.SaveAsync(ConnectionConfig);
    }

    private async Task WriteSelectedCoilAsync(bool value)
    {
        if (SelectedTag == null || SelectedTag.Register != RegisterType.Coils)
            return;
        try
        {
            await _modbusService.WriteCoilAsync(RegisterAddress, value);
            // Optimistic local update; next poll will refresh
            SelectedTag.Value = value ? 1.0 : 0.0;
            if (SelectedTag.Name == "Pump1Running") Pump1Running = value;
            if (SelectedTag.Name == "Pump1Alarm") Pump1Alarm = value;
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Ошибка записи coil: {ex.Message}";
        }
    }

    private async Task WriteCoilWithAddressAsync(Func<ushort> getAddress, bool value, Action<bool> updateState)
    {
        if (!IsConnected)
            return;
        try
        {
            ushort address = getAddress();
            await _modbusService.WriteCoilAsync(address, value);
            updateState(value);
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Ошибка записи coil {getAddress()}: {ex.Message}";
        }
    }

    /// <summary>
    /// Публичный метод для записи coil (используется динамическими кнопками)
    /// </summary>
    public async Task WriteCoilAsync(ushort address, bool value)
    {
        if (!IsConnected)
            return;
        try
        {
            await _modbusService.WriteCoilAsync(address, value);
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Ошибка записи coil {address}: {ex.Message}";
        }
    }

    /// <summary>
    /// Публичный метод для сохранения текущей конфигурации (включая мнемосхему)
    /// </summary>
    public Task SaveConfigurationAsync()
    {
        return SaveSettingsAsync();
    }
}
