# Changelog

## 2025-10-27 - ImageButton State Synchronization & Coil Polling Fix

### Major Changes

**Fixed ImageButton (Motor/Valve/Fan) state synchronization**
- Added `CoilTagsUpdated` event in `MainWindowViewModel` to notify UI of Coil value changes
- Implemented `OnCoilTagsUpdated` handler in `MainWindow.axaml.cs` to update button states
- ImageButton controls now properly reflect real-time Modbus Coil state during polling

**Changed Coil polling strategy**
- **Old approach**: Block read from min to max address (e.g., 1536-3074 = 1539 coils)
- **New approach**: Individual reads per enabled Coil tag
- Fixes "allowable address" errors when address gaps exist
- Each Coil read has individual error handling (failed reads don't break polling)

**Improved error handling and UX**
- Better error messages for Coil read/write failures
- Status messages show detailed polling progress
- Increased context menu dialog sizes for better visibility
- `UpdateButtonAvailableTags` method updates tag ComboBoxes without app restart

### Technical Details

**Event-based state sync pattern**:
```csharp
// ViewModel (MainWindowViewModel.cs)
public event EventHandler<Dictionary<ushort, bool>>? CoilTagsUpdated;

private async Task PollGroupCoilsAsync(...)
{
    var coilValues = new Dictionary<ushort, bool>();
    foreach (var tag in enabledCoilTags)
    {
        var result = await _modbusService.ReadCoilsAsync(tag.Address, 1);
        coilValues[tag.Address] = result[0];
    }
    CoilTagsUpdated?.Invoke(this, coilValues);
}

// View (MainWindow.axaml.cs)
private void OnWindowLoaded(...)
{
    if (DataContext is MainWindowViewModel vm)
    {
        vm.CoilTagsUpdated += OnCoilTagsUpdated;
    }
}

private void OnCoilTagsUpdated(object? sender, Dictionary<ushort, bool> coilValues)
{
    foreach (var child in canvas.Children)
    {
        if (child is DraggableControl draggable)
        {
            if (draggable.Content is ImageButton imgBtn)
            {
                if (coilValues.TryGetValue(imgBtn.CoilAddress, out bool value))
                {
                    imgBtn.IsActive = value; // Updates visual state
                }
            }
        }
    }
}
```

### Bug Fixes
- Fixed: ImageButton controls not updating state during Modbus polling
- Fixed: "allowable address" errors with non-contiguous Coil addresses
- Fixed: Tag name changes in settings not visible until app restart

### Commits
- `9271e83` - Fix ImageButton state synchronization and Coil polling
- `7cf1a48` - Update button AvailableTags immediately after settings change
- `170a12c` - Update copilot-instructions.md with polymorphic serialization details
- `90a0bc6` - Fix polymorphic JSON deserialization for mnemoscheme elements

---

## 2025-10-26 - Initial Development

### Features Implemented

**Full SCADA mnemoscheme system**
- Canvas-based drag-and-drop layout for all controls
- Persistent element positions/labels/parameters (saved to `%AppData%/Scada.Client/settings.json`)
- Copy/paste functionality for buttons (Ctrl+C / Ctrl+V)
- Right-click context menus for all controls with:
  - Label editing
  - Tag selection via ComboBox (filtered by RegisterType)
  - Address configuration

**Control types**
- `CoilButton` - simple ON/OFF button for Modbus Coils
- `ImageButton` - graphical buttons (Motor, Valve, Fan, Heater, Light)
- `PumpControl` - animated pump visualization
- `ValveControl` - valve open/close indicator
- `SensorIndicator` - numeric value display with thresholds
- `DraggableControl` - wrapper enabling drag-drop for all controls

**Modbus communication**
- FluentModbus 5.3.2 client
- Tag-based polling system with configurable interval
- Support for Holding Registers, Input Registers, and Coils
- Data type conversion (UInt16/Int16/UInt32/Int32/Float32/Int64/Double)
- Word order swapping for endianness compatibility
- Scaling and offset for sensor values

**Settings system**
- JSON persistence to `%AppData%/Scada.Client/settings.json`
- Polymorphic serialization for mnemoscheme elements using `[JsonPolymorphic]` attributes
- Settings window with DataGrid for tag configuration
- Validation for tag address overlaps

**Theme support**
- Auto light/dark mode following Windows system theme
- `DynamicResource` bindings for all colors
- FluentTheme with `ThemeVariant.Default`

### Architecture Decisions
- ReactiveUI MVVM pattern
- No DI container (direct service instantiation)
- Compiled XAML bindings (`x:DataType`)
- StyledProperty for UserControl bindable properties
- Event-based communication (RequestOpenSettings, SettingsLoadedEvent, CoilTagsUpdated)

### Known Issues (Resolved)
- ✅ Elements disappearing on second launch (fixed with polymorphic JSON)
- ✅ ImageButton not updating state (fixed with CoilTagsUpdated event)
- ✅ Large address gaps causing polling errors (fixed with individual reads)

---

## Git Repository
- GitHub: https://github.com/DmitryPozd/Scada.git
- Branch: main
