using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

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
            Width = 300,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
        
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
            StartAddress = (ushort)addressInput.Value;
            BitCount = (int)bitCountInput.Value;
            dialog.Close();
        };

        cancelButton.Click += (_, _) => dialog.Close();

        buttonsPanel.Children.Add(okButton);
        buttonsPanel.Children.Add(cancelButton);
        panel.Children.Add(buttonsPanel);

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
