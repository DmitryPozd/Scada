using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Scada.Client.Models;
using Scada.Client.Services;

namespace Scada.Client.Views.Controls;

public partial class CoilButton : UserControl
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<CoilButton, bool>(nameof(IsActive));

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<CoilButton, string>(nameof(Label), defaultValue: "Coil");

    public static readonly StyledProperty<ushort> CoilAddressProperty =
        AvaloniaProperty.Register<CoilButton, ushort>(nameof(CoilAddress), defaultValue: (ushort)0);

    public static readonly StyledProperty<ObservableCollection<TagDefinition>?> AvailableTagsProperty =
        AvaloniaProperty.Register<CoilButton, ObservableCollection<TagDefinition>?>(nameof(AvailableTags));

    public static readonly StyledProperty<TagDefinition?> SelectedTagProperty =
        AvaloniaProperty.Register<CoilButton, TagDefinition?>(nameof(SelectedTag));

    public static readonly StyledProperty<ICommand?> OnCommandProperty =
        AvaloniaProperty.Register<CoilButton, ICommand?>(nameof(OnCommand));

    public static readonly StyledProperty<ICommand?> OffCommandProperty =
        AvaloniaProperty.Register<CoilButton, ICommand?>(nameof(OffCommand));

    public static readonly StyledProperty<string?> IconPathProperty =
        AvaloniaProperty.Register<CoilButton, string?>(nameof(IconPath));

    public static readonly StyledProperty<string?> IconPathOnProperty =
        AvaloniaProperty.Register<CoilButton, string?>(nameof(IconPathOn));

    public static readonly StyledProperty<string?> IconPathOffProperty =
        AvaloniaProperty.Register<CoilButton, string?>(nameof(IconPathOff));

    public static readonly StyledProperty<double> ButtonWidthProperty =
        AvaloniaProperty.Register<CoilButton, double>(nameof(ButtonWidth), defaultValue: 100.0);

    public static readonly StyledProperty<double> ButtonHeightProperty =
        AvaloniaProperty.Register<CoilButton, double>(nameof(ButtonHeight), defaultValue: 100.0);

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

    public string? IconPath
    {
        get => GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
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

    public CoilButton()
    {
        InitializeComponent();
        
        // Subscribe to SelectedTag changes to update CoilAddress
        this.GetObservable(SelectedTagProperty).Subscribe(tag =>
        {
            if (tag != null && tag.Register == RegisterType.Coils)
            {
                CoilAddress = tag.Address;
                // Уведомляем об изменении тега для сохранения настроек
                TagChanged?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    // Переменные для изменения размера
    private bool _isResizing = false;
    private Point _resizeStartPoint;
    private double _resizeStartWidth;
    private double _resizeStartHeight;
    private string _resizeMode = "";

    private void OnResizeGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border grip)
        {
            _isResizing = true;
            _resizeStartPoint = e.GetPosition(this);
            _resizeStartWidth = ButtonWidth;
            _resizeStartHeight = ButtonHeight;
            
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
                var newWidth = Math.Max(50, Math.Min(500, _resizeStartWidth + deltaX));
                var newHeight = Math.Max(50, Math.Min(500, _resizeStartHeight + deltaY));
                ButtonWidth = newWidth;
                ButtonHeight = newHeight;
            }
            else if (_resizeMode == "right")
            {
                // Изменение только по ширине
                var newWidth = Math.Max(50, Math.Min(500, _resizeStartWidth + deltaX));
                ButtonWidth = newWidth;
            }
            else if (_resizeMode == "bottom")
            {
                // Изменение только по высоте
                var newHeight = Math.Max(50, Math.Min(500, _resizeStartHeight + deltaY));
                ButtonHeight = newHeight;
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
            TagChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isResizing = false;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        
        // Левый клик - переключить состояние (только если не было перетаскивания)
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            OnToggleClick();
            e.Handled = true;
        }
        // Показываем контекстное меню при правом клике
        else if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ShowContextMenu();
            e.Handled = true;
        }
    }

    private void OnToggleClick()
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
            IsImageButton = false,
            X = parentDraggable?.X ?? 0,
            Y = parentDraggable?.Y ?? 0
        };
        CopyRequested?.Invoke(this, info);
    }

    private async void ShowContextMenu()
    {
        var dialog = new Window
        {
            Title = "Настройки кнопки",
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
        var labelTextBlock = new TextBlock { Text = "Название кнопки:", FontWeight = FontWeight.SemiBold };
        var labelInput = new TextBox 
        { 
            Text = Label,
            Watermark = "Введите название кнопки"
        };
        stack.Children.Add(labelTextBlock);
        stack.Children.Add(labelInput);

        // Поле для выбора иконки ON
        var iconOnTextBlock = new TextBlock { Text = "Иконка ON (включено):", FontWeight = FontWeight.SemiBold };
        var iconOnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
        var iconOnInput = new TextBox 
        { 
            Text = IconPathOn ?? "",
            Watermark = "Assets/icon_on.png",
            MinWidth = 300
        };
        var iconOnBrowseBtn = new Button { Content = "📁", Width = 35, Padding = new Thickness(5) };
        iconOnBrowseBtn.Click += async (s, e) =>
        {
            if (dialog.StorageProvider.CanOpen)
            {
                // Определяем путь к папке Assets
                var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
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
            Watermark = "Assets/icon_off.png",
            MinWidth = 300
        };
        var iconOffBrowseBtn = new Button { Content = "📁", Width = 35, Padding = new Thickness(5) };
        iconOffBrowseBtn.Click += async (s, e) =>
        {
            if (dialog.StorageProvider.CanOpen)
            {
                // Определяем путь к папке Assets
                var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
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

        // Старое поле для обратной совместимости (скрыто)
        var iconPanel = new StackPanel { IsVisible = false };
        var iconInput = new TextBox { Text = IconPath ?? "" };

        // Разделитель
        stack.Children.Add(new Separator { Margin = new Thickness(0, 5, 0, 5) });

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
                // Сохраняем иконки ON/OFF
                IconPathOn = !string.IsNullOrWhiteSpace(iconOnInput.Text) ? iconOnInput.Text : null;
                IconPathOff = !string.IsNullOrWhiteSpace(iconOffInput.Text) ? iconOffInput.Text : null;
                
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
        }
        else
        {
            // Показываем выбор тега
            ShowTagSelectionInDialog(stack, dialog, labelInput, iconOnInput, iconOffInput);
        }
        
        scrollViewer.Content = stack;
        dialog.Content = scrollViewer;

        if (this.VisualRoot is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
    }

    private void ShowTagSelectionInDialog(StackPanel stack, Window dialog, TextBox labelInput, TextBox iconOnInput, TextBox iconOffInput)
    {
        // Проверяем наличие тегов
        if (AvailableTags == null || !AvailableTags.Any())
        {
            var errorLabel = new TextBlock 
            { 
                Text = "Нет доступных тегов!\nТеги не загружены из settings.json.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Red,
                Margin = new Thickness(0, 10, 0, 10)
            };
            stack.Children.Add(errorLabel);
            
            var closeButton = new Button { Content = "OK", Width = 80 };
            closeButton.Click += (s, e) => dialog.Close();
            stack.Children.Add(closeButton);
            return;
        }

        var label = new TextBlock 
        { 
            Text = $"Кнопка: {Label}\nТекущий адрес: {CoilAddress}\nВыберите тег M (Modbus Coils):",
            TextWrapping = TextWrapping.Wrap
        };

        // Фильтруем только теги M (Modbus Coils)
        var coilTags = new ObservableCollection<TagDefinition>(
            AvailableTags.Where(t => t.Register == RegisterType.Coils && t.Name.StartsWith("M"))
        );

        Console.WriteLine($"AvailableTags count: {AvailableTags.Count}");
        Console.WriteLine($"M tags count: {coilTags.Count}");

        if (coilTags.Count == 0)
        {
            var warningLabel = new TextBlock 
            { 
                Text = $"Нет M тегов!\nВсего тегов: {AvailableTags.Count}\nФильтруется по тегам M (Modbus Coils)",
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
        foreach (var tag in coilTags)
        {
            combo.Items.Add(new ComboBoxItem 
            { 
                Content = $"{tag.Name} (адрес: {tag.Address})",
                Tag = tag
            });
        }
        combo.SelectedIndex = coilTags.ToList().FindIndex(t => t.Address == CoilAddress);

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

public class CoilStateBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? Brushes.LimeGreen : Brushes.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class CoilStateBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b 
            ? new SolidColorBrush(Color.Parse("#DCFCE7")) 
            : new SolidColorBrush(Color.Parse("#F3F4F6"));

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class CoilStateIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "●" : "○";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class CoilStateTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? Brushes.Green : Brushes.DarkGray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class CoilStateTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "ON" : "OFF";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class PathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            try
            {
                // Если это SVG файл, конвертируем его в PNG
                if (SvgToPngConverter.IsSvgFile(path))
                {
                    // Проверяем абсолютный путь
                    string? svgPath = null;
                    if (File.Exists(path))
                    {
                        svgPath = path;
                    }
                    else
                    {
                        // Пробуем относительный путь от базовой директории
                        var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                        if (File.Exists(fullPath))
                        {
                            svgPath = fullPath;
                        }
                    }

                    if (svgPath != null)
                    {
                        // Конвертируем SVG в PNG
                        var pngPath = SvgToPngConverter.ConvertSvgToPng(svgPath);
                        if (pngPath != null && File.Exists(pngPath))
                        {
                            return new Avalonia.Media.Imaging.Bitmap(pngPath);
                        }
                    }
                }

                // Для не-SVG файлов или если конвертация не удалась
                // Сначала пробуем avares:// схему для ресурсов
                if (!path.StartsWith("avares://") && !Path.IsPathRooted(path))
                {
                    // Если относительный путь, пробуем как avares
                    var avaresPath = $"avares://Scada.Client/{path}";
                    try
                    {
                        var uri = new Uri(avaresPath);
                        using var assetStream = Avalonia.Platform.AssetLoader.Open(uri);
                        return new Avalonia.Media.Imaging.Bitmap(assetStream);
                    }
                    catch
                    {
                        // Если не получилось как ресурс, пробуем как файл
                    }
                }
                
                // Проверяем абсолютный путь
                if (File.Exists(path))
                {
                    return new Avalonia.Media.Imaging.Bitmap(path);
                }
                
                // Пробуем относительный путь от базовой директории
                var fullPath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                if (File.Exists(fullPath2))
                {
                    return new Avalonia.Media.Imaging.Bitmap(fullPath2);
                }
            }
            catch (Exception ex)
            {
                // Для отладки выводим ошибку
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения '{value}': {ex.Message}");
            }
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
