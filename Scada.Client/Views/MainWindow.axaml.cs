using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Scada.Client.ViewModels;
using Scada.Client.Models;
using Scada.Client.Views.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;

namespace Scada.Client.Views;

public partial class MainWindow : Window
{
    private CoilButtonInfo? _copiedButtonInfo;
    private int _dynamicButtonCounter = 1;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnWindowLoaded;
        Closing += OnWindowClosing;
    }

    private void OnWindowLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SubscribeToButtonEvents(this);
        
        // Подписываемся на событие завершения загрузки настроек
        if (DataContext is MainWindowViewModel vm)
        {
            // Подписка на обновление Coil тегов
            vm.CoilTagsUpdated += OnCoilTagsUpdated;
            this.Title = "SCADA - Подписка на CoilTagsUpdated выполнена!";
            
            vm.SettingsLoadedEvent += (s, args) =>
            {
                System.Diagnostics.Debug.WriteLine("SettingsLoadedEvent fired, calling RestoreMnemoschemeElements");
                RestoreMnemoschemeElements();
            };
            
            // Если настройки уже загружены (синхронный путь)
            if (vm.SettingsLoaded)
            {
                System.Diagnostics.Debug.WriteLine("Settings already loaded, calling RestoreMnemoschemeElements immediately");
                RestoreMnemoschemeElements();
            }
        }
        else
        {
            this.Title = "SCADA - ERROR: DataContext is NOT MainWindowViewModel!";
        }
    }

    private void OnCoilTagsUpdated(object? sender, Dictionary<ushort, bool> coilValues)
    {
        System.Diagnostics.Debug.WriteLine($"OnCoilTagsUpdated: received {coilValues.Count} coil values");
        
        // Временно для отладки - показываем в заголовке окна
        this.Title = $"SCADA - Получено {coilValues.Count} coils: {string.Join(", ", coilValues.Select(kv => $"{kv.Key}={kv.Value}"))}";
        
        foreach (var kv in coilValues)
        {
            System.Diagnostics.Debug.WriteLine($"  Coil {kv.Key} = {kv.Value}");
        }
        
        var canvas = this.FindControl<Canvas>("MnemoCanvas");
        if (canvas == null)
        {
            System.Diagnostics.Debug.WriteLine("OnCoilTagsUpdated: Canvas not found!");
            this.Title = "SCADA - Canvas NOT FOUND!";
            return;
        }

        int updatedCount = 0;
        int totalButtons = 0;
        foreach (var child in canvas.Children)
        {
            if (child is DraggableControl draggable)
            {
                if (draggable.Content is CoilButton coilBtn)
                {
                    totalButtons++;
                    if (coilValues.TryGetValue(coilBtn.CoilAddress, out bool value))
                    {
                        System.Diagnostics.Debug.WriteLine($"  Updating CoilButton at address {coilBtn.CoilAddress} to {value}, WAS: {coilBtn.IsActive}");
                        coilBtn.IsActive = value;
                        updatedCount++;
                    }
                }
                else if (draggable.Content is ImageButton imgBtn)
                {
                    totalButtons++;
                    if (coilValues.TryGetValue(imgBtn.CoilAddress, out bool value))
                    {
                        System.Diagnostics.Debug.WriteLine($"  Updating ImageButton '{imgBtn.Label}' at address {imgBtn.CoilAddress} to {value}, WAS: {imgBtn.IsActive}");
                        var oldValue = imgBtn.IsActive;
                        imgBtn.IsActive = value;
                        System.Diagnostics.Debug.WriteLine($"  ImageButton '{imgBtn.Label}' AFTER update: {imgBtn.IsActive}");
                        updatedCount++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  ImageButton '{imgBtn.Label}' at address {imgBtn.CoilAddress} - NO MATCH in coilValues");
                    }
                }
            }
        }
        this.Title = $"SCADA - Обновлено {updatedCount}/{totalButtons} кнопок";
        System.Diagnostics.Debug.WriteLine($"OnCoilTagsUpdated: updated {updatedCount} out of {totalButtons} buttons");
    }

    private void RestoreMnemoschemeElements()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            System.Diagnostics.Debug.WriteLine("RestoreMnemoschemeElements: DataContext is not MainWindowViewModel");
            return;
        }
        
        var canvas = this.FindControl<Canvas>("MnemoCanvas");
        if (canvas == null)
        {
            System.Diagnostics.Debug.WriteLine("RestoreMnemoschemeElements: Canvas not found!");
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"RestoreMnemoschemeElements: Found {vm.ConnectionConfig.MnemoschemeElements.Count} saved elements");
        
        // Если нет сохранённых элементов, оставляем статические из AXAML
        if (vm.ConnectionConfig.MnemoschemeElements.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("RestoreMnemoschemeElements: No saved elements, keeping AXAML static elements");
            return;
        }
        
        System.Diagnostics.Debug.WriteLine("RestoreMnemoschemeElements: Clearing canvas and restoring elements...");
        canvas.Children.Clear();
        foreach (var element in vm.ConnectionConfig.MnemoschemeElements)
        {
            Control? control = null;
            switch (element)
            {
                case CoilElement coilElem when coilElem.Type == ElementType.CoilButton:
                    var coilBtn = new CoilButton
                    {
                        Label = coilElem.Label,
                        CoilAddress = coilElem.CoilAddress,
                        AvailableTags = vm.ConnectionConfig.Tags,
                        IconPathOn = coilElem.IconPathOn,
                        IconPathOff = coilElem.IconPathOff
                    };
                    if (!string.IsNullOrEmpty(coilElem.TagName))
                    {
                        coilBtn.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == coilElem.TagName);
                    }
                    coilBtn.OnCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await vm.WriteCoilAsync(coilBtn.CoilAddress, true);
                        coilBtn.IsActive = true;
                    });
                    coilBtn.OffCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await vm.WriteCoilAsync(coilBtn.CoilAddress, false);
                        coilBtn.IsActive = false;
                    });
                    coilBtn.CopyRequested += OnButtonCopyRequested;
                    coilBtn.PasteRequested += OnButtonPasteRequested;
                    control = coilBtn;
                    break;
                case CoilElement imgElem when imgElem.Type == ElementType.ImageButton:
                    if (Enum.TryParse<ImageButtonType>(imgElem.ImageType, out var imgType))
                    {
                        var imgBtn = new ImageButton
                        {
                            Label = imgElem.Label,
                            CoilAddress = imgElem.CoilAddress,
                            ImageType = imgType,
                            AvailableTags = vm.ConnectionConfig.Tags
                        };
                        if (!string.IsNullOrEmpty(imgElem.TagName))
                        {
                            imgBtn.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == imgElem.TagName);
                        }
                        imgBtn.OnCommand = ReactiveCommand.CreateFromTask(async () =>
                        {
                            await vm.WriteCoilAsync(imgBtn.CoilAddress, true);
                            imgBtn.IsActive = true;
                        });
                        imgBtn.OffCommand = ReactiveCommand.CreateFromTask(async () =>
                        {
                            await vm.WriteCoilAsync(imgBtn.CoilAddress, false);
                            imgBtn.IsActive = false;
                        });
                        imgBtn.CopyRequested += OnButtonCopyRequested;
                        imgBtn.PasteRequested += OnButtonPasteRequested;
                        control = imgBtn;
                    }
                    break;
                case PumpElement pumpElem:
                    control = new PumpControl { Label = pumpElem.Label };
                    break;
                case ValveElement valveElem:
                    control = new ValveControl { Label = valveElem.Label };
                    break;
                case SensorElement sensorElem:
                    control = new SensorIndicator
                    {
                        Label = sensorElem.Label,
                        Unit = sensorElem.Unit,
                        ThresholdLow = sensorElem.ThresholdLow,
                        ThresholdHigh = sensorElem.ThresholdHigh
                    };
                    break;
            }
            if (control != null)
            {
                var draggable = new DraggableControl
                {
                    X = element.X,
                    Y = element.Y,
                    Content = control
                };
                canvas.Children.Add(draggable);
            }
        }
    }

    private void SubscribeToButtonEvents(Control parent)
    {
        foreach (var child in parent.GetVisualDescendants())
        {
            if (child is CoilButton coilBtn)
            {
                coilBtn.CopyRequested += OnButtonCopyRequested;
                coilBtn.PasteRequested += OnButtonPasteRequested;
            }
            else if (child is ImageButton imgBtn)
            {
                imgBtn.CopyRequested += OnButtonCopyRequested;
                imgBtn.PasteRequested += OnButtonPasteRequested;
            }
        }
    }

    private void OnButtonCopyRequested(object? sender, CoilButtonInfo info)
    {
        _copiedButtonInfo = info;
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ConnectionStatus = $"Скопировано: {info.Label}";
        }
    }

    private void OnButtonPasteRequested(object? sender, EventArgs e)
    {
        if (_copiedButtonInfo == null) return;
        CreateDynamicButton(_copiedButtonInfo);
    }

    private void CreateDynamicButton(CoilButtonInfo info)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        var canvas = this.FindControl<Canvas>("MnemoCanvas");
        if (canvas == null) return;
        var newLabel = $"{info.Label} (копия {_dynamicButtonCounter++})";
        var newX = info.X + 20;
        var newY = info.Y + 20;
        Control buttonControl;
        if (info.IsImageButton && Enum.TryParse<ImageButtonType>(info.ImageType, out var imgType))
        {
            var newButton = new ImageButton
            {
                Label = newLabel,
                CoilAddress = info.CoilAddress,
                ImageType = imgType,
                AvailableTags = vm.ConnectionConfig.Tags
            };
            newButton.OnCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await vm.WriteCoilAsync(newButton.CoilAddress, true);
                newButton.IsActive = true;
            });
            newButton.OffCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await vm.WriteCoilAsync(newButton.CoilAddress, false);
                newButton.IsActive = false;
            });
            newButton.CopyRequested += OnButtonCopyRequested;
            newButton.PasteRequested += OnButtonPasteRequested;
            buttonControl = newButton;
        }
        else
        {
            var newButton = new CoilButton
            {
                Label = newLabel,
                CoilAddress = info.CoilAddress,
                AvailableTags = vm.ConnectionConfig.Tags
            };
            newButton.OnCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await vm.WriteCoilAsync(newButton.CoilAddress, true);
                newButton.IsActive = true;
            });
            newButton.OffCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await vm.WriteCoilAsync(newButton.CoilAddress, false);
                newButton.IsActive = false;
            });
            newButton.CopyRequested += OnButtonCopyRequested;
            newButton.PasteRequested += OnButtonPasteRequested;
            buttonControl = newButton;
        }
        var draggable = new DraggableControl
        {
            X = newX,
            Y = newY,
            Content = buttonControl
        };
        canvas.Children.Add(draggable);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.RequestOpenSettings += OnRequestOpenSettings;
        }
    }

    private async void OnRequestOpenSettings(object? sender, EventArgs e)
    {
        var settingsVm = new SettingsWindowViewModel();
        if (DataContext is MainWindowViewModel mainVm)
        {
            settingsVm.Host = mainVm.ConnectionConfig.Host;
            settingsVm.Port = mainVm.ConnectionConfig.Port;
            settingsVm.UnitId = mainVm.ConnectionConfig.UnitId;
            settingsVm.PollingIntervalMs = mainVm.ConnectionConfig.PollingIntervalMs;
        }

        var settingsWindow = new SettingsWindow { DataContext = settingsVm };
        var result = await settingsWindow.ShowDialog<bool>(this);
        
        if (result && DataContext is MainWindowViewModel vm)
        {
            vm.ConnectionConfig.Host = settingsVm.Host;
            vm.ConnectionConfig.Port = settingsVm.Port;
            vm.ConnectionConfig.UnitId = settingsVm.UnitId;
            vm.ConnectionConfig.PollingIntervalMs = settingsVm.PollingIntervalMs;
            await vm.SaveSettingsAsync();
        }
    }

    private void UpdateButtonAvailableTags(MainWindowViewModel vm)
    {
        var canvas = this.FindControl<Canvas>("MnemoCanvas");
        if (canvas == null) return;

        foreach (var child in canvas.Children)
        {
            if (child is DraggableControl draggable)
            {
                if (draggable.Content is CoilButton coilBtn)
                {
                    var selectedTagName = coilBtn.SelectedTag?.Name;
                    coilBtn.AvailableTags = vm.ConnectionConfig.Tags;
                    // Восстанавливаем выбранный тег по имени
                    if (!string.IsNullOrEmpty(selectedTagName))
                    {
                        coilBtn.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == selectedTagName);
                    }
                }
                else if (draggable.Content is ImageButton imgBtn)
                {
                    var selectedTagName = imgBtn.SelectedTag?.Name;
                    imgBtn.AvailableTags = vm.ConnectionConfig.Tags;
                    // Восстанавливаем выбранный тег по имени
                    if (!string.IsNullOrEmpty(selectedTagName))
                    {
                        imgBtn.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == selectedTagName);
                    }
                }
            }
        }
    }

    private async void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            CollectMnemoschemeElements(vm);
            await vm.SaveConfigurationAsync();
        }
    }

    private void CollectMnemoschemeElements(MainWindowViewModel vm)
    {
        var canvas = this.FindControl<Canvas>("MnemoCanvas");
        if (canvas == null)
        {
            System.Diagnostics.Debug.WriteLine("CollectMnemoschemeElements: Canvas not found!");
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"CollectMnemoschemeElements: Canvas has {canvas.Children.Count} children");
        
        vm.ConnectionConfig.MnemoschemeElements.Clear();
        foreach (var child in canvas.Children)
        {
            System.Diagnostics.Debug.WriteLine($"  Child type: {child.GetType().Name}");
            
            if (child is not DraggableControl draggable) continue;
            
            var element = draggable.Content;
            System.Diagnostics.Debug.WriteLine($"    DraggableControl contains: {element?.GetType().Name}");
            
            MnemoschemeElement? mnemoElement = null;
            if (element is CoilButton coilBtn)
            {
                mnemoElement = new CoilElement
                {
                    Type = ElementType.CoilButton,
                    X = draggable.X,
                    Y = draggable.Y,
                    Label = coilBtn.Label,
                    CoilAddress = coilBtn.CoilAddress,
                    TagName = coilBtn.SelectedTag?.Name,
                    IconPathOn = coilBtn.IconPathOn,
                    IconPathOff = coilBtn.IconPathOff
                };
            }
            else if (element is ImageButton imgBtn)
            {
                mnemoElement = new CoilElement
                {
                    Type = ElementType.ImageButton,
                    X = draggable.X,
                    Y = draggable.Y,
                    Label = imgBtn.Label,
                    CoilAddress = imgBtn.CoilAddress,
                    TagName = imgBtn.SelectedTag?.Name,
                    ImageType = imgBtn.ImageType.ToString()
                };
            }
            else if (element is PumpControl pump)
            {
                mnemoElement = new PumpElement
                {
                    X = draggable.X,
                    Y = draggable.Y,
                    Label = pump.Label
                };
            }
            else if (element is ValveControl valve)
            {
                mnemoElement = new ValveElement
                {
                    X = draggable.X,
                    Y = draggable.Y,
                    Label = valve.Label
                };
            }
            else if (element is SensorIndicator sensor)
            {
                mnemoElement = new SensorElement
                {
                    X = draggable.X,
                    Y = draggable.Y,
                    Label = sensor.Label,
                    Unit = sensor.Unit,
                    ThresholdLow = sensor.ThresholdLow,
                    ThresholdHigh = sensor.ThresholdHigh
                };
            }
            if (mnemoElement != null)
            {
                vm.ConnectionConfig.MnemoschemeElements.Add(mnemoElement);
            }
        }
    }
}