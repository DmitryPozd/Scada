using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Scada.Client.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Scada.Client.Views.Controls;

public partial class SliderControl : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<SliderControl, string>(nameof(Label), defaultValue: "Регулятор");

    public static readonly StyledProperty<ushort> RegisterAddressProperty =
        AvaloniaProperty.Register<SliderControl, ushort>(nameof(RegisterAddress));

    public static readonly StyledProperty<int> MinValueProperty =
        AvaloniaProperty.Register<SliderControl, int>(nameof(MinValue), defaultValue: 0);

    public static readonly StyledProperty<int> MaxValueProperty =
        AvaloniaProperty.Register<SliderControl, int>(nameof(MaxValue), defaultValue: 100);

    public static readonly StyledProperty<int> CurrentValueProperty =
        AvaloniaProperty.Register<SliderControl, int>(nameof(CurrentValue), defaultValue: 0);

    public static readonly StyledProperty<ICommand?> WriteCommandProperty =
        AvaloniaProperty.Register<SliderControl, ICommand?>(nameof(WriteCommand));

    public static readonly StyledProperty<ObservableCollection<TagDefinition>?> AvailableTagsProperty =
        AvaloniaProperty.Register<SliderControl, ObservableCollection<TagDefinition>?>(nameof(AvailableTags));

    public static readonly StyledProperty<TagDefinition?> SelectedTagProperty =
        AvaloniaProperty.Register<SliderControl, TagDefinition?>(nameof(SelectedTag));

    // События для копирования/вставки/удаления
    public static readonly RoutedEvent<RoutedEventArgs> DeleteRequestedEvent =
        RoutedEvent.Register<SliderControl, RoutedEventArgs>(nameof(DeleteRequested), RoutingStrategies.Bubble);

    public event EventHandler? TagChanged;

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public ushort RegisterAddress
    {
        get => GetValue(RegisterAddressProperty);
        set => SetValue(RegisterAddressProperty, value);
    }

    public int MinValue
    {
        get => GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public int MaxValue
    {
        get => GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public int CurrentValue
    {
        get => GetValue(CurrentValueProperty);
        set => SetValue(CurrentValueProperty, value);
    }

    public ICommand? WriteCommand
    {
        get => GetValue(WriteCommandProperty);
        set => SetValue(WriteCommandProperty, value);
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

    public event EventHandler<RoutedEventArgs> DeleteRequested
    {
        add => AddHandler(DeleteRequestedEvent, value);
        remove => RemoveHandler(DeleteRequestedEvent, value);
    }

    public SliderControl()
    {
        InitializeComponent();
        Focusable = true;
        
        // Подписка на изменение выбранного тега
        SelectedTagProperty.Changed.AddClassHandler<SliderControl>((control, args) =>
        {
            if (args.NewValue is TagDefinition tag)
            {
                control.RegisterAddress = tag.Address;
                control.TagChanged?.Invoke(control, EventArgs.Empty);
            }
        });
        
        // Автоматическая запись при изменении значения ползунка
        CurrentValueProperty.Changed.AddClassHandler<SliderControl>((control, args) =>
        {
            // Записываем только если команда задана и значение изменилось пользователем
            if (control.WriteCommand?.CanExecute(null) == true)
            {
                System.Diagnostics.Debug.WriteLine($"SliderControl: CurrentValue changed to {args.NewValue}, executing WriteCommand");
                control.WriteCommand.Execute(null);
            }
        });
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        
        // Правый клик - показать контекстное меню
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ShowContextMenu();
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        
        // Delete - удалить элемент
        if (e.Key == Key.Delete)
        {
            RaiseEvent(new RoutedEventArgs(DeleteRequestedEvent));
            e.Handled = true;
        }
    }

    private async void ShowContextMenu()
    {
        var dialog = new Window
        {
            Title = "Настройки ползунка",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            MaxWidth = 700,
            MaxHeight = 600
        };

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var stack = new StackPanel { Margin = new Thickness(20), Spacing = 12 };

        // Поле для редактирования надписи
        var labelTextBlock = new TextBlock { Text = "Название:", FontWeight = FontWeight.SemiBold };
        var labelInput = new TextBox 
        { 
            Text = Label,
            Watermark = "Введите название"
        };
        stack.Children.Add(labelTextBlock);
        stack.Children.Add(labelInput);

        // Диапазон значений
        var rangePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        rangePanel.Children.Add(new TextBlock { Text = "Мин:", VerticalAlignment = VerticalAlignment.Center });
        var minInput = new NumericUpDown { Minimum = 0, Maximum = 65535, Value = MinValue, Width = 100 };
        rangePanel.Children.Add(minInput);
        rangePanel.Children.Add(new TextBlock { Text = "Макс:", VerticalAlignment = VerticalAlignment.Center });
        var maxInput = new NumericUpDown { Minimum = 0, Maximum = 65535, Value = MaxValue, Width = 100 };
        rangePanel.Children.Add(maxInput);
        stack.Children.Add(new TextBlock { Text = "Диапазон значений:", FontWeight = FontWeight.SemiBold });
        stack.Children.Add(rangePanel);

        // Разделитель
        stack.Children.Add(new Separator());

        // Кнопка удаления
        var deleteBtn = new Button 
        { 
            Content = "🗑️ Удалить элемент", 
            Padding = new Thickness(10, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        deleteBtn.Click += (s, e) =>
        {
            RaiseEvent(new RoutedEventArgs(DeleteRequestedEvent));
            dialog.Close();
        };
        stack.Children.Add(deleteBtn);

        // Разделитель
        stack.Children.Add(new Separator());

        // Выбор тега
        ShowTagSelectionInDialog(stack, dialog, labelInput, minInput, maxInput);
        
        scrollViewer.Content = stack;
        dialog.Content = scrollViewer;

        if (this.VisualRoot is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
    }

    private void ShowTagSelectionInDialog(StackPanel stack, Window dialog, TextBox labelInput, NumericUpDown minInput, NumericUpDown maxInput)
    {
        if (AvailableTags == null || !AvailableTags.Any())
        {
            // Если тегов нет, показываем простой ввод адреса
            var label = new TextBlock 
            { 
                Text = $"Текущий адрес: {RegisterAddress}\nВведите новый адрес (0-65535):",
                TextWrapping = TextWrapping.Wrap
            };
            
            var input = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 65535,
                Value = RegisterAddress,
                Increment = 1,
                FormatString = "0"
            };

            var buttons = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Spacing = 10, 
                HorizontalAlignment = HorizontalAlignment.Right 
            };
            
            var okButton = new Button { Content = "OK", Width = 80 };
            okButton.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(labelInput.Text))
                {
                    Label = labelInput.Text;
                }
                MinValue = (int)minInput.Value!;
                MaxValue = (int)maxInput.Value!;
                RegisterAddress = (ushort)input.Value;
                dialog.Close();
            };
            
            var cancelButton = new Button { Content = "Отмена", Width = 80 };
            cancelButton.Click += (s, e) => dialog.Close();

            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);
            
            stack.Children.Add(label);
            stack.Children.Add(input);
            stack.Children.Add(buttons);
            return;
        }

        // Показываем выбор тега для Holding Register
        var tagLabel = new TextBlock 
        { 
            Text = $"Текущий адрес: {RegisterAddress}\nВыберите тег (AQ, V - Holding Register):",
            TextWrapping = TextWrapping.Wrap
        };

        // Фильтруем теги Holding Register (AQ, V)
        var holdingTags = new ObservableCollection<TagDefinition>(
            AvailableTags.Where(t => t.Register == RegisterType.Holding && 
                                     (t.Name.StartsWith("AQ") || t.Name.StartsWith("V")))
        );

        var combo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        foreach (var tag in holdingTags)
        {
            combo.Items.Add(new ComboBoxItem 
            { 
                Content = $"{tag.Name} (адрес: {tag.Address})",
                Tag = tag
            });
        }

        // Выбираем текущий тег если есть
        if (SelectedTag != null)
        {
            var currentItem = combo.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(item => (item.Tag as TagDefinition)?.Address == SelectedTag.Address);
            if (currentItem != null)
            {
                combo.SelectedItem = currentItem;
            }
        }

        var buttonsPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal, 
            Spacing = 10, 
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        
        var okBtn = new Button { Content = "OK", Width = 80 };
        okBtn.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(labelInput.Text))
            {
                Label = labelInput.Text;
            }
            MinValue = (int)minInput.Value!;
            MaxValue = (int)maxInput.Value!;
            
            if (combo.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is TagDefinition tag)
            {
                SelectedTag = tag;
                RegisterAddress = tag.Address;
            }
            dialog.Close();
        };
        
        var cancelBtn = new Button { Content = "Отмена", Width = 80 };
        cancelBtn.Click += (s, e) => dialog.Close();

        buttonsPanel.Children.Add(okBtn);
        buttonsPanel.Children.Add(cancelBtn);
        
        stack.Children.Add(tagLabel);
        stack.Children.Add(combo);
        stack.Children.Add(buttonsPanel);
    }
}
