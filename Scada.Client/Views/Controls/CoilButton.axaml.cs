using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Scada.Client.Models;

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

    public event EventHandler<CoilButtonInfo>? CopyRequested;
    public event EventHandler? PasteRequested;

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

    public CoilButton()
    {
        InitializeComponent();
        
        // Subscribe to SelectedTag changes to update CoilAddress
        this.GetObservable(SelectedTagProperty).Subscribe(tag =>
        {
            if (tag != null && tag.Register == RegisterType.Coils)
            {
                CoilAddress = tag.Address;
            }
        });
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
            Width = 500,
            Height = 550,
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
            ShowTagSelectionInDialog(stack, dialog, labelInput);
        }
        
        dialog.Content = stack;

        if (this.VisualRoot is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
    }

    private void ShowTagSelectionInDialog(StackPanel stack, Window dialog, TextBox labelInput)
    {
        var infoText = new TextBlock 
        { 
            Text = $"Кнопка: {Label}\nТекущий адрес: {CoilAddress}",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        stack.Children.Add(infoText);

        // Фильтруем только Coil теги
        var coilTags = new ObservableCollection<TagDefinition>(
            AvailableTags!.Where(t => t.Register == RegisterType.Coils)
        );

        // Добавляем поле поиска
        var searchLabel = new TextBlock 
        { 
            Text = "Поиск тега:",
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 5, 0, 3)
        };
        stack.Children.Add(searchLabel);

        var searchBox = new TextBox 
        { 
            Watermark = "Введите имя или адрес тега...",
            Margin = new Thickness(0, 0, 0, 8)
        };
        stack.Children.Add(searchBox);

        var tagLabel = new TextBlock 
        { 
            Text = $"Доступно тегов: {coilTags.Count}",
            FontSize = 11,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 3)
        };
        stack.Children.Add(tagLabel);

        var combo = new ComboBox
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            MaxDropDownHeight = 300
        };

        var filteredTags = new ObservableCollection<TagDefinition>(coilTags);

        // Функция для обновления списка тегов
        void UpdateComboBox(string searchText)
        {
            combo.Items.Clear();
            filteredTags.Clear();

            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? coilTags
                : new ObservableCollection<TagDefinition>(coilTags.Where(t =>
                    t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Address.ToString().Contains(searchText)));

            foreach (var tag in filtered)
            {
                filteredTags.Add(tag);
                combo.Items.Add(new ComboBoxItem 
                { 
                    Content = $"{tag.Name} (адрес: {tag.Address})",
                    Tag = tag
                });
            }

            tagLabel.Text = $"Найдено тегов: {filtered.Count} из {coilTags.Count}";
            
            // Если текущий адрес совпадает с тегом, выделяем его
            combo.SelectedIndex = filtered.ToList().FindIndex(t => t.Address == CoilAddress);
        }

        // Инициализация списка
        UpdateComboBox(string.Empty);

        // Подписка на изменение текста поиска
        searchBox.TextChanged += (s, e) => UpdateComboBox(searchBox.Text ?? string.Empty);

        stack.Children.Add(combo);

        var buttons = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal, 
            Spacing = 10, 
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        
        var okButton = new Button { Content = "OK", Width = 80 };
        okButton.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(labelInput.Text))
            {
                Label = labelInput.Text;
            }
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
        
        stack.Children.Add(buttons);
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
