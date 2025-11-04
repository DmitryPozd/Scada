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
    private readonly ITagsConfigService _tagsConfigService;
    private string _connectionStatus = "Отключен";
    private bool _isConnected;
    private ushort _registerValue;
    private ushort _registerAddress = 0;
    private ModbusConnectionConfig _connectionConfig = new();
    private IDisposable? _pollingSubscription;
    private bool _isPolling;
    private int _pollingIntervalMs = 1000;

    // Event for coil tags update
    public event EventHandler<Dictionary<ushort, bool>>? CoilTagsUpdated;

    // Event for register tags update (Holding and Input registers)
    public event EventHandler<Dictionary<ushort, ushort>>? RegisterTagsUpdated;

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
    
    // Read coil button properties
    private ushort _readCoilAddress = 100;
    private bool? _readCoilValue;
    
    // Momentary button properties
    private bool _momentaryActive;
    private ushort _momentaryAddress = 20;
    
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
    _tagsConfigService = new TagsConfigService();

    // Load settings asynchronously on startup
    _ = LoadSettingsAsync();

        ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync);
        DisconnectCommand = ReactiveCommand.CreateFromTask(DisconnectAsync);
            ReadRegisterCommand = ReactiveCommand.CreateFromTask(ReadRegisterAsync, this.WhenAnyValue(vm => vm.IsConnected));
            OpenSettingsCommand = ReactiveCommand.Create(OpenSettings);
            OpenTagsEditorCommand = ReactiveCommand.Create(OpenTagsEditor);

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
            
            // Read coil command
            ReadCoilCommand = ReactiveCommand.CreateFromTask(ReadCoilAsync, canWrite);
            
            // Momentary button commands
            MomentaryOnCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _momentaryAddress, true, v => MomentaryActive = v), canWrite);
            MomentaryOffCommand = ReactiveCommand.CreateFromTask(async () => await WriteCoilWithAddressAsync(() => _momentaryAddress, false, v => MomentaryActive = v), canWrite);
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
    public ReactiveCommand<Unit, Unit> OpenTagsEditorCommand { get; }
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
    
    // Read coil command
    public ReactiveCommand<Unit, Unit>? ReadCoilCommand { get; private set; }
    
    // Momentary button commands
    public ReactiveCommand<Unit, Unit>? MomentaryOnCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? MomentaryOffCommand { get; private set; }

    // Event to request settings window opening (handled in View code-behind)
    public event EventHandler? RequestOpenSettings;
    
    // Event to request tags editor window opening (handled in View code-behind)
    public event EventHandler? RequestOpenTagsEditor;

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
    
    private void OpenTagsEditor()
    {
        RequestOpenTagsEditor?.Invoke(this, EventArgs.Empty);
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

    // Read coil button properties
    public ushort ReadCoilAddress
    {
        get => _readCoilAddress;
        set => this.RaiseAndSetIfChanged(ref _readCoilAddress, value);
    }
    
    // Momentary button properties
    public bool MomentaryActive
    {
        get => _momentaryActive;
        set => this.RaiseAndSetIfChanged(ref _momentaryActive, value);
    }
    
    public ushort MomentaryAddress
    {
        get => _momentaryAddress;
        set => this.RaiseAndSetIfChanged(ref _momentaryAddress, value);
    }

    public bool? ReadCoilValue
    {
        get => _readCoilValue;
        set => this.RaiseAndSetIfChanged(ref _readCoilValue, value);
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
            await PollGroupDiscreteInputBitsAsync(enabled); // Чтение X-тегов (входные биты)
            
            // Опрашиваем регистры элементов мнемосхемы (если не в активных тегах)
            await PollMnemoschemeRegistersAsync();
            
            // НЕ перезаписываем статус - оставляем сообщение из PollGroupCoilsAsync
        }
        catch (FluentModbus.ModbusException ex) when (ex.Message.Contains("protocol identifier"))
        {
            // Критическая ошибка протокола - переподключаемся
            ConnectionStatus = "⚠️ Ошибка протокола, переподключение...";
            System.Diagnostics.Debug.WriteLine($"Protocol error: {ex.Message}. Reconnecting...");
            
            try
            {
                await DisconnectAsync();
                await Task.Delay(1000); // Пауза перед переподключением
                await ConnectAsync();
            }
            catch (Exception reconEx)
            {
                ConnectionStatus = $"❌ Ошибка переподключения: {reconEx.Message}";
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"⚠ Ошибка опроса: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"PollTagsOnceAsync exception: {ex}");
        }
    }

    /// <summary>
    /// Опрос регистров, назначенных элементам мнемосхемы (Slider, NumericInput, Display)
    /// </summary>
    private async Task PollMnemoschemeRegistersAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== PollMnemoschemeRegistersAsync START ===");
            System.Diagnostics.Debug.WriteLine($"Total mnemoscheme elements: {ConnectionConfig.MnemoschemeElements.Count}");
            
            var holdingAddresses = new HashSet<ushort>();
            var inputAddresses = new HashSet<ushort>();
            
            // Собираем адреса из элементов мнемосхемы
            foreach (var element in ConnectionConfig.MnemoschemeElements)
            {
                ushort address = 0;
                bool isHolding = false;
                bool isInput = false;
                string elementType = element.GetType().Name;
                
                if (element is SliderElement slider)
                {
                    address = slider.RegisterAddress;
                    isHolding = true;
                    System.Diagnostics.Debug.WriteLine($"  Found SliderElement: Address={address}, TagName={slider.TagName}");
                }
                else if (element is NumericInputElement numeric)
                {
                    address = numeric.RegisterAddress;
                    isHolding = true;
                    System.Diagnostics.Debug.WriteLine($"  Found NumericInputElement: Address={address}, TagName={numeric.TagName}");
                }
                else if (element is DisplayElement display)
                {
                    address = display.RegisterAddress;
                    System.Diagnostics.Debug.WriteLine($"  Found DisplayElement: Address={address}, TagName={display.TagName}");
                    
                    // DisplayControl может читать из обоих типов
                    // Проверим по тегу если есть
                    if (!string.IsNullOrEmpty(display.TagName))
                    {
                        var tag = ConnectionConfig.Tags.FirstOrDefault(t => t.Name == display.TagName);
                        if (tag != null)
                        {
                            isHolding = tag.Register == RegisterType.Holding;
                            isInput = tag.Register == RegisterType.Input;
                            System.Diagnostics.Debug.WriteLine($"    Tag found in active tags: Register={tag.Register}");
                        }
                        else
                        {
                            // Если тега нет в активных, попробуем угадать по имени
                            isHolding = display.TagName.StartsWith("V") || display.TagName.StartsWith("AQ");
                            isInput = display.TagName.StartsWith("AI") || display.TagName.StartsWith("TV") || 
                                     display.TagName.StartsWith("CV") || display.TagName.StartsWith("SV");
                            System.Diagnostics.Debug.WriteLine($"    Tag NOT in active tags, guessed: isHolding={isHolding}, isInput={isInput}");
                        }
                    }
                    else
                    {
                        // По умолчанию считаем Input Register
                        isInput = true;
                        System.Diagnostics.Debug.WriteLine($"    No tag name, assuming Input Register");
                    }
                }
                
                // ВАЖНО: Пропускаем адрес 0 (невалидный/не настроенный элемент)
                if (address > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"    Checking address {address}: isHolding={isHolding}, isInput={isInput}");
                    
                    // Проверяем, не опрашивается ли уже через активные теги
                    var existingTag = ConnectionConfig.Tags.FirstOrDefault(t => t.Address == address);
                    if (existingTag == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"    Address {address} NOT in active tags, adding to poll list");
                        if (isHolding)
                            holdingAddresses.Add(address);
                        if (isInput)
                            inputAddresses.Add(address);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"    Address {address} ALREADY in active tags (tag: {existingTag.Name}), skipping");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"    Skipping invalid address 0");
                }
            }
            
            // Если нет адресов для опроса - выходим
            if (holdingAddresses.Count == 0 && inputAddresses.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"No addresses to poll from mnemoscheme");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"Addresses to poll: Holding={holdingAddresses.Count}, Input={inputAddresses.Count}");
            System.Diagnostics.Debug.WriteLine($"  Holding: [{string.Join(", ", holdingAddresses)}]");
            System.Diagnostics.Debug.WriteLine($"  Input: [{string.Join(", ", inputAddresses)}]");
            
            // Читаем Holding регистры
            if (holdingAddresses.Count > 0)
            {
                var registerValues = new Dictionary<ushort, ushort>();
                foreach (var addr in holdingAddresses)
                {
                    try
                    {
                        var values = await _modbusService.ReadHoldingRegistersAsync(addr, 1);
                        if (values.Length > 0)
                        {
                            registerValues[addr] = values[0];
                            System.Diagnostics.Debug.WriteLine($"  Holding[{addr}] = {values[0]}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Failed to read Holding[{addr}]: {ex.Message}");
                    }
                }
                
                if (registerValues.Count > 0 && RegisterTagsUpdated != null)
                {
                    RegisterTagsUpdated.Invoke(this, registerValues);
                }
            }
            
            // Читаем Input регистры
            if (inputAddresses.Count > 0)
            {
                var registerValues = new Dictionary<ushort, ushort>();
                foreach (var addr in inputAddresses)
                {
                    try
                    {
                        var values = await _modbusService.ReadInputRegistersAsync(addr, 1);
                        if (values.Length > 0)
                        {
                            registerValues[addr] = values[0];
                            System.Diagnostics.Debug.WriteLine($"  Input[{addr}] = {values[0]}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Failed to read Input[{addr}]: {ex.Message}");
                    }
                }
                
                if (registerValues.Count > 0 && RegisterTagsUpdated != null)
                {
                    RegisterTagsUpdated.Invoke(this, registerValues);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PollMnemoschemeRegistersAsync failed: {ex.Message}");
            // НЕ пробрасываем исключение дальше - не должно ломать общий опрос
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
        System.Diagnostics.Debug.WriteLine($"=== PollGroupAsync START: RegisterType={regType} ===");
        
        var group = tags.Where(t => t.Enabled && t.Register == regType).OrderBy(t => t.Address).ToList();
        
        System.Diagnostics.Debug.WriteLine($"  Total tags in collection: {tags.Count}");
        System.Diagnostics.Debug.WriteLine($"  Filtered tags for {regType}: {group.Count}");
        
        if (group.Count > 0)
        {
            foreach (var tag in group)
            {
                System.Diagnostics.Debug.WriteLine($"    Tag: {tag.Name}, Address={tag.Address}, Enabled={tag.Enabled}, Type={tag.Type}");
            }
        }
        
        if (group.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine($"PollGroupAsync: No enabled tags for {regType}, skipping");
            return;
        }

        // Compute block span [min, max]
        ushort min = group.Min(t => t.Address);
        int max = group.Max(t => t.Address + GetWordCount(t.Type) - 1);
        int count = max - min + 1;
        
        System.Diagnostics.Debug.WriteLine($"  Block range: [{min}-{max}], count={count}");

        // Если диапазон слишком большой (>125 регистров) - читаем поштучно
        const int MAX_BLOCK_SIZE = 125;
        
        if (count > MAX_BLOCK_SIZE)
        {
            // Читаем каждый тег отдельно
            foreach (var tag in group)
            {
                try
                {
                    ushort[] values = regType == RegisterType.Holding
                        ? await _modbusService.ReadHoldingRegistersAsync(tag.Address, (ushort)GetWordCount(tag.Type))
                        : await _modbusService.ReadInputRegistersAsync(tag.Address, (ushort)GetWordCount(tag.Type));
                    
                    ParseTagValueFromRegisters(tag, values);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to read {regType} tag {tag.Name} at {tag.Address}: {ex.Message}");
                }
            }
            
            // ВАЖНО: Генерируем событие даже при индивидуальных чтениях
            GenerateRegisterTagsUpdatedEvent(group);
            return;
        }

        ushort[] block;
        try
        {
            block = regType == RegisterType.Holding
                ? await _modbusService.ReadHoldingRegistersAsync(min, (ushort)count)
                : await _modbusService.ReadInputRegistersAsync(min, (ushort)count);
        }
        catch (Exception ex)
        {
            // Блочное чтение не удалось - пробуем читать поштучно
            System.Diagnostics.Debug.WriteLine($"Block read {regType} [{min}-{max}] failed: {ex.Message}. Switching to individual reads.");
            
            foreach (var tag in group)
            {
                try
                {
                    ushort[] values = regType == RegisterType.Holding
                        ? await _modbusService.ReadHoldingRegistersAsync(tag.Address, (ushort)GetWordCount(tag.Type))
                        : await _modbusService.ReadInputRegistersAsync(tag.Address, (ushort)GetWordCount(tag.Type));
                    
                    ParseTagValueFromRegisters(tag, values);
                }
                catch (Exception tagEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to read {regType} tag {tag.Name} at {tag.Address}: {tagEx.Message}");
                }
            }
            
            // ВАЖНО: Генерируем событие даже при индивидуальных чтениях после ошибки блока
            GenerateRegisterTagsUpdatedEvent(group);
            return;
        }

        foreach (var tag in group)
        {
            int words = GetWordCount(tag.Type);
            int offset = tag.Address - min;
            var regs = new ushort[words];
            for (int i = 0; i < words; i++) regs[i] = block[offset + i];

            ParseTagValueFromRegisters(tag, regs);
        }

        // Генерируем событие с обновлёнными значениями регистров после блочного чтения
        GenerateRegisterTagsUpdatedEvent(group);
    }

    /// <summary>
    /// Генерирует событие RegisterTagsUpdated для группы тегов
    /// </summary>
    private void GenerateRegisterTagsUpdatedEvent(List<TagDefinition> group)
    {
        if (RegisterTagsUpdated != null && group.Count > 0)
        {
            var registerValues = new Dictionary<ushort, ushort>();
            foreach (var tag in group)
            {
                // Для DisplayControl берём только первый регистр (упрощённо)
                // В будущем можно добавить поддержку многословных значений
                ushort regValue = (ushort)tag.Value;
                registerValues[tag.Address] = regValue;
                System.Diagnostics.Debug.WriteLine($"  RegisterEvent: {tag.Name} (addr={tag.Address}, type={tag.Register}) = {regValue}");
            }
            
            System.Diagnostics.Debug.WriteLine($"GenerateRegisterTagsUpdatedEvent: sending {registerValues.Count} register values, invoking event...");
            RegisterTagsUpdated.Invoke(this, registerValues);
            System.Diagnostics.Debug.WriteLine($"GenerateRegisterTagsUpdatedEvent: event invoked successfully");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"GenerateRegisterTagsUpdatedEvent: NOT sending (event={RegisterTagsUpdated != null}, count={group.Count})");
        }
    }

    private void ParseTagValueFromRegisters(TagDefinition tag, ushort[] regs)
    {
        // Apply word order for multi-word types
        if (regs.Length > 1 && tag.WordOrder == WordOrder.LowHigh)
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
        
        if (group.Count == 0)
        {
            return;
        }

        try
        {
            // Словарь для хранения адресов coil и их значений
            var coilValues = new Dictionary<ushort, bool>();

            // Читаем каждый Coil ОТДЕЛЬНО, а не блоком (чтобы избежать ошибок с недоступными адресами)
            foreach (var tag in group)
            {
                try
                {
                    var result = await _modbusService.ReadCoilsAsync(tag.Address, 1);
                    bool bit = result[0];
                    double val = bit ? 1.0 : 0.0;
                    val = val * tag.Scale + tag.Offset;
                    tag.Value = val;

                    // Добавляем в словарь для обновления кнопок
                    coilValues[tag.Address] = bit;

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
                catch (Exception ex)
                {
                    // Ошибка чтения конкретного Coil - тихо пропускаем
                    System.Diagnostics.Debug.WriteLine($"Failed to read Coil at address {tag.Address}: {ex.Message}");
                }
            }

            // Уведомляем подписчиков об обновлении значений coils
            if (CoilTagsUpdated != null)
            {
                CoilTagsUpdated.Invoke(this, coilValues);
            }
        }
        catch (Exception ex)
        {
            // Ошибка чтения Coils не должна ломать весь опрос
            System.Diagnostics.Debug.WriteLine($"PollGroupCoilsAsync failed: {ex}");
        }
    }

    private async Task PollGroupDiscreteInputsAsync(System.Collections.ObjectModel.ObservableCollection<TagDefinition> tags)
    {
        // Читаем теги Y через Discrete Inputs (функция Modbus 0x02)
        // Для Haiwell PLC, если потребуется
        var yTags = tags.Where(t => t.Enabled && t.Register == RegisterType.Input && t.Name.StartsWith("Y")).OrderBy(t => t.Address).ToList();
        
        if (yTags.Count == 0)
        {
            return;
        }

        try
        {
            var coilValues = new Dictionary<ushort, bool>();

            // Читаем каждый Y-тег ОТДЕЛЬНО через Discrete Inputs
            foreach (var tag in yTags)
            {
                try
                {
                    var result = await _modbusService.ReadDiscreteInputsAsync(tag.Address, 1);
                    bool bit = result[0];
                    double val = bit ? 1.0 : 0.0;
                    val = val * tag.Scale + tag.Offset;
                    tag.Value = val;

                    // Добавляем в словарь для обновления кнопок
                    coilValues[tag.Address] = bit;
                }
                catch (Exception ex)
                {
                    // Тихо пропускаем ошибки
                    System.Diagnostics.Debug.WriteLine($"Failed to read Discrete Input at address {tag.Address}: {ex.Message}");
                }
            }

            // Уведомляем подписчиков об обновлении Y-тегов
            if (CoilTagsUpdated != null && coilValues.Count > 0)
            {
                CoilTagsUpdated.Invoke(this, coilValues);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PollGroupDiscreteInputsAsync failed: {ex}");
        }
    }

    private async Task PollGroupDiscreteInputBitsAsync(System.Collections.ObjectModel.ObservableCollection<TagDefinition> tags)
    {
        // Читаем теги X (входные биты) через Discrete Inputs (функция Modbus 0x02)
        var xTags = tags.Where(t => t.Enabled && t.Register == RegisterType.Input && t.Type == DataType.Bool && t.Name.StartsWith("X")).OrderBy(t => t.Address).ToList();
        
        if (xTags.Count == 0)
        {
            return;
        }

        try
        {
            var coilValues = new Dictionary<ushort, bool>();

            // Читаем каждый X-тег ОТДЕЛЬНО через Discrete Inputs
            foreach (var tag in xTags)
            {
                try
                {
                    var result = await _modbusService.ReadDiscreteInputsAsync(tag.Address, 1);
                    bool bit = result[0];
                    double val = bit ? 1.0 : 0.0;
                    val = val * tag.Scale + tag.Offset;
                    tag.Value = val;

                    // Добавляем в словарь для обновления кнопок
                    coilValues[tag.Address] = bit;
                }
                catch (Exception ex)
                {
                    // Тихо пропускаем ошибки
                    System.Diagnostics.Debug.WriteLine($"Failed to read Discrete Input X at address {tag.Address}: {ex.Message}");
                }
            }

            // Уведомляем подписчиков об обновлении X-тегов
            if (CoilTagsUpdated != null && coilValues.Count > 0)
            {
                CoilTagsUpdated.Invoke(this, coilValues);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PollGroupDiscreteInputBitsAsync failed: {ex}");
        }
    }

    private async Task LoadSettingsAsync()
    {
        var loaded = await _settingsService.LoadAsync();
        if (loaded != null)
        {
            ConnectionConfig = loaded;
        }
        
        // Загружаем теги из tags.json ТОЛЬКО если они еще не были инициализированы
        if (!ConnectionConfig.TagsInitialized && ConnectionConfig.Tags.Count == 0)
        {
            Console.WriteLine("=== First run: Loading tags from tags.json... ===");
            var tagsFromFile = await _tagsConfigService.LoadFirstNTagsPerTypeAsync(20);
            Console.WriteLine($"=== Received {tagsFromFile.Count} tags from TagsConfigService ===");
            
            if (tagsFromFile.Count > 0)
            {
                foreach (var tag in tagsFromFile)
                {
                    ConnectionConfig.Tags.Add(tag);
                    Console.WriteLine($"  Added tag: {tag.Name}, Register={tag.Register}, Type={tag.Type}");
                }
                Console.WriteLine($"=== Total tags in ConnectionConfig: {ConnectionConfig.Tags.Count} ===");
                
                // Проверяем типы регистров
                var coilCount = ConnectionConfig.Tags.Count(t => t.Register == RegisterType.Coils);
                var inputCount = ConnectionConfig.Tags.Count(t => t.Register == RegisterType.Input);
                var holdingCount = ConnectionConfig.Tags.Count(t => t.Register == RegisterType.Holding);
                Console.WriteLine($"=== Register types: Coils={coilCount}, Input={inputCount}, Holding={holdingCount} ===");
                
                // Помечаем что теги инициализированы
                ConnectionConfig.TagsInitialized = true;
                
                // Сохраняем настройки с загруженными тегами
                await SaveSettingsAsync();
            }
            else
            {
                Console.WriteLine("=== No tags loaded from tags.json, using default tags ===");
                // Если tags.json не найден, используем старые дефолтные теги
                InitializeDefaultTags();
                ConnectionConfig.TagsInitialized = true;
            }
        }
        else if (ConnectionConfig.TagsInitialized)
        {
            Console.WriteLine($"=== Tags already initialized: {ConnectionConfig.Tags.Count} tags ===");
        }
        else
        {
            Console.WriteLine($"=== Tags already exist in settings: {ConnectionConfig.Tags.Count} tags ===");
            // Помечаем что теги инициализированы (для старых конфигураций)
            ConnectionConfig.TagsInitialized = true;
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

    public Task SaveSettingsAsync()
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
            var errorMsg = ex.Message.Contains("allowable address") 
                ? $"Адрес {RegisterAddress} недоступен на сервере." 
                : ex.Message;
            ConnectionStatus = $"⚠ Ошибка записи: {errorMsg}";
        }
    }

    /// <summary>
    /// Метод для чтения одной катушки
    /// </summary>
    private async Task ReadCoilAsync()
    {
        if (!IsConnected)
        {
            ConnectionStatus = "Ошибка: не подключен к серверу";
            return;
        }

        try
        {
            bool value = await _modbusService.ReadCoilAsync(ReadCoilAddress);
            ReadCoilValue = value;
        }
        catch (Exception ex)
        {
            ReadCoilValue = null;
            var errorMsg = ex.Message.Contains("allowable address") 
                ? $"Адрес {ReadCoilAddress} недоступен на сервере." 
                : ex.Message;
            ConnectionStatus = $"⚠ Ошибка чтения: {errorMsg}";
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
            var errorMsg = ex.Message.Contains("allowable address") 
                ? $"Адрес {getAddress()} недоступен на сервере." 
                : ex.Message;
            ConnectionStatus = $"⚠ Ошибка записи: {errorMsg}";
        }
    }

    /// <summary>
    /// Публичный метод для записи coil (используется динамическими кнопками)
    /// </summary>
    public async Task WriteCoilAsync(ushort address, bool value)
    {
        if (!IsConnected)
        {
            ConnectionStatus = "Ошибка: не подключен к серверу";
            return;
        }
        try
        {
            await _modbusService.WriteCoilAsync(address, value);
        }
        catch (Exception ex)
        {
            string errorMsg;
            if (ex.Message.Contains("allowable address"))
            {
                errorMsg = $"Адрес {address} недоступен на сервере.";
            }
            else if (ex.Message.Contains("function code is invalid") || ex.Message.Contains("код функции"))
            {
                errorMsg = $"Адрес {address} не поддерживает запись катушки. Проверьте тип регистра.";
            }
            else
            {
                errorMsg = ex.Message;
            }
            
            ConnectionStatus = $"⚠ Ошибка записи: {errorMsg}";
            
            // Логируем для отладки
            System.Diagnostics.Debug.WriteLine($"WriteCoilAsync failed: address={address}, value={value}, error={ex.Message}");
        }
    }

    /// <summary>
    /// Запись в Holding Register
    /// </summary>
    public async Task WriteRegisterAsync(ushort address, ushort value)
    {
        System.Diagnostics.Debug.WriteLine($"=== WriteRegisterAsync START: address={address}, value={value} ===");
        
        if (!IsConnected)
        {
            System.Diagnostics.Debug.WriteLine($"WriteRegisterAsync: NOT CONNECTED, aborting");
            ConnectionStatus = "Ошибка: не подключен к серверу";
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"WriteRegisterAsync: Connected, calling ModbusService.WriteHoldingRegisterAsync...");
        
        try
        {
            await _modbusService.WriteHoldingRegisterAsync(address, value);
            ConnectionStatus = $"✓ Записано в регистр {address}: {value}";
            System.Diagnostics.Debug.WriteLine($"WriteRegisterAsync: SUCCESS - written to address {address}, value={value}");
        }
        catch (Exception ex)
        {
            string errorMsg;
            if (ex.Message.Contains("allowable address"))
            {
                errorMsg = $"Адрес {address} недоступен на сервере.";
            }
            else if (ex.Message.Contains("function code is invalid") || ex.Message.Contains("код функции"))
            {
                errorMsg = $"Адрес {address} не поддерживает запись регистра. Проверьте тип регистра.";
            }
            else
            {
                errorMsg = ex.Message;
            }
            
            ConnectionStatus = $"⚠ Ошибка записи: {errorMsg}";
            
            // Логируем для отладки
            System.Diagnostics.Debug.WriteLine($"WriteRegisterAsync FAILED: address={address}, value={value}, error={ex.Message}");
            System.Diagnostics.Debug.WriteLine($"  Exception type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"  Stack trace: {ex.StackTrace}");
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
