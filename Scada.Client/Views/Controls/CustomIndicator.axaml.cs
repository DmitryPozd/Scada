using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Scada.Client.Models;

namespace Scada.Client.Views.Controls;

public partial class CustomIndicator : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<CustomIndicator, string>(nameof(Label), defaultValue: "Индикатор");

    public static readonly StyledProperty<string> BackgroundImagePathProperty =
        AvaloniaProperty.Register<CustomIndicator, string>(nameof(BackgroundImagePath), defaultValue: string.Empty);

    public static readonly StyledProperty<string> BackgroundColorProperty =
        AvaloniaProperty.Register<CustomIndicator, string>(nameof(BackgroundColor), defaultValue: "#2563EB");

    public static readonly StyledProperty<double> IndicatorWidthProperty =
        AvaloniaProperty.Register<CustomIndicator, double>(nameof(IndicatorWidth), defaultValue: 150);

    public static readonly StyledProperty<double> IndicatorHeightProperty =
        AvaloniaProperty.Register<CustomIndicator, double>(nameof(IndicatorHeight), defaultValue: 150);

    public static readonly StyledProperty<bool> ShowLabelProperty =
        AvaloniaProperty.Register<CustomIndicator, bool>(nameof(ShowLabel), defaultValue: true);

    public static readonly StyledProperty<ushort> RegisterAddressProperty =
        AvaloniaProperty.Register<CustomIndicator, ushort>(nameof(RegisterAddress));

    public static readonly StyledProperty<string> DisplayValueProperty =
        AvaloniaProperty.Register<CustomIndicator, string>(nameof(DisplayValue), defaultValue: "0");

    public static readonly StyledProperty<string> UnitProperty =
        AvaloniaProperty.Register<CustomIndicator, string>(nameof(Unit), defaultValue: "");

    public static readonly StyledProperty<ObservableCollection<TagDefinition>?> AvailableTagsProperty =
        AvaloniaProperty.Register<CustomIndicator, ObservableCollection<TagDefinition>?>(nameof(AvailableTags));

    public static readonly StyledProperty<TagDefinition?> SelectedTagProperty =
        AvaloniaProperty.Register<CustomIndicator, TagDefinition?>(nameof(SelectedTag));

    public string Label 
    { 
        get => GetValue(LabelProperty); 
        set => SetValue(LabelProperty, value); 
    }

    public string BackgroundImagePath 
    { 
        get => GetValue(BackgroundImagePathProperty); 
        set => SetValue(BackgroundImagePathProperty, value); 
    }

    public string BackgroundColor 
    { 
        get => GetValue(BackgroundColorProperty); 
        set => SetValue(BackgroundColorProperty, value); 
    }

    public double IndicatorWidth 
    { 
        get => GetValue(IndicatorWidthProperty); 
        set => SetValue(IndicatorWidthProperty, value); 
    }

    public double IndicatorHeight 
    { 
        get => GetValue(IndicatorHeightProperty); 
        set => SetValue(IndicatorHeightProperty, value); 
    }

    public bool ShowLabel 
    { 
        get => GetValue(ShowLabelProperty); 
        set => SetValue(ShowLabelProperty, value); 
    }

    public ushort RegisterAddress
    {
        get => GetValue(RegisterAddressProperty);
        set => SetValue(RegisterAddressProperty, value);
    }

    public string DisplayValue
    {
        get => GetValue(DisplayValueProperty);
        set => SetValue(DisplayValueProperty, value);
    }

    public string Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
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

    public event EventHandler? TagChanged;

    // События для MainWindow
    public static readonly RoutedEvent<RoutedEventArgs> ImageChangedEvent =
        RoutedEvent.Register<CustomIndicator, RoutedEventArgs>(nameof(ImageChanged), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> LabelChangedEvent =
        RoutedEvent.Register<CustomIndicator, RoutedEventArgs>(nameof(LabelChanged), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> ColorChangedEvent =
        RoutedEvent.Register<CustomIndicator, RoutedEventArgs>(nameof(ColorChanged), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> SizeChangedCustomEvent =
        RoutedEvent.Register<CustomIndicator, RoutedEventArgs>(nameof(SizeChangedCustom), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> DeleteRequestedEvent =
        RoutedEvent.Register<CustomIndicator, RoutedEventArgs>(nameof(DeleteRequested), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> ImageChanged
    {
        add => AddHandler(ImageChangedEvent, value);
        remove => RemoveHandler(ImageChangedEvent, value);
    }

    public event EventHandler<RoutedEventArgs> LabelChanged
    {
        add => AddHandler(LabelChangedEvent, value);
        remove => RemoveHandler(LabelChangedEvent, value);
    }

    public event EventHandler<RoutedEventArgs> ColorChanged
    {
        add => AddHandler(ColorChangedEvent, value);
        remove => RemoveHandler(ColorChangedEvent, value);
    }

    public event EventHandler<RoutedEventArgs> SizeChangedCustom
    {
        add => AddHandler(SizeChangedCustomEvent, value);
        remove => RemoveHandler(SizeChangedCustomEvent, value);
    }

    public event EventHandler<RoutedEventArgs> DeleteRequested
    {
        add => AddHandler(DeleteRequestedEvent, value);
        remove => RemoveHandler(DeleteRequestedEvent, value);
    }

    private Border? _backgroundBorder;
    private Image? _backgroundImage;
    private Border? _colorBackground;
    private TextBlock? _labelText;

    // Поля для изменения размера
    private bool _isResizing = false;
    private Point _resizeStartPoint;
    private double _resizeStartWidth;
    private double _resizeStartHeight;
    private string _resizeMode = "";

    public CustomIndicator()
    {
        InitializeComponent();
        
        PropertyChanged += OnPropertyChanged;
        
        SelectedTagProperty.Changed.AddClassHandler<CustomIndicator>((control, args) =>
        {
            if (args.NewValue is TagDefinition tag)
            {
                control.RegisterAddress = tag.Address;
                control.TagChanged?.Invoke(control, EventArgs.Empty);
            }
        });
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        _backgroundBorder = this.FindControl<Border>("BackgroundBorder");
        _backgroundImage = this.FindControl<Image>("BackgroundImage");
        _colorBackground = this.FindControl<Border>("ColorBackground");
        _labelText = this.FindControl<TextBlock>("LabelText");
        
        UpdateVisual();
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BackgroundImagePathProperty)
        {
            UpdateVisual();
        }
        else if (e.Property == BackgroundColorProperty)
        {
            UpdateVisual();
        }
        else if (e.Property == ShowLabelProperty)
        {
            if (_labelText != null)
            {
                _labelText.IsVisible = ShowLabel;
            }
        }
    }

    private void UpdateVisual()
    {
        if (_backgroundImage == null || _colorBackground == null) return;

        if (!string.IsNullOrEmpty(BackgroundImagePath) && File.Exists(BackgroundImagePath))
        {
            try
            {
                _backgroundImage.Source = new Bitmap(BackgroundImagePath);
                _backgroundImage.IsVisible = true;
                _colorBackground.IsVisible = false;
            }
            catch
            {
                // Если не удалось загрузить картинку, показываем цвет
                _backgroundImage.IsVisible = false;
                _colorBackground.IsVisible = true;
                UpdateBackgroundColor();
            }
        }
        else
        {
            _backgroundImage.IsVisible = false;
            _colorBackground.IsVisible = true;
            UpdateBackgroundColor();
        }
    }

    private void UpdateBackgroundColor()
    {
        if (_colorBackground == null) return;

        try
        {
            _colorBackground.Background = Brush.Parse(BackgroundColor);
        }
        catch
        {
            _colorBackground.Background = Brush.Parse("#2563EB");
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // Правый клик для настроек (только если не было изменения размера)
        if (e.InitialPressMouseButton == MouseButton.Right && !_isResizing)
        {
            ShowSettingsDialog();
            e.Handled = true;
        }
    }

    private async void ShowSettingsDialog()
    {
        var dialog = new Window
        {
            Title = "Настройки индикатора",
            Width = 600,
            Height = 650,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var stack = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

        // Переменные для хранения ссылок на элементы управления
        ComboBox? tagCombo = null;
        NumericUpDown? addressInput = null;
        TextBox? unitInput = null;

        // Надпись
        stack.Children.Add(new TextBlock { Text = "Надпись:", FontWeight = FontWeight.SemiBold });
        var labelInput = new TextBox 
        { 
            Text = Label,
            Watermark = "Введите текст надписи"
        };
        stack.Children.Add(labelInput);

        // Показывать надпись
        var showLabelCheck = new CheckBox 
        { 
            Content = "Показывать надпись",
            IsChecked = ShowLabel
        };
        stack.Children.Add(showLabelCheck);

        // Тэг регистра
        stack.Children.Add(new TextBlock { Text = "Регистр для отображения:", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 10, 0, 0) });
        
        var tagPanel = new StackPanel { Spacing = 10 };
        
        if (AvailableTags != null && AvailableTags.Count > 0)
        {
            tagCombo = new ComboBox
            {
                ItemsSource = AvailableTags,
                SelectedItem = SelectedTag,
                Width = 400,
                PlaceholderText = "Выберите тэг..."
            };
            
            tagCombo.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<TagDefinition>((tag, _) =>
            {
                if (tag == null) return new TextBlock { Text = "Не выбрано" };
                return new TextBlock { Text = $"{tag.Name} (адрес: {tag.Address}, тип: {tag.Type})" };
            });
            
            tagPanel.Children.Add(tagCombo);
            
            var tagOrPanel = new StackPanel 
            { 
                Orientation = Avalonia.Layout.Orientation.Horizontal, 
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5)
            };
            tagOrPanel.Children.Add(new TextBlock 
            { 
                Text = "или", 
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Brushes.Gray
            });
            tagPanel.Children.Add(tagOrPanel);
            
            var manualPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
            manualPanel.Children.Add(new TextBlock { Text = "Адрес:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            addressInput = new NumericUpDown
            {
                Value = RegisterAddress,
                Minimum = 0,
                Maximum = 65535,
                Width = 120,
                FormatString = "0"
            };
            manualPanel.Children.Add(addressInput);
            
            manualPanel.Children.Add(new TextBlock { Text = "Единица измерения:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) });
            unitInput = new TextBox
            {
                Text = Unit,
                Width = 100,
                Watermark = "°C, %, кПа"
            };
            manualPanel.Children.Add(unitInput);
            
            tagPanel.Children.Add(manualPanel);
            
            // Синхронизация между ComboBox и NumericUpDown
            tagCombo.SelectionChanged += (s, e) =>
            {
                if (tagCombo.SelectedItem is TagDefinition tag)
                {
                    addressInput.Value = tag.Address;
                }
            };
            
            addressInput.ValueChanged += (s, e) =>
            {
                if (addressInput.Value.HasValue)
                {
                    var matchingTag = AvailableTags.FirstOrDefault(t => t.Address == (ushort)addressInput.Value.Value);
                    if (matchingTag != null)
                    {
                        tagCombo.SelectedItem = matchingTag;
                    }
                    else
                    {
                        tagCombo.SelectedItem = null;
                    }
                }
            };
            
            stack.Children.Add(tagPanel);
        }
        else
        {
            var manualPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
            manualPanel.Children.Add(new TextBlock { Text = "Адрес регистра:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            addressInput = new NumericUpDown
            {
                Value = RegisterAddress,
                Minimum = 0,
                Maximum = 65535,
                Width = 120,
                FormatString = "0"
            };
            manualPanel.Children.Add(addressInput);
            
            manualPanel.Children.Add(new TextBlock { Text = "Единица измерения:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) });
            unitInput = new TextBox
            {
                Text = Unit,
                Width = 100,
                Watermark = "°C, %, кПа"
            };
            manualPanel.Children.Add(unitInput);
            
            stack.Children.Add(manualPanel);
        }

        // Размеры
        stack.Children.Add(new TextBlock { Text = "Размеры:", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 10, 0, 0) });
        
        var sizePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        sizePanel.Children.Add(new TextBlock { Text = "Ширина:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        var widthInput = new NumericUpDown 
        { 
            Value = (decimal)IndicatorWidth,
            Minimum = 50,
            Maximum = 1000,
            Width = 150,
            FormatString = "0"
        };
        sizePanel.Children.Add(widthInput);
        
        sizePanel.Children.Add(new TextBlock { Text = "Высота:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) });
        var heightInput = new NumericUpDown 
        { 
            Value = (decimal)IndicatorHeight,
            Minimum = 50,
            Maximum = 1000,
            Width = 150,
            FormatString = "0"
        };
        sizePanel.Children.Add(heightInput);
        stack.Children.Add(sizePanel);

        // Фоновое изображение
        stack.Children.Add(new TextBlock { Text = "Фоновое изображение:", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 10, 0, 0) });
        
        var imagePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        var imagePathText = new TextBox 
        { 
            Text = BackgroundImagePath,
            Width = 350,
            IsReadOnly = true,
            Watermark = "Не выбрано"
        };
        imagePanel.Children.Add(imagePathText);
        
        var browseButton = new Button { Content = "Обзор...", Width = 100 };
        browseButton.Click += async (s, e) =>
        {
            if (dialog.StorageProvider == null) return;

            var files = await dialog.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите изображение",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Изображения")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" }
                    }
                }
            });

            if (files.Count > 0)
            {
                imagePathText.Text = files[0].Path.LocalPath;
            }
        };
        imagePanel.Children.Add(browseButton);
        
        var clearImageButton = new Button { Content = "Очистить", Width = 100 };
        clearImageButton.Click += (s, e) => imagePathText.Text = string.Empty;
        imagePanel.Children.Add(clearImageButton);
        
        stack.Children.Add(imagePanel);

        // Цвет фона (если нет картинки)
        stack.Children.Add(new TextBlock { Text = "Цвет фона (если нет картинки):", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 10, 0, 0) });
        
        var colorPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        var colorInput = new TextBox 
        { 
            Text = BackgroundColor,
            Width = 150,
            Watermark = "#2563EB"
        };
        colorPanel.Children.Add(colorInput);
        
        var colorPreview = new Border 
        { 
            Width = 50, 
            Height = 30, 
            CornerRadius = new CornerRadius(4),
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1)
        };
        try
        {
            colorPreview.Background = Brush.Parse(BackgroundColor);
        }
        catch
        {
            colorPreview.Background = Brush.Parse("#2563EB");
        }
        colorPanel.Children.Add(colorPreview);
        
        colorInput.TextChanged += (s, e) =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(colorInput.Text))
                {
                    colorPreview.Background = Brush.Parse(colorInput.Text);
                }
            }
            catch { /* Игнорируем некорректные цвета */ }
        };
        
        stack.Children.Add(colorPanel);
        
        stack.Children.Add(new TextBlock 
        { 
            Text = "Примеры: #2563EB, #FF5733, Red, Blue, Green",
            FontSize = 11,
            Foreground = Brushes.Gray
        });

        // Кнопки
        var buttonPanel = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal, 
            Spacing = 10, 
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };
        
        var deleteButton = new Button 
        { 
            Content = "🗑 Удалить", 
            Width = 100,
            Background = Brush.Parse("#DC2626")
        };
        deleteButton.Click += (s, e) =>
        {
            RaiseEvent(new RoutedEventArgs(DeleteRequestedEvent));
            dialog.Close();
        };
        buttonPanel.Children.Add(deleteButton);

        var okButton = new Button { Content = "OK", Width = 80 };
        okButton.Click += (s, e) =>
        {
            bool changed = false;

            if (!string.IsNullOrWhiteSpace(labelInput.Text) && labelInput.Text != Label)
            {
                Label = labelInput.Text;
                changed = true;
                RaiseEvent(new RoutedEventArgs(LabelChangedEvent));
            }

            if (showLabelCheck.IsChecked != ShowLabel)
            {
                ShowLabel = showLabelCheck.IsChecked ?? true;
                changed = true;
            }

            // Сохранение тэга и адреса регистра
            if (tagCombo != null && tagCombo.SelectedItem is TagDefinition selectedTag)
            {
                if (SelectedTag != selectedTag)
                {
                    SelectedTag = selectedTag;
                    RegisterAddress = selectedTag.Address;
                    changed = true;
                }
            }
            else if (addressInput != null && addressInput.Value.HasValue)
            {
                var newAddress = (ushort)addressInput.Value.Value;
                if (RegisterAddress != newAddress)
                {
                    RegisterAddress = newAddress;
                    // Если адрес изменился вручную, сбрасываем выбранный тэг
                    if (tagCombo != null && tagCombo.SelectedItem != null)
                    {
                        var matchingTag = AvailableTags?.FirstOrDefault(t => t.Address == newAddress);
                        SelectedTag = matchingTag;
                    }
                    changed = true;
                }
            }

            if (unitInput != null && !string.IsNullOrWhiteSpace(unitInput.Text) && unitInput.Text != Unit)
            {
                Unit = unitInput.Text;
                changed = true;
            }

            if (widthInput.Value.HasValue && (double)widthInput.Value.Value != IndicatorWidth)
            {
                IndicatorWidth = (double)widthInput.Value.Value;
                changed = true;
                RaiseEvent(new RoutedEventArgs(SizeChangedCustomEvent));
            }

            if (heightInput.Value.HasValue && (double)heightInput.Value.Value != IndicatorHeight)
            {
                IndicatorHeight = (double)heightInput.Value.Value;
                changed = true;
                RaiseEvent(new RoutedEventArgs(SizeChangedCustomEvent));
            }

            if (imagePathText.Text != BackgroundImagePath)
            {
                BackgroundImagePath = imagePathText.Text ?? string.Empty;
                changed = true;
                RaiseEvent(new RoutedEventArgs(ImageChangedEvent));
            }

            if (!string.IsNullOrWhiteSpace(colorInput.Text) && colorInput.Text != BackgroundColor)
            {
                try
                {
                    // Проверяем корректность цвета
                    Brush.Parse(colorInput.Text);
                    BackgroundColor = colorInput.Text;
                    changed = true;
                    RaiseEvent(new RoutedEventArgs(ColorChangedEvent));
                }
                catch { /* Игнорируем некорректные цвета */ }
            }

            dialog.Close(changed);
        };
        
        var cancelButton = new Button { Content = "Отмена", Width = 80 };
        cancelButton.Click += (s, e) => dialog.Close(false);

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        
        stack.Children.Add(buttonPanel);
        
        scrollViewer.Content = stack;
        dialog.Content = scrollViewer;

        if (this.VisualRoot is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
    }

    private void OnResizeGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border grip)
        {
            _isResizing = true;
            _resizeStartPoint = e.GetPosition(this);
            _resizeStartWidth = IndicatorWidth;
            _resizeStartHeight = IndicatorHeight;
            
            // Определяем режим изменения размера
            if (grip.Name == "ResizeGripBottomRight")
                _resizeMode = "bottomright";
            else if (grip.Name == "ResizeGripRight")
                _resizeMode = "right";
            else if (grip.Name == "ResizeGripBottom")
                _resizeMode = "bottom";
            
            e.Pointer.Capture(grip);
            e.Handled = true;
        }
    }

    private void OnResizeGripMoved(object? sender, PointerEventArgs e)
    {
        if (_isResizing && sender is Border grip)
        {
            var currentPoint = e.GetPosition(this);
            var deltaX = currentPoint.X - _resizeStartPoint.X;
            var deltaY = currentPoint.Y - _resizeStartPoint.Y;

            if (_resizeMode == "bottomright")
            {
                // Изменение по обеим осям
                var newWidth = Math.Max(50, Math.Min(1000, _resizeStartWidth + deltaX));
                var newHeight = Math.Max(50, Math.Min(1000, _resizeStartHeight + deltaY));
                IndicatorWidth = newWidth;
                IndicatorHeight = newHeight;
            }
            else if (_resizeMode == "right")
            {
                // Изменение только по ширине
                var newWidth = Math.Max(50, Math.Min(1000, _resizeStartWidth + deltaX));
                IndicatorWidth = newWidth;
            }
            else if (_resizeMode == "bottom")
            {
                // Изменение только по высоте
                var newHeight = Math.Max(50, Math.Min(1000, _resizeStartHeight + deltaY));
                IndicatorHeight = newHeight;
            }

            e.Handled = true;
        }
    }

    private void OnResizeGripReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isResizing)
        {
            _isResizing = false;
            if (sender is Border grip)
            {
                e.Pointer.Capture(null);
            }
            // Уведомляем об изменении для сохранения настроек
            RaiseEvent(new RoutedEventArgs(SizeChangedCustomEvent));
            TagChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isResizing = false;
    }
}
