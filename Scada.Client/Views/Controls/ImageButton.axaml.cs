using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Scada.Client.Models;
using Scada.Client.Services;

namespace Scada.Client.Views.Controls;

public partial class ImageButton : UserControl
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<ImageButton, bool>(nameof(IsActive));

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ImageButton, string>(nameof(Label), defaultValue: "Устройство");

    public static readonly StyledProperty<ushort> CoilAddressProperty =
        AvaloniaProperty.Register<ImageButton, ushort>(nameof(CoilAddress), defaultValue: (ushort)0);

    public static readonly StyledProperty<ObservableCollection<TagDefinition>?> AvailableTagsProperty =
        AvaloniaProperty.Register<ImageButton, ObservableCollection<TagDefinition>?>(nameof(AvailableTags));

    public static readonly StyledProperty<TagDefinition?> SelectedTagProperty =
        AvaloniaProperty.Register<ImageButton, TagDefinition?>(nameof(SelectedTag));

    public static readonly StyledProperty<ICommand?> OnCommandProperty =
        AvaloniaProperty.Register<ImageButton, ICommand?>(nameof(OnCommand));

    public static readonly StyledProperty<ICommand?> OffCommandProperty =
        AvaloniaProperty.Register<ImageButton, ICommand?>(nameof(OffCommand));

    public static readonly StyledProperty<string?> IconPathOnProperty =
        AvaloniaProperty.Register<ImageButton, string?>(nameof(IconPathOn));

    public static readonly StyledProperty<string?> IconPathOffProperty =
        AvaloniaProperty.Register<ImageButton, string?>(nameof(IconPathOff));

    public static readonly StyledProperty<double> ButtonWidthProperty =
        AvaloniaProperty.Register<ImageButton, double>(nameof(ButtonWidth), defaultValue: 100.0);

    public static readonly StyledProperty<double> ButtonHeightProperty =
        AvaloniaProperty.Register<ImageButton, double>(nameof(ButtonHeight), defaultValue: 120.0);

    public static readonly StyledProperty<CoilButtonType> ButtonTypeProperty =
        AvaloniaProperty.Register<ImageButton, CoilButtonType>(nameof(ButtonType), defaultValue: CoilButtonType.Toggle);

    public static readonly StyledProperty<bool> ShowLabelProperty =
        AvaloniaProperty.Register<ImageButton, bool>(nameof(ShowLabel), defaultValue: true);

    public static readonly StyledProperty<DisplaySettings?> DisplaySettingsProperty =
        AvaloniaProperty.Register<ImageButton, DisplaySettings?>(nameof(DisplaySettings));

    public static readonly StyledProperty<string> DisplayValueProperty =
        AvaloniaProperty.Register<ImageButton, string>(nameof(DisplayValue), defaultValue: string.Empty);

    public event EventHandler<CoilButtonInfo>? CopyRequested;
    public event EventHandler? PasteRequested;
    public event EventHandler? DeleteRequested; // Событие для удаления элемента
    public event EventHandler? TagChanged; // Новое событие для уведомления об изменении тега

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public ushort CoilAddress
    {
        get => GetValue(CoilAddressProperty);
        set => SetValue(CoilAddressProperty, value);
    }

    public ObservableCollection<TagDefinition>? AvailableTags
    {
        get => GetValue(AvailableTagsProperty);
        set => SetValue(AvailableTagsProperty, value);
    }

    public TagDefinition? SelectedTag
    {
        get => GetValue(SelectedTagProperty);
        set => SetValue(SelectedTagProperty, value);
    }

    public ICommand? OnCommand
    {
        get => GetValue(OnCommandProperty);
        set => SetValue(OnCommandProperty, value);
    }

    public ICommand? OffCommand
    {
        get => GetValue(OffCommandProperty);
        set => SetValue(OffCommandProperty, value);
    }

    public string? IconPathOn
    {
        get => GetValue(IconPathOnProperty);
        set => SetValue(IconPathOnProperty, value);
    }

    public string? IconPathOff
    {
        get => GetValue(IconPathOffProperty);
        set => SetValue(IconPathOffProperty, value);
    }

    public double ButtonWidth
    {
        get => GetValue(ButtonWidthProperty);
        set => SetValue(ButtonWidthProperty, value);
    }

    public double ButtonHeight
    {
        get => GetValue(ButtonHeightProperty);
        set => SetValue(ButtonHeightProperty, value);
    }

    public CoilButtonType ButtonType
    {
        get => GetValue(ButtonTypeProperty);
        set => SetValue(ButtonTypeProperty, value);
    }

    public bool ShowLabel
    {
        get => GetValue(ShowLabelProperty);
        set => SetValue(ShowLabelProperty, value);
    }

    public DisplaySettings? DisplaySettings
    {
        get => GetValue(DisplaySettingsProperty);
        set => SetValue(DisplaySettingsProperty, value);
    }

    public string DisplayValue
    {
        get => GetValue(DisplayValueProperty);
        set => SetValue(DisplayValueProperty, value);
    }

    public ImageButton()
    {
        InitializeComponent();
        
        // Subscribe to SelectedTag changes to update CoilAddress
        this.GetObservable(SelectedTagProperty).Subscribe(tag =>
        {
            if (tag != null && (tag.Register == RegisterType.Coils || tag.Register == RegisterType.Input))
            {
                CoilAddress = tag.Address;
                // Уведомляем об изменении тега для сохранения настроек
                TagChanged?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    // Поля для обработки кликов
    private bool _isMomentaryPressed = false;
    private Point _pressStartPoint;
    private bool _wasPressed = false;

    // Обработчики для моментальной кнопки
    private void OnMainButtonPressed(object? sender, PointerPressedEventArgs e)
    {
        // Игнорируем не левую кнопку мыши (правый клик для контекстного меню)
        var properties = e.GetCurrentPoint(this).Properties;
        if (!properties.IsLeftButtonPressed)
        {
            return;
        }

        _pressStartPoint = e.GetPosition(this);
        _wasPressed = true;

        if (ButtonType == CoilButtonType.Momentary && !_isMomentaryPressed)
        {
            _isMomentaryPressed = true;
            // Активируем катушку
            if (OnCommand?.CanExecute(null) == true)
            {
                OnCommand.Execute(null);
            }
            // НЕ устанавливаем e.Handled = true для возможности перетаскивания
        }
        else if (ButtonType == CoilButtonType.Toggle)
        {
            // Для Toggle только запоминаем, что была нажата
            // НЕ устанавливаем e.Handled = true для возможности перетаскивания
        }
    }

    private void OnMainButtonReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Игнорируем не левую кнопку мыши
        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        if (ButtonType == CoilButtonType.Momentary && _isMomentaryPressed)
        {
            _isMomentaryPressed = false;
            // Деактивируем катушку
            if (OffCommand?.CanExecute(null) == true)
            {
                OffCommand.Execute(null);
            }
            // НЕ устанавливаем e.Handled = true для возможности перетаскивания
        }
        else if (ButtonType == CoilButtonType.Toggle && _wasPressed)
        {
            // Проверяем, что это был клик, а не перетаскивание
            var releasePoint = e.GetPosition(this);
            var distance = Math.Sqrt(
                Math.Pow(releasePoint.X - _pressStartPoint.X, 2) +
                Math.Pow(releasePoint.Y - _pressStartPoint.Y, 2)
            );

            // Если курсор сместился меньше чем на 5 пикселей - это клик
            if (distance < 5)
            {
                // Переключаем состояние
                if (IsActive)
                {
                    if (OffCommand?.CanExecute(null) == true)
                    {
                        OffCommand.Execute(null);
                    }
                }
                else
                {
                    if (OnCommand?.CanExecute(null) == true)
                    {
                        OnCommand.Execute(null);
                    }
                }
            }
            // НЕ устанавливаем e.Handled = true для возможности перетаскивания
        }

        _wasPressed = false;
    }

    // Поля для изменения размера
    private bool _isResizing = false;
    private Point _resizeStartPoint;
    private double _resizeStartWidth;
    private double _resizeStartHeight;
    private string _resizeMode = ""; // "bottomright", "right", "bottom"

    // Поля для хранения UI элементов настроек отображения (для сохранения в диалоге)
    private CheckBox? _showValueCheckBox;
    private NumericUpDown? _registerAddressInput;
    private ComboBox? _registerTypeCombo;
    private ComboBox? _dataTypeCombo;
    private NumericUpDown? _scaleInput;
    private NumericUpDown? _offsetInput;
    private NumericUpDown? _minValueInput;
    private NumericUpDown? _maxValueInput;
    private TextBox? _unitInput;
    private CheckBox? _showUnitCheckBox;
    private NumericUpDown? _decimalPlacesInput;
    private CheckBox? _colorByStateCheckBox;
    private TextBox? _offColorInput;
    private TextBox? _onColorInput;
    private TextBox? _offTextInput;
    private TextBox? _onTextInput;
    private CheckBox? _useStateTextCheckBox;
    private TextBox? _lowColorInput;
    private TextBox? _normalColorInput;
    private TextBox? _highColorInput;
    private NumericUpDown? _lowThresholdInput;
    private NumericUpDown? _highThresholdInput;

    private void OnResizeGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border grip)
        {
            _isResizing = true;
            _resizeStartPoint = e.GetPosition(this);
            _resizeStartWidth = ButtonWidth;
            _resizeStartHeight = ButtonHeight;
            
            // Определяем режим изменения размера
            _resizeMode = grip.Name switch
            {
                "ResizeGripBottomRight" => "bottomright",
                "ResizeGripRight" => "right",
                "ResizeGripBottom" => "bottom",
                _ => ""
            };
            
            grip.PointerCaptureLost += OnPointerCaptureLost;
            e.Pointer.Capture(grip);
            e.Handled = true;
        }
    }

    private void OnResizeGripMoved(object? sender, PointerEventArgs e)
    {
        if (_isResizing && sender is Border)
        {
            var currentPoint = e.GetPosition(this);
            var deltaX = currentPoint.X - _resizeStartPoint.X;
            var deltaY = currentPoint.Y - _resizeStartPoint.Y;

            if (_resizeMode == "bottomright" || _resizeMode == "right")
            {
                var newWidth = Math.Max(50, Math.Min(500, _resizeStartWidth + deltaX));
                ButtonWidth = newWidth;
            }

            if (_resizeMode == "bottomright" || _resizeMode == "bottom")
            {
                var newHeight = Math.Max(50, Math.Min(500, _resizeStartHeight + deltaY));
                ButtonHeight = newHeight;
            }

            e.Handled = true;
        }
    }

    private void OnResizeGripReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isResizing && sender is Border grip)
        {
            _isResizing = false;
            grip.PointerCaptureLost -= OnPointerCaptureLost;
            e.Pointer.Capture(null);
            
            // Уведомляем об изменении для сохранения
            TagChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isResizing = false;
    }

    private void OnToggleClick(object? sender, RoutedEventArgs e)
    {
        // Toggle состояние и выполнить соответствующую команду
        if (IsActive)
        {
            // Текущее состояние ON, переключаем в OFF
            OffCommand?.Execute(null);
        }
        else
        {
            // Текущее состояние OFF, переключаем в ON
            OnCommand?.Execute(null);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        
        // Показываем контекстное меню при правом клике
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ShowContextMenu();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        
        // Ctrl+C - копировать
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
        {
            CopyButton();
            e.Handled = true;
        }
        // Ctrl+V - вставить
        else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.V)
        {
            PasteRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
        // Delete - удалить элемент
        else if (e.Key == Key.Delete)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void CopyButton()
    {
        // Получаем позицию из родительского DraggableControl
        var parentDraggable = this.Parent as DraggableControl;
        
        var info = new CoilButtonInfo
        {
            Label = Label,
            CoilAddress = CoilAddress,
            TagName = SelectedTag?.Name,
            IsImageButton = true,
            ImageType = string.Empty,
            X = parentDraggable?.X ?? 0,
            Y = parentDraggable?.Y ?? 0
        };
        CopyRequested?.Invoke(this, info);
    }

    private async void ShowContextMenu()
    {
        var dialog = new Window
        {
            Title = "Настройки устройства",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            MaxWidth = 700,
            MaxHeight = 700
        };

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var stack = new StackPanel { Margin = new Thickness(20), Spacing = 12 };

        // Поле для редактирования надписи
        var labelTextBlock = new TextBlock { Text = "Название устройства:", FontWeight = FontWeight.SemiBold };
        var labelInput = new TextBox 
        { 
            Text = Label,
            Watermark = "Введите название устройства"
        };
        stack.Children.Add(labelTextBlock);
        stack.Children.Add(labelInput);

        // Поле для выбора иконки ON
        var iconOnTextBlock = new TextBlock { Text = "Иконка ON (включено):", FontWeight = FontWeight.SemiBold };
        var iconOnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        var iconOnInput = new TextBox 
        { 
            Text = IconPathOn ?? "",
            Watermark = "Assets/device_on.png",
            MinWidth = 300
        };
        var iconOnBrowseBtn = new Button { Content = "📁", Width = 35, Padding = new Thickness(5) };
        iconOnBrowseBtn.Click += async (s, e) =>
        {
            if (dialog.StorageProvider.CanOpen)
            {
                var assetsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
                IStorageFolder? suggestedStartLocation = null;
                
                if (Directory.Exists(assetsPath))
                {
                    suggestedStartLocation = await dialog.StorageProvider.TryGetFolderFromPathAsync(new Uri(assetsPath));
                }
                
                var files = await dialog.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Выберите иконку ON",
                    AllowMultiple = false,
                    SuggestedStartLocation = suggestedStartLocation,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Изображения")
                        {
                            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.svg" }
                        }
                    }
                });
                
                if (files.Count > 0)
                {
                    iconOnInput.Text = files[0].Path.LocalPath;
                }
            }
        };
        iconOnPanel.Children.Add(iconOnInput);
        iconOnPanel.Children.Add(iconOnBrowseBtn);
        stack.Children.Add(iconOnTextBlock);
        stack.Children.Add(iconOnPanel);

        // Поле для выбора иконки OFF
        var iconOffTextBlock = new TextBlock { Text = "Иконка OFF (выключено):", FontWeight = FontWeight.SemiBold };
        var iconOffPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        var iconOffInput = new TextBox 
        { 
            Text = IconPathOff ?? "",
            Watermark = "Assets/device_off.png",
            MinWidth = 300
        };
        var iconOffBrowseBtn = new Button { Content = "📁", Width = 35, Padding = new Thickness(5) };
        iconOffBrowseBtn.Click += async (s, e) =>
        {
            if (dialog.StorageProvider.CanOpen)
            {
                var assetsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
                IStorageFolder? suggestedStartLocation = null;
                
                if (Directory.Exists(assetsPath))
                {
                    suggestedStartLocation = await dialog.StorageProvider.TryGetFolderFromPathAsync(new Uri(assetsPath));
                }
                
                var files = await dialog.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Выберите иконку OFF",
                    AllowMultiple = false,
                    SuggestedStartLocation = suggestedStartLocation,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Изображения")
                        {
                            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.svg" }
                        }
                    }
                });
                
                if (files.Count > 0)
                {
                    iconOffInput.Text = files[0].Path.LocalPath;
                }
            }
        };
        iconOffPanel.Children.Add(iconOffInput);
        iconOffPanel.Children.Add(iconOffBrowseBtn);
        stack.Children.Add(iconOffTextBlock);
        stack.Children.Add(iconOffPanel);

        // Размеры кнопки
        var sizeTextBlock = new TextBlock { Text = "Размеры кнопки (пиксели):", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 10, 0, 0) };
        stack.Children.Add(sizeTextBlock);
        
        var sizePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        sizePanel.Children.Add(new TextBlock { Text = "Ширина:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var widthInput = new NumericUpDown 
        { 
            Minimum = 50, 
            Maximum = 500, 
            Value = (decimal)ButtonWidth, 
            Width = 120,
            Increment = 10
        };
        sizePanel.Children.Add(widthInput);
        sizePanel.Children.Add(new TextBlock { Text = "Высота:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
        var heightInput = new NumericUpDown 
        { 
            Minimum = 50, 
            Maximum = 500, 
            Value = (decimal)ButtonHeight, 
            Width = 120,
            Increment = 10
        };
        sizePanel.Children.Add(heightInput);
        stack.Children.Add(sizePanel);

        // Поле для выбора типа кнопки
        var buttonTypeTextBlock = new TextBlock { Text = "Тип кнопки:", FontWeight = FontWeight.SemiBold };
        var buttonTypeCombo = new ComboBox
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };
        buttonTypeCombo.Items.Add(new ComboBoxItem { Content = "С фиксацией (переключатель)", Tag = CoilButtonType.Toggle });
        buttonTypeCombo.Items.Add(new ComboBoxItem { Content = "Моментальная (удержание)", Tag = CoilButtonType.Momentary });
        buttonTypeCombo.SelectedIndex = ButtonType == CoilButtonType.Toggle ? 0 : 1;
        
        stack.Children.Add(buttonTypeTextBlock);
        stack.Children.Add(buttonTypeCombo);

        // Чекбокс для отображения надписи
        var showLabelCheckBox = new CheckBox
        {
            Content = "Показывать надпись",
            IsChecked = ShowLabel,
            Margin = new Thickness(0, 5, 0, 0)
        };
        stack.Children.Add(showLabelCheckBox);

        // Разделитель
        stack.Children.Add(new Separator { Margin = new Thickness(0, 5, 0, 5) });

        // === СЕКЦИЯ НАСТРОЕК ОТОБРАЖЕНИЯ ЗНАЧЕНИЯ ===
        var displaySettingsHeader = new TextBlock 
        { 
            Text = "📊 Настройки отображения значения регистра", 
            FontWeight = FontWeight.Bold,
            FontSize = 13,
            Margin = new Thickness(0, 5, 0, 10)
        };
        stack.Children.Add(displaySettingsHeader);

        // Инициализируем DisplaySettings если null
        if (DisplaySettings == null)
        {
            DisplaySettings = new DisplaySettings();
        }

        // Чекбокс "Показывать значение"
        var showValueCheckBox = new CheckBox
        {
            Content = "Показывать значение регистра",
            IsChecked = DisplaySettings.ShowValue
        };
        _showValueCheckBox = showValueCheckBox;
        stack.Children.Add(showValueCheckBox);

        // Панель настроек (видна только если ShowValue = true)
        var displaySettingsPanel = new StackPanel 
        { 
            Spacing = 10,
            Margin = new Thickness(20, 10, 0, 0),
            IsVisible = DisplaySettings.ShowValue
        };

        // Привязываем видимость панели к чекбоксу
        showValueCheckBox.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(CheckBox.IsChecked))
            {
                displaySettingsPanel.IsVisible = showValueCheckBox.IsChecked ?? false;
            }
        };

        // Адрес регистра
        var registerAddressPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        registerAddressPanel.Children.Add(new TextBlock { Text = "Адрес регистра:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Width = 120 });
        var registerAddressInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 65535,
            Value = DisplaySettings.RegisterAddress,
            Width = 100,
            Increment = 1
        };
        _registerAddressInput = registerAddressInput;
        registerAddressPanel.Children.Add(registerAddressInput);
        displaySettingsPanel.Children.Add(registerAddressPanel);

        // Тип регистра
        var registerTypePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        registerTypePanel.Children.Add(new TextBlock { Text = "Тип регистра:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Width = 120 });
        var registerTypeCombo = new ComboBox { Width = 150 };
        registerTypeCombo.Items.Add(new ComboBoxItem { Content = "Holding Register", Tag = RegisterType.Holding });
        registerTypeCombo.Items.Add(new ComboBoxItem { Content = "Input Register", Tag = RegisterType.Input });
        registerTypeCombo.Items.Add(new ComboBoxItem { Content = "Coil", Tag = RegisterType.Coils });
        registerTypeCombo.SelectedIndex = DisplaySettings.RegisterType switch
        {
            RegisterType.Holding => 0,
            RegisterType.Input => 1,
            RegisterType.Coils => 2,
            _ => 0
        };
        _registerTypeCombo = registerTypeCombo;
        registerTypePanel.Children.Add(registerTypeCombo);
        displaySettingsPanel.Children.Add(registerTypePanel);

        // Тип данных
        var dataTypePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        dataTypePanel.Children.Add(new TextBlock { Text = "Тип данных:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Width = 120 });
        var dataTypeCombo = new ComboBox { Width = 150 };
        dataTypeCombo.Items.Add(new ComboBoxItem { Content = "UInt16", Tag = DataType.UInt16 });
        dataTypeCombo.Items.Add(new ComboBoxItem { Content = "Int16", Tag = DataType.Int16 });
        dataTypeCombo.Items.Add(new ComboBoxItem { Content = "UInt32", Tag = DataType.UInt32 });
        dataTypeCombo.Items.Add(new ComboBoxItem { Content = "Int32", Tag = DataType.Int32 });
        dataTypeCombo.Items.Add(new ComboBoxItem { Content = "Float32", Tag = DataType.Float32 });
        dataTypeCombo.Items.Add(new ComboBoxItem { Content = "Bool", Tag = DataType.Bool });
        dataTypeCombo.SelectedIndex = DisplaySettings.DataType switch
        {
            DataType.UInt16 => 0,
            DataType.Int16 => 1,
            DataType.UInt32 => 2,
            DataType.Int32 => 3,
            DataType.Float32 => 4,
            DataType.Bool => 5,
            _ => 0
        };
        _dataTypeCombo = dataTypeCombo;
        dataTypePanel.Children.Add(dataTypeCombo);
        displaySettingsPanel.Children.Add(dataTypePanel);

        // Масштаб и смещение
        var scaleOffsetPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        scaleOffsetPanel.Children.Add(new TextBlock { Text = "Масштаб:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var scaleInput = new NumericUpDown
        {
            Minimum = -1000,
            Maximum = 1000,
            Value = (decimal)DisplaySettings.Scale,
            Width = 80,
            Increment = 0.1m,
            FormatString = "0.###"
        };
        _scaleInput = scaleInput;
        scaleOffsetPanel.Children.Add(scaleInput);
        scaleOffsetPanel.Children.Add(new TextBlock { Text = "Смещение:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
        var offsetInput = new NumericUpDown
        {
            Minimum = -10000,
            Maximum = 10000,
            Value = (decimal)DisplaySettings.Offset,
            Width = 80,
            Increment = 1,
            FormatString = "0.###"
        };
        _offsetInput = offsetInput;
        scaleOffsetPanel.Children.Add(offsetInput);
        displaySettingsPanel.Children.Add(scaleOffsetPanel);

        // Диапазон значений
        var rangePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        rangePanel.Children.Add(new TextBlock { Text = "Мин:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var minValueInput = new NumericUpDown
        {
            Minimum = -100000,
            Maximum = 100000,
            Value = DisplaySettings.MinValue.HasValue ? (decimal)DisplaySettings.MinValue.Value : 0,
            Width = 80,
            Increment = 10,
            FormatString = "0.##"
        };
        _minValueInput = minValueInput;
        rangePanel.Children.Add(minValueInput);
        rangePanel.Children.Add(new TextBlock { Text = "Макс:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var maxValueInput = new NumericUpDown
        {
            Minimum = -100000,
            Maximum = 100000,
            Value = DisplaySettings.MaxValue.HasValue ? (decimal)DisplaySettings.MaxValue.Value : 100,
            Width = 80,
            Increment = 10,
            FormatString = "0.##"
        };
        _maxValueInput = maxValueInput;
        rangePanel.Children.Add(maxValueInput);
        displaySettingsPanel.Children.Add(rangePanel);

        // Единицы измерения
        var unitPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        unitPanel.Children.Add(new TextBlock { Text = "Единицы:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Width = 120 });
        var unitInput = new TextBox
        {
            Text = DisplaySettings.Unit,
            Width = 100,
            Watermark = "°C, %, bar..."
        };
        _unitInput = unitInput;
        unitPanel.Children.Add(unitInput);
        var showUnitCheckBox = new CheckBox
        {
            Content = "Показывать",
            IsChecked = DisplaySettings.ShowUnit,
            Margin = new Thickness(10, 0, 0, 0)
        };
        _showUnitCheckBox = showUnitCheckBox;
        unitPanel.Children.Add(showUnitCheckBox);
        displaySettingsPanel.Children.Add(unitPanel);

        // Знаки после запятой
        var decimalPlacesPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        decimalPlacesPanel.Children.Add(new TextBlock { Text = "Знаков после запятой:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Width = 150 });
        var decimalPlacesInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 5,
            Value = DisplaySettings.DecimalPlaces,
            Width = 70,
            Increment = 1
        };
        _decimalPlacesInput = decimalPlacesInput;
        decimalPlacesPanel.Children.Add(decimalPlacesInput);
        displaySettingsPanel.Children.Add(decimalPlacesPanel);

        // Цвета в зависимости от состояния
        var colorByStateCheckBox = new CheckBox
        {
            Content = "Изменять цвет в зависимости от значения",
            IsChecked = DisplaySettings.ColorByState,
            Margin = new Thickness(0, 5, 0, 5)
        };
        _colorByStateCheckBox = colorByStateCheckBox;
        displaySettingsPanel.Children.Add(colorByStateCheckBox);

        // Панель настроек цветов (видна только если ColorByState = true)
        var colorSettingsPanel = new StackPanel 
        { 
            Spacing = 8,
            Margin = new Thickness(20, 5, 0, 0),
            IsVisible = DisplaySettings.ColorByState
        };

        colorByStateCheckBox.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(CheckBox.IsChecked))
            {
                colorSettingsPanel.IsVisible = colorByStateCheckBox.IsChecked ?? false;
            }
        };

        // Настройки для Bool типа (ON/OFF)
        var boolColorsPanel = new StackPanel { Spacing = 5 };
        
        var offStatePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        offStatePanel.Children.Add(new TextBlock { Text = "OFF - Цвет:", Width = 100, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var offColorInput = new TextBox { Text = DisplaySettings.OffStateColor, Width = 80 };
        _offColorInput = offColorInput;
        offStatePanel.Children.Add(offColorInput);
        offStatePanel.Children.Add(new TextBlock { Text = "Текст:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var offTextInput = new TextBox { Text = DisplaySettings.OffStateText, Width = 80 };
        _offTextInput = offTextInput;
        offStatePanel.Children.Add(offTextInput);
        boolColorsPanel.Children.Add(offStatePanel);

        var onStatePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        onStatePanel.Children.Add(new TextBlock { Text = "ON - Цвет:", Width = 100, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var onColorInput = new TextBox { Text = DisplaySettings.OnStateColor, Width = 80 };
        _onColorInput = onColorInput;
        onStatePanel.Children.Add(onColorInput);
        onStatePanel.Children.Add(new TextBlock { Text = "Текст:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var onTextInput = new TextBox { Text = DisplaySettings.OnStateText, Width = 80 };
        _onTextInput = onTextInput;
        onStatePanel.Children.Add(onTextInput);
        boolColorsPanel.Children.Add(onStatePanel);

        var useStateTextCheckBox = new CheckBox
        {
            Content = "Использовать текст вместо значений для Bool",
            IsChecked = DisplaySettings.UseStateText
        };
        _useStateTextCheckBox = useStateTextCheckBox;
        boolColorsPanel.Children.Add(useStateTextCheckBox);

        colorSettingsPanel.Children.Add(boolColorsPanel);

        // Настройки для числовых типов (Low/Normal/High)
        var numericColorsPanel = new StackPanel { Spacing = 5, Margin = new Thickness(0, 10, 0, 0) };
        
        var lowValuePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        lowValuePanel.Children.Add(new TextBlock { Text = "Низкое - Цвет:", Width = 120, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var lowColorInput = new TextBox { Text = DisplaySettings.LowValueColor, Width = 80 };
        _lowColorInput = lowColorInput;
        lowValuePanel.Children.Add(lowColorInput);
        lowValuePanel.Children.Add(new TextBlock { Text = "Порог <", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var lowThresholdInput = new NumericUpDown
        {
            Minimum = -100000,
            Maximum = 100000,
            Value = DisplaySettings.LowThreshold.HasValue ? (decimal)DisplaySettings.LowThreshold.Value : 20,
            Width = 80,
            Increment = 10
        };
        _lowThresholdInput = lowThresholdInput;
        lowValuePanel.Children.Add(lowThresholdInput);
        numericColorsPanel.Children.Add(lowValuePanel);

        var normalValuePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        normalValuePanel.Children.Add(new TextBlock { Text = "Нормальное - Цвет:", Width = 120, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var normalColorInput = new TextBox { Text = DisplaySettings.NormalValueColor, Width = 80 };
        _normalColorInput = normalColorInput;
        normalValuePanel.Children.Add(normalColorInput);
        numericColorsPanel.Children.Add(normalValuePanel);

        var highValuePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        highValuePanel.Children.Add(new TextBlock { Text = "Высокое - Цвет:", Width = 120, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var highColorInput = new TextBox { Text = DisplaySettings.HighValueColor, Width = 80 };
        _highColorInput = highColorInput;
        highValuePanel.Children.Add(highColorInput);
        highValuePanel.Children.Add(new TextBlock { Text = "Порог >", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var highThresholdInput = new NumericUpDown
        {
            Minimum = -100000,
            Maximum = 100000,
            Value = DisplaySettings.HighThreshold.HasValue ? (decimal)DisplaySettings.HighThreshold.Value : 80,
            Width = 80,
            Increment = 10
        };
        _highThresholdInput = highThresholdInput;
        highValuePanel.Children.Add(highThresholdInput);
        numericColorsPanel.Children.Add(highValuePanel);

        colorSettingsPanel.Children.Add(numericColorsPanel);
        displaySettingsPanel.Children.Add(colorSettingsPanel);

        stack.Children.Add(displaySettingsPanel);

        // Разделитель
        stack.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 5) });

        // Кнопки управления (в сетке 2x2 для красивого расположения)
        var actionsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var copyBtn = new Button 
        { 
            Content = "📋 Копировать", 
            Padding = new Thickness(10, 8),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 5, 5)
        };
        copyBtn.Click += (s, e) =>
        {
            CopyButton();
            dialog.Close();
        };

        var pasteBtn = new Button 
        { 
            Content = "📌 Вставить", 
            Padding = new Thickness(10, 8),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(5, 0, 0, 5)
        };
        pasteBtn.Click += (s, e) =>
        {
            PasteRequested?.Invoke(this, EventArgs.Empty);
            dialog.Close();
        };

        var deleteBtn = new Button 
        { 
            Content = "🗑️ Удалить", 
            Padding = new Thickness(10, 8),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 5, 0)
        };
        deleteBtn.Click += (s, e) =>
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
            dialog.Close();
        };

        var hintText = new TextBlock
        {
            Text = "Горячие клавиши: Ctrl+C, Ctrl+V, Delete",
            FontSize = 11,
            Foreground = Brushes.Gray,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(5, 5, 0, 0)
        };

        Grid.SetColumn(copyBtn, 0);
        Grid.SetRow(copyBtn, 0);
        Grid.SetColumn(pasteBtn, 1);
        Grid.SetRow(pasteBtn, 0);
        Grid.SetColumn(deleteBtn, 0);
        Grid.SetRow(deleteBtn, 1);
        Grid.SetColumn(hintText, 1);
        Grid.SetRow(hintText, 1);

        actionsGrid.Children.Add(copyBtn);
        actionsGrid.Children.Add(pasteBtn);
        actionsGrid.Children.Add(deleteBtn);
        actionsGrid.Children.Add(hintText);
        stack.Children.Add(actionsGrid);

        // Разделитель
        stack.Children.Add(new Separator());

        if (AvailableTags == null || !AvailableTags.Any())
        {
            // Если тегов нет, показываем простой ввод адреса
            await ShowSimpleAddressDialogInStack(stack, dialog, labelInput, iconOnInput, iconOffInput, buttonTypeCombo, showLabelCheckBox);
        }
        else
        {
            // Показываем выбор тега
            ShowTagSelectionInDialog(stack, dialog, labelInput, iconOnInput, iconOffInput, widthInput, heightInput, buttonTypeCombo, showLabelCheckBox);
        }
        
        scrollViewer.Content = stack;
        dialog.Content = scrollViewer;

        if (this.VisualRoot is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
    }

    private void SaveDisplaySettingsFromUI()
    {
        if (DisplaySettings == null)
            DisplaySettings = new DisplaySettings();

        if (_showValueCheckBox != null)
            DisplaySettings.ShowValue = _showValueCheckBox.IsChecked ?? false;
        
        if (_registerAddressInput?.Value != null)
            DisplaySettings.RegisterAddress = (ushort)_registerAddressInput.Value.Value;
        
        if (_registerTypeCombo?.SelectedItem is ComboBoxItem regTypeItem && regTypeItem.Tag is RegisterType selectedRegType)
            DisplaySettings.RegisterType = selectedRegType;
        
        if (_dataTypeCombo?.SelectedItem is ComboBoxItem dataTypeItem && dataTypeItem.Tag is DataType selectedDataType)
            DisplaySettings.DataType = selectedDataType;
        
        if (_scaleInput?.Value != null)
            DisplaySettings.Scale = (double)_scaleInput.Value.Value;
        
        if (_offsetInput?.Value != null)
            DisplaySettings.Offset = (double)_offsetInput.Value.Value;
        
        if (_minValueInput != null)
            DisplaySettings.MinValue = (double?)_minValueInput.Value;
        
        if (_maxValueInput != null)
            DisplaySettings.MaxValue = (double?)_maxValueInput.Value;
        
        if (_unitInput != null)
            DisplaySettings.Unit = _unitInput.Text ?? string.Empty;
        
        if (_showUnitCheckBox != null)
            DisplaySettings.ShowUnit = _showUnitCheckBox.IsChecked ?? true;
        
        if (_decimalPlacesInput?.Value != null)
            DisplaySettings.DecimalPlaces = (int)_decimalPlacesInput.Value.Value;
        
        if (_colorByStateCheckBox != null)
            DisplaySettings.ColorByState = _colorByStateCheckBox.IsChecked ?? false;
        
        if (_offColorInput != null)
            DisplaySettings.OffStateColor = _offColorInput.Text ?? "#808080";
        
        if (_onColorInput != null)
            DisplaySettings.OnStateColor = _onColorInput.Text ?? "#00FF00";
        
        if (_offTextInput != null)
            DisplaySettings.OffStateText = _offTextInput.Text ?? "OFF";
        
        if (_onTextInput != null)
            DisplaySettings.OnStateText = _onTextInput.Text ?? "ON";
        
        if (_useStateTextCheckBox != null)
            DisplaySettings.UseStateText = _useStateTextCheckBox.IsChecked ?? false;
        
        if (_lowColorInput != null)
            DisplaySettings.LowValueColor = _lowColorInput.Text ?? "#0000FF";
        
        if (_normalColorInput != null)
            DisplaySettings.NormalValueColor = _normalColorInput.Text ?? "#00FF00";
        
        if (_highColorInput != null)
            DisplaySettings.HighValueColor = _highColorInput.Text ?? "#FF0000";
        
        if (_lowThresholdInput != null)
            DisplaySettings.LowThreshold = (double?)_lowThresholdInput.Value;
        
        if (_highThresholdInput != null)
            DisplaySettings.HighThreshold = (double?)_highThresholdInput.Value;
    }

    private void ShowTagSelectionInDialog(StackPanel stack, Window dialog, TextBox labelInput, TextBox iconOnInput, TextBox iconOffInput, NumericUpDown widthInput, NumericUpDown heightInput, ComboBox buttonTypeCombo, CheckBox showLabelCheckBox)
    {
        var label = new TextBlock 
        { 
            Text = $"Устройство: {Label}\nТекущий адрес: {CoilAddress}\nВыберите тег X или Y:",
            TextWrapping = TextWrapping.Wrap
        };

        // Фильтруем только теги X (Input) и Y (Coils)
        var bitTags = new ObservableCollection<TagDefinition>(
            AvailableTags!.Where(t => 
                (t.Register == RegisterType.Input && t.Name.StartsWith("X")) ||
                (t.Register == RegisterType.Coils && t.Name.StartsWith("Y"))
            )
        );

        Console.WriteLine($"AvailableTags count: {AvailableTags?.Count ?? 0}");
        Console.WriteLine($"X/Y tags count: {bitTags.Count}");

        if (bitTags.Count == 0)
        {
            var warningLabel = new TextBlock 
            { 
                Text = $"Нет X/Y тегов!\nВсего тегов: {AvailableTags?.Count ?? 0}\nФильтруется по тегам X (Input) и Y (Coils)",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Orange,
                Margin = new Thickness(0, 10, 0, 10)
            };
            stack.Children.Add(warningLabel);
        }

        var combo = new ComboBox
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };

        // Добавляем элементы с отображаемым текстом
        foreach (var tag in bitTags)
        {
            combo.Items.Add(new ComboBoxItem 
            { 
                Content = $"{tag.Name} (адрес: {tag.Address})",
                Tag = tag
            });
        }
        combo.SelectedIndex = bitTags.ToList().FindIndex(t => t.Address == CoilAddress);

        var buttons = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal, 
            Spacing = 10, 
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right 
        };
        
        var okButton = new Button { Content = "OK", Width = 80 };
        okButton.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(labelInput.Text))
            {
                Label = labelInput.Text;
            }
            // Сохраняем иконки ON/OFF
            IconPathOn = !string.IsNullOrWhiteSpace(iconOnInput.Text) ? iconOnInput.Text : null;
            IconPathOff = !string.IsNullOrWhiteSpace(iconOffInput.Text) ? iconOffInput.Text : null;
            
            // Сохраняем размеры
            if (widthInput.Value.HasValue)
                ButtonWidth = (double)widthInput.Value.Value;
            if (heightInput.Value.HasValue)
                ButtonHeight = (double)heightInput.Value.Value;
            
            // Сохраняем тип кнопки
            if (buttonTypeCombo.SelectedItem is ComboBoxItem typeItem && typeItem.Tag is CoilButtonType selectedType)
            {
                ButtonType = selectedType;
            }
            
            // Сохраняем видимость надписи
            ShowLabel = showLabelCheckBox.IsChecked ?? true;
            
            // Сохраняем настройки отображения регистра
            SaveDisplaySettingsFromUI();
            
            if (combo.SelectedItem is ComboBoxItem item && item.Tag is TagDefinition selectedTag)
            {
                SelectedTag = selectedTag;
                CoilAddress = selectedTag.Address;
            }
            dialog.Close();
        };
        
        var cancelButton = new Button { Content = "Отмена", Width = 80 };
        cancelButton.Click += (s, e) => dialog.Close();

        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);
        
        stack.Children.Add(label);
        stack.Children.Add(combo);
        stack.Children.Add(buttons);
    }

    private System.Threading.Tasks.Task ShowSimpleAddressDialogInStack(StackPanel stack, Window dialog, TextBox labelInput, TextBox iconOnInput, TextBox iconOffInput, ComboBox buttonTypeCombo, CheckBox showLabelCheckBox)
    {
        var label = new TextBlock 
        { 
            Text = $"Текущий адрес: {CoilAddress}\nВведите новый адрес (0-65535):",
            TextWrapping = TextWrapping.Wrap
        };
        
        var input = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 65535,
            Value = CoilAddress,
            Increment = 1,
            FormatString = "0"
        };

        var buttons = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal, 
            Spacing = 10, 
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right 
        };
        
        var okButton = new Button { Content = "OK", Width = 80 };
        okButton.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(labelInput.Text))
            {
                Label = labelInput.Text;
            }
            CoilAddress = (ushort)input.Value;
            
            // Сохранить пути к иконкам
            IconPathOn = !string.IsNullOrWhiteSpace(iconOnInput.Text) ? iconOnInput.Text : null;
            IconPathOff = !string.IsNullOrWhiteSpace(iconOffInput.Text) ? iconOffInput.Text : null;
            
            // Сохраняем тип кнопки
            if (buttonTypeCombo.SelectedItem is ComboBoxItem typeItem && typeItem.Tag is CoilButtonType selectedType)
            {
                ButtonType = selectedType;
            }
            
            // Сохраняем видимость надписи
            ShowLabel = showLabelCheckBox.IsChecked ?? true;
            
            // Сохраняем настройки отображения регистра
            SaveDisplaySettingsFromUI();
            
            dialog.Close();
        };
        
        var cancelButton = new Button { Content = "Отмена", Width = 80 };
        cancelButton.Click += (s, e) => dialog.Close();

        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);
        
        stack.Children.Add(label);
        stack.Children.Add(input);
        stack.Children.Add(buttons);

        return System.Threading.Tasks.Task.CompletedTask;
    }

    private async System.Threading.Tasks.Task ShowSimpleAddressDialog()
    {
        var dialog = new Window
        {
            Title = "Изменить адрес Coil",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            MaxWidth = 450,
            MaxHeight = 300
        };

        var stack = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
        
        var label = new TextBlock 
        { 
            Text = $"Устройство: {Label}\nТекущий адрес: {CoilAddress}\nВведите новый адрес (0-65535):",
            TextWrapping = TextWrapping.Wrap
        };
        
        var input = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 65535,
            Value = CoilAddress,
            Increment = 1,
            FormatString = "0"
        };

        var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        
        var okButton = new Button { Content = "OK", Width = 80 };
        okButton.Click += (s, e) =>
        {
            CoilAddress = (ushort)input.Value;
            dialog.Close();
        };
        
        var cancelButton = new Button { Content = "Отмена", Width = 80 };
        cancelButton.Click += (s, e) => dialog.Close();

        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);
        
        stack.Children.Add(label);
        stack.Children.Add(input);
        stack.Children.Add(buttons);
        
        dialog.Content = stack;

        if (this.VisualRoot is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
    }
}

public class ActiveStateBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? Brushes.Green : Brushes.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class ActiveStateTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "ВКЛЮЧЕНО" : "ВЫКЛЮЧЕНО";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class ImageButtonStateTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "ВЫКЛ" : "ВКЛ";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
