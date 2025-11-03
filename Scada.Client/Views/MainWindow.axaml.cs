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

    private Point? _rightClickPosition;

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
            this.Title = "SCADA - Подписка на CoilTagsUpdated выполнена!";
            
            vm.SettingsLoadedEvent += (s, args) =>
            {
                System.Diagnostics.Debug.WriteLine("SettingsLoadedEvent fired, calling RestoreMnemoschemeElements");
                RestoreMnemoschemeElements();
                // Обновляем теги для всех существующих кнопок
                UpdateButtonAvailableTags(vm);
            };
            
            // Если настройки уже загружены (синхронный путь)
            if (vm.SettingsLoaded)
            {
                System.Diagnostics.Debug.WriteLine("Settings already loaded, calling RestoreMnemoschemeElements immediately");
                RestoreMnemoschemeElements();
                // Обновляем теги для всех существующих кнопок
                UpdateButtonAvailableTags(vm);
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
                else if (draggable.Content is CoilMomentaryButton momentaryBtn)
                {
                    totalButtons++;
                    if (coilValues.TryGetValue(momentaryBtn.CoilAddress, out bool value))
                    {
                        System.Diagnostics.Debug.WriteLine($"  Updating CoilMomentaryButton at address {momentaryBtn.CoilAddress} to {value}, WAS: {momentaryBtn.IsActive}");
                        momentaryBtn.IsActive = value;
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
                        AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags),
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
                    coilBtn.DeleteRequested += OnButtonDeleteRequested;
                    coilBtn.TagChanged += OnButtonTagChanged; // Подписка на изменение тега
                    control = coilBtn;
                    break;
                case CoilElement momentaryElem when momentaryElem.Type == ElementType.CoilMomentaryButton:
                    var momentaryBtn = new CoilMomentaryButton
                    {
                        Label = momentaryElem.Label,
                        CoilAddress = momentaryElem.CoilAddress,
                        AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags),
                        IconPathOn = momentaryElem.IconPathOn,
                        IconPathOff = momentaryElem.IconPathOff
                    };
                    if (!string.IsNullOrEmpty(momentaryElem.TagName))
                    {
                        momentaryBtn.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == momentaryElem.TagName);
                    }
                    momentaryBtn.OnCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await vm.WriteCoilAsync(momentaryBtn.CoilAddress, true);
                        momentaryBtn.IsActive = true;
                    });
                    momentaryBtn.OffCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await vm.WriteCoilAsync(momentaryBtn.CoilAddress, false);
                        momentaryBtn.IsActive = false;
                    });
                    momentaryBtn.CopyRequested += OnButtonCopyRequested;
                    momentaryBtn.PasteRequested += OnButtonPasteRequested;
                    momentaryBtn.DeleteRequested += OnButtonDeleteRequested;
                    momentaryBtn.TagChanged += OnButtonTagChanged; // Подписка на изменение тега
                    control = momentaryBtn;
                    break;
                case CoilElement imgElem when imgElem.Type == ElementType.ImageButton:
                    if (Enum.TryParse<ImageButtonType>(imgElem.ImageType, out var imgType))
                    {
                        var imgBtn = new ImageButton
                        {
                            Label = imgElem.Label,
                            CoilAddress = imgElem.CoilAddress,
                            ImageType = imgType,
                            AvailableTags = GetFilteredTagsForImageButton(vm.ConnectionConfig.Tags),
                            IconPathOn = imgElem.IconPathOn,
                            IconPathOff = imgElem.IconPathOff
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
                        imgBtn.TagChanged += OnButtonTagChanged; // Подписка на изменение тега
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
                coilBtn.DeleteRequested += OnButtonDeleteRequested;
            }
            else if (child is CoilMomentaryButton momentaryBtn)
            {
                momentaryBtn.CopyRequested += OnButtonCopyRequested;
                momentaryBtn.PasteRequested += OnButtonPasteRequested;
                momentaryBtn.DeleteRequested += OnButtonDeleteRequested;
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
        if (info.IsImageButton && Enum.TryParse<ImageButtonType>(info.ImageType, out var imgType))
        {
            var newButton = new ImageButton
            {
                Label = newLabel,
                CoilAddress = info.CoilAddress,
                ImageType = imgType,
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
        else if (info.IsMomentary)
        {
            var newButton = new CoilMomentaryButton
            {
                Label = newLabel,
                CoilAddress = info.CoilAddress,
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
            buttonControl = newButton;
        }
        else
        {
            var newButton = new CoilButton
            {
                Label = newLabel,
                CoilAddress = info.CoilAddress,
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
            await mainVm.SaveSettingsAsync();

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
                else if (child is CoilMomentaryButton momentaryBtn)
                {
                    momentaryBtn.AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags);
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
                else if (draggable.Content is CoilMomentaryButton momentaryBtn)
                {
                    var selectedTagName = momentaryBtn.SelectedTag?.Name;
                    momentaryBtn.AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags);
                    // Восстанавливаем выбранный тег по имени
                    if (!string.IsNullOrEmpty(selectedTagName))
                    {
                        momentaryBtn.SelectedTag = vm.ConnectionConfig.Tags.FirstOrDefault(t => t.Name == selectedTagName);
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
                    IconPathOn = coilBtn.IconPathOn,
                    IconPathOff = coilBtn.IconPathOff
                };
            }
            else if (element is CoilMomentaryButton momentaryBtn)
            {
                mnemoElement = new CoilElement
                {
                    Type = ElementType.CoilMomentaryButton,
                    X = draggable.X,
                    Y = draggable.Y,
                    Label = momentaryBtn.Label,
                    CoilAddress = momentaryBtn.CoilAddress,
                    TagName = momentaryBtn.SelectedTag?.Name,
                    IconPathOn = momentaryBtn.IconPathOn,
                    IconPathOff = momentaryBtn.IconPathOff
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
                    ImageType = imgBtn.ImageType.ToString(),
                    IconPathOn = imgBtn.IconPathOn,
                    IconPathOff = imgBtn.IconPathOff
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

    // Вспомогательные методы для фильтрации тэгов
    private ObservableCollection<TagDefinition> GetFilteredTagsForCoilButton(ObservableCollection<TagDefinition> allTags)
    {
        return new ObservableCollection<TagDefinition>(
            allTags.Where(t => t.Register == RegisterType.Coils && t.Name.StartsWith("M") && t.Enabled)
        );
    }

    private ObservableCollection<TagDefinition> GetFilteredTagsForImageButton(ObservableCollection<TagDefinition> allTags)
    {
        return new ObservableCollection<TagDefinition>(
            allTags.Where(t => 
                ((t.Register == RegisterType.Input && t.Name.StartsWith("X")) ||
                 (t.Register == RegisterType.Coils && t.Name.StartsWith("Y"))) && 
                t.Enabled)
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
            Width = 450,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
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

        var momentaryButtonBtn = CreateMenuButton("⏺️ Кнопка Momentary (без фиксации)", "Кнопка, активная только при удержании");
        momentaryButtonBtn.Click += (s, e) =>
        {
            CreateElementAtLastPosition(ElementType.CoilMomentaryButton);
            dialog.Close();
        };

        var imageMotorBtn = CreateMenuButton("⚙️ Мотор (ImageButton)", "Графический элемент управления мотором");
        imageMotorBtn.Click += (s, e) =>
        {
            CreateElementAtLastPosition(ElementType.ImageButton, ImageButtonType.Motor);
            dialog.Close();
        };

        var imageValveBtn = CreateMenuButton("🔧 Клапан (ImageButton)", "Графический элемент управления клапаном");
        imageValveBtn.Click += (s, e) =>
        {
            CreateElementAtLastPosition(ElementType.ImageButton, ImageButtonType.Valve);
            dialog.Close();
        };

        var imageFanBtn = CreateMenuButton("🌀 Вентилятор (ImageButton)", "Графический элемент управления вентилятором");
        imageFanBtn.Click += (s, e) =>
        {
            CreateElementAtLastPosition(ElementType.ImageButton, ImageButtonType.Fan);
            dialog.Close();
        };

        stack.Children.Add(coilButtonBtn);
        stack.Children.Add(momentaryButtonBtn);
        stack.Children.Add(imageMotorBtn);
        stack.Children.Add(imageValveBtn);
        stack.Children.Add(imageFanBtn);

        var cancelBtn = new Button
        {
            Content = "❌ Отмена",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Padding = new Thickness(20, 8),
            Margin = new Thickness(0, 10, 0, 0)
        };
        cancelBtn.Click += (s, e) => dialog.Close();
        stack.Children.Add(cancelBtn);

        dialog.Content = stack;

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

    private async void CreateElementAtLastPosition(ElementType elementType, ImageButtonType? imageType = null)
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
                var momentaryBtn = new CoilMomentaryButton
                {
                    Label = $"Момент. кнопка {_dynamicButtonCounter++}",
                    CoilAddress = 0,
                    AvailableTags = GetFilteredTagsForCoilButton(vm.ConnectionConfig.Tags)
                };
                momentaryBtn.OnCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    await vm.WriteCoilAsync(momentaryBtn.CoilAddress, true);
                    momentaryBtn.IsActive = true;
                });
                momentaryBtn.OffCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    await vm.WriteCoilAsync(momentaryBtn.CoilAddress, false);
                    momentaryBtn.IsActive = false;
                });
                momentaryBtn.CopyRequested += OnButtonCopyRequested;
                momentaryBtn.PasteRequested += OnButtonPasteRequested;
                momentaryBtn.DeleteRequested += OnButtonDeleteRequested;
                momentaryBtn.TagChanged += OnButtonTagChanged;
                control = momentaryBtn;
                break;

            case ElementType.ImageButton when imageType.HasValue:
                var imgBtn = new ImageButton
                {
                    Label = $"{imageType} {_dynamicButtonCounter++}",
                    CoilAddress = 0,
                    ImageType = imageType.Value,
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

            default:
                return;
        }

        var draggable = new DraggableControl
        {
            X = x,
            Y = y,
            Content = control
        };

        canvas.Children.Add(draggable);

        // Сохраняем изменения
        CollectMnemoschemeElements(vm);
        await vm.SaveConfigurationAsync();
        vm.ConnectionStatus = $"Добавлен элемент: {elementType}";
    }
}
