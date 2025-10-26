# SCADA Client Application

Приложение SCADA для работы с контроллерами по протоколу Modbus TCP. Построено на Avalonia UI (C#) с использованием ReactiveUI для реактивного MVVM и FluentModbus для связи с контроллером.

## Архитектура

```
Scada.Client/
├── Models/           # Модели данных (конфигурация, регистры)
├── ViewModels/       # ViewModels (ReactiveUI)
├── Views/            # AXAML представления (окна)
└── Services/         # Сервисы (Modbus-клиент)
```

### Стек технологий

- **.NET 8.0** - Целевая платформа
- **Avalonia 11.3.6** - Кросс-платформенный UI-фреймворк
- **ReactiveUI** - Реактивная MVVM-библиотека
- **FluentModbus 5.3.2** - Modbus TCP/RTU клиент

## Сборка и запуск

### Требования

- .NET SDK 8.0 или выше
- Windows / Linux / macOS

### Команды PowerShell

```powershell
# Восстановление зависимостей
dotnet restore

# Сборка решения
dotnet build Scada.sln

# Запуск приложения
dotnet run --project Scada.Client

# Сборка Release
dotnet build -c Release
```

## Основные функции

### 1. Главное окно (мнемосхема)

### 2. Окно настроек

## Settings persistence

- Modbus connection settings (Host, Port, UnitId) are persisted between runs.
- Storage location: `%AppData%/Scada.Client/settings.json` (Windows user profile).
- To reset settings, delete this file; defaults will be used on next start.

### 3. Modbus-сервис
- **IModbusClientService** - интерфейс для работы с Modbus
- **ModbusClientService** - реализация на FluentModbus
- Поддержка операций:
  - `ReadHoldingRegisterAsync(ushort address)` - чтение одного регистра
  - `ReadHoldingRegistersAsync(ushort start, ushort count)` - чтение блока регистров
  - `WriteHoldingRegisterAsync(ushort address, ushort value)` - запись регистра

## Структура MVVM

### ViewModels
- **ViewModelBase** - базовый класс для всех VM (наследует ReactiveObject)
- **MainWindowViewModel** - главное окно с командами подключения и чтения
- **SettingsWindowViewModel** - настройки подключения

### Views (AXAML)
- **MainWindow** - мнемосхема, область для визуализации процесса
- **SettingsWindow** - диалог настроек

### Команды (ReactiveCommand)
- `ConnectCommand` - подключение к контроллеру
- `DisconnectCommand` - отключение
- `ReadRegisterCommand` - чтение регистра (доступна только при подключении)
- `OpenSettingsCommand` - открытие настроек

## Расширение приложения

### Добавление визуализации на мнемосхему

1. Создайте пользовательский контрол в `Views/Controls/`
2. Добавьте свойства с привязкой к данным из ViewModel
3. Обновите `MainWindowViewModel`, добавьте таймер для опроса регистров:

```csharp
private System.Timers.Timer _pollTimer;

public MainWindowViewModel()
{
    // ...
    _pollTimer = new System.Timers.Timer(1000); // 1 sec
    _pollTimer.Elapsed += async (s, e) => await PollRegistersAsync();
}

private async Task PollRegistersAsync()
{
    if (!IsConnected) return;
    RegisterValue = await _modbusService.ReadHoldingRegisterAsync(RegisterAddress);
}
```

### Добавление нового окна

1. Создайте AXAML + code-behind в `Views/`
2. Создайте ViewModel в `ViewModels/`
3. В команде из MainWindowViewModel:

```csharp
private async void OpenNewWindow()
{
    var vm = new NewWindowViewModel();
    var window = new NewWindow { DataContext = vm };
    await window.ShowDialog(this);
}
```

## Подключение к реальному контроллеру

По умолчанию настройки:
- **Host**: `127.0.0.1`
- **Port**: `502`
- **Unit ID**: `1`

Измените в `MainWindowViewModel.ConnectAsync()` или через окно настроек (в разработке).

Для тестирования без физического контроллера используйте Modbus-симуляторы:
- **ModRSsim2** (Windows)
- **diagslave** (Linux/macOS)

```powershell
# Пример запуска diagslave на порту 502
diagslave -m tcp -p 502
```

## Известные ограничения

- NModbus4 устарел (совместимость .NET Framework), используется FluentModbus
- Окно настроек создано, но не интегрировано с MainWindowViewModel (TODO)
- Мнемосхема - пока заготовка без визуальных элементов

## Лицензия

Внутренний проект.
