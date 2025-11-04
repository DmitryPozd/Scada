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

public partial class NumericInputControl : UserControl
{
    // Флаг для блокировки обновления при редактировании
    private bool _isEditing = false;
    
    // Публичное свойство для проверки состояния
    public bool IsEditing => _isEditing;
    
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<NumericInputControl, string>(nameof(Label), defaultValue: "Числовой ввод");

    public static readonly StyledProperty<ushort> RegisterAddressProperty =
        AvaloniaProperty.Register<NumericInputControl, ushort>(nameof(RegisterAddress));

    public static readonly StyledProperty<string> InputValueProperty =
        AvaloniaProperty.Register<NumericInputControl, string>(nameof(InputValue), defaultValue: "0");

    public static readonly StyledProperty<string> UnitProperty =
        AvaloniaProperty.Register<NumericInputControl, string>(nameof(Unit), defaultValue: "");

    public static readonly StyledProperty<ICommand?> WriteCommandProperty =
        AvaloniaProperty.Register<NumericInputControl, ICommand?>(nameof(WriteCommand));

    public static readonly StyledProperty<ObservableCollection<TagDefinition>?> AvailableTagsProperty =
        AvaloniaProperty.Register<NumericInputControl, ObservableCollection<TagDefinition>?>(nameof(AvailableTags));

    public static readonly StyledProperty<TagDefinition?> SelectedTagProperty =
        AvaloniaProperty.Register<NumericInputControl, TagDefinition?>(nameof(SelectedTag));

    // События
    public static readonly RoutedEvent<RoutedEventArgs> DeleteRequestedEvent =
        RoutedEvent.Register<NumericInputControl, RoutedEventArgs>(nameof(DeleteRequested), RoutingStrategies.Bubble);

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

    public string InputValue
    {
        get => GetValue(InputValueProperty);
        set => SetValue(InputValueProperty, value);
    }

    public string Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
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

    public NumericInputControl()
    {
        InitializeComponent();
        Focusable = true;
        
        SelectedTagProperty.Changed.AddClassHandler<NumericInputControl>((control, args) =>
        {
            if (args.NewValue is TagDefinition tag)
            {
                control.RegisterAddress = tag.Address;
                control.TagChanged?.Invoke(control, EventArgs.Empty);
            }
        });
        
        // Обработка Enter в TextBox и управление фокусом
        this.Loaded += (s, e) =>
        {
            var textBox = this.FindControl<TextBox>("ValueTextBox");
            if (textBox != null)
            {
                // При получении фокуса - блокируем обновления
                textBox.GotFocus += (sender, args) =>
                {
                    _isEditing = true;
                    System.Diagnostics.Debug.WriteLine("NumericInputControl: GotFocus - blocking updates");
                };
                
                // При нажатии Enter - выполняем запись и снимаем блокировку
                textBox.KeyDown += (sender, args) =>
                {
                    if (args.Key == Key.Enter)
                    {
                        System.Diagnostics.Debug.WriteLine("NumericInputControl: Enter pressed, executing WriteCommand");
                        if (WriteCommand?.CanExecute(null) == true)
                        {
                            WriteCommand.Execute(null);
                        }
                        
                        // ВАЖНО: Убираем фокус с TextBox, чтобы снять блокировку
                        this.Focus();
                        System.Diagnostics.Debug.WriteLine("NumericInputControl: Enter processed - focus moved, updates will resume on LostFocus");
                        args.Handled = true;
                    }
                    else if (args.Key == Key.Escape)
                    {
                        // Escape - отменяем редактирование
                        this.Focus();
                        System.Diagnostics.Debug.WriteLine("NumericInputControl: Escape pressed - focus moved");
                        args.Handled = true;
                    }
                };
                
                // При потере фокуса - снимаем блокировку
                textBox.LostFocus += (sender, args) =>
                {
                    _isEditing = false;
                    System.Diagnostics.Debug.WriteLine("NumericInputControl: LostFocus - allowing updates");
                };
            }
        };
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ShowContextMenu();
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        
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
            Title = "Настройки числового ввода",
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

        // Название
        var labelTextBlock = new TextBlock { Text = "Название:", FontWeight = FontWeight.SemiBold };
        var labelInput = new TextBox 
        { 
            Text = Label,
            Watermark = "Введите название"
        };
        stack.Children.Add(labelTextBlock);
        stack.Children.Add(labelInput);

        // Единицы измерения
        var unitTextBlock = new TextBlock { Text = "Единицы измерения:", FontWeight = FontWeight.SemiBold };
        var unitInput = new TextBox 
        { 
            Text = Unit,
            Watermark = "°C, %, м³/ч и т.д."
        };
        stack.Children.Add(unitTextBlock);
        stack.Children.Add(unitInput);

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

        stack.Children.Add(new Separator());

        ShowTagSelectionInDialog(stack, dialog, labelInput, unitInput);
        
        scrollViewer.Content = stack;
        dialog.Content = scrollViewer;

        if (this.VisualRoot is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
    }

    private void ShowTagSelectionInDialog(StackPanel stack, Window dialog, TextBox labelInput, TextBox unitInput)
    {
        if (AvailableTags == null || !AvailableTags.Any())
        {
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
                    Label = labelInput.Text;
                Unit = unitInput.Text ?? "";
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

        var tagLabel = new TextBlock 
        { 
            Text = $"Текущий адрес: {RegisterAddress}\nВыберите тег (AQ, V - Holding Register):",
            TextWrapping = TextWrapping.Wrap
        };

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
                Label = labelInput.Text;
            Unit = unitInput.Text ?? "";
            
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
