using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
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
        
        System.Diagnostics.Debug.WriteLine("*** MainWindow constructor called ***");
        
        DataContextChanged += OnDataContextChanged;
        Loaded += OnWindowLoaded;
        
        // Дополнительная подписка на AttachedToVisualTree для гарантированной инициализации
        this.AttachedToVisualTree += OnAttachedToVisualTree;
        
        Closing += OnWindowClosing;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("*** OnAttachedToVisualTree called ***");
        
        // Устанавливаем центр Canvas для создания элементов по умолчанию
        var canvas = this.Find<Canvas>("MnemoCanvas");
        if (canvas != null)
        {
            _lastCanvasClickPosition = new Point(400, 300); // Центр по умолчанию
        }
    }

    private async void OnAddElementButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("*** OnAddElementButtonClick called ***");
        
        // Устанавливаем позицию в центре Canvas
        var canvas = this.Find<Canvas>("MnemoCanvas");
        if (canvas != null)
        {
            _lastCanvasClickPosition = new Point(
                canvas.Bounds.Width / 2 - 50, 
                canvas.Bounds.Height / 2 - 50
            );
        }
        
        await ShowCanvasContextMenuAsync();
    }

    private void OnWindowLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("*** OnWindowLoaded called ***");
        
        SubscribeToButtonEvents(this);
        
        // Добавляем обработчик правого клика на Canvas программно для надёжности
        var canvas = this.Find<Canvas>("MnemoCanvas");
        System.Diagnostics.Debug.WriteLine($"*** Canvas search result: {(canvas != null ? "FOUND" : "NOT FOUND")} ***");
        
        if (canvas != null)
        {
            System.Diagnostics.Debug.WriteLine("*** Canvas found, adding PointerReleased handler ***");
            // Дополнительная подписка через AddHandler для перехвата событий
            canvas.AddHandler(PointerReleasedEvent, OnCanvasPointerReleased, handledEventsToo: true);
            
            // Дополнительно подписываемся на PointerPressed для отладки
            canvas.AddHandler(PointerPressedEvent, (s, args) =>
            {
                var point = args.GetCurrentPoint(this);
                System.Diagnostics.Debug.WriteLine($"*** Canvas PointerPressed: Button={args.GetCurrentPoint(this).Properties.IsRightButtonPressed}, UpdateKind={point.Properties.PointerUpdateKind} ***");
            }, handledEventsToo: true);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("*** ERROR: Canvas NOT found! ***");
        }
        
        // Подписываемся на событие завершения загрузки настроек
        if (DataContext is MainWindowViewModel vm)
        {
            // Подписка на обновление Coil тегов
            vm.CoilTagsUpdated += OnCoilTagsUpdated;
            // Подписка на обновление регистров (Holding/Input)
            vm.RegisterTagsUpdated += OnRegisterTagsUpdated;
            this.Title = "SCADA - Подписка на события выполнена!";
            
            vm.SettingsLoadedEvent += (s, args) =>
            {
                System.Diagnostics.Debug.WriteLine("SettingsLoadedEvent fired, calling RestoreMnemoschemeElements");
                RestoreMnemoschemeElements();
                // Обновляем теги для всех существующих кнопок
                UpdateButtonAvailableTags(vm);
                // Восстанавливаем размер и позицию окна
                RestoreWindowState(vm);
            };
            
            // Если настройки уже загружены (синхронный путь)
            if (vm.SettingsLoaded)
            {
                System.Diagnostics.Debug.WriteLine("Settings already loaded, calling RestoreMnemoschemeElements immediately");
                RestoreMnemoschemeElements();
                // Обновляем теги для всех существующих кнопок
                UpdateButtonAvailableTags(vm);
                // Восстанавливаем размер и позицию окна
                RestoreWindowState(vm);
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

    private void OnRegisterTagsUpdated(object? sender, Dictionary<ushort, ushort> registerValues)
    {
        System.Diagnostics.Debug.WriteLine($"OnRegisterTagsUpdated: received {registerValues.Count} register values");
        foreach (var kvp in registerValues)
        {
            System.Diagnostics.Debug.WriteLine($"  RegisterValue: addr={kvp.Key}, value={kvp.Value}");
        }
        
        var canvas = this.FindControl<Canvas>("MnemoCanvas");
        if (canvas == null)
        {
            System.Diagnostics.Debug.WriteLine("OnRegisterTagsUpdated: Canvas not found!");
            return;
        }

        int updatedCount = 0;
        foreach (var child in canvas.Children)
        {
            if (child is DraggableControl draggable)
            {
                if (draggable.Content is DisplayControl display)
                {
                    if (registerValues.TryGetValue(display.RegisterAddress, out ushort value))
                    {
                        display.DisplayValue = value.ToString();
                        updatedCount++;
                        System.Diagnostics.Debug.WriteLine($"  Updated DisplayControl at address {display.RegisterAddress} to {value}");
                    }
                }
                else if (draggable.Content is SliderControl slider)
                {
                    if (registerValues.TryGetValue(slider.RegisterAddress, out ushort value))
                    {
                        slider.CurrentValue = value;
                        updatedCount++;
                        System.Diagnostics.Debug.WriteLine($"  Updated SliderControl at address {slider.RegisterAddress} to {value}");
                    }
                }
                else if (draggable.Content is NumericInputControl numeric)
                {
                    // НЕ обновляем, если пользователь редактирует поле
                    if (!numeric.IsEditing)
                    {
                        if (registerValues.TryGetValue(numeric.RegisterAddress, out ushort value))
                        {
                            numeric.InputValue = value.ToString();
                            updatedCount++;
                            System.Diagnostics.Debug.WriteLine($"  Updated NumericInputControl at address {numeric.RegisterAddress} to {value}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  Skipping NumericInputControl at address {numeric.RegisterAddress} - user is editing");
                    }
                }
                else if (draggable.Content is CustomIndicator customIndicator)
                {
                    if (registerValues.TryGetValue(customIndicator.RegisterAddress, out ushort value))
                    {
                        customIndicator.DisplayValue = value.ToString();
                        updatedCount++;
                        System.Diagnostics.Debug.WriteLine($"  Updated CustomIndicator at address {customIndicator.RegisterAddress} to {value}");
                    }
                }
            }
        }
        System.Diagnostics.Debug.WriteLine($"OnRegisterTagsUpdated: updated {updatedCount} controls");
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
                        ButtonType = coilElem.ButtonType,
                        AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags),
                        IconPathOn = coilElem.IconPathOn,
                        IconPathOff = coilElem.IconPathOff,
                        ButtonWidth = coilElem.ButtonWidth ?? 100.0,
                        ButtonHeight = coilElem.ButtonHeight ?? 100.0,
                        ShowLabel = coilElem.ShowLabel
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
                    coilBtn.DeleteRequested += OnButtonDeleteRequested;
                    coilBtn.TagChanged += OnButtonTagChanged; // Подписка на изменение тега
                    control = coilBtn;
                    break;
                case CoilElement momentaryElem when momentaryElem.Type == ElementType.CoilMomentaryButton:
                    // Конвертируем старый тип CoilMomentaryButton в CoilButton с ButtonType.Momentary
                    var momentaryAsCoilBtn = new CoilButton
                    {
                        Label = momentaryElem.Label,
                        CoilAddress = momentaryElem.CoilAddress,
                        ButtonType = CoilButtonType.Momentary,
                        AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags),
                        IconPathOn = momentaryElem.IconPathOn,
                        IconPathOff = momentaryElem.IconPathOff,
                        ButtonWidth = momentaryElem.ButtonWidth ?? 100.0,
                        ButtonHeight = momentaryElem.ButtonHeight ?? 100.0,
                        ShowLabel = momentaryElem.ShowLabel
                    };
                    if (!string.IsNullOrEmpty(momentaryElem.TagName))
                    {
                        momentaryAsCoilBtn.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == momentaryElem.TagName);
                    }
                    momentaryAsCoilBtn.OnCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await vm.WriteCoilAsync(momentaryAsCoilBtn.CoilAddress, true);
                        momentaryAsCoilBtn.IsActive = true;
                    });
                    momentaryAsCoilBtn.OffCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await vm.WriteCoilAsync(momentaryAsCoilBtn.CoilAddress, false);
                        momentaryAsCoilBtn.IsActive = false;
                    });
                    momentaryAsCoilBtn.CopyRequested += OnButtonCopyRequested;
                    momentaryAsCoilBtn.PasteRequested += OnButtonPasteRequested;
                    momentaryAsCoilBtn.DeleteRequested += OnButtonDeleteRequested;
                    momentaryAsCoilBtn.TagChanged += OnButtonTagChanged;
                    control = momentaryAsCoilBtn;
                    break;
                case CoilElement imgElem when imgElem.Type == ElementType.ImageButton:
                    var imgBtn = new ImageButton
                    {
                        Label = imgElem.Label,
                        CoilAddress = imgElem.CoilAddress,
                        ButtonType = imgElem.ButtonType,
                        AvailableTags = GetFilteredTagsForImageButton(vm.ConnectionConfig.Tags),
                        IconPathOn = imgElem.IconPathOn,
                        IconPathOff = imgElem.IconPathOff,
                        ButtonWidth = imgElem.ButtonWidth ?? 100.0,
                        ButtonHeight = imgElem.ButtonHeight ?? 120.0,
                        ShowLabel = imgElem.ShowLabel,
                        DisplaySettings = imgElem.DisplaySettings
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
                    imgBtn.DeleteRequested += OnButtonDeleteRequested;
                    imgBtn.TagChanged += OnButtonTagChanged;
                    control = imgBtn;
                    break;
                case PumpElement pumpElem:
                    control = new PumpControl { Label = pumpElem.Label };
                    break;
                case ValveElement valveElem:
                    control = new ValveControl { Label = valveElem.Label };
                    break;
                case SliderElement sliderElem:
                    var slider = new SliderControl
                    {
                        Label = sliderElem.Label,
                        RegisterAddress = sliderElem.RegisterAddress,
                        MinValue = sliderElem.MinValue,
                        MaxValue = sliderElem.MaxValue,
                        CurrentValue = sliderElem.MinValue,
                        ControlWidth = sliderElem.Width,
                        ControlHeight = sliderElem.Height,
                        AvailableTags = GetFilteredTagsForHoldingRegister(vm.ConnectionConfig.Tags)
                    };
                    if (!string.IsNullOrEmpty(sliderElem.TagName))
                    {
                        slider.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == sliderElem.TagName);
                    }
                    slider.WriteCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await vm.WriteRegisterAsync(slider.RegisterAddress, (ushort)slider.CurrentValue);
                    });
                    slider.DeleteRequested += OnButtonDeleteRequested;
                    slider.TagChanged += OnButtonTagChanged;
                    control = slider;
                    break;
                case NumericInputElement numericElem:
                    var numeric = new NumericInputControl
                    {
                        Label = numericElem.Label,
                        RegisterAddress = numericElem.RegisterAddress,
                        InputValue = "0",
                        Unit = numericElem.Unit,
                        ControlWidth = numericElem.Width,
                        ControlHeight = numericElem.Height,
                        AvailableTags = GetFilteredTagsForHoldingRegister(vm.ConnectionConfig.Tags)
                    };
                    if (!string.IsNullOrEmpty(numericElem.TagName))
                    {
                        numeric.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == numericElem.TagName);
                    }
                    numeric.WriteCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        if (ushort.TryParse(numeric.InputValue, out ushort value))
                        {
                            await vm.WriteRegisterAsync(numeric.RegisterAddress, value);
                        }
                        else
                        {
                            vm.ConnectionStatus = "⚠ Ошибка: введите корректное число (0-65535)";
                        }
                    });
                    numeric.DeleteRequested += OnButtonDeleteRequested;
                    numeric.TagChanged += OnButtonTagChanged;
                    control = numeric;
                    break;
                case DisplayElement displayElem:
                    var display = new DisplayControl
                    {
                        Label = displayElem.Label,
                        RegisterAddress = displayElem.RegisterAddress,
                        DisplayValue = "0",
                        Unit = displayElem.Unit,
                        ControlWidth = displayElem.Width,
                        ControlHeight = displayElem.Height,
                        AvailableTags = GetFilteredTagsForInputRegister(vm.ConnectionConfig.Tags)
                    };
                    if (!string.IsNullOrEmpty(displayElem.TagName))
                    {
                        display.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == displayElem.TagName);
                    }
                    display.DeleteRequested += OnButtonDeleteRequested;
                    display.TagChanged += OnButtonTagChanged;
                    control = display;
                    break;
                case ImageElement imageElem:
                    var imageCtrl = new ImageControl
                    {
                        Label = imageElem.Label,
                        ImagePath = imageElem.ImagePath,
                        ImageWidth = imageElem.Width,
                        ImageHeight = imageElem.Height,
                        ShowLabel = imageElem.ShowLabel
                    };
                    imageCtrl.DeleteRequested += OnButtonDeleteRequested;
                    control = imageCtrl;
                    break;
                case CustomIndicatorElement customIndElem:
                    var customInd = new CustomIndicator
                    {
                        Label = customIndElem.Label,
                        BackgroundImagePath = customIndElem.BackgroundImagePath,
                        BackgroundColor = customIndElem.BackgroundColor,
                        IndicatorWidth = customIndElem.Width,
                        IndicatorHeight = customIndElem.Height,
                        ShowLabel = customIndElem.ShowLabel,
                        RegisterAddress = customIndElem.RegisterAddress,
                        Unit = customIndElem.Unit,
                        AvailableTags = GetFilteredTagsForInputRegister(vm.ConnectionConfig.Tags)
                    };
                    if (!string.IsNullOrEmpty(customIndElem.TagName))
                    {
                        customInd.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == customIndElem.TagName);
                    }
                    customInd.DeleteRequested += OnButtonDeleteRequested;
                    customInd.ImageChanged += OnButtonTagChanged;
                    customInd.LabelChanged += OnButtonTagChanged;
                    customInd.ColorChanged += OnButtonTagChanged;
                    customInd.SizeChangedCustom += OnButtonTagChanged;
                    customInd.TagChanged += OnButtonTagChanged;
                    control = customInd;
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
                draggable.PositionChanged += async (s, e) =>
                {
                    if (DataContext is MainWindowViewModel mainVm)
                    {
                        CollectMnemoschemeElements(mainVm);
                        await mainVm.SaveConfigurationAsync();
                    }
                };
                draggable.SizeChangedCustom += async (s, e) =>
                {
                    if (DataContext is MainWindowViewModel mainVm)
                    {
                        CollectMnemoschemeElements(mainVm);
                        await mainVm.SaveConfigurationAsync();
                    }
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
                coilBtn.DeleteRequested += OnButtonDeleteRequested;
            }
            else if (child is ImageButton imgBtn)
            {
                imgBtn.CopyRequested += OnButtonCopyRequested;
                imgBtn.PasteRequested += OnButtonPasteRequested;
                imgBtn.DeleteRequested += OnButtonDeleteRequested;
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

    private async void OnButtonDeleteRequested(object? sender, EventArgs e)
    {
        // Находим родительский DraggableControl
        if (sender is Control control)
        {
            var draggable = control.Parent as DraggableControl;
            if (draggable != null)
            {
                var canvas = this.FindControl<Canvas>("MnemoCanvas");
                if (canvas != null)
                {
                    // Удаляем элемент с Canvas
                    canvas.Children.Remove(draggable);
                    
                    // Сохраняем изменения
                    if (DataContext is MainWindowViewModel vm)
                    {
                        CollectMnemoschemeElements(vm);
                        await vm.SaveConfigurationAsync();
                        vm.ConnectionStatus = $"Элемент удалён";
                    }
                }
            }
        }
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
        if (info.IsImageButton)
        {
            var newButton = new ImageButton
            {
                Label = newLabel,
                CoilAddress = info.CoilAddress,
                AvailableTags = GetFilteredTagsForImageButton(vm.ConnectionConfig.Tags)
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
            newButton.DeleteRequested += OnButtonDeleteRequested;
            newButton.TagChanged += OnButtonTagChanged; // Подписка на изменение тега
            buttonControl = newButton;
        }
        else
        {
            var newButton = new CoilButton
            {
                Label = newLabel,
                CoilAddress = info.CoilAddress,
                ButtonType = info.IsMomentary ? CoilButtonType.Momentary : CoilButtonType.Toggle,
                AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags)
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
            newButton.DeleteRequested += OnButtonDeleteRequested;
            newButton.TagChanged += OnButtonTagChanged; // Подписка на изменение тега
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
            viewModel.RequestOpenTagsEditor += OnRequestOpenTagsEditor;
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
            settingsVm.Theme = mainVm.ConnectionConfig.Theme;
        }

        var settingsWindow = new SettingsWindow { DataContext = settingsVm };
        var result = await settingsWindow.ShowDialog<bool>(this);
        
        if (result && DataContext is MainWindowViewModel vm)
        {
            vm.ConnectionConfig.Host = settingsVm.Host;
            vm.ConnectionConfig.Port = settingsVm.Port;
            vm.ConnectionConfig.UnitId = settingsVm.UnitId;
            vm.ConnectionConfig.PollingIntervalMs = settingsVm.PollingIntervalMs;
            vm.ConnectionConfig.Theme = settingsVm.Theme;
            
            // Применяем новую тему
            App.ApplyTheme(settingsVm.Theme);
            
            await vm.SaveSettingsAsync();
        }
    }

    private async void OnRequestOpenTagsEditor(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainVm)
            return;

        // Создаём копию коллекции тегов для редактирования
        var tagsCopy = new ObservableCollection<TagDefinition>(
            mainVm.ConnectionConfig.Tags.Select(t => new TagDefinition
            {
                Name = t.Name,
                Address = t.Address,
                Register = t.Register,
                Type = t.Type,
                Scale = t.Scale,
                Offset = t.Offset,
                Enabled = t.Enabled,
                WordOrder = t.WordOrder
            })
        );

        // Создаём TagsConfigService для доступа к полному списку тэгов
        var tagsService = new Services.TagsConfigService();
        var editorVm = new TagsEditorWindowViewModel(tagsCopy, tagsService);
        var editorWindow = new TagsEditorWindow { DataContext = editorVm };
        var result = await editorWindow.ShowDialog<bool>(this);

        if (result)
        {
            // Сохраняем изменённые теги обратно в конфигурацию
            mainVm.ConnectionConfig.Tags.Clear();
            foreach (var tag in editorVm.ActiveTags)
            {
                mainVm.ConnectionConfig.Tags.Add(tag);
            }
            
            // Помечаем что теги были инициализированы пользователем
            mainVm.ConnectionConfig.TagsInitialized = true;
            
            System.Diagnostics.Debug.WriteLine($"MainWindow: After TagsEditor - {mainVm.ConnectionConfig.Tags.Count} tags in config, TagsInitialized={mainVm.ConnectionConfig.TagsInitialized}");
            
            // Собираем текущие элементы мнемосхемы перед сохранением
            CollectMnemoschemeElements(mainVm);
            
            // Сохраняем полную конфигурацию
            await mainVm.SaveConfigurationAsync();
            
            System.Diagnostics.Debug.WriteLine($"MainWindow: SaveConfigurationAsync completed");

            // Обновляем доступные теги для всех элементов управления
            UpdateAllButtonAvailableTags();
        }
    }

    private void UpdateAllButtonAvailableTags()
    {
        if (this.Find<Canvas>("MnemoschemeCanvas") is Canvas canvas && DataContext is MainWindowViewModel vm)
        {
            foreach (var child in canvas.Children)
            {
                if (child is CoilButton coilBtn)
                {
                    coilBtn.AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags);
                }
                else if (child is CoilReadButton readBtn)
                {
                    readBtn.AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags);
                }
                else if (child is ImageButton imgBtn)
                {
                    // ImageButton - только X (Input) и Y (Coils)
                    imgBtn.AvailableTags = GetFilteredTagsForImageButton(vm.ConnectionConfig.Tags);
                }
            }
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
                    coilBtn.AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags);
                    // Восстанавливаем выбранный тег по имени
                    if (!string.IsNullOrEmpty(selectedTagName))
                    {
                        coilBtn.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == selectedTagName);
                    }
                }
                else if (draggable.Content is ImageButton imgBtn)
                {
                    var selectedTagName = imgBtn.SelectedTag?.Name;
                    imgBtn.AvailableTags = GetFilteredTagsForImageButton(vm.ConnectionConfig.Tags);
                    // Восстанавливаем выбранный тег по имени
                    if (!string.IsNullOrEmpty(selectedTagName))
                    {
                        imgBtn.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == selectedTagName);
                    }
                }
            }
        }
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            // Сохраняем размер и позицию окна
            vm.ConnectionConfig.WindowWidth = this.Width;
            vm.ConnectionConfig.WindowHeight = this.Height;
            vm.ConnectionConfig.WindowX = this.Position.X;
            vm.ConnectionConfig.WindowY = this.Position.Y;
            vm.ConnectionConfig.IsMaximized = this.WindowState == WindowState.Maximized;
            
            System.Diagnostics.Debug.WriteLine($"Saving window state: {Width}x{Height} at ({Position.X},{Position.Y}), Maximized={vm.ConnectionConfig.IsMaximized}");
            
            CollectMnemoschemeElements(vm);
            await vm.SaveConfigurationAsync();
        }
    }

    private async void OnButtonTagChanged(object? sender, EventArgs e)
    {
        // Автосохранение при изменении тега кнопки
        if (DataContext is MainWindowViewModel vm)
        {
            CollectMnemoschemeElements(vm);
            await vm.SaveConfigurationAsync();
            System.Diagnostics.Debug.WriteLine("Tag changed - settings auto-saved");
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
                    ButtonType = coilBtn.ButtonType,
                    IconPathOn = coilBtn.IconPathOn,
                    IconPathOff = coilBtn.IconPathOff,
                    ButtonWidth = coilBtn.ButtonWidth,
                    ButtonHeight = coilBtn.ButtonHeight,
                    ShowLabel = coilBtn.ShowLabel
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
                    ButtonType = imgBtn.ButtonType,
                    ImageType = string.Empty,
                    IconPathOn = imgBtn.IconPathOn,
                    IconPathOff = imgBtn.IconPathOff,
                    ButtonWidth = imgBtn.ButtonWidth,
                    ButtonHeight = imgBtn.ButtonHeight,
                    ShowLabel = imgBtn.ShowLabel,
                    DisplaySettings = imgBtn.DisplaySettings
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
            else if (element is SliderControl slider)
            {
                mnemoElement = new SliderElement
                {
                    X = draggable.X,
                    Y = draggable.Y,
                    Label = slider.Label,
                    RegisterAddress = slider.RegisterAddress,
                    TagName = slider.SelectedTag?.Name,
                    MinValue = slider.MinValue,
                    MaxValue = slider.MaxValue,
                    Unit = "",
                    Width = slider.ControlWidth,
                    Height = slider.ControlHeight
                };
            }
            else if (element is NumericInputControl numeric)
            {
                mnemoElement = new NumericInputElement
                {
                    X = draggable.X,
                    Y = draggable.Y,
                    Label = numeric.Label,
                    RegisterAddress = numeric.RegisterAddress,
                    TagName = numeric.SelectedTag?.Name,
                    Unit = numeric.Unit,
                    Width = numeric.ControlWidth,
                    Height = numeric.ControlHeight
                };
            }
            else if (element is DisplayControl display)
            {
                mnemoElement = new DisplayElement
                {
                    X = draggable.X,
                    Y = draggable.Y,
                    Label = display.Label,
                    RegisterAddress = display.RegisterAddress,
                    TagName = display.SelectedTag?.Name,
                    Unit = display.Unit,
                    Width = display.ControlWidth,
                    Height = display.ControlHeight
                };
            }
            else if (element is ImageControl imageCtrl)
            {
                mnemoElement = new ImageElement
                {
                    X = draggable.X,
                    Y = draggable.Y,
                    Label = imageCtrl.Label,
                    ImagePath = imageCtrl.ImagePath,
                    Width = imageCtrl.ImageWidth,
                    Height = imageCtrl.ImageHeight,
                    ShowLabel = imageCtrl.ShowLabel
                };
            }
            else if (element is CustomIndicator customInd)
            {
                mnemoElement = new CustomIndicatorElement
                {
                    X = draggable.X,
                    Y = draggable.Y,
                    Label = customInd.Label,
                    BackgroundImagePath = customInd.BackgroundImagePath,
                    BackgroundColor = customInd.BackgroundColor,
                    Width = customInd.IndicatorWidth,
                    Height = customInd.IndicatorHeight,
                    ShowLabel = customInd.ShowLabel,
                    RegisterAddress = customInd.RegisterAddress,
                    TagName = customInd.SelectedTag?.Name,
                    Unit = customInd.Unit
                };
            }
            if (mnemoElement != null)
            {
                vm.ConnectionConfig.MnemoschemeElements.Add(mnemoElement);
            }
        }
    }

    // Вспомогательные методы для фильтрации тэгов
    private ObservableCollection<TagDefinition> GetFilteredTagsForCoilButton(ObservableCollection<TagDefinition> allTags)
    {
        return new ObservableCollection<TagDefinition>(
            allTags.Where(t => t.Register == RegisterType.Coils && t.Name.StartsWith("M"))
        );
    }

    private ObservableCollection<TagDefinition> GetFilteredTagsForImageButton(ObservableCollection<TagDefinition> allTags)
    {
        return new ObservableCollection<TagDefinition>(
            allTags.Where(t => 
                ((t.Register == RegisterType.Input && t.Name.StartsWith("X")) ||
                 (t.Register == RegisterType.Coils && t.Name.StartsWith("Y"))))
        );
    }

    private ObservableCollection<TagDefinition> GetFilteredTagsForHoldingRegister(ObservableCollection<TagDefinition> allTags)
    {
        return new ObservableCollection<TagDefinition>(
            allTags.Where(t => t.Register == RegisterType.Holding && 
                              (t.Name.StartsWith("AQ") || t.Name.StartsWith("V")))
        );
    }

    private ObservableCollection<TagDefinition> GetFilteredTagsForInputRegister(ObservableCollection<TagDefinition> allTags)
    {
        return new ObservableCollection<TagDefinition>(
            allTags.Where(t => 
                // Input Register теги
                (t.Register == RegisterType.Input && 
                 (t.Name.StartsWith("AI") || t.Name.StartsWith("V") || 
                  t.Name.StartsWith("TV") || t.Name.StartsWith("CV") || 
                  t.Name.StartsWith("SV"))) ||
                // Holding Register теги (только для чтения через DisplayControl)
                (t.Register == RegisterType.Holding && 
                 (t.Name.StartsWith("V") || t.Name.StartsWith("AQ"))))
        );
    }

    private Point _lastCanvasClickPosition;

    private async void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        System.Diagnostics.Debug.WriteLine($"OnCanvasPointerReleased: InitialButton={e.InitialPressMouseButton}, UpdateKind={point.Properties.PointerUpdateKind}, Source={e.Source?.GetType().Name}");
        
        // Проверяем, что клик был правой кнопкой
        if (e.InitialPressMouseButton != MouseButton.Right && 
            point.Properties.PointerUpdateKind != PointerUpdateKind.RightButtonReleased)
        {
            System.Diagnostics.Debug.WriteLine("Not right button, ignoring");
            return;
        }

        var canvas = sender as Canvas;
        if (canvas == null)
        {
            System.Diagnostics.Debug.WriteLine("Sender is not Canvas");
            return;
        }

        // УПРОЩЕННАЯ ЛОГИКА: ищем DraggableControl среди всех элементов под курсором
        var clickPosition = e.GetPosition(canvas);
        
        // Проходим по всем DraggableControl на Canvas и проверяем, попал ли клик в их bounds
        foreach (var child in canvas.Children)
        {
            if (child is DraggableControl draggable)
            {
                var childBounds = new Rect(draggable.Bounds.Size);
                var childPosition = draggable.TranslatePoint(new Point(0, 0), canvas) ?? new Point(0, 0);
                var absoluteBounds = new Rect(childPosition, draggable.Bounds.Size);
                
                if (absoluteBounds.Contains(clickPosition))
                {
                    System.Diagnostics.Debug.WriteLine($"Click is inside DraggableControl at {childPosition}, ignoring");
                    return;
                }
            }
        }

        System.Diagnostics.Debug.WriteLine("Click on empty canvas area - showing context menu!");
        
        // Запоминаем позицию клика для создания элемента
        _lastCanvasClickPosition = clickPosition;

        // Показываем контекстное меню
        await ShowCanvasContextMenuAsync();
        e.Handled = true;
    }

    private async System.Threading.Tasks.Task ShowCanvasContextMenuAsync()
    {
        var dialog = new Window
        {
            Title = "Добавить элемент управления",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            MaxWidth = 600,
            MaxHeight = 800
        };

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var stack = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

        var headerText = new TextBlock
        {
            Text = "Выберите тип элемента для добавления:",
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 10)
        };
        stack.Children.Add(headerText);

        // Создаём кнопки для каждого типа элемента
        var coilButtonBtn = CreateMenuButton("🔘 Кнопка Coil (с фиксацией)", "Управление выходом с фиксацией состояния");
        coilButtonBtn.Click += (s, e) =>
        {
            CreateElementAtLastPosition(ElementType.CoilButton);
            dialog.Close();
        };

        var imageButtonBtn = CreateMenuButton("🖼️ Графическая кнопка", "Универсальная кнопка с изображением и отображением данных регистра");
        imageButtonBtn.Click += (s, e) =>
        {
            CreateElementAtLastPosition(ElementType.ImageButton);
            dialog.Close();
        };

        var sliderBtn = CreateMenuButton("🎚️ Ползунок", "Управление Holding Register с помощью ползунка");
        sliderBtn.Click += (s, e) =>
        {
            CreateElementAtLastPosition(ElementType.Slider);
            dialog.Close();
        };

        var numericInputBtn = CreateMenuButton("🔢 Числовой ввод", "Точный ввод значения в Holding Register");
        numericInputBtn.Click += (s, e) =>
        {
            CreateElementAtLastPosition(ElementType.NumericInput);
            dialog.Close();
        };

        var displayBtn = CreateMenuButton("📊 Индикатор", "Отображение значения Input Register (только чтение)");
        displayBtn.Click += (s, e) =>
        {
            CreateElementAtLastPosition(ElementType.Display);
            dialog.Close();
        };

        var imageBtn = CreateMenuButton("🖼️ Картинка", "Вставка изображения на мнемосхему (без управления)");
        imageBtn.Click += (s, e) =>
        {
            CreateElementAtLastPosition(ElementType.ImageControl);
            dialog.Close();
        };

        var customIndicatorBtn = CreateMenuButton("🎨 Настраиваемый индикатор", "Индикатор с фоновой картинкой и надписью");
        customIndicatorBtn.Click += (s, e) =>
        {
            CreateElementAtLastPosition(ElementType.CustomIndicator);
            dialog.Close();
        };

        stack.Children.Add(coilButtonBtn);
        stack.Children.Add(imageButtonBtn);
        stack.Children.Add(sliderBtn);
        stack.Children.Add(numericInputBtn);
        stack.Children.Add(displayBtn);
        stack.Children.Add(imageBtn);
        stack.Children.Add(customIndicatorBtn);

        var cancelBtn = new Button
        {
            Content = "❌ Отмена",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Padding = new Thickness(20, 8),
            Margin = new Thickness(0, 10, 0, 0)
        };
        cancelBtn.Click += (s, e) => dialog.Close();
        stack.Children.Add(cancelBtn);

        scrollViewer.Content = stack;
        dialog.Content = scrollViewer;

        if (this.IsVisible)
        {
            await dialog.ShowDialog(this);
        }
    }

    private Button CreateMenuButton(string title, string description)
    {
        var button = new Button
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            Padding = new Thickness(15, 10)
        };

        var panel = new StackPanel { Spacing = 3 };
        var titleText = new TextBlock
        {
            Text = title,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            FontSize = 13
        };
        var descText = new TextBlock
        {
            Text = description,
            FontSize = 11,
            Foreground = Avalonia.Media.Brushes.Gray
        };

        panel.Children.Add(titleText);
        panel.Children.Add(descText);
        button.Content = panel;

        return button;
    }

    private async void CreateElementAtLastPosition(ElementType elementType)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var canvas = this.FindControl<Canvas>("MnemoCanvas");
        if (canvas == null)
            return;

        Control control;
        var x = _lastCanvasClickPosition.X;
        var y = _lastCanvasClickPosition.Y;

        switch (elementType)
        {
            case ElementType.CoilButton:
                var coilBtn = new CoilButton
                {
                    Label = $"Кнопка {_dynamicButtonCounter++}",
                    CoilAddress = 0,
                    AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags)
                };
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
                coilBtn.DeleteRequested += OnButtonDeleteRequested;
                coilBtn.TagChanged += OnButtonTagChanged;
                control = coilBtn;
                break;

            case ElementType.CoilMomentaryButton:
                // Создаем CoilButton с типом Momentary для обратной совместимости
                var momentaryAsBtn = new CoilButton
                {
                    Label = $"Момент. кнопка {_dynamicButtonCounter++}",
                    CoilAddress = 0,
                    ButtonType = CoilButtonType.Momentary,
                    AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags)
                };
                momentaryAsBtn.OnCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    await vm.WriteCoilAsync(momentaryAsBtn.CoilAddress, true);
                    momentaryAsBtn.IsActive = true;
                });
                momentaryAsBtn.OffCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    await vm.WriteCoilAsync(momentaryAsBtn.CoilAddress, false);
                    momentaryAsBtn.IsActive = false;
                });
                momentaryAsBtn.CopyRequested += OnButtonCopyRequested;
                momentaryAsBtn.PasteRequested += OnButtonPasteRequested;
                momentaryAsBtn.DeleteRequested += OnButtonDeleteRequested;
                momentaryAsBtn.TagChanged += OnButtonTagChanged;
                control = momentaryAsBtn;
                break;

            case ElementType.ImageButton:
                var imgBtn = new ImageButton
                {
                    Label = $"ImageButton {_dynamicButtonCounter++}",
                    CoilAddress = 0,
                    AvailableTags = GetFilteredTagsForImageButton(vm.ConnectionConfig.Tags)
                };
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
                imgBtn.DeleteRequested += OnButtonDeleteRequested;
                imgBtn.TagChanged += OnButtonTagChanged;
                control = imgBtn;
                break;

            case ElementType.Slider:
                var slider = new SliderControl
                {
                    Label = $"Ползунок {_dynamicButtonCounter++}",
                    RegisterAddress = 0,
                    MinValue = 0,
                    MaxValue = 100,
                    CurrentValue = 0,
                    AvailableTags = GetFilteredTagsForHoldingRegister(vm.ConnectionConfig.Tags)
                };
                slider.WriteCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    await vm.WriteRegisterAsync(slider.RegisterAddress, (ushort)slider.CurrentValue);
                });
                slider.DeleteRequested += OnButtonDeleteRequested;
                slider.TagChanged += OnButtonTagChanged;
                control = slider;
                break;

            case ElementType.NumericInput:
                var numericInput = new NumericInputControl
                {
                    Label = $"Ввод {_dynamicButtonCounter++}",
                    RegisterAddress = 0,
                    InputValue = "0",
                    Unit = "",
                    AvailableTags = GetFilteredTagsForHoldingRegister(vm.ConnectionConfig.Tags)
                };
                numericInput.WriteCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    if (ushort.TryParse(numericInput.InputValue, out ushort value))
                    {
                        await vm.WriteRegisterAsync(numericInput.RegisterAddress, value);
                    }
                    else
                    {
                        vm.ConnectionStatus = "⚠ Ошибка: введите корректное число (0-65535)";
                    }
                });
                numericInput.DeleteRequested += OnButtonDeleteRequested;
                numericInput.TagChanged += OnButtonTagChanged;
                control = numericInput;
                break;

            case ElementType.Display:
                var display = new DisplayControl
                {
                    Label = $"Индикатор {_dynamicButtonCounter++}",
                    RegisterAddress = 0,
                    DisplayValue = "0",
                    Unit = "",
                    AvailableTags = GetFilteredTagsForInputRegister(vm.ConnectionConfig.Tags)
                };
                display.DeleteRequested += OnButtonDeleteRequested;
                display.TagChanged += OnButtonTagChanged;
                control = display;
                break;

            case ElementType.ImageControl:
                var imageControl = new ImageControl
                {
                    Label = $"Картинка {_dynamicButtonCounter++}",
                    ImagePath = "",
                    Width = 200,
                    Height = 200,
                    ShowLabel = true
                };
                imageControl.DeleteRequested += OnButtonDeleteRequested;
                control = imageControl;
                break;

            case ElementType.CustomIndicator:
                var customIndicator = new CustomIndicator
                {
                    Label = $"Индикатор {_dynamicButtonCounter++}",
                    BackgroundImagePath = "",
                    BackgroundColor = "#2563EB",
                    IndicatorWidth = 150,
                    IndicatorHeight = 150,
                    ShowLabel = true,
                    RegisterAddress = 0,
                    Unit = "",
                    AvailableTags = GetFilteredTagsForInputRegister(vm.ConnectionConfig.Tags)
                };
                customIndicator.DeleteRequested += OnButtonDeleteRequested;
                customIndicator.ImageChanged += OnButtonTagChanged;
                customIndicator.LabelChanged += OnButtonTagChanged;
                customIndicator.ColorChanged += OnButtonTagChanged;
                customIndicator.SizeChangedCustom += OnButtonTagChanged;
                customIndicator.TagChanged += OnButtonTagChanged;
                control = customIndicator;
                break;

            default:
                return;
        }

        var draggable = new DraggableControl
        {
            X = x,
            Y = y,
            Content = control
        };

        draggable.PositionChanged += async (s, e) =>
        {
            CollectMnemoschemeElements(vm);
            await vm.SaveConfigurationAsync();
        };
        
        draggable.SizeChangedCustom += async (s, e) =>
        {
            CollectMnemoschemeElements(vm);
            await vm.SaveConfigurationAsync();
        };

        canvas.Children.Add(draggable);

        // Сохраняем изменения
        CollectMnemoschemeElements(vm);
        await vm.SaveConfigurationAsync();
        vm.ConnectionStatus = $"Добавлен элемент: {elementType}";
    }

    private void RestoreWindowState(MainWindowViewModel vm)
    {
        System.Diagnostics.Debug.WriteLine($"Restoring window state: {vm.ConnectionConfig.WindowWidth}x{vm.ConnectionConfig.WindowHeight} at ({vm.ConnectionConfig.WindowX},{vm.ConnectionConfig.WindowY}), Maximized={vm.ConnectionConfig.IsMaximized}");
        
        // Восстанавливаем размер окна
        if (vm.ConnectionConfig.WindowWidth > 0 && vm.ConnectionConfig.WindowHeight > 0)
        {
            this.Width = vm.ConnectionConfig.WindowWidth;
            this.Height = vm.ConnectionConfig.WindowHeight;
        }
        
        // Восстанавливаем позицию окна
        if (vm.ConnectionConfig.WindowX >= 0 && vm.ConnectionConfig.WindowY >= 0)
        {
            this.Position = new PixelPoint((int)vm.ConnectionConfig.WindowX, (int)vm.ConnectionConfig.WindowY);
        }
        
        // Восстанавливаем состояние (развернуто/нормальное)
        if (vm.ConnectionConfig.IsMaximized)
        {
            this.WindowState = WindowState.Maximized;
        }
    }
}
