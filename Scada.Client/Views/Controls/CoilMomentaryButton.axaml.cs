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

public partial class CoilMomentaryButton : UserControl
{
    private Point _pressPoint;
    private bool _wasPressed;

    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<CoilMomentaryButton, bool>(nameof(IsActive));

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<CoilMomentaryButton, string>(nameof(Label), defaultValue: "Кнопка");

    public static readonly StyledProperty<ushort> CoilAddressProperty =
        AvaloniaProperty.Register<CoilMomentaryButton, ushort>(nameof(CoilAddress), defaultValue: (ushort)0);

    public static readonly StyledProperty<ObservableCollection<TagDefinition>?> AvailableTagsProperty =
        AvaloniaProperty.Register<CoilMomentaryButton, ObservableCollection<TagDefinition>?>(nameof(AvailableTags));

    public static readonly StyledProperty<TagDefinition?> SelectedTagProperty =
        AvaloniaProperty.Register<CoilMomentaryButton, TagDefinition?>(nameof(SelectedTag));

    public static readonly StyledProperty<ICommand?> OnCommandProperty =
        AvaloniaProperty.Register<CoilMomentaryButton, ICommand?>(nameof(OnCommand));

    public static readonly StyledProperty<ICommand?> OffCommandProperty =
        AvaloniaProperty.Register<CoilMomentaryButton, ICommand?>(nameof(OffCommand));

    public static readonly StyledProperty<string?> IconPathOnProperty =
        AvaloniaProperty.Register<CoilMomentaryButton, string?>(nameof(IconPathOn));

    public static readonly StyledProperty<string?> IconPathOffProperty =
        AvaloniaProperty.Register<CoilMomentaryButton, string?>(nameof(IconPathOff));

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

    public CoilMomentaryButton()
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

    private void OnPointerPressedHandler(object? sender, PointerPressedEventArgs e)
    {
        // При нажатии левой кнопки мыши - включить катушку
        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsLeftButtonPressed)
        {
            _wasPressed = true;
            _pressPoint = e.GetPosition(this);
            OnCommand?.Execute(null);
            IsActive = true; // Локально обновляем состояние
            // НЕ помечаем как обработанное, чтобы DraggableControl мог начать перетаскивание
        }
    }

    private void OnPointerReleasedHandler(object? sender, PointerReleasedEventArgs e)
    {
        // При отпускании левой кнопки - выключить катушку
        if (e.InitialPressMouseButton == MouseButton.Left && _wasPressed)
        {
            _wasPressed = false;
            OffCommand?.Execute(null);
            IsActive = false; // Локально обновляем состояние
            // НЕ помечаем как обработанное
        }
        // Правая кнопка - показать контекстное меню
        else if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ShowContextMenu();
            e.Handled = true;
        }
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        OnPointerPressedHandler(this, e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        OnPointerReleasedHandler(this, e);
    }
    
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        
        // Если указатель был захвачен и кнопка была нажата, отпускаем её
        if (_wasPressed)
        {
            _wasPressed = false;
            OffCommand?.Execute(null);
            IsActive = false;
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
        // Пробел - нажатие кнопки (только если еще не активна)
        else if (e.Key == Key.Space && !IsActive)
        {
            OnCommand?.Execute(null);
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        
        // Пробел отпущен - отпускание кнопки
        if (e.Key == Key.Space)
        {
            OffCommand?.Execute(null);
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
            IsMomentary = true,
            X = parentDraggable?.X ?? 0,
            Y = parentDraggable?.Y ?? 0
        };
        CopyRequested?.Invoke(this, info);
    }

    private async void ShowContextMenu()
    {
        var dialog = new Window
        {
            Title = "Настройки кнопки без фиксации",
            Width = 500,
            Height = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
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
        var iconOnTextBlock = new TextBlock { Text = "Иконка ON (нажата):", FontWeight = FontWeight.SemiBold };
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
        var iconOffTextBlock = new TextBlock { Text = "Иконка OFF (отпущена):", FontWeight = FontWeight.SemiBold };
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
            Text = $"Кнопка: {Label}\nТекущий адрес: {CoilAddress}\nВыберите тег Coil:",
            TextWrapping = TextWrapping.Wrap
        };

        // Фильтруем только Coil теги
        var coilTags = new ObservableCollection<TagDefinition>(
            AvailableTags!.Where(t => t.Register == RegisterType.Coils)
        );

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
}

public class MomentaryStateTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "НАЖАТА" : "ОТПУЩЕНА";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
