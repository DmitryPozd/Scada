using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Scada.Client.Models;
using Scada.Client.ViewModels;

namespace Scada.Client.Views.Controls;

public partial class InputBitsIndicator : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<InputBitsIndicator, string>(nameof(Label), "Входы");

    public static readonly StyledProperty<ushort> StartAddressProperty =
        AvaloniaProperty.Register<InputBitsIndicator, ushort>(nameof(StartAddress));

    public static readonly StyledProperty<int> BitCountProperty =
        AvaloniaProperty.Register<InputBitsIndicator, int>(nameof(BitCount), 8);

    public static readonly StyledProperty<bool[]?> BitValuesProperty =
        AvaloniaProperty.Register<InputBitsIndicator, bool[]?>(nameof(BitValues));

    public static readonly RoutedEvent<RoutedEventArgs> CopyRequestedEvent =
        RoutedEvent.Register<InputBitsIndicator, RoutedEventArgs>(
            nameof(CopyRequested), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> PasteRequestedEvent =
        RoutedEvent.Register<InputBitsIndicator, RoutedEventArgs>(
            nameof(PasteRequested), RoutingStrategies.Bubble);

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public ushort StartAddress
    {
        get => GetValue(StartAddressProperty);
        set => SetValue(StartAddressProperty, value);
    }

    public int BitCount
    {
        get => GetValue(BitCountProperty);
        set => SetValue(BitCountProperty, value);
    }

    public bool[]? BitValues
    {
        get => GetValue(BitValuesProperty);
        set => SetValue(BitValuesProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? CopyRequested
    {
        add => AddHandler(CopyRequestedEvent, value);
        remove => RemoveHandler(CopyRequestedEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? PasteRequested
    {
        add => AddHandler(PasteRequestedEvent, value);
        remove => RemoveHandler(PasteRequestedEvent, value);
    }

    public ObservableCollection<BitDisplayItem> Bits { get; } = new();

    public InputBitsIndicator()
    {
        InitializeComponent();
        
        System.Diagnostics.Debug.WriteLine("*** InputBitsIndicator CREATED ***");
        
        // Subscribe to property changes
        this.GetObservable(BitCountProperty).Subscribe(_ => UpdateBitsDisplay());
        this.GetObservable(BitValuesProperty).Subscribe(_ => UpdateBitsDisplay());
        this.GetObservable(StartAddressProperty).Subscribe(_ => UpdateBitsDisplay());
        
        // Initialize bits display
        UpdateBitsDisplay();
        
        // Set DataContext for ItemsControl
        var bitsPanel = this.FindControl<ItemsControl>("BitsPanel");
        if (bitsPanel != null)
        {
            bitsPanel.ItemsSource = Bits;
            
            // Set ItemTemplate programmatically
            bitsPanel.ItemTemplate = new FuncDataTemplate<BitDisplayItem>((item, _) =>
            {
                var border = new Border
                {
                    Width = 35,
                    Height = 35,
                    Margin = new Thickness(2),
                    CornerRadius = new CornerRadius(4),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Black
                };
                
                var stack = new StackPanel
                {
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                
                var bitNumText = new TextBlock
                {
                    FontSize = 8,
                    Foreground = Brushes.White,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
                bitNumText.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("BitNumber"));
                
                var valueText = new TextBlock
                {
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
                valueText.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("Value"));
                
                stack.Children.Add(bitNumText);
                stack.Children.Add(valueText);
                border.Child = stack;
                
                // Bind background color
                border.Bind(Border.BackgroundProperty, new Avalonia.Data.Binding("Color"));
                
                return border;
            });
            
            System.Diagnostics.Debug.WriteLine("*** BitsPanel ItemsSource SET ***");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("*** BitsPanel NOT FOUND ***");
        }
    }

    private void UpdateBitsDisplay()
    {
        Bits.Clear();
        
        for (int i = 0; i < BitCount; i++)
        {
            bool value = BitValues != null && i < BitValues.Length ? BitValues[i] : false;
            Bits.Add(new BitDisplayItem
            {
                BitNumber = i.ToString(),
                Value = value ? "1" : "0",
                Color = value ? Brushes.Green : Brushes.Red
            });
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.C && e.KeyModifiers == KeyModifiers.Control)
        {
            RaiseEvent(new RoutedEventArgs(CopyRequestedEvent));
            e.Handled = true;
            return;
        }
        
        if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
        {
            RaiseEvent(new RoutedEventArgs(PasteRequestedEvent));
            e.Handled = true;
            return;
        }
        
        base.OnKeyDown(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ShowContextMenu();
            e.Handled = true;
            return;
        }
        
        base.OnPointerReleased(e);
    }

    private async void ShowContextMenu()
    {
        var dialog = new Window
        {
            Title = "Настройка индикатора входов",
            Width = 500,
            Height = 550,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 };

        // Поле для названия
        panel.Children.Add(new TextBlock { Text = "Название:", FontWeight = FontWeight.SemiBold });
        var labelInput = new TextBox 
        { 
            Text = Label,
            Watermark = "Введите название индикатора"
        };
        panel.Children.Add(labelInput);

        panel.Children.Add(new Separator());

        // Проверяем наличие тегов в ViewModel
        var vm = (DataContext as MainWindowViewModel);
        var availableTags = vm?.ConnectionConfig.Tags;

        if (availableTags != null && availableTags.Count > 0)
        {
            // Показываем выбор тега
            var infoText = new TextBlock 
            { 
                Text = $"Текущий стартовый адрес: {StartAddress}",
                Margin = new Thickness(0, 5, 0, 8)
            };
            panel.Children.Add(infoText);

            // Фильтруем только Coil теги
            var coilTags = new ObservableCollection<TagDefinition>(
                availableTags.Where(t => t.Register == RegisterType.Coils)
            );

            // Добавляем поле поиска
            var searchLabel = new TextBlock 
            { 
                Text = "Поиск тега:",
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 5, 0, 3)
            };
            panel.Children.Add(searchLabel);

            var searchBox = new TextBox 
            { 
                Watermark = "Введите имя или адрес тега...",
                Margin = new Thickness(0, 0, 0, 8)
            };
            panel.Children.Add(searchBox);

            var tagLabel = new TextBlock 
            { 
                Text = $"Доступно тегов: {coilTags.Count}",
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 3)
            };
            panel.Children.Add(tagLabel);

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
                combo.SelectedIndex = filtered.ToList().FindIndex(t => t.Address == StartAddress);
            }

            // Инициализация списка
            UpdateComboBox(string.Empty);

            // Подписка на изменение текста поиска
            searchBox.TextChanged += (s, e) => UpdateComboBox(searchBox.Text ?? string.Empty);

            panel.Children.Add(combo);

            // Количество битов
            panel.Children.Add(new TextBlock { Text = "Количество битов:", Margin = new Thickness(0, 5, 0, 0) });
            var bitCountInput = new NumericUpDown
            {
                Value = BitCount,
                Minimum = 1,
                Maximum = 16,
                Increment = 1
            };
            panel.Children.Add(bitCountInput);

            var buttonsPanel = new StackPanel 
            { 
                Orientation = Avalonia.Layout.Orientation.Horizontal, 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10,
                Margin = new Thickness(0, 15, 0, 0)
            };

            var okButton = new Button { Content = "OK", MinWidth = 80 };
            var cancelButton = new Button { Content = "Отмена", MinWidth = 80 };

            okButton.Click += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(labelInput.Text))
                {
                    Label = labelInput.Text;
                }
                if (combo.SelectedItem is ComboBoxItem item && item.Tag is TagDefinition selectedTag)
                {
                    StartAddress = selectedTag.Address;
                }
                BitCount = (int)bitCountInput.Value;
                dialog.Close();
            };

            cancelButton.Click += (_, _) => dialog.Close();

            buttonsPanel.Children.Add(okButton);
            buttonsPanel.Children.Add(cancelButton);
            panel.Children.Add(buttonsPanel);
        }
        else
        {
            // Fallback - если тегов нет, показываем простой ввод адреса
            panel.Children.Add(new TextBlock { Text = "Стартовый адрес:" });
            var addressInput = new NumericUpDown
            {
                Value = StartAddress,
                Minimum = 0,
                Maximum = 65535,
                Increment = 1
            };
            panel.Children.Add(addressInput);

            panel.Children.Add(new TextBlock { Text = "Количество битов:" });
            var bitCountInput = new NumericUpDown
            {
                Value = BitCount,
                Minimum = 1,
                Maximum = 16,
                Increment = 1
            };
            panel.Children.Add(bitCountInput);

            var buttonsPanel = new StackPanel 
            { 
                Orientation = Avalonia.Layout.Orientation.Horizontal, 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var okButton = new Button { Content = "OK", MinWidth = 80 };
            var cancelButton = new Button { Content = "Отмена", MinWidth = 80 };

            okButton.Click += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(labelInput.Text))
                {
                    Label = labelInput.Text;
                }
                StartAddress = (ushort)addressInput.Value;
                BitCount = (int)bitCountInput.Value;
                dialog.Close();
            };

            cancelButton.Click += (_, _) => dialog.Close();

            buttonsPanel.Children.Add(okButton);
            buttonsPanel.Children.Add(cancelButton);
            panel.Children.Add(buttonsPanel);
        }

        dialog.Content = panel;

        if (TopLevel.GetTopLevel(this) is Window parentWindow)
        {
            await dialog.ShowDialog(parentWindow);
        }
    }
}

public class BitDisplayItem
{
    public string BitNumber { get; set; } = "";
    public string Value { get; set; } = "0";
    public IBrush Color { get; set; } = Brushes.Red;
}
