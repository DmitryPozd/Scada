# Copilot instructions for this repository

Last updated: 2025-10-27

Avalonia UI desktop SCADA application for Modbus TCP controller communication. Built with .NET 8, ReactiveUI MVVM, and FluentModbus client.

## Environment and commands
- OS: Windows. Default shell: Windows PowerShell (v5.1).
- Commands must be PowerShell-compatible. Chain with `;`.
- Build/run: use `dotnet` CLI throughout.
- Theme: FluentTheme with `ThemeVariant.Default` - automatically follows Windows system theme (Light/Dark).

## Architecture

**Stack**: .NET 8 + Avalonia 11.3.6 + ReactiveUI + FluentModbus 5.3.2 + Avalonia.Controls.DataGrid

**Project structure**:
```
Scada.Client/
├── Models/          ModbusConnectionConfig, TagDefinition, TagEnums (RegisterType, DataType, WordOrder), CoilButtonInfo, MnemoschemeElement hierarchy
├── ViewModels/      ViewModelBase, MainWindowViewModel, SettingsWindowViewModel
├── Views/           MainWindow.axaml, SettingsWindow.axaml
│   └── Controls/    PumpControl, ValveControl, SensorIndicator, CoilButton, ImageButton, DraggableControl (mnemoscheme UI components)
└── Services/        IModbusClientService, ModbusClientService, ISettingsService, SettingsService
```

**Data flow**:
1. `MainWindowViewModel` owns `IModbusClientService` and `ISettingsService` instances (direct instantiation, no DI yet).
2. ReactiveCommands (`ConnectCommand`, `ReadRegisterCommand`, `CoilOnCommand/OffCommand`) trigger async service calls.
3. Service uses `FluentModbus.ModbusTcpClient` to read/write registers/coils over TCP.
4. Tag-based polling: `Observable.Interval` polls enabled tags at configured interval, groups by register type for batch reads.
5. Properties (`ConnectionStatus`, `Pump1Running`, etc.) raise change notifications via ReactiveUI's `RaiseAndSetIfChanged`.
6. AXAML bindings (compiled, with `x:DataType`) update UI automatically.
7. Settings persist to `%AppData%/Scada.Client/settings.json` (JSON serialization).

**Key decisions**:
- FluentModbus over NModbus4 for .NET 8 compatibility (NModbus4 targets .NET Framework).
- ReactiveCommand for declarative, testable command handling.
- Namespace-per-folder: `Scada.Client.Views`, `Scada.Client.ViewModels`, etc.
- Tag definitions with scaling/offset for flexible industrial data mapping.
- Block reads: batch-read contiguous register ranges to minimize Modbus transactions.
- No DI container yet—services instantiated directly in ViewModels.

## Build / Test / Run workflows

```powershell
# Restore dependencies
dotnet restore

# Build solution
dotnet build Scada.sln

# Run application (launches Avalonia desktop window)
dotnet run --project Scada.Client

# Release build
dotnet build -c Release
```

**No tests yet**. When added, `dotnet test` from solution root.

**Debugging**:
- Avalonia DevTools available in Debug builds only (excluded from Release via conditional `<IncludeAssets>`).
- Settings JSON: `%AppData%\Scada.Client\settings.json` — delete to reset to defaults.
- Modbus TCP simulator recommended for local testing: ModRSsim2 (Windows) or `diagslave -m tcp -p 502` (cross-platform).

## Conventions

- **File-scoped namespaces** (`namespace Scada.Client.Views;`)
- **Nullable enabled** (`<Nullable>enable</Nullable>`)
- **Compiled bindings default**: `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`
- **ViewModels**: inherit `ViewModelBase : ReactiveObject`, use `this.RaiseAndSetIfChanged(ref _field, value)` for properties.
- **Commands**: `ReactiveCommand<Unit, Unit>` for async operations, with `WhenAnyValue(...)` for CanExecute.
- **AXAML**: `x:DataType="vm:...ViewModel"` for compiled bindings, `xmlns:vm="using:Scada.Client.ViewModels"` namespace.
- **Services**: async Task methods, throw `InvalidOperationException` if preconditions fail (e.g., not connected).
- **UserControls**: define `StyledProperty<T>` for bindable properties, use `RelativeSource={RelativeSource AncestorType=...}` in bindings.

## Integration points

**Modbus TCP**:
- Library: FluentModbus (`ModbusTcpClient`)
- Default endpoint: `127.0.0.1:502`, Unit ID `1` (hardcoded in `MainWindowViewModel.ConnectAsync`)
- Operations: `ReadHoldingRegisters` returns `Span<byte>`; use `BinaryPrimitives.ReadUInt16BigEndian(...)` for ushort conversion.
- Endianness: `ModbusEndianness.BigEndian` (standard Modbus)
- Tag system: `TagDefinition` models support `RegisterType` (Holding/Input/Coils), `DataType` (UInt16/Int32/Float32/etc.), word-order swapping, scaling/offset.
- **Coil polling strategy**: Each Coil is read INDIVIDUALLY (not as a block) to avoid errors with non-contiguous addresses. Handles large address gaps gracefully.
- Individual coil reads with per-tag error handling prevent "allowable address" errors.

**UI State Synchronization**:
- `CoilTagsUpdated` event in `MainWindowViewModel`: fired after each Coil poll cycle with `Dictionary<ushort, bool>` of address→value pairs.
- `OnCoilTagsUpdated` handler in `MainWindow.axaml.cs`: iterates Canvas children, updates `IsActive` property of all `CoilButton` and `ImageButton` controls.
- Subscription happens in `OnWindowLoaded` - critical to establish before polling starts.
- ImageButton (Motor/Valve/Fan) visual state now reflects real-time Modbus Coil values.

**Settings persistence**:
- JSON file: `%AppData%\Scada.Client\settings.json`
- `ISettingsService` / `SettingsService` handle async load/save of `ModbusConnectionConfig` (includes Tags + MnemoschemeElements collections).
- Loaded on app startup, saved on window closing.
- **Polymorphic serialization**: `MnemoschemeElement` base class uses `[JsonPolymorphic]` and `[JsonDerivedType]` attributes for proper deserialization of derived types (`CoilElement`, `PumpElement`, `ValveElement`, `SensorElement`).
- Settings.json includes `"$type"` discriminator field for each mnemoscheme element to preserve type information.

**External dependencies**:
- Avalonia + ReactiveUI NuGet packages (auto-restored).
- Real or simulated Modbus TCP server for runtime testing (e.g., ModRSsim2, diagslave).

## Examples

**Typical ViewModel property**:
```csharp
private string _status = "Idle";
public string Status
{
    get => _status;
    set => this.RaiseAndSetIfChanged(ref _status, value);
}
```

**ReactiveCommand with condition**:
```csharp
ReadCommand = ReactiveCommand.CreateFromTask(
    ReadAsync,
    this.WhenAnyValue(vm => vm.IsConnected)
);
```

**Adding AXAML window**:
1. Create `Views/NewWindow.axaml` + `.axaml.cs` with `namespace Scada.Client.Views;`
2. Create `ViewModels/NewWindowViewModel.cs : ViewModelBase`
3. In caller VM: `var w = new Views.NewWindow { DataContext = new NewWindowViewModel() }; await w.ShowDialog(...);`

**Creating UserControl with StyledProperty** (see `PumpControl.axaml.cs`):
```csharp
public static readonly StyledProperty<bool> IsRunningProperty =
    AvaloniaProperty.Register<PumpControl, bool>(nameof(IsRunning));

public bool IsRunning
{
    get => GetValue(IsRunningProperty);
    set => SetValue(IsRunningProperty, value);
}
```

**Binding UserControl property in AXAML**:
```xml
<controls:PumpControl IsRunning="{Binding Pump1Running}"
                      Label="Насос 1"/>
```

**Tag-based polling pattern** (see `MainWindowViewModel.PollTagsOnceAsync`):
1. Filter enabled tags by `RegisterType`.
2. Find min/max address range (accounting for multi-word data types).
3. Read entire block with single Modbus call.
4. Parse each tag's value using `DataType`, apply `WordOrder`, `Scale`, `Offset`.
5. Update ViewModel properties based on tag name mapping.

**Multi-register data parsing** (UInt32, Float32, etc.):
- For `Float32`: combine 2 registers into 4 bytes (big-endian), call `BitConverter.ToSingle`.
- For word-swapped devices: set `WordOrder.LowHigh` in tag definition, array is reversed before parsing.

**Coil control pattern** (see `CoilButton`, `ImageButton`):
- UserControls accept `OnCommand`/`OffCommand` via `StyledProperty<ICommand?>`.
- ViewModels create paired ReactiveCommands for ON/OFF operations.
- Direct coil writes use `_modbusService.WriteCoilAsync(address, value)`.
- Local state updated optimistically after write, refreshed by next poll cycle.
- **Context menu tag selection**: Right-click on control opens dialog with ComboBox listing available Coil tags from `ConnectionConfig.Tags`.
- Selected tag updates `CoilAddress` property (TwoWay binding) and `SelectedTag` reference.
- If no tags available, falls back to numeric address input dialog.
- Address changes persist in ViewModel properties (`Coil1Address`, `MotorAddress`, etc.) and affect next write operation.

**ImageButton types** (`ImageButtonType` enum):
- `Motor`, `Valve`, `Fan`, `Heater`, `Light` — visual shapes rendered via `ImageTypeToShapeConverter`.
- Canvas-based graphics created in code-behind for each type.

**Context menu implementation** (UserControls):
- Override `OnPointerReleased(PointerReleasedEventArgs e)` to detect right-click.
- Show modal dialog window with ComboBox populated from `AvailableTags` (filtered by `RegisterType.Coils`).
- ComboBox items display tag name and address: `"{tag.Name} (адрес: {tag.Address})"`.
- Update `CoilAddress` StyledProperty on OK, propagates to ViewModel via TwoWay binding.
- Subscribe to `SelectedTagProperty` changes to auto-update `CoilAddress` when tag selected.

**Copy/Paste pattern for button duplication** (see `CoilButton`, `ImageButton`, `MainWindow.axaml.cs`):
- UserControls define `CopyRequested` and `PasteRequested` routed events (bubbling).
- `CoilButtonInfo` model carries: `Label`, `CoilAddress`, `TagName`, `IsImageButton`, `ImageType`.
- **Ctrl+C on focused control**: raises `CopyRequested` event with CoilButtonInfo payload.
- **MainWindow.axaml.cs** subscribes via `GetVisualDescendants<CoilButton/ImageButton>()` in constructor.
- Clipboard storage: `_copiedButtonInfo` field stores last copied button data.
- **Ctrl+V in window**: creates dynamic button in `DynamicButtonsPanel` WrapPanel.
- Dynamic button command: `ReactiveCommand.CreateFromTask(async () => await vm.WriteCoilAsync(address, value))`.
- Pattern supports both CoilButton and ImageButton types, preserving tag selection and visual style.

## Notes for agents

- Always run `dotnet build` after file edits to catch errors early.
- When adding Modbus operations, remember FluentModbus returns `Span<byte>`, not `ushort[]`.
- Update this file when new patterns emerge (DI container, config file, logging, etc.).
