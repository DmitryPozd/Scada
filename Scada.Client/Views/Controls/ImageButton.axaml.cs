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

public enum ImageButtonType
{
    Motor,
    Valve,
    Fan,
    Heater,
    Light
}

public partial class ImageButton : UserControl
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<ImageButton, bool>(nameof(IsActive));

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ImageButton, string>(nameof(Label), defaultValue: "Устройство");

    public static readonly StyledProperty<ImageButtonType> ImageTypeProperty =
        AvaloniaProperty.Register<ImageButton, ImageButtonType>(nameof(ImageType), defaultValue: ImageButtonType.Motor);

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

    public event EventHandler<CoilButtonInfo>? CopyRequested;
    public event EventHandler? PasteRequested;
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

    public ImageButtonType ImageType
    {
        get => GetValue(ImageTypeProperty);
        set => SetValue(ImageTypeProperty, value);
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
            ImageType = ImageType.ToString(),
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
            Width = 500,
            Height = 580,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
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

        // Разделитель
        stack.Children.Add(new Separator { Margin = new Thickness(0, 5, 0, 5) });

        // Кнопки копировать/вставить
        var copyPastePanel = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal, 
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var copyBtn = new Button { Content = "📋 Копировать (Ctrl+C)", Padding = new Thickness(10, 5) };
        copyBtn.Click += (s, e) =>
        {
            CopyButton();
            dialog.Close();
        };

        var pasteBtn = new Button { Content = "📌 Вставить (Ctrl+V)", Padding = new Thickness(10, 5) };
        pasteBtn.Click += (s, e) =>
        {
            PasteRequested?.Invoke(this, EventArgs.Empty);
            dialog.Close();
        };

        copyPastePanel.Children.Add(copyBtn);
        copyPastePanel.Children.Add(pasteBtn);
        stack.Children.Add(copyPastePanel);

        // Разделитель
        stack.Children.Add(new Separator());

        if (AvailableTags == null || !AvailableTags.Any())
        {
            // Если тегов нет, показываем простой ввод адреса
            await ShowSimpleAddressDialogInStack(stack, dialog, labelInput, iconOnInput, iconOffInput);
        }
        else
        {
            // Показываем выбор тега
            ShowTagSelectionInDialog(stack, dialog, labelInput, iconOnInput, iconOffInput);
        }
        
        dialog.Content = stack;

        if (this.VisualRoot is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
    }

    private void ShowTagSelectionInDialog(StackPanel stack, Window dialog, TextBox labelInput, TextBox iconOnInput, TextBox iconOffInput)
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

    private System.Threading.Tasks.Task ShowSimpleAddressDialogInStack(StackPanel stack, Window dialog, TextBox labelInput, TextBox iconOnInput, TextBox iconOffInput)
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
            Width = 350,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
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

public class ImageTypeToShapeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ImageButtonType type) return null;

        return type switch
        {
            ImageButtonType.Motor => CreateMotorShape(),
            ImageButtonType.Valve => CreateValveShape(),
            ImageButtonType.Fan => CreateFanShape(),
            ImageButtonType.Heater => CreateHeaterShape(),
            ImageButtonType.Light => CreateLightShape(),
            _ => CreateMotorShape()
        };
    }

    private static Canvas CreateMotorShape()
    {
        var canvas = new Canvas { Width = 100, Height = 80 };
        
        // Корпус мотора
        var body = new Rectangle
        {
            Width = 60,
            Height = 50,
            Fill = Brushes.SteelBlue,
            Stroke = Brushes.DarkSlateGray,
            StrokeThickness = 2,
            RadiusX = 5,
            RadiusY = 5
        };
        Canvas.SetLeft(body, 20);
        Canvas.SetTop(body, 15);
        
        // Вал
        var shaft = new Rectangle
        {
            Width = 30,
            Height = 8,
            Fill = Brushes.DimGray,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(shaft, 70);
        Canvas.SetTop(shaft, 36);
        
        // Буква M
        var label = new TextBlock
        {
            Text = "M",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        };
        Canvas.SetLeft(label, 40);
        Canvas.SetTop(label, 25);
        
        canvas.Children.Add(body);
        canvas.Children.Add(shaft);
        canvas.Children.Add(label);
        
        return canvas;
    }

    private static Canvas CreateValveShape()
    {
        var canvas = new Canvas { Width = 100, Height = 80 };
        
        // Труба слева
        var pipeLeft = new Rectangle
        {
            Width = 30,
            Height = 12,
            Fill = Brushes.Gray,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(pipeLeft, 10);
        Canvas.SetTop(pipeLeft, 34);
        
        // Корпус клапана
        var body = new Polygon
        {
            Points = new Points { new Point(40, 20), new Point(60, 20), new Point(70, 40), new Point(60, 60), new Point(40, 60), new Point(30, 40) },
            Fill = Brushes.DarkOrange,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        
        // Труба справа
        var pipeRight = new Rectangle
        {
            Width = 30,
            Height = 12,
            Fill = Brushes.Gray,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(pipeRight, 60);
        Canvas.SetTop(pipeRight, 34);
        
        canvas.Children.Add(pipeLeft);
        canvas.Children.Add(body);
        canvas.Children.Add(pipeRight);
        
        return canvas;
    }

    private static Canvas CreateFanShape()
    {
        var canvas = new Canvas { Width = 100, Height = 80 };
        
        // Корпус
        var housing = new Ellipse
        {
            Width = 60,
            Height = 60,
            Fill = Brushes.LightSteelBlue,
            Stroke = Brushes.DarkSlateGray,
            StrokeThickness = 2
        };
        Canvas.SetLeft(housing, 20);
        Canvas.SetTop(housing, 10);
        
        // Центр
        var center = new Ellipse
        {
            Width = 15,
            Height = 15,
            Fill = Brushes.DarkGray
        };
        Canvas.SetLeft(center, 42.5);
        Canvas.SetTop(center, 32.5);
        
        // Лопасти (упрощенно - буква F)
        var label = new TextBlock
        {
            Text = "F",
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.DarkSlateGray
        };
        Canvas.SetLeft(label, 41);
        Canvas.SetTop(label, 22);
        
        canvas.Children.Add(housing);
        canvas.Children.Add(label);
        canvas.Children.Add(center);
        
        return canvas;
    }

    private static Canvas CreateHeaterShape()
    {
        var canvas = new Canvas { Width = 100, Height = 80 };
        
        // Корпус нагревателя
        var body = new Rectangle
        {
            Width = 60,
            Height = 50,
            Fill = Brushes.OrangeRed,
            Stroke = Brushes.DarkRed,
            StrokeThickness = 2,
            RadiusX = 5,
            RadiusY = 5
        };
        Canvas.SetLeft(body, 20);
        Canvas.SetTop(body, 15);
        
        // Символ нагрева (волны)
        var wave1 = new TextBlock
        {
            Text = "≈",
            FontSize = 32,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.Yellow
        };
        Canvas.SetLeft(wave1, 38);
        Canvas.SetTop(wave1, 20);
        
        canvas.Children.Add(body);
        canvas.Children.Add(wave1);
        
        return canvas;
    }

    private static Canvas CreateLightShape()
    {
        var canvas = new Canvas { Width = 100, Height = 80 };
        
        // Лампа
        var bulb = new Ellipse
        {
            Width = 50,
            Height = 50,
            Fill = Brushes.Gold,
            Stroke = Brushes.DarkGoldenrod,
            StrokeThickness = 2
        };
        Canvas.SetLeft(bulb, 25);
        Canvas.SetTop(bulb, 10);
        
        // Цоколь
        var base1 = new Rectangle
        {
            Width = 30,
            Height = 15,
            Fill = Brushes.Gray,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(base1, 35);
        Canvas.SetTop(base1, 55);
        
        canvas.Children.Add(bulb);
        canvas.Children.Add(base1);
        
        return canvas;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
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
