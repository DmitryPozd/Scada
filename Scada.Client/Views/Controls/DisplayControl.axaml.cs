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

namespace Scada.Client.Views.Controls;

public partial class DisplayControl : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<DisplayControl, string>(nameof(Label), defaultValue: "Датчик");

    public static readonly StyledProperty<ushort> RegisterAddressProperty =
        AvaloniaProperty.Register<DisplayControl, ushort>(nameof(RegisterAddress));

    public static readonly StyledProperty<string> DisplayValueProperty =
        AvaloniaProperty.Register<DisplayControl, string>(nameof(DisplayValue), defaultValue: "0");

    public static readonly StyledProperty<string> UnitProperty =
        AvaloniaProperty.Register<DisplayControl, string>(nameof(Unit), defaultValue: "");

    public static readonly StyledProperty<ObservableCollection<TagDefinition>?> AvailableTagsProperty =
        AvaloniaProperty.Register<DisplayControl, ObservableCollection<TagDefinition>?>(nameof(AvailableTags));

    public static readonly StyledProperty<TagDefinition?> SelectedTagProperty =
        AvaloniaProperty.Register<DisplayControl, TagDefinition?>(nameof(SelectedTag));

    public static readonly StyledProperty<double> ControlWidthProperty =
        AvaloniaProperty.Register<DisplayControl, double>(nameof(ControlWidth), defaultValue: 180);

    public static readonly StyledProperty<double> ControlHeightProperty =
        AvaloniaProperty.Register<DisplayControl, double>(nameof(ControlHeight), defaultValue: 80);

    // События
    public static readonly RoutedEvent<RoutedEventArgs> DeleteRequestedEvent =
        RoutedEvent.Register<DisplayControl, RoutedEventArgs>(nameof(DeleteRequested), RoutingStrategies.Bubble);

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

    public double ControlWidth
    {
        get => GetValue(ControlWidthProperty);
        set => SetValue(ControlWidthProperty, value);
    }

    public double ControlHeight
    {
        get => GetValue(ControlHeightProperty);
        set => SetValue(ControlHeightProperty, value);
    }

    public event EventHandler<RoutedEventArgs> DeleteRequested
    {
        add => AddHandler(DeleteRequestedEvent, value);
        remove => RemoveHandler(DeleteRequestedEvent, value);
    }

    public DisplayControl()
    {
        InitializeComponent();
        Focusable = true;
        
        SelectedTagProperty.Changed.AddClassHandler<DisplayControl>((control, args) =>
        {
            if (args.NewValue is TagDefinition tag)
            {
                control.RegisterAddress = tag.Address;
                control.TagChanged?.Invoke(control, EventArgs.Empty);
            }
        });
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        
        if (e.InitialPressMouseButton == MouseButton.Right && !_isResizing)
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
            Title = "Настройки индикатора",
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
            Text = $"Текущий адрес: {RegisterAddress}\nВыберите тег для отображения (Input или Holding Register):",
            TextWrapping = TextWrapping.Wrap
        };

        // Фильтруем теги Input Register и Holding Register
        var displayTags = new ObservableCollection<TagDefinition>(
            AvailableTags.Where(t => 
                // Input Register
                (t.Register == RegisterType.Input && 
                 (t.Name.StartsWith("AI") || t.Name.StartsWith("V") || 
                  t.Name.StartsWith("TV") || t.Name.StartsWith("CV") || 
                  t.Name.StartsWith("SV"))) ||
                // Holding Register (для чтения)
                (t.Register == RegisterType.Holding && 
                 (t.Name.StartsWith("V") || t.Name.StartsWith("AQ"))))
        );

        var combo = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        foreach (var tag in displayTags)
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

    // Поля для изменения размера
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
            _resizeStartWidth = ControlWidth;
            _resizeStartHeight = ControlHeight;
            
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
                var newWidth = Math.Max(150, Math.Min(500, _resizeStartWidth + deltaX));
                var newHeight = Math.Max(60, Math.Min(300, _resizeStartHeight + deltaY));
                ControlWidth = newWidth;
                ControlHeight = newHeight;
            }
            else if (_resizeMode == "right")
            {
                var newWidth = Math.Max(150, Math.Min(500, _resizeStartWidth + deltaX));
                ControlWidth = newWidth;
            }
            else if (_resizeMode == "bottom")
            {
                var newHeight = Math.Max(60, Math.Min(300, _resizeStartHeight + deltaY));
                ControlHeight = newHeight;
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
